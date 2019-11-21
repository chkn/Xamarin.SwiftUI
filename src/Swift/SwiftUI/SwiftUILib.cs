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
		public ViewType Text => _text ?? (_text = new ViewType (this, "Text", typeof (Text)));

		#endregion
	}
}
