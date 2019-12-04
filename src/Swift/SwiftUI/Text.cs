using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
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

		// FIXME: Remove when this is fixed: https://github.com/mono/mono/issues/17869
		unsafe MemoryHandle ISwiftValue.GetHandle ()
		{
			var gch = GCHandle.Alloc (this, GCHandleType.Pinned);
			return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
		}

		public Text (string verbatim) : this (new Swift.String (verbatim))
		{
		}

		public Text (Swift.String verbatim) => Init (out this, verbatim);

		public Text Copy () => SwiftType.Transfer (in this, SwiftType.CopyInitFunc);

		public void Dispose () => SwiftType.Destroy (in this);

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Text_verbatim")]
		static extern void Init (out Text txt, Swift.String verbatim);
	}
}