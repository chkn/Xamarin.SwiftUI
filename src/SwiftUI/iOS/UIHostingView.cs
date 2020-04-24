using System;
using System.Runtime.InteropServices;

using UIKit;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
    public unsafe class UIHostingViewController : UIViewController
    {
		public UIHostingViewController (View view)
			: this (view.GetSwiftHandle ())
		{
		}

		public UIHostingViewController (SwiftHandle viewHandle)
			: this (Init (viewHandle.Pointer, viewHandle.SwiftType.Metadata, viewHandle.SwiftType.GetProtocolConformance (SwiftUILib.ViewProtocol)))
		{
			// release extra ref added by Xamarin runtime
			DangerousRelease ();
		}

		public UIHostingViewController (IntPtr handle)
			: base (handle)
		{
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_UIHostingController_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
