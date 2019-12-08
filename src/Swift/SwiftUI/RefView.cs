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
	public unsafe abstract class RefView : IView
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
			fixed (void* ptr = &data[0])
				ViewType.Destroy (ptr);
		}

		public virtual void Dispose ()
		{
			if (data != null) {
				DestroyNativeData (data);
				data = null;
			}
		}
	}
}
