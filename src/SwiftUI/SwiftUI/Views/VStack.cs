using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static VStack;

	[SwiftImport (SwiftUILib.Path)]
	public sealed class VStack<TContent> : View where TContent : View
	{
		readonly HorizontalAlignment alignment;
		readonly double? spacing;
		readonly TContent content;

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

		public VStack (HorizontalAlignment alignment, double? spacing, TContent content)
		{
			this.alignment = alignment;
			this.spacing = spacing;
			this.content = content;
		}

		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			using (var ctnt = content.GetSwiftHandle (nullability [0])) {
				var cty = ctnt.SwiftType;
				Init (handle, alignment, spacing.HasValue, spacing ?? 0, ctnt.Pointer, cty.Metadata, cty.GetProtocolConformance (SwiftUILib.ViewProtocol));
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
