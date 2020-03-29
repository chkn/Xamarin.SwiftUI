using System;
using System.Runtime.InteropServices;

namespace Swift.Interop {

	//https://github.com/apple/swift/blob/852585e491e929d1d0da115f92626dd5aa1f871b/include/swift/Reflection/Records.h#L126
	public enum FieldDescriptorKind : short
	{
		// Swift nominal types.
		Struct,
		Class,
		Enum,

		// Fixed-size multi-payload enums have a special descriptor format that
		// encodes spare bits.
		//
		// FIXME: Actually implement this. For now, a descriptor with this kind
		// just means we also have a builtin descriptor from which we get the
		// size and alignment.
		MultiPayloadEnum,

		// A Swift opaque protocol. There are no fields, just a record for the
		// type itself.
		Protocol,

		// A Swift class-bound protocol.
		ClassProtocol,

		// An Objective-C protocol, which may be imported or defined in Swift.
		ObjCProtocol,

		// An Objective-C class, which may be imported or defined in Swift.
		// In the former case, field type metadata is not emitted, and
		// must be obtained from the Objective-C runtime.
		ObjCClass
	};

	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct FieldDescriptor
	{
		internal RelativePointer MangledTypeNamePtr;
		internal RelativePointer SuperclassPtr;

		public FieldDescriptorKind Kind;
		public ushort FieldRecordSize;
		public uint NumFields;
		// Vector of NumFields FieldRecords follows...

		// maybe don't expose this.. a lot of the time it's a symbolic ref
		//public string MangledTypeName => Marshal.PtrToStringAnsi ((IntPtr)MangledTypeNamePtr.Target);
	}

	[Flags]
	public enum FieldRecordFlags
	{
		// Is this an indirect enum case?
		IsIndirectCase = 0x1,

		// Is this a mutable `var` property?
		IsVar = 0x2,
	}

	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct FieldRecord
	{
		public FieldRecordFlags Flags;
		internal RelativePointer MangledTypeNamePtr;
		internal RelativePointer FieldNamePtr;

		public string FieldName => Marshal.PtrToStringAnsi ((IntPtr)FieldNamePtr.Target);
	}
}
