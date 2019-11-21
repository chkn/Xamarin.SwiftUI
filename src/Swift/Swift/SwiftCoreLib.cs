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

		SwiftType? _int8;
		public SwiftType Int8 => _int8 ?? (_int8 = new SwiftType (this, "Swift", "Int8"));

		SwiftType? _string;
		public SwiftType String => _string ?? (_string = new SwiftType (this, "SS", typeof (String)));

		SwiftType? _unsafeRawPointer;
		public SwiftType UnsafeRawPointer => _unsafeRawPointer ?? (_unsafeRawPointer = new SwiftType (this, "SV"));

		#endregion

		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/Runtime/Casting.h#L233
		[DllImport (Path, EntryPoint = "swift_conformsToProtocol")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolConformance (TypeMetadata* typeMetadata, ProtocolDescriptor* protocolDescriptor);

		// https://github.com/apple/swift/blob/10aac5696a3948b8580d188921786301328eb3a3/include/swift/Runtime/Metadata.h#L353
		[DllImport (Path, EntryPoint = "swift_getWitnessTable")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolWitnessTable (ProtocolConformanceDescriptor* conformanceDescriptor, TypeMetadata* typeMetadata, void* instantiationArgs);
	}
}
