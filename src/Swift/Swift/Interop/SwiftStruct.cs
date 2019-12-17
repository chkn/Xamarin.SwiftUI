using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	unsafe interface ISwiftFieldExposable : ISwiftValue
	{
		/// <summary>
		/// Initializes the native data for this Swift type in the given array
		///  at the given offset.
		/// </summary>
		void InitNativeData (byte [] data, int offset);

		/// <summary>
		/// Destroys the native data at the given location.
		/// </summary>
		void DestroyNativeData (void* handle);
	}

	/// <summary>
	/// A base class used to implement a Swift struct as a reference type,
	///  generally because we are unable to model the type as a managed value type.
	/// </summary>
	/// <remarks>
	/// Generally, it is preferable to bind Swift structs as managed value types.
	///  However, there are cases where this is not possible, such as when dealing
	///  with generic Swift structs that are not statically sized. This class is also
	///  used in cases where fields must be exposed in Swift metadata, since it
	///  can either be backed by memory it owns, or by a pointer to an external buffer.
	/// </remarks>
	public unsafe abstract class SwiftStruct<T> : ISwiftFieldExposable
		where T : SwiftStruct<T>
	{
		// implementing classes must add, by convention:
		// public static SwiftType SwiftType { get; }

		protected abstract SwiftType SwiftStructType { get; } // => SwiftType;
		SwiftType ISwiftValue.SwiftType => SwiftStructType;

		// We either store our own data or have a pointer to external data
		//  (e.g. a Swift field in a custom type), represented as an offset into
		//  someone else's data.
		// Using a byte array for the view's data allows the GC to collect it,
		//  so we don't need to dispose if the value ownership is moved to native
		byte []? data;
		int offset;

		protected bool NativeDataInitialized => data != null;

		// HACK: When Swift calls us back (e.g. View.Body), we overwrite our
		//  native data array so we are operating on the new data...
		internal void OverwriteNativeData (void* newData)
		{
			// FIXME: Figure out the ownership semantics.. I think this DestroyNativeData
			//  is wrong, since we theoretically already passed it owned previously
			fixed (void* dest = &data! [offset]) {
				DestroyNativeData (dest);
				SwiftStructType.Transfer (dest, newData, TransferFuncType.InitWithCopy);
			}
		}

		public MemoryHandle GetHandle ()
		{
			if (data == null) {
				Debug.Assert (offset == 0);
				data = new byte [SwiftStructType.NativeDataSize];
				try {
					InitNativeData (data, 0);
				} catch {
					data = null;
					throw;
				}
			}

			var gch = GCHandle.Alloc (data, GCHandleType.Pinned);
			return new MemoryHandle ((byte*)gch.AddrOfPinnedObject () + offset, gch);
		}

		/// <summary>
		/// Initializes our native data in the given buffer.
		/// </summary>
		void ISwiftFieldExposable.InitNativeData (byte [] data, int offset)
		{
			if (NativeDataInitialized)
				throw new InvalidOperationException ();
			this.data = data;
			this.offset = offset;
			InitNativeData (data, offset);
		}

		protected virtual void InitNativeData (byte [] data, int offset)
		{
			fixed (void* handle = &data [offset])
				InitNativeData (handle);
		}

		protected virtual void InitNativeData (void* handle)
			=> throw new NotImplementedException ();

		void ISwiftFieldExposable.DestroyNativeData (void* handle)
			=> DestroyNativeData (handle);

		protected virtual void DestroyNativeData (void* handle)
			=> SwiftStructType.Destroy (handle);

		public virtual T Copy ()
		{
			var copy = (T)MemberwiseClone ();

			if (data != null) {
				copy.data = new byte [SwiftStructType.NativeDataSize];
				fixed (void* src = &data [offset])
				fixed (void* dest = &copy.data [0])
					SwiftStructType.Transfer (dest, src, TransferFuncType.InitWithCopy);
			}

			return copy;
		}

		public void Dispose ()
		{
			if (data != null) {
				fixed (void* handle = &data [offset])
					DestroyNativeData (handle);
				data = null;
			}
		}
	}
}
