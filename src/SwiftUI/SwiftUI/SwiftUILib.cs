using System;
using System.Runtime.InteropServices;
using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe static class SwiftUILib
	{
		public const string Path = "/System/Library/Frameworks/SwiftUI.framework/SwiftUI";

		// convenience
		internal static NativeLib Lib => NativeLib.Get (Path);

		#region Protocols

		static ProtocolDescriptor* _view;
		public static ProtocolDescriptor* ViewProtocol => _view == null ? (_view = Lib.GetProtocol ("SwiftUI", "View")) : _view;

		#endregion
	}
}
