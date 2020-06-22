using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	//https://github.com/apple/swift/blob/a021e6ca020e667ce4bc8ee174e2de1cc0d9be73/include/swift/ABI/MetadataValues.h#L948
	[StructLayout (LayoutKind.Sequential)]
	readonly struct TupleTypeFlags
	{
		readonly ulong/*size_t*/ data;

		public TupleTypeFlags (ushort numElements)
		{
			data = numElements;
		}
	}
}
