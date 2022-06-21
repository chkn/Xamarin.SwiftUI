using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static TupleView;

	[SwiftImport (SwiftUILib.Path)]
	public sealed record TupleView<TTuple>(TTuple Value) : View
		where TTuple : ITuple
	{
		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
			using (var val = Value.GetSwiftHandle (nullability [0]))
				Init (handle, val.Pointer, val.SwiftType.Metadata);
		}
	}

	unsafe static class TupleView
	{
		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_TupleView_value")]
		internal static extern void Init (void* result, void* valuePtr, TypeMetadata* valueMetadata);
	}
}
