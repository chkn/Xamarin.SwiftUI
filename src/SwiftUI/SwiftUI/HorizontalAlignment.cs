using System;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	[StructLayout (LayoutKind.Sequential)]
	public readonly struct HorizontalAlignment : ISwiftBlittableStruct<HorizontalAlignment>
	{
		readonly IntPtr opaqueData;

		public static extern HorizontalAlignment Leading {
			[DllImport (SwiftUILib.Path,
				CallingConvention = CallingConvention.Cdecl,
				EntryPoint = "$s7SwiftUI19HorizontalAlignmentV7leadingACvgZ")]
			get;
		}

		public static extern HorizontalAlignment Center {
			[DllImport (SwiftUILib.Path,
				CallingConvention = CallingConvention.Cdecl,
				EntryPoint = "$s7SwiftUI19HorizontalAlignmentV6centerACvgZ")]
			get;
		}

		public static extern HorizontalAlignment Trailing {
			[DllImport (SwiftUILib.Path,
				CallingConvention = CallingConvention.Cdecl,
				EntryPoint = "$s7SwiftUI19HorizontalAlignmentV8trailingACvgZ")]
			get;
		}
	}
}
