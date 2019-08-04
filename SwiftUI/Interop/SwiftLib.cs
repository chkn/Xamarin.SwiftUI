using System;
using System.Runtime.InteropServices;

namespace SwiftUI.Interop
{
	public class SwiftLib : NativeLib
	{
		public const string Core = "/usr/lib/swift/libswiftCore.dylib";
		public const string Foundation = "/usr/lib/swift/libswiftFoundation.dylib";

		public static SwiftLib Types { get; } = new SwiftLib ();

		SwiftLib () : base (Core)
		{
		}

		#region Types

		SwiftType _int8;
		public SwiftType Int8 => _int8 ?? (_int8 = new SwiftType (this, "Swift", "Int8"));

		SwiftType<SwiftString> _string;
		public SwiftType<SwiftString> String => _string ?? (_string = new SwiftType<SwiftString> (this, "SS"));

		SwiftType _unsafeRawPointer;
		public SwiftType UnsafeRawPointer => _unsafeRawPointer ?? (_unsafeRawPointer = new SwiftType (this, "SV"));

		#endregion

		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/Runtime/Casting.h#L233
		[DllImport (Core, EntryPoint = "swift_conformsToProtocol")]
		internal unsafe static extern IntPtr GetProtocolConformance (TypeMetadata* typeMetadata, IntPtr protocolDescriptor);
	}
}
