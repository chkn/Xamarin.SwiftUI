using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static VStack;

	[SwiftImport (SwiftUILib.Path)]
	public sealed record VStack<TContent>(HorizontalAlignment Alignment, double? Spacing, TContent Content) : View where TContent : View
	{
		public VStack (TContent content)
			// FIXME: Call the functions to get the default values from SwiftUI
			: this (HorizontalAlignment.Center, null, content)
		{
		}

		public VStack (HorizontalAlignment alignment, TContent content)
			// FIXME: Call the functions to get the default values from SwiftUI
			: this (alignment, null, content)
		{
		}

		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			using (var ctnt = Content.GetSwiftHandle (nullability [0])) {
				var cty = ctnt.SwiftType;
				Init (handle, Alignment, Spacing.HasValue, Spacing ?? 0, ctnt.Pointer, cty.Metadata, cty.GetProtocolConformance (SwiftUILib.ViewProtocol));
			}
		}
	}

	unsafe static class VStack
	{
		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_VStack_align_spacing_content")]
		internal static extern void Init (void* result, HorizontalAlignment align, bool spacingHasValue, double spacing, void* contentData, TypeMetadata* contentType, ProtocolWitnessTable* contentViewConformance);
	}
}
