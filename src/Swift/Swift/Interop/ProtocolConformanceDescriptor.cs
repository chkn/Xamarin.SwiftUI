using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	/*
	// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L341
	public enum TypeReferenceKind
	{
		/// The conformance is for a nominal type referenced directly;
		/// getTypeDescriptor() points to the type context descriptor.
		DirectTypeDescriptor = 0x00,

		/// The conformance is for a nominal type referenced indirectly;
		/// getTypeDescriptor() points to the type context descriptor.
		IndirectTypeDescriptor = 0x01,

		// ... (we don't support the obj-c ones) ...
	}

	// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L559
	public enum ConformanceFlags
	{
		UnusedLowBits = 0x07,      // historical conformance kind

		TypeMetadataKindMask = 0x7 << 3, // 8 type reference kinds
		TypeMetadataKindShift = 3,

		IsRetroactiveMask = 0x01 << 6,
		IsSynthesizedNonUniqueMask = 0x01 << 7,

		NumConditionalRequirementsMask = 0xFF << 8,
		NumConditionalRequirementsShift = 8,

		HasResilientWitnessesMask = 0x01 << 16,
		HasGenericWitnessTableMask = 0x01 << 17,
	}
	*/

	// https://github.com/apple/swift/blob/659c49766be5e5cfa850713f43acc4a86f347fd8/include/swift/ABI/Metadata.h#L2270
	[StructLayout (LayoutKind.Sequential)]
	public ref struct ProtocolConformanceDescriptor
	{
		public RelativeIndirectablePointer ProtocolPtr;

		// Either direct or indirect (determined by Flags) pointer to type descriptor
		public RelativePointer TypeDescriptorPtr;

		/// The witness table pattern, which may also serve as the witness table.
		public RelativePointer WitnessTablePatternPtr;

		/// Various flags, including the kind of conformance.
		/*ConformanceFlags*/ int Flags; // FIXME: Care about these? For now, we just use 0
	}
}
