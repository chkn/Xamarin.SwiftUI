using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Swift.Interop;

namespace Swift
{
	[StructLayout(LayoutKind.Sequential)]
	readonly unsafe struct Double : ISwiftBlittableStruct<Double>, IDisposable
	{
		public static SwiftType SwiftType => SwiftCoreLib.Types.Double;
		SwiftType ISwiftValue.SwiftType => SwiftCoreLib.Types.Double;

		public static Double Empty => default;

		public Double Copy() => SwiftType.Transfer(in this, TransferFuncType.InitWithCopy);

		public void Dispose() => SwiftType.Destroy(in this);

		// FIXME: Remove when this is fixed: https://github.com/mono/mono/issues/17869
		MemoryHandle ISwiftValue.GetHandle()
		{
			var gch = GCHandle.Alloc(this, GCHandleType.Pinned);
			return new MemoryHandle((void*)gch.AddrOfPinnedObject(), gch);
		}
	}
}
