using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public unsafe sealed class Text : View
	{
		public string Verbatim { get; }

		public Text (string verbatim)
		{
			Verbatim = verbatim;
		}

		protected override void InitNativeData (void* handle, Nullability nullability)
		{
			using var str = new Swift.String (Verbatim);
			Init (handle, str);
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Text_verbatim")]
		static extern void Init (void* result, Swift.String verbatim);
	}
}