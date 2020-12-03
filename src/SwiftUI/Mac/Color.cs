using System;
using System.Runtime.InteropServices;

using Foundation;
using AppKit;

namespace SwiftUI
{
	public unsafe partial record Color
	{
		#region Constructors
		public Color (NSColor color)
		{
			opaqueData = CreateFromNSColor (color.Handle);
		}

		public Color (string name, NSBundle? bundle = null)
		{
			using (var swiftName = new Swift.String (name))
				opaqueData = CreateFromStringBundle (swiftName, bundle?.Handle ?? IntPtr.Zero);
		}
		#endregion

		#region DllImports
		// Initialisers
		[DllImport(SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorVyACSo7NSColorCcfC")]
		static extern IntPtr CreateFromNSColor (IntPtr colorPointer);


		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_6bundleACSS_So8NSBundleCSgtcfC")]
		static extern IntPtr CreateFromStringBundle (
			Swift.String str,
			IntPtr bundlePointer);
        #endregion
    }
}
