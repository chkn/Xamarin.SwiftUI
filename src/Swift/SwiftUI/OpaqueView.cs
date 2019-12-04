using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe abstract class OpaqueView : IView
	{
		protected abstract ViewType ViewType { get; }
		SwiftType ISwiftValue.SwiftType => ViewType;
		ViewType IView.SwiftType => ViewType;

		void* handle;
		GCHandle gcHandle;

		protected GCHandle GCHandle
			=> gcHandle.IsAllocated? gcHandle : (gcHandle = GCHandle.Alloc (this));

		public MemoryHandle GetHandle ()
		{
			if (handle == null) {
				var ptr = AllocNativeData ();
				handle = (void*)ptr;
				InitNativeData (ptr);
			}
			return new MemoryHandle (handle);
		}

		protected virtual IntPtr AllocNativeData ()
		{
			var nativeSize = ViewType.ValueWitnessTable->Size;
			return Marshal.AllocHGlobal (nativeSize);
		}

		protected virtual void FreeNativeData (IntPtr handle)
			=> Marshal.FreeHGlobal (handle);

		protected abstract void InitNativeData (IntPtr handle);

		public void Dispose ()
		{
			if (handle != null) {
				FreeNativeData ((IntPtr)handle);
				handle = null;
			}

			if (gcHandle.IsAllocated)
				gcHandle.Free ();
		}
	}
}
