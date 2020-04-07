using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	public unsafe static class ViewExtensions
	{
		public static ModifiedOpacity<T> Opacity <T> (this T view, double opacity) where T : View
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

		[DllImport(SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_opacity")]
		internal static extern void ViewOpacity (void* resultPointer, void* viewPointer, double opacity, TypeMetadata* viewMetatdata, ProtocolWitnessTable* viewConformance);
	}
}
