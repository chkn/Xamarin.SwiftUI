using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	public unsafe static class ViewExtensions
	{
		public static ModifiedOpacity<T> Opacity<T> (this T view, double opacity) where T : View
		{
			var opaqueOpacityMetadata = ModifiedOpacity<T>.SwiftType;
			var resultPointer = Marshal.AllocHGlobal (opaqueOpacityMetadata.NativeDataSize);
			try
			{
				using (var viewHandle = view.GetHandle ())
				{
					ViewOpacity (resultPointer.ToPointer (), viewHandle.Pointer, opacity, view.ViewType.Metadata, view.ViewType.GetProtocolConformance (SwiftUILib.Types.View));

					return new ModifiedOpacity<T> (new TaggedPointer(resultPointer, true), opaqueOpacityMetadata);
				}
			}
			catch
			{
				Marshal.FreeHGlobal (resultPointer);
				throw;
			}
		}

		public static ModifiedBackground<TView, TBackground> Background<TView, TBackground> (this TView view, TBackground background)
			where TView : View
			where TBackground: View
		{
			var opaqueBackgroundMetadata = ModifiedBackground<TView, TBackground>.SwiftType;
			var resultPointer = Marshal.AllocHGlobal (opaqueBackgroundMetadata.NativeDataSize);
			try {
				using (var viewHandle = view.GetHandle ())
				using (var backgroundHandle = background.GetHandle ())
				{
					ViewBackground (resultPointer.ToPointer (), viewHandle.Pointer, backgroundHandle.Pointer, view.ViewType.Metadata, view.ViewType.GetProtocolConformance (SwiftUILib.Types.View));

					return new ModifiedBackground<TView, TBackground> (new TaggedPointer (resultPointer, true), opaqueBackgroundMetadata);
				}
			} catch {
				Marshal.FreeHGlobal (resultPointer);
				throw;
			}
		}

		[DllImport(SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_opacity")]
		internal static extern void ViewOpacity (void* resultPointer, void* viewPointer, double opacity, TypeMetadata* viewMetatdata, ProtocolWitnessTable* viewConformance);

		[DllImport(SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_background")]
		internal static extern void ViewBackground (void* resultPointer, void* viewPointer, void* backgroundPointer, TypeMetadata* viewMetatdata, ProtocolWitnessTable* viewConformance);
	}
}