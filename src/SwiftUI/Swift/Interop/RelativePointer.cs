using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct RelativePointer
	{
		int offset;

		public static RelativePointer Zero => default;

		// FIXME: Should be able to freely take &this without fixed as this is a ref struct
		public void* Target {
			get {
				if (offset == 0)
					return null;
				fixed (void* ptr = &this)
					return (byte*)ptr + offset;
			}
			set {
				if (value == null)
					offset = 0;
				else fixed (void* ptr = &this)
					offset = checked ((int)((byte*)value - (byte*)ptr));
			}
		}

#if DEBUG
		public override string ToString ()
			=> offset.ToString ();
#endif
	}

	// https://github.com/apple/swift/blob/6399016c7b103b2616ea26bd8ed5ece5b2dc3945/include/swift/Basic/RelativePointer.h#L228
	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct RelativeIndirectablePointer
	{
		int offsetPlusIndirect;

		public static RelativePointer Zero => default;

		// FIXME: Should be able to freely take &this without fixed as this is a ref struct
		public void* Target {
			get {
				if (offsetPlusIndirect == 0)
					return null;
				fixed (void* ptr = &this) {
					var address = (byte*)ptr + (offsetPlusIndirect & ~1);
					// If the low bit is set, then this is an indirect address. Otherwise,
					// it's direct.
					return (offsetPlusIndirect & 1) == 1 ? *(void**)address : address;
				}
			}
		}

		// FIXME: Should be able to freely take &this without fixed as this is a ref struct
		public void SetTarget (void* value, bool indirect)
		{
			if (value == null)
				offsetPlusIndirect = 0;
			else fixed (void* ptr = &this)
				offsetPlusIndirect = checked((int)((byte*)value - (byte*)ptr));
			Debug.Assert ((offsetPlusIndirect & 1) == 0);
			if (indirect)
				offsetPlusIndirect |= 1;
		}
	}
}
