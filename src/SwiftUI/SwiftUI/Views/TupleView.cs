using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	using static TupleView;

	[SwiftImport (SwiftUILib.Path)]
	public sealed class TupleView<TTuple> : View, ISwiftValue
	// FIXME: Add this constraint back and remove static ctor if this is ever resolved:
	//  https://github.com/dotnet/fsharp/issues/5654#issuecomment-696504156
	//	where TTuple : ITuple
	{
		public TTuple Value { get; }

		public TupleView (TTuple value)
		{
			Value = value;
		}

		static TupleView ()
		{
			if (!typeof (ITuple).IsAssignableFrom (typeof (TTuple)))
				throw new ArgumentException ("Must be tuple type", nameof (TTuple));
		}

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
