using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// We bind Swift values in a couple ways:
	//  1. ISwiftValue<T> represents a managed struct of type T that is itself the native data.
	//  2. ISwiftValue represents a wrapper type that has a Handle (pointer) to the data.

	/// <summary>
	/// A Swift struct or class type.
	/// </summary>
	public interface ISwiftValue
	{
		// by convention:
		// public static SwiftType SwiftType { get; }

		SwiftType SwiftType { get; }

		/// <summary>
		/// A <see cref="MemoryHandle"/> to the native data.
		/// </summary>
		MemoryHandle Handle { get; }

		// Note: this interface doesn't implement IDisposable because this causes
		//  F# to require the use of the 'new' operator, which harshes our DSL.
		//
		// In the common SwiftUI case, most values we create are moved to native and
		//  not retained by managed code, so calling Dispose is not needed. For types
		//  that we expect to have references retained by managed code, we'll explicitly
		//  implement IDisposable in those specific cases.
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
	}
}
