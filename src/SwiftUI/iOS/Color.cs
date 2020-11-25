using System;
using System.Runtime.InteropServices;

using Foundation;
using UIKit;

using Swift.Interop;

namespace SwiftUI
{
	public unsafe partial record Color
	{
		#region Constructors
		public Color (UIColor color)
		{
			opaqueData = CreateFromUIColor (color.Handle);
		}

		public Color (string name, NSBundle? bundle = null)
		{
			using (var swiftName = new Swift.String (name))
				opaqueData = CreateFromStringBundle (swiftName, bundle?.Handle ?? IntPtr.Zero);
		}
		#endregion

		#region DllImports
		// Initialisers
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorVyACSo7UIColorCcfC")]
		static extern IntPtr CreateFromUIColor (IntPtr colorPointer);

		// Initialisations
		[DllImport(SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_6bundleACSS_So8NSBundleCSgtcfC")]
		static extern IntPtr CreateFromStringBundle (
			Swift.String str,
			IntPtr bundlePointer);
		#endregion
	}
}
