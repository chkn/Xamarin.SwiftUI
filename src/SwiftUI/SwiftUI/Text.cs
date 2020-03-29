using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	public unsafe sealed class Text : View
	{
		public static SwiftType SwiftType => SwiftUILib.Types.Text;
		public override SwiftType ViewType => SwiftUILib.Types.Text;

		string verbatim;

		public Text (string verbatim)
		{
			this.verbatim = verbatim;
		}

		protected override void InitNativeData (void* handle)
		{
			using (var str = new Swift.String (verbatim))
				Init (handle, str);
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_Text_verbatim")]
		static extern void Init (void* result, Swift.String verbatim);
	}
}