using System;
using System.Runtime.InteropServices;
using Swift;
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

		SwiftType? _text;
		public SwiftType Text
			=> _text ??= new SwiftType (this, typeof (Text));

		public SwiftType Button (SwiftType label)
			=> new SwiftType (GetButtonType (0, label.Metadata, label.GetProtocolConformance (View)), genericArgs: new[] { label });

		public SwiftType State (SwiftType value)
			=> new SwiftType (GetStateType (0, value.Metadata), genericArgs: new[] { value });

		SwiftType? _color;
		public SwiftType Color
			=> _color ??= new SwiftType (this, typeof (Color));

		#endregion

		IntPtr _viewOpacityTypeDescriptor;
		public IntPtr ViewOpacityTypeDescriptor
			=> _viewOpacityTypeDescriptor == IntPtr.Zero ? (_viewOpacityTypeDescriptor = RequireSymbol("$s7SwiftUI4ViewPAAE7opacityyQrSdFQOMQ")) : _viewOpacityTypeDescriptor;

		IntPtr _viewBackgroundTypeDescriptor;
		public IntPtr ViewBackgroundTypeDescriptor
			=> _viewBackgroundTypeDescriptor == IntPtr.Zero ? (_viewBackgroundTypeDescriptor = RequireSymbol ("$s7SwiftUI4ViewPAAE10background_9alignmentQrqd___AA9AlignmentVtAaBRd__lFQOMQ")) : _viewBackgroundTypeDescriptor;

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
