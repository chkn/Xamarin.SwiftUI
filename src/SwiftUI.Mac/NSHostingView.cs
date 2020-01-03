using System;
using System.Runtime.InteropServices;

using AppKit;

using Swift;
using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public unsafe class NSHostingView : NSView, /*INSUserInterfaceValidations,*/ INSDraggingSource
	{
		public static NSHostingView Create (View view)
		{
			using (var handle = view.GetHandle ())
				return Create (handle.Pointer, view.ViewType);
		}

		static NSHostingView Create (void* viewData, ViewType swiftType)
		{
			var obj = new NSHostingView (Init (viewData, swiftType.Metadata, swiftType.ViewConformance));
			// release extra ref added by Xamarin runtime
			obj.DangerousRelease ();
			return obj;
		}

		NSHostingView (IntPtr handle): base (handle)
		{
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_NSHostingView_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
