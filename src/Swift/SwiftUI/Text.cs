using System;
using System.Runtime.InteropServices;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	[StructLayout (LayoutKind.Sequential)]
	public readonly struct Text : IView<Text>
	{
		public static ViewType SwiftType => SwiftUILib.Types.Text;
		SwiftType ISwiftValue.SwiftType => SwiftUILib.Types.Text;
		ViewType IView.SwiftType => SwiftUILib.Types.Text;

		// Opaque data
		readonly IntPtr p1, p2, p3, p4;

		public Text (string verbatim) : this (new Swift.String (verbatim))
		{
		}

		public Text (Swift.String verbatim) => Init (out this, verbatim);

		public Text Copy () => SwiftType.Copy (in this);

		public void Dispose () => SwiftType.Destroy (in this);

		[DllImport ("libSwiftUIGlue.dylib",
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Text_verbatim")]
		static extern void Init (out Text txt, Swift.String verbatim);
	}
}