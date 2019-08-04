using System;
using System.Runtime.InteropServices;

using AppKit;
using SwiftUI;
using SwiftUI.Interop;

namespace XamMacSwiftUITest
{
	static class MainClass
	{
		static int Main (string [] args)
		{
			NSApplication.Init ();

			var txt = new Text ("HELLO SwiftUI FROM C#!");

			return NetUIMain (&txt);
		}

		unsafe static int NetUIMain (View view) => NetUIMain (view.NativeData);

		unsafe static int NetUIMain<T> (T* view) where T : unmanaged, IView<T>
			=> NetUIMain (view, view->SwiftType.Metadata, view->SwiftType.ViewConformance);

		[DllImport ("libSwiftUIBackend.dylib",
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "_netui_main")]
		static extern unsafe int NetUIMain (void* viewData, TypeMetadata* viewType, IntPtr viewConformance);
		// ^ Compare: https://github.com/apple/swift/blob/01823ca52138a9844e84ee7e8efba13970e1e25d/stdlib/public/runtime/SwiftValue.mm#L333-L336
		//    as a call to: https://github.com/apple/swift/blob/da61cc8cdf7aa2bfb3ab03200c52c4d371dc6751/stdlib/public/core/Hashable.swift#L161
	}
}
