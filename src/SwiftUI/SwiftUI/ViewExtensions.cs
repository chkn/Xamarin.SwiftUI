using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	public unsafe static class ViewExtensions
	{
		public static ModifiedOpacity<T> Opacity<T> (this T view, double opacity)
			where T : View
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

		public static ModifiedBackground<TView, TBackground> Background<TView, TBackground> (this TView view, TBackground background)
			where TView : View
			where TBackground: View
		{
			var opaqueBackgroundMetadata = SwiftType.Of (typeof (ModifiedBackground<TView, TBackground>))!;
			var result = TaggedPointer.AllocHGlobal (opaqueBackgroundMetadata.NativeDataSize);
			try {
				using (var viewHandle = view.GetSwiftHandle ())
				using (var backgroundHandle = background.GetSwiftHandle ())
				{
					var viewType = viewHandle.SwiftType;
					var backgroundType = backgroundHandle.SwiftType;

					// Note : When passing 2 generic parameters (in this case TView and TBackground) the order is IMPORTANT. The order is Generic1Pointer, Generic2Pointer, Generic1Metadata, Generic2Metadata, Generic1Prototcol, Generic2Prototcol
					ViewBackground (result.Pointer, viewHandle.Pointer, backgroundHandle.Pointer, viewType.Metadata, backgroundType.Metadata, viewType.GetProtocolConformance (SwiftUILib.ViewProtocol), backgroundType.GetProtocolConformance (SwiftUILib.ViewProtocol));

					return new ModifiedBackground<TView, TBackground> (result);
				}
			} catch {
				result.Dispose ();
				throw;
			}
		}

		public static ModifiedView<TView, TViewModifier> Modifier<TView, TViewModifier> (this TView view, TViewModifier viewmodifier)
			where TView : View
			where TViewModifier : ViewModifier<View>
		{
			var opaqueBackgroundMetadata = SwiftType.Of (typeof (ModifiedView<TView, TViewModifier>))!;
			var result = TaggedPointer.AllocHGlobal (opaqueBackgroundMetadata.NativeDataSize);
			try {
				using (var viewHandle = view.GetSwiftHandle ())
				using (var viewModifierHandle = viewmodifier.GetSwiftHandle ()) {
					var viewType = viewHandle.SwiftType;
					var viewModifierType = viewModifierHandle.SwiftType;

					// Note : When passing 2 generic parameters (in this case TView and TViewModifier) the order is IMPORTANT. The order is Generic1Pointer, Generic2Pointer, Generic1Metadata, Generic2Metadata, Generic1Prototcol, Generic2Prototcol
					ViewModifier (result.Pointer, viewHandle.Pointer, viewModifierHandle.Pointer, viewType.Metadata, viewModifierType.Metadata, viewType.GetProtocolConformance (SwiftUILib.ViewProtocol), viewModifierType.GetProtocolConformance (SwiftUILib.ViewProtocol));

					return new ModifiedView<TView, TViewModifier> (result);
				}
			} catch {
				result.Dispose ();
				throw;
			}
		}

			[DllImport (SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_opacity")]
		internal static extern void ViewOpacity (void* resultPointer, void* viewPointer, double opacity, TypeMetadata* viewMetatdata, ProtocolWitnessTable* viewConformance);

		[DllImport (SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_background")]
		internal static extern void ViewBackground (void* resultPointer, void* viewPointer, void* backgroundPointer, TypeMetadata* viewMetatdata, TypeMetadata* backgroundMetatdata, ProtocolWitnessTable* viewConformance, ProtocolWitnessTable* backgroundConformance);

		[DllImport (SwiftGlueLib.Path,
		CallingConvention = CallingConvention.Cdecl,
		EntryPoint = "swiftui_View_modifier")]
		internal static extern void ViewModifier (void* resultPointer, void* viewPointer, void* viewModifierPointer, TypeMetadata* viewMetatdata, TypeMetadata* viewModifierMetatdata, ProtocolWitnessTable* viewConformance, ProtocolWitnessTable* viewModifierConformance);
	}
}