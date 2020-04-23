using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
		public static unsafe SwiftHandle GetSwiftHandle<T> (this T obj, Nullability nullability = default)
		{
			var type = typeof (T);
			var swiftType = SwiftType.Of (type, nullability);
			if (swiftType == null && obj != null) {
				type = obj.GetType ();
				swiftType = SwiftType.Of (type, nullability);
			}
			if (swiftType == null)
				throw new ArgumentException ($"Type '{type}' cannot be bridged to Swift");

			// Nullable types are bridged to Swift optionals
			if (nullability.IsNullable || Nullability.IsReifiedNullable (type)) {
				if (Nullability.IsNull (obj)) {
					var underlyingType = Nullability.GetUnderlyingType (type);
					var underlyingSwiftType = SwiftType.Of (underlyingType, nullability.Strip ())!;
					return CopyAsOptional (null, swiftType, underlyingSwiftType);
				} else {
					var unwrapped = Nullability.Unwrap (obj);
					using (var unwrappedHandle = unwrapped.GetSwiftHandle (nullability.Strip ()))
						return CopyAsOptional (unwrappedHandle.Pointer, swiftType, unwrappedHandle.SwiftType);
				}
			} else if (obj is null) {
				throw new ArgumentNullException (nameof (obj));
			}

			return obj switch {
				ISwiftValue swiftValue => swiftValue.GetSwiftHandle (),
				string val => new SwiftHandle (new Swift.String (val), swiftType, destroyOnDispose: true),
				_ when type.IsPrimitive => new SwiftHandle (obj, swiftType),
				_ => throw new NotImplementedException (type.ToString ())
			};
		}

		unsafe static SwiftHandle CopyAsOptional (void* src, SwiftType optionalType, SwiftType wrappedType)
		{
			var data = new byte [optionalType.NativeDataSize];
			fixed (void* dest = &data [0]) {
				var tag = 1; // nil
				if (src != null) {
					tag = 0;
					wrappedType.Transfer (dest, src, TransferFuncType.InitWithCopy);
				}
				wrappedType.StoreEnumTagSinglePayload (dest, tag, 1);
			}
			return new SwiftHandle (data, optionalType, destroyOnDispose: true);
		}

		public unsafe static TValue FromNative<TValue> (IntPtr ptr, Nullability nullability = default)
		{
			var ty = typeof (TValue);

			if (nullability.IsNullable || Nullability.IsReifiedNullable (ty)) {
				// assume this is a Swift Optional
				var underlyingType = Nullability.GetUnderlyingType (ty);
				var wrappedType = SwiftType.Of (underlyingType);
				if (wrappedType == null)
					throw new ArgumentException ($"Type '{underlyingType}' cannot be bridged to Swift");

				if (wrappedType.GetEnumTagSinglePayload ((void*)ptr, 1) == 1 /*nil*/)
					return Nullability.Wrap<TValue> (null);
				else
					return Nullability.Wrap<TValue> (FromNative (ptr, underlyingType));
			}

			return (TValue)FromNative (ptr, ty);
		}

		static object FromNative (IntPtr ptr, Type ty)
		{
			switch (Type.GetTypeCode (ty)) {

			case TypeCode.String:
				var str = Marshal.PtrToStructure<Swift.String> (ptr);
				// FIXME: lifetime?? Should we dispose this? Depends on where the ptr is coming from
				return str.ToString ();
			}

			if (ty.IsValueType)
				return Marshal.PtrToStructure (ptr, ty);

			return Activator.CreateInstance (ty, ptr); // FIXME: Be more creative
		}
	}
}
