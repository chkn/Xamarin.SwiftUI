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
		where TTuple : ITuple
	{
		readonly TTuple value;

		SwiftType? swiftType;
		Nullability valueNullability;

		protected override SwiftType SwiftType => swiftType ??= base.SwiftType;

		public TupleView (TTuple value)
		{
			this.value = value;
		}

		protected override unsafe void InitNativeData (void* handle)
		{
			using (var val = value.GetSwiftHandle (valueNullability))
				Init (handle, val.Pointer, val.SwiftType.Metadata);
		}

		void ISwiftValue.SetSwiftType (SwiftType swiftType, Nullability nullability)
		{
			this.swiftType = swiftType;
			this.valueNullability = nullability [0];
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
