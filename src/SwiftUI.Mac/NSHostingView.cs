using System;
using System.Runtime.InteropServices;

using AppKit;
using Foundation;

using Swift.Interop;

namespace SwiftUI
{
	public unsafe class NSHostingView : NSView, /*INSUserInterfaceValidations,*/ INSDraggingSource
	{

		public static NSHostingView Create (IView view)
		{
			var swiftType = view.SwiftType;
			using var handle = view.Handle;
			return new NSHostingView (handle.Pointer, swiftType.Metadata, swiftType.ViewConformance);
		}

		public static NSHostingView Create<T> (in T view)
			where T : unmanaged, IView<T>
		{
			fixed (T* viewPtr = &view)
				return Create (viewPtr);
		}

		public static NSHostingView Create<T> (T* view)
			where T : unmanaged, IView<T>
		{
			var swiftType = view->SwiftType;
			return new NSHostingView (view, swiftType.Metadata, swiftType.ViewConformance);
		}

		protected unsafe NSHostingView (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance)
			: base (Init (viewData, viewType, viewConformance))
		{
		}

		[DllImport ("libSwiftUIGlue.dylib",
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_NSHostingView_rootView")]
		static extern IntPtr Init (void* viewData, TypeMetadata* viewType, ProtocolWitnessTable* viewConformance);
	}
}
