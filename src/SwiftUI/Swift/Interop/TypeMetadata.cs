using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	using static MetadataFlags;

	// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L50-L63
	[Flags]
	public enum MetadataFlags
	{
		MetadataKindIsNonType = 0x400,
		MetadataKindIsNonHeap = 0x200,
		MetadataKindIsRuntimePrivate = 0x100,
	}

	/// <summary>
	/// See https://github.com/apple/swift/blob/master/include/swift/ABI/MetadataKind.def
	/// </summary>
	public enum MetadataKinds : long // technically, should be pointer sized
	{
		Class = 0,
		Struct = 0 | MetadataKindIsNonHeap,
		Enum = 1 | MetadataKindIsNonHeap,
		Optional = 2 | MetadataKindIsNonHeap,

		/// <summary>
		/// A foreign class, such as a Core Foundation class.
		/// </summary>
		ForeignClass = 3 | MetadataKindIsNonHeap,

		/// <summary>
		/// A type whose value is not exposed in the metadata system.
		/// </summary>
		Opaque = 0 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		Tuple = 1 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		Function = 2 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		Existential = 3 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		Metatype = 4 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		ObjCClassWrapper = 5 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,
		ExistentialMetatype = 6 | MetadataKindIsRuntimePrivate | MetadataKindIsNonHeap,

		/// <summary>
		/// A heap-allocated local variable using statically-generated metadata.
		/// </summary>
		HeapLocalVariable = 0 | MetadataKindIsNonType,

		/// <summary>
		/// A heap-allocated local variable using runtime-instantiated metadata.
		/// </summary>
		HeapGenericLocalVariable = 0 | MetadataKindIsNonType | MetadataKindIsRuntimePrivate,

		/// <summary>
		/// A native error object.
		/// </summary>
		ErrorObject = 1 | MetadataKindIsNonType | MetadataKindIsRuntimePrivate
	}

	public static class MetadataKind
	{
		public static bool IsClass (this MetadataKinds kind)
			=> kind == MetadataKinds.Class || (long)kind > 2047;

		public static MetadataKinds OfType (Type type)
		{
			if ((type.IsValueType && !type.IsEnum) || type.IsSubclassOf (typeof (SwiftStruct)))
				return MetadataKinds.Struct;
			//FIXME:
			throw new NotImplementedException ();
		}
	}

	[StructLayout (LayoutKind.Sequential)]
	public unsafe struct FullTypeMetadata
	{
		public ValueWitnessTable* ValueWitnessTable;
		public TypeMetadata Metadata;
	}

	[StructLayout (LayoutKind.Sequential)]
	public unsafe struct TypeMetadata
	{
		public MetadataKinds Kind;
		public NominalTypeDescriptor* TypeDescriptor;

#if DEBUG_TOSTRING
		public override string ToString ()
		{
			var str = Kind.ToString ();
			if (Kind == MetadataKinds.Struct)
				str += ((StructDescriptor*)TypeDescriptor)->ToString ();
			else
				str += TypeDescriptor->ToString ();
			return str;
		}
#endif
	}

	//https://github.com/apple/swift/blob/db4ce1f01bbb1ecda5fe744905a7fe61b3ff5a25/include/swift/ABI/Metadata.h#L1475
	[StructLayout (LayoutKind.Sequential)]
	public unsafe struct TupleTypeMetadata
	{
		public MetadataKinds Kind;
		/// The number of elements.
		public ulong NumElements;
		public IntPtr Labels;

		// ... followed by NumElements * Element...
		[StructLayout (LayoutKind.Sequential)]
		public struct Element
		{
			public TypeMetadata* Type;
			public ulong Offset;
		}
	}
}
