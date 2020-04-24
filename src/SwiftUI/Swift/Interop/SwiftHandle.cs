using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	[StructLayout (LayoutKind.Auto)]
	public unsafe struct SwiftHandle : IDisposable
	{
		SwiftType swiftType;
		TaggedPointer tp;

		// optionally allocated if pointer was pinned
		GCHandle handle;

		public void* Pointer => tp.Pointer;
		public SwiftType SwiftType => swiftType;

		public SwiftHandle (void* pointer, SwiftType swiftType, GCHandle handle = default)
		{
			this.swiftType = swiftType ?? throw new ArgumentNullException (nameof (swiftType));
			this.tp = new TaggedPointer (pointer, false);
			this.handle = handle;
		}

		public SwiftHandle (object toPin, SwiftType swiftType, bool destroyOnDispose = false)
		{
			this.swiftType = swiftType ?? throw new ArgumentNullException (nameof (swiftType));
			this.handle = GCHandle.Alloc (toPin, GCHandleType.Pinned);
			this.tp = new TaggedPointer (handle.AddrOfPinnedObject (), destroyOnDispose);
		}

		public void Dispose ()
		{
			if (tp.IsOwned)
				swiftType.Destroy (tp.Pointer);
			if (handle.IsAllocated)
				handle.Free ();
			tp = default;
		}
	}
}
