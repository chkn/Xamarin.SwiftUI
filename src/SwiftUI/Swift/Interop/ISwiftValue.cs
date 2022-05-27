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
		/// Gets a <see cref="SwiftHandle"/> to the native Swift data.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="SwiftHandle"/> must be disposed when no longer needed.
		/// </remarks>
		//
		// Not public to ensure calls go through SwiftValue.GetSwiftHandle
		protected internal SwiftHandle GetSwiftHandle (Nullability nullability);

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
		SwiftHandle ISwiftValue.GetSwiftHandle (Nullability nullability)
			=> new SwiftHandle (this, SwiftType.Of (typeof (T), nullability)!);

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

		/// <inheritdoc cref="GetSwiftHandle{T}(T, Nullability)" />
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
					using var unwrappedHandle = unwrapped.GetSwiftHandle (nullability.Strip ());
					return Optional.Wrap (unwrappedHandle.Pointer, swiftType, unwrappedHandle.SwiftType);
				}
			} else if (obj is null) {
				throw new ArgumentNullException (nameof (obj), "Value cannot be null given the nullability info provided.");
			}

			return obj switch {
				ISwiftValue swiftValue => swiftValue.GetSwiftHandle (nullability),
				string val => new SwiftHandle (new Swift.String (val), swiftType, destroyOnDispose: true),
				ITuple tup => GetTupleHandle (tup, swiftType, nullability),
				bool val => new SwiftHandle (val? (byte)1 : (byte)0, swiftType), // FIXME: Don't box and pin a byte
				_ when type.IsPrimitive => new SwiftHandle (obj, swiftType),
				_ => throw new NotImplementedException (type.ToString ())
			};
		}

		unsafe static SwiftHandle GetTupleHandle (ITuple tuple, SwiftType swiftType, Nullability nullability)
		{
			var tupleType = tuple.GetType ();
			var types = Tuples.GetElementTypes (tupleType);
			Debug.Assert (types.Length == tuple.Length);

			// Swift 1-ples are just the bare value
			if (types.Length == 1)
				return GetSwiftHandle (tuple [0], types [0], nullability [0]);

			// Otherwise, it must be an actual Swift tuple type
			var swiftTupleType = (SwiftTupleType)swiftType;
			nullability = Tuples.FlattenNullability (tupleType, nullability);
			Debug.Assert (swiftTupleType.Metadata->NumElements == (ulong)types.Length);

			var data = new byte [swiftTupleType.NativeDataSize];
			var elts = (TupleTypeMetadata.Element*)(swiftTupleType.Metadata + 1);
			fixed (byte* dataPtr = &data [0]) {
				for (var i = 0; i < types.Length; i++) {
					using (var handle = tuple [i].GetSwiftHandle (types [i], nullability [i])) {
						var sty = handle.SwiftType;
						var dest = dataPtr + elts [i].Offset;
						sty.Transfer (dest, handle.Pointer, TransferFuncType.InitWithCopy);
					}
				}
			}
			return new SwiftHandle (data, swiftTupleType, destroyOnDispose: true);
		}

		unsafe static object GetTupleFromNative (byte* ptr, Type tupleType, Nullability nullability)
		{
			Debug.Assert (!nullability.IsNullable);
			var types = Tuples.GetElementTypes (tupleType);
			var args = new object [types.Length];

			// Swift 1-ples are just the bare value
			if (types.Length == 1) {
				args [0] = FromNative ((IntPtr)ptr, types [0], nullability [0])!;
			} else {
				var tupleMetadata = ((SwiftTupleType)SwiftType.Of (tupleType)!).Metadata;
				var elts = (TupleTypeMetadata.Element*)(tupleMetadata + 1);
				for (var i = 0; i < types.Length; i++)
					args [i] = FromNative ((IntPtr)(ptr + elts [i].Offset), types [i], nullability [i])!;
			}

			return Tuples.CreateTuple (tupleType, args);
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

			case TypeCode.Boolean:
				return Marshal.ReadByte (ptr) == 1;
			}

			if (typeof (ITuple).IsAssignableFrom (ty))
				return GetTupleFromNative ((byte*)ptr, ty, nullability);

			if (ty.IsValueType)
				return Marshal.PtrToStructure (ptr, ty);

			return Activator.CreateInstance (ty, ptr); // FIXME: Be more creative
		}
	}
}
