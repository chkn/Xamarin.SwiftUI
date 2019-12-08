using System;
using System.Buffers;
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
		// by convention:
		// public static SwiftType SwiftType { get; }

		SwiftType SwiftType { get; }

		/// <summary>
		/// Gets a <see cref="MemoryHandle"/> to the native data.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="MemoryHandle"/> must be disposed when no longer needed.
		/// </remarks>
		MemoryHandle GetHandle ();

		/// <summary>
		/// Decrements any references to reference-counted data held by this
		///  <see cref="ISwiftValue"/>.
		/// </summary>
		/// <remarks>
		/// Note: this interface doesn't implement IDisposable because this causes
		///  F# to require the use of the <c>new</c> operator, which harshes our DSL.
		///
		/// In the common SwiftUI case, most values we create are moved to native and
		///  not retained by managed code, so calling <see cref="Dispose"/> is not needed.
		///  For types where we expect the user will need to call <see cref="Dispose"/>
		///  explicitly, we'll explicitly implement <see cref="IDisposable"/> in those
		///  specific cases.
		/// </remarks>
		void Dispose ();
	}

	/// <summary>
	/// A Swift struct that is fully represented as a blittable managed struct.
	/// </summary>
	/// <typeparam name="T">The blittable managed struct type.
	///	  This must be the type that implements this interface.</typeparam>
	public interface ISwiftBlittableStruct<T> : ISwiftValue
		where T : unmanaged, ISwiftBlittableStruct<T>
	{
		// FIXME: Disabled to workaround https://github.com/mono/mono/issues/17869
#if false
		/// <remarks>
		/// This is a fallback that should be avoided because it causes boxing.
		///  Methods that deal with Swift values should provide a generic overload
		///  for <see cref="ISwiftBlittableStruct{T}"/> that takes a pointer to the value
		///  directly (e.g. using a <c>fixed</c> statement), rather than calling
		///  this property.
		/// </remarks>
		unsafe MemoryHandle ISwiftValue.Handle {
			get {
				var gch = GCHandle.Alloc (this, GCHandleType.Pinned);
				return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
			}
		}
#endif

		/// <summary>
		/// Creates a copy of this value, while retaining ownership of the original value.
		/// </summary>
		/// <remarks>
		/// If this method is not called, passing this value is considered a move operation.
		///  The original value is not valid after a move operation, and it should not be used
		///  or disposed.
		/// </remarks>
		T Copy ();
	}
}
