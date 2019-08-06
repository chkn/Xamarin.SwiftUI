using System;
using System.Runtime.InteropServices;

using SwiftUI.Interop;

namespace SwiftUI
{
	[StructLayout (LayoutKind.Sequential)]
	public readonly struct Text : IView<Text>
	{
		readonly IntPtr p1, p2, p3, p4;

		public ViewType<Text> SwiftType => SwiftUILib.Types.Text;
		SwiftType<Text> ISwiftValue<Text>.SwiftType => SwiftType;

		public Text (string verbatim) : this (new SwiftString (verbatim))
		{
		}

		public Text (SwiftString verbatim)
		{
			Init_verbatim (out this, verbatim);
		}

		public Text Copy () => SwiftType.Copy (in this);

		public void Dispose () => SwiftType.Destroy (in this);

		[DllImport ("libSwiftUIGlue.dylib",
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Text_verbatim")]
		static extern IntPtr Init_verbatim (out Text txt, SwiftString swiftStr);
	}
}