using System;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace Swift
{
	public class SwiftCoreLib : NativeLib
	{
		public const string Path = "/usr/lib/swift/libswiftCore.dylib";

		public static SwiftCoreLib Types { get; } = new SwiftCoreLib ();

		SwiftCoreLib () : base (Path)
		{
		}

		#region Types
		// Special types like String, Double require the mangling from https://github.com/apple/swift/blob/master/docs/ABI/Mangling.rst#types
		// This usually take the form of starting with S with the KNOWN-TYPE-KIND letter from the above link. eg String is SS, Float64/Double is Sd

		SwiftType? _int8;
		public SwiftType Int8 => _int8 ?? (_int8 = new SwiftType (this, "Swift", "Int8", SwiftTypeCode.Struct));

		SwiftType? _int16;
		public SwiftType Int16 => _int16 ?? (_int16 = new SwiftType (this, "Swift", "Int16", SwiftTypeCode.Struct));

		SwiftType? _int32;
		public SwiftType Int32 => _int32 ?? (_int32 = new SwiftType (this, "Swift", "Int32", SwiftTypeCode.Struct));

		SwiftType? _int64;
		public SwiftType Int64 => _int64 ?? (_int64 = new SwiftType (this, "Swift", "Int64", SwiftTypeCode.Struct));

		SwiftType? _string;
		public SwiftType String => _string ?? (_string = new SwiftType (this, "SS", typeof (String)));

		SwiftType? _unsafeRawPointer;
		public SwiftType UnsafeRawPointer => _unsafeRawPointer ?? (_unsafeRawPointer = new SwiftType (this, "SV"));

		// Double is actually a type alias for Float64, hence why we use Sd for mangling here
		SwiftType? _double;
		public SwiftType Double => _double ?? (_double = new SwiftType (this, "Sd", typeof(Double)));
		#endregion

		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/Runtime/Casting.h#L233
		[DllImport (Path, EntryPoint = "swift_conformsToProtocol")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolConformance (TypeMetadata* typeMetadata, ProtocolDescriptor* protocolDescriptor);

		// https://github.com/apple/swift/blob/10aac5696a3948b8580d188921786301328eb3a3/include/swift/Runtime/Metadata.h#L353
		[DllImport (Path, EntryPoint = "swift_getWitnessTable")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolWitnessTable (ProtocolConformanceDescriptor* conformanceDescriptor, TypeMetadata* typeMetadata, void* instantiationArgs);

		// Generic type metadata accessors:
		//  For values for the first arg, see https://github.com/apple/swift/blob/ffc0f6f783a53573eb79440f16584e0422378b16/include/swift/ABI/MetadataValues.h#L1594
		//  (generally we pass 0 for complete metadata)
		[DllImport(Path, EntryPoint = "swift_getOpaqueTypeMetadata")]
		internal unsafe static extern TypeMetadata* GetOpaqueTypeMetadata (long metadataReq, void** arguments, IntPtr descriptor, uint index);
	}
}
