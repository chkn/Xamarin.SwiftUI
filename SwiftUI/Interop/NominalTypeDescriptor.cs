using System;
using System.Runtime.InteropServices;

namespace SwiftUI.Interop
{
	[StructLayout (LayoutKind.Sequential)]
	public readonly unsafe ref struct RelativePointer
	{
		readonly int offset;

		// FIXME: Should be able to freely take &this without fixed as this is a ref struct
		public void* Target {
			get {
				fixed (void* ptr = &this)
					return (byte*)ptr + offset;
			}
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	public readonly unsafe ref struct NominalTypeDescriptor
	{
		public readonly int Flags;
		public readonly int Parent;
		readonly RelativePointer namePtr;

		public string Name => Marshal.PtrToStringAnsi ((IntPtr)namePtr.Target);
	}
}
