using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static TupleView;

	[SwiftImport (SwiftUILib.Path)]
	public sealed class TupleView<TTuple> : View
		where TTuple : ITuple
	{
		readonly TTuple value;

		public TupleView (TTuple value)
		{
			this.value = value;
		}

		protected override unsafe void InitNativeData (void* handle)
		{
			using (var val = value.GetSwiftHandle ())
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
