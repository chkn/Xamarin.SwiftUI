using System;
using System.Runtime.InteropServices;

using AppKit;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	public unsafe class NSHostingView : NSView, /*INSUserInterfaceValidations,*/ INSDraggingSource
	{
		public NSHostingView (View view)
			: this (view.GetSwiftHandle ())
		{
		}

		public NSHostingView (SwiftHandle viewHandle)
			: this (Init (viewHandle.Pointer, viewHandle.SwiftType.Metadata, viewHandle.SwiftType.GetProtocolConformance (SwiftUILib.ViewProtocol)))
		{
			// release extra ref added by Xamarin runtime
			DangerousRelease ();
		}

		public NSHostingView (IntPtr handle)
			: base (handle)
		{
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_NSHostingView_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
