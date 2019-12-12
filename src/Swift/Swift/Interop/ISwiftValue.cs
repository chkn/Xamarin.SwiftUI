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
		// by convention:
		// public static SwiftType SwiftType { get; }

		// should be implemented explicitly:
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

	// Sync with SwiftType.Of
	public static class SwiftValue
	{
		/// <summary>
		/// Returns an <see cref="ISwiftValue"/> representing the given object.
		/// </summary>
		/// <param name="obj">The manage object to bridge to Swift</param>
		/// <exception cref="ArgumentException">Thrownhe given managed object cannot
		///  be directly bridged to Swift</exception>
		public static ISwiftValue? ToSwiftValue (this object? obj)
		{
			switch (obj) {

			case null: return null;
			case ISwiftValue swiftValue: return swiftValue;
			case string val: return new Swift.String (val);
			}

			// FIXME: obj cannot be null due to "case null" above - nullability bug?
			var swiftType = SwiftType.Of (obj!.GetType ());
			if (swiftType is null)
				throw new ArgumentException ("Given object cannot be bridged to Swift");

			return new POD (obj, swiftType);
		}

		public static TSwiftValue FromNative<TSwiftValue> (IntPtr ptr)
		{
			var ty = typeof (TSwiftValue);
			if (ty.IsValueType)
				return Marshal.PtrToStructure<TSwiftValue> (ptr);

			return (TSwiftValue)Activator.CreateInstance (typeof (TSwiftValue), ptr);
		}

		/// <summary>
		/// A wrapper for POD types to expose them as <see cref="ISwiftValue"/>s.
		/// </summary>
		class POD : ISwiftValue
		{
			public SwiftType SwiftType { get; }

			object value;

			public unsafe MemoryHandle GetHandle ()
			{
				var gch = GCHandle.Alloc (value, GCHandleType.Pinned);
				return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
			}

			public POD (object value, SwiftType swiftType)
			{
				this.value = value;
				SwiftType = swiftType;
				unsafe {
					Debug.Assert (!swiftType.ValueWitnessTable->IsNonPOD);
				}
			}

			public void Dispose ()
			{
				// nop, since this is POD
			}
		}
	}
}
