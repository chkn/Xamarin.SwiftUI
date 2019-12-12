using System;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe class SwiftUILib : NativeLib
	{
		public const string Path = "/System/Library/Frameworks/SwiftUI.framework/SwiftUI";

		public static SwiftUILib Types { get; } = new SwiftUILib ();

		SwiftUILib () : base (Path)
		{
		}

		#region Protocols

		ProtocolDescriptor* _view;
		public ProtocolDescriptor* View => _view == null ? (_view = GetProtocol ("SwiftUI", "View")) : _view;

		#endregion

		#region Types

		ViewType? _text;
		public ViewType Text => _text ??= new ViewType (this, typeof (Text));

		public ViewType Button (ViewType label)
			=> new ViewType (GetButtonType (0, label.Metadata, label.ViewConformance), genericArgs: new[] { label });

		public SwiftType State (SwiftType value)
			=> new SwiftType (GetStateType (0, value.Metadata), genericArgs: new[] { value });

		#endregion

		// Generic type metadata accessors:
		//  For values for the first arg, see https://github.com/apple/swift/blob/ffc0f6f783a53573eb79440f16584e0422378b16/include/swift/ABI/MetadataValues.h#L1594
		//  (generally we pass 0 for complete metadata)

		[DllImport (Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI6ButtonVMa")]
		static extern IntPtr GetButtonType (long metadataReq, TypeMetadata* labelType, ProtocolWitnessTable* labelViewConformance);

		[DllImport (Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5StateVMa")]
		static extern IntPtr GetStateType (long metadataReq, TypeMetadata* valueType);
	}
}
