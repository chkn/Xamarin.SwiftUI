using System;
using System.Runtime.InteropServices;

using Foundation;
using UIKit;

using Swift.Interop;

namespace SwiftUI
{
	public unsafe partial class Color
	{
		#region Constructors
		public Color (UIColor color)
		{
			Data = CreateFromUIColor(color.Handle.ToPointer());
		}

		public Color (string name, NSBundle? bundle = null)
		{
			Data = CreateFromStringBundle (new Swift.String (name), bundle == null ? IntPtr.Zero.ToPointer() : bundle.Handle.ToPointer());
		}
		#endregion

		#region DllImports
		// Initialisers
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorVyACSo7UIColorCcfC")]
		static extern IntPtr CreateFromUIColor (void* colorPointer);

		// Initialisations
		[DllImport(SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_6bundleACSS_So8NSBundleCSgtcfC")]
		static extern IntPtr CreateFromStringBundle (
			Swift.String str,
			void* bundlePointer);
		#endregion
	}
}
