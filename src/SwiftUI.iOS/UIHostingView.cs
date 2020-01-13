using System;
using System.Runtime.InteropServices;

using UIKit;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;
using ObjCRuntime;

[assembly: LinkWith("../../../../../../../src/SwiftUIGlue/" + SwiftGlueiOSLib.Path, LinkTarget.Simulator | LinkTarget.Simulator64 | LinkTarget.ArmV7 | LinkTarget.ArmV7s, ForceLoad = true)]

namespace SwiftUI
{
    public unsafe class UIHostingViewController: UIViewController
    {
		public static UIHostingViewController Create (View view)
		{
			using (var handle = view.GetHandle())
				return Create (handle.Pointer, view.ViewType);
		}

		static UIHostingViewController Create (void* viewData, SwiftType swiftType)
		{
			var obj = new UIHostingViewController(Init (viewData, swiftType.Metadata, swiftType.GetProtocolConformance(SwiftUILib.Types.View)));
			// release extra ref added by Xamarin runtime
			obj.DangerousRelease ();
			return obj;
		}

		UIHostingViewController(IntPtr handle) : base (handle)
		{
		}

		[DllImport (SwiftGlueiOSLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_UIHostingController_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
