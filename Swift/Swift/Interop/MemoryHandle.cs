using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// FIXME: Switch to System.Buffers.MemoryHandle when we get .NET Standard 2.1
	public unsafe struct MemoryHandle : IDisposable
	{
		void* handle;
		GCHandle gcHandle;

		public void* Pointer => handle;

		public MemoryHandle (object objToPin)
		{
			gcHandle = GCHandle.Alloc (objToPin, GCHandleType.Pinned);
			handle = (void*)gcHandle.AddrOfPinnedObject ();
		}

		public MemoryHandle (void* handle, GCHandle gcHandle = default)
		{
			this.handle = handle;
			this.gcHandle = gcHandle;
		}

		public void Dispose ()
		{
			handle = null;
			if (gcHandle.IsAllocated) {
				gcHandle.Free ();
				gcHandle = default;
			}
		}
	}
}
