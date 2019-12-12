using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// https://github.com/apple/swift/blob/master/docs/ABI/Mangling.rst#symbolic-references
	public enum SymbolicReferenceKind : byte
	{
		DirectContext = 1,
		IndirectContext = 2,
		// ...
	}

	[StructLayout (LayoutKind.Sequential, Pack = 1)]
	public ref struct SymbolicReference
	{
		public SymbolicReferenceKind Kind;
		public RelativePointer Pointer;
	}
}
