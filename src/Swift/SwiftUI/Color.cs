using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[StructLayout(LayoutKind.Sequential)]
	readonly unsafe struct Color : ISwiftBlittableStruct<Color>, IDisposable
	{
        public SwiftType SwiftType => SwiftUILib.Types.Color;

		SwiftType ISwiftValue.SwiftType => SwiftCoreLib.Types.Double;

		public static Color Empty => default;

		public Color Copy() => SwiftType.Transfer(in this, TransferFuncType.InitWithCopy);

		public void Dispose() => SwiftType.Destroy(in this);

		// FIXME: Remove when this is fixed: https://github.com/mono/mono/issues/17869
		MemoryHandle ISwiftValue.GetHandle()
		{
			var gch = GCHandle.Alloc(this, GCHandleType.Pinned);
			return new MemoryHandle((void*)gch.AddrOfPinnedObject(), gch);
		}
	}
}
