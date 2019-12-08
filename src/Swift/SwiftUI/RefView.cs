using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	/// <summary>
	/// A base class used to implement a SwiftUI view as a reference type,
	///  generally because we cannot model the view type as a C# struct.
	/// </summary>
	public unsafe abstract class RefView<T> : IView
		where T : RefView<T>
	{
		protected abstract ViewType ViewType { get; }
		SwiftType ISwiftValue.SwiftType => ViewType;
		ViewType IView.SwiftType => ViewType;

		// using a byte array for the view's data allows the GC to collect it,
		//  so we don't need to dispose if the value ownership is moved to native
		byte []? data;

		protected virtual long NativeDataSize
			=> (long)ViewType.ValueWitnessTable->Size;

		public MemoryHandle GetHandle ()
		{
			if (data == null) {
				data = new byte [NativeDataSize];
				try {
					InitNativeData (data);
				} catch {
					data = null;
					throw;
				}
			}
			var gch = GCHandle.Alloc (data, GCHandleType.Pinned);
			return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
		}

		protected abstract void InitNativeData (byte [] data);

		protected virtual void DestroyNativeData (byte [] data)
		{
			fixed (void* ptr = &data [0])
				ViewType.Destroy (ptr);
		}

		public virtual T Copy ()
		{
			var copy = (T)MemberwiseClone ();
			if (data != null) {
				copy.data = new byte [data.LongLength];
				fixed (void* src = &data [0])
				fixed (void* dest = &copy.data [0])
					ViewType.Transfer (dest, src, ViewType.CopyInitFunc);
			}
			return copy;
		}

		public virtual void Dispose ()
		{
			if (data != null) {
				var d = data;
				data = null;
				DestroyNativeData (d);
			}
		}
	}
}
