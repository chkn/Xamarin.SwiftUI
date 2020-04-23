using System;
using System.Runtime.InteropServices;

using UIKit;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
    public unsafe class UIHostingViewController: UIViewController
    {
		public static UIHostingViewController Create (View view)
		{
			using (var handle = view.GetSwiftHandle())
				return Create (handle.Pointer, handle.SwiftType);
		}

		static UIHostingViewController Create (void* viewData, SwiftType swiftType)
		{
			var obj = new UIHostingViewController(Init (viewData, swiftType.Metadata, swiftType.GetProtocolConformance (SwiftUILib.ViewProtocol)));
			// release extra ref added by Xamarin runtime
			obj.DangerousRelease ();
			return obj;
		}

		UIHostingViewController(IntPtr handle) : base (handle)
		{
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_UIHostingController_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
