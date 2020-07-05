using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Swift.Interop
{
	// We bind Swift values in a couple ways:
	//  1. ISwiftValue represents any wrapper type that can get a Handle (pointer) to the data.
	//  2. ISwiftBlittableStruct<T> is an ISwiftValue that represents a managed struct of
	//      type T that is itself the native data.

	/// <summary>
	/// A Swift struct or class type.
	/// </summary>
	public interface ISwiftValue
	{
		/// <summary>
		/// Sets updated <see cref="SwiftType"/> and <see cref="Nullability"/> information for generic types
		///  exposed to Swift.
		/// </summary>
		/// <remarks>
		/// This only needs to be implemented for generic types, and should generally be implemented explicitly.
		/// <para/>
		/// Swift only supports nullability by wrapping values in <c>Optional</c>. So, if a managed value is
		///  nullable, we must wrap its <see cref="SwiftType"/> accordingly.
		/// When exposed as a field or return value, nullability information for reference types is
		///  only available as an attribute on the member declaration itself. Thus, if this is a generic
		///  type that exposes values of its type parameter(s) to Swift, in order to calculate an
		///  accurate <see cref="SwiftType"/> for those values, this type would need access to the attributes
		///  on the member in which it is declared. That information can be provided through this method.
		/// </remarks>
		void SetSwiftType (SwiftType swiftType, Nullability nullability)
		{
			var ty = GetType ();
			if (ty.IsGenericType)
				throw new NotImplementedException ($"Generic type '{ty}' must implement SetSwiftType");
		}

		/// <summary>
		/// Gets a <see cref="SwiftHandle"/> to the native Swift data.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="SwiftHandle"/> must be disposed when no longer needed.
		/// </remarks>
		SwiftHandle GetSwiftHandle ();

		/// <summary>
		/// Decrements any references to reference-counted data held by this
		///  <see cref="ISwiftValue"/>.
		/// </summary>
		/// <remarks>
		/// This interface doesn't implement <see cref="IDisposable"/> because that
		///  would cause F# to require the use of the <c>new</c> operator, which
		///  harshes our DSL. In cases where it is important, a finalizer will be
		///  provided that will automatically dispose of the object if you do not call
		///  <see cref="Dispose"/>. 
		/// </remarks>
		void Dispose ();
	}

	/// <summary>
	/// A Swift struct that is fully represented as a blittable managed struct.
	/// </summary>
	/// <typeparam name="T">The blittable managed struct type.
	///	  This should be the type that implements this interface.</typeparam>
	public interface ISwiftBlittableStruct<T> : ISwiftValue
		where T : unmanaged, ISwiftBlittableStruct<T>
	{
		/// <summary>
		/// Gets a <see cref="SwiftHandle"/> to the native Swift data.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="SwiftHandle"/> must be disposed when no longer needed.
		/// <para/>
		/// This is a fallback that should be avoided because it causes boxing.
		///  Methods that deal with Swift values should provide a generic overload
		///  for <see cref="ISwiftBlittableStruct{T}"/> that takes a pointer to the value
		///  directly (e.g. using a <c>fixed</c> statement), rather than calling
		///  this method.
		/// </remarks>
		SwiftHandle ISwiftValue.GetSwiftHandle () => new SwiftHandle (this, SwiftType.Of (typeof (T))!);

		// Default implementation provided for POD only.
		void ISwiftValue.Dispose ()
		{
		}
	}

	// Sync with SwiftType.Of
	public static class SwiftValue
	{
		/// <summary>
		/// Returns a <see cref="SwiftHandle"/> bridging the given object to Swift.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="SwiftHandle"/> must be disposed when no longer needed.
		/// </remarks>
		/// <param name="obj">The managed object to bridge to Swift</param>
		/// <param name="nullability">Determines whether to bridge the value to Swift
		///  as an Optional value. If <typeparamref name="T"/> is identified as a known
		///  nullable wrapper, such as <see cref="Nullable"/>, then the value is bridged
		///  as an Optional regardless of the value of this parameter.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is
		///	 <c>null</c>, <typeparamref name="T"/> is not a known nullable wrapper type,
		///	 and <paramref name="nullability"/> returns <c>false</c> from <see cref="Nullability.IsNullable"/>.</exception>
		/// <exception cref="ArgumentException">Thrown when the type <typeparamref name="T"/> cannot
		///  be directly bridged to Swift</exception>
		//
		// N.B. We need this overload so we don't lose type information for nullable value types.
		public static SwiftHandle GetSwiftHandle<T> (this T obj, Nullability nullability = default)
			=> GetSwiftHandle (obj, typeof (T), nullability);

		public static unsafe SwiftHandle GetSwiftHandle (this object? obj, Type type, Nullability nullability = default)
		{
			var swiftType = SwiftType.Of (type, nullability);
			if (swiftType == null && obj != null) {
				type = obj.GetType ();
				swiftType = SwiftType.Of (type, nullability);
			}
			if (swiftType == null) {
				var msg = $"Type '{type}' cannot be bridged to Swift.";
				if (type == typeof (object))
					msg += " You may need to pass a more specific type to GetSwiftHandle.";
				throw new ArgumentException (msg);
			}

			// Nullable types are bridged to Swift optionals
			if (nullability.IsNullable || Nullability.IsReifiedNullable (type)) {
				if (Nullability.IsNull (obj)) {
					var underlyingType = Nullability.GetUnderlyingType (type);
					var underlyingSwiftType = SwiftType.Of (underlyingType, nullability.Strip ())!;
					return Optional.Wrap (null, swiftType, underlyingSwiftType);
				} else {
					var unwrapped = Nullability.Unwrap (obj);
					using (var unwrappedHandle = unwrapped.GetSwiftHandle (nullability.Strip ()))
						return Optional.Wrap (unwrappedHandle.Pointer, swiftType, unwrappedHandle.SwiftType);
				}
			} else if (obj is null) {
				throw new ArgumentNullException (nameof (obj), "Value cannot be null given the nullability info provided.");
			}

			return obj switch {
				ISwiftValue swiftValue => swiftValue.GetSwiftHandle (),
				string val => new SwiftHandle (new Swift.String (val), swiftType, destroyOnDispose: true),
				ITuple tup => GetTupleHandle (tup, swiftType, nullability),
				_ when type.IsPrimitive => new SwiftHandle (obj, swiftType),
				_ => throw new NotImplementedException (type.ToString ())
			};
		}

		unsafe static SwiftHandle GetTupleHandle (ITuple tuple, SwiftType tupleType, Nullability nullability)
		{
			var data = new byte [tupleType.NativeDataSize];
			var tupleMetadata = (TupleTypeMetadata*)tupleType.Metadata;
			Debug.Assert (tupleMetadata->NumElements == (ulong)tuple.Length);

			var elts = (TupleTypeMetadata.Element*)(tupleMetadata + 1);
			var types = tuple.GetType ().GetGenericArguments ();
			fixed (byte* dataPtr = &data [0]) {
				for (var i = 0; i < tuple.Length; i++) {
					using (var handle = tuple [i].GetSwiftHandle (types [i], nullability [i])) {
						var sty = handle.SwiftType;
						var dest = dataPtr + elts [i].Offset;
						sty.Transfer (dest, handle.Pointer, TransferFuncType.InitWithCopy);
					}
				}
			}
			return new SwiftHandle (data, tupleType, destroyOnDispose: true);
		}

		// Assumes tupleType has a constructor that takes all the elements
		unsafe static object GetTupleFromNative (byte* ptr, Type tupleType, Nullability nullability)
		{
			Debug.Assert (!nullability.IsNullable);
			var tupleMetadata = (TupleTypeMetadata*)SwiftType.Of (tupleType)!.Metadata;
			var elts = (TupleTypeMetadata.Element*)(tupleMetadata + 1);

			var types = tupleType.GetGenericArguments ();
			var args = new object? [types.Length];
			for (var i = 0; i < types.Length; i++)
				args [i] = FromNative ((IntPtr)(ptr + elts [i].Offset), types [i], nullability [i]);

			return Activator.CreateInstance (tupleType, args);
		}

		public static unsafe object? FromNative (IntPtr ptr, Type ty, Nullability nullability = default)
		{
			if (nullability.IsNullable || Nullability.IsReifiedNullable (ty)) {
				// assume this is a Swift Optional
				var underlyingType = Nullability.GetUnderlyingType (ty);
				var wrappedType = SwiftType.Of (underlyingType);
				if (wrappedType == null)
					throw new ArgumentException ($"Type '{underlyingType}' cannot be bridged to Swift");

				if (wrappedType.GetEnumTagSinglePayload ((void*)ptr, 1) == 1 /*nil*/)
					return Nullability.Wrap (null, ty)!;
				else
					return Nullability.Wrap (FromNative (ptr, underlyingType, nullability.Strip ()), ty);
			}

			switch (Type.GetTypeCode (ty)) {

			case TypeCode.String:
				var str = Marshal.PtrToStructure<Swift.String> (ptr);
				// FIXME: lifetime?? Should we dispose this? Depends on where the ptr is coming from
				return str.ToString ();
			}

			if (typeof (ITuple).IsAssignableFrom (ty))
				return GetTupleFromNative ((byte*)ptr, ty, nullability);

			if (ty.IsValueType)
				return Marshal.PtrToStructure (ptr, ty);

			return Activator.CreateInstance (ty, ptr); // FIXME: Be more creative
		}
	}
}
