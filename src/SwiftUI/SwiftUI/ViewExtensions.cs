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
			var opaqueOpacityMetadata = SwiftType.Of (typeof (ModifiedOpacity<T>))!;
			var result = TaggedPointer.AllocHGlobal (opaqueOpacityMetadata.NativeDataSize);
			try {
				using (var viewHandle = view.GetSwiftHandle ()) {
					var viewType = viewHandle.SwiftType;
					ViewOpacity (result.Pointer, viewHandle.Pointer, opacity, viewType.Metadata, viewType.GetProtocolConformance (SwiftUILib.ViewProtocol));
					return new ModifiedOpacity<T> (result);
				}
			} catch {
				result.Dispose ();
				throw;
			}
		}

		public static ModifiedOpacity<T> Background<T> (this T view, Color color) where T : View
		{
			var opaqueOpacityMetadata = ModifiedOpacity<T>.SwiftType;
			var resultPointer = Marshal.AllocHGlobal (opaqueOpacityMetadata.NativeDataSize);
			try
			{
				using (var viewHandle = view.GetHandle())
				{
					ViewBackground (resultPointer.ToPointer(), viewHandle.Pointer, color, view.ViewType.Metadata, view.ViewType.GetProtocolConformance (SwiftUILib.Types.View));

					return new ModifiedOpacity<T >(new TaggedPointer (resultPointer, true), opaqueOpacityMetadata);
				}
			}
			catch
			{
				Marshal.FreeHGlobal(resultPointer);
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
		internal static extern void ViewBackground (void* resultPointer, void* viewPointer, Color opacity, TypeMetadata* viewMetatdata, ProtocolWitnessTable* viewConformance);
	}
}
