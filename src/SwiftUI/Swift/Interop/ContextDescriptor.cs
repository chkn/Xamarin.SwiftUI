using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// https://github.com/apple/swift/blob/db4ce1f01bbb1ecda5fe744905a7fe61b3ff5a25/include/swift/ABI/Metadata.h#L2460
	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct ContextDescriptor
	{
		public ContextDescriptorFlags Flags;
		internal RelativePointer ParentPtr;

		public ContextDescriptor* Parent => (ContextDescriptor*)ParentPtr.Target;
#if DEBUG_TOSTRING
		public override string ToString ()
			=> $"{{Flags = {Flags}, Parent = {(Parent == null ? "(null)" : Parent->ToString ())}}}";
#endif
	}

	// https://github.com/apple/swift/blob/db4ce1f01bbb1ecda5fe744905a7fe61b3ff5a25/include/swift/ABI/Metadata.h#L2510
	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct ModuleDescriptor
	{
		public ContextDescriptor Context;
		internal RelativePointer NamePtr;
		public string? Name => Marshal.PtrToStringAnsi ((IntPtr)NamePtr.Target);

#if DEBUG_TOSTRING
		public override string ToString ()
			=> $"{{Name = {Name}}}";
#endif
	}

	// https://github.com/apple/swift/blob/db4ce1f01bbb1ecda5fe744905a7fe61b3ff5a25/include/swift/ABI/Metadata.h#L3573
	[StructLayout (LayoutKind.Sequential)]
	public unsafe ref struct NominalTypeDescriptor
	{
		public ContextDescriptor Context;

		internal RelativePointer NamePtr;

		// A pointer to the metadata access function for this type.
		// MetadataRequest -> MetadataResponse
		internal RelativePointer AccessFunctionPtr;

		// A pointer to the field descriptor for the type, if any.
		internal RelativePointer FieldsPtr;

		public string? Name => Marshal.PtrToStringAnsi ((IntPtr)NamePtr.Target);

#if DEBUG_TOSTRING
		public override string ToString ()
			=> $"{{Context = {Context.ToString ()}"
			 + $", Name = {Name}"
			 + $", AccessFtn = {AccessFunctionPtr.ToString ()}"
			 + $", Fields = {FieldsPtr.ToString ()}}}";
#endif
	}

	// https://github.com/apple/swift/blob/db4ce1f01bbb1ecda5fe744905a7fe61b3ff5a25/include/swift/ABI/Metadata.h#L4097
	[StructLayout (LayoutKind.Sequential)]
	public ref struct StructDescriptor
	{
		public NominalTypeDescriptor NominalType;

		// The number of stored properties in the struct.
		// If there is a field offset vector, this is its length.
		public int NumberOfFields;

		// The offset in pointer-sized words of the field offset vector for this struct's stored
		// properties *in its metadata*, if any. 0 means there is no field offset
		// vector (if there is one, this is usually 2)
		public uint FieldOffsetVectorOffset;

#if DEBUG_TOSTRING
		public override string ToString ()
			=> $"{{NominalType = {NominalType.ToString ()}"
			 + $", NumberOfFields = {NumberOfFields}"
			 + $", FieldOffsVecOffs = {FieldOffsetVectorOffset}}}";
#endif
	}

	// https://github.com/apple/swift/blob/a2e0d08862274c8e2a7fef2e1fa656ac686ac676/include/swift/ABI/Metadata.h#L2942
	[StructLayout (LayoutKind.Sequential)]
	public ref struct ProtocolDescriptor
	{
		public ContextDescriptor Context;

		public RelativePointer NamePtr;

		/// The number of generic requirements in the requirement signature of the
		/// protocol.
		public uint NumRequirementsInSignature;

		public uint NumRequirements;

		public RelativePointer AssociatedTypeNamesPtr;

		// .. possibly followed by one or more "trailing objects":

		// GenericRequirementDescriptor * NumRequirementsInSignature
		// ProtocolRequirement * NumRequirements

#if DEBUG_TOSTRING
		public override unsafe string ToString ()
			 => $"{{Name = {Marshal.PtrToStringAnsi ((IntPtr)NamePtr.Target)}"
			  + $", NumRequirementsInSignature = {NumRequirementsInSignature}"
			  + $", NumRequirements = {NumRequirements}}}";
#endif
	}
}
