using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
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
	[StructLayout (LayoutKind.Sequential)]
	public readonly struct ConformanceFlags
	{
		const int UnusedLowBits = 0x07;      // historical conformance kind

		const int TypeMetadataKindMask = 0x7 << 3; // 8 type reference kinds
		const int TypeMetadataKindShift = 3;

		const int IsRetroactiveMask = 0x01 << 6;
		const int IsSynthesizedNonUniqueMask = 0x01 << 7;

		const int NumConditionalRequirementsMask = 0xFF << 8;
		const int NumConditionalRequirementsShift = 8;

		const int HasResilientWitnessesMask = 0x01 << 16;
		const int HasGenericWitnessTableMask = 0x01 << 17;

		readonly int Value;

		public TypeReferenceKind TypeReferenceKind
			=> (TypeReferenceKind)((Value & TypeMetadataKindMask) >> TypeMetadataKindShift);

		public bool IsRetroactive => (Value & IsRetroactiveMask) != 0;

		public bool IsSynthesizedNonUnique => (Value & IsSynthesizedNonUniqueMask) != 0;

		public uint NumConditionalRequirements
			=> (uint)((Value & NumConditionalRequirementsMask) >> NumConditionalRequirementsShift);

		public bool HasResilientWitnesses => (Value & HasResilientWitnessesMask) != 0;

		public bool HasGenericWitnessTable => (Value & HasGenericWitnessTableMask) != 0;

		/// <summary>
		/// Size of trailing objects, not including any resilient witnesses
		/// </summary>
		internal unsafe int TrailingSize =>
			  (IsRetroactive? sizeof (RelativeIndirectablePointer) : 0)
			+ (NumConditionalRequirements > 0 ? throw new NotImplementedException () : 0)
			+ (HasResilientWitnesses? sizeof (ResilientWitnessesHeader) : 0)
			+ (HasGenericWitnessTable? sizeof (GenericWitnessTable) : 0);

		public ConformanceFlags (
			TypeReferenceKind kind = TypeReferenceKind.DirectTypeDescriptor,
			bool isRetroactive = false,
			bool isSynthesizedNonUnique = false,
			int numConditionalRequirements = 0,
			bool hasResilientWitnesses = false,
			bool hasGenericWitnessTable = false)
		{
			Value =
				  ((int)kind << TypeMetadataKindShift)
				| (isRetroactive ? IsRetroactiveMask : 0)
				| (isSynthesizedNonUnique ? IsSynthesizedNonUniqueMask : 0)
				| (numConditionalRequirements << NumConditionalRequirementsShift)
				| (hasResilientWitnesses ? HasResilientWitnessesMask : 0)
				| (hasGenericWitnessTable ? HasGenericWitnessTableMask : 0)
				;
		}
	}

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
		public ConformanceFlags Flags;

		// .. possibly followed by one or more "trailing objects":

		// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2423
		//public int NumRetroactiveContextPointers => Flags.IsRetroactive? 1 : 0;

		// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2428
		//public int NumGenericRequirementDescriptors => checked ((int)Flags.NumConditionalRequirements);

		// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2432
		//public int NumResilientWitnessesHeaders => Flags.HasResilientWitnesses? 1 : 0;

		// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2436
		//public int NumResilientWitnesses => Flags.HasResilientWitnesses?

		//public int NumGenericWitnessTables => Flags.HasGenericWitnessTable? 1 : 0;
	}

	// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2263
	[StructLayout (LayoutKind.Sequential)]
	struct ResilientWitnessesHeader
	{
		public uint NumWitnesses;
	}

	// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L1997
	[StructLayout (LayoutKind.Sequential)]
	ref struct ResilientWitness
	{
		public RelativeIndirectablePointer Requirement;
		public RelativePointer Witness;
	}

	// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2048
	[StructLayout (LayoutKind.Sequential)]
	ref struct GenericWitnessTable
	{
		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L338
		public const int NumGenericMetadataPrivateDataWords = 16;

		/// The size of the witness table in words.  This amount is copied from
		/// the witness table template into the instantiated witness table.
		public ushort WitnessTableSizeInWords;

		/// The amount of private storage to allocate before the address point,
		/// in words. This memory is zeroed out in the instantiated witness table
		/// template.
		///
		/// The low bit is used to indicate whether this witness table is known
		/// to require instantiation.
		public ushort WitnessTablePrivateSizeInWordsAndRequiresInstantiation;

		/// The instantiation function, which is called after the template is copied.
		public RelativePointer Instantiator;

		// Private data for instantiator
		public RelativePointer PrivateData;
	}
}
