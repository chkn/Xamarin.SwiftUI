using System;
using System.Runtime.InteropServices;

using Foundation;
using AppKit;

using Swift.Interop;

namespace SwiftUI
{
	public unsafe partial struct Color
	{
		public Color (NSColor color)
		{
			// We can't guarantee the NSColor will have RGBA components so will null for now. Otherwise we need to jump through hoops and trap execptions
			RedComponent = null;
			GreenComponent = null;
			BlueComponent = null;
			OpacityComponent = null;

			ColorSpace = null;

			HueComponent = null;
			SaturationComponent = null;
			BrightnessComponent = null;

			WhiteComponent = null;

			_ = CreateFromNSColor (color.Handle.ToPointer ());
		}

		public Color (string name, NSBundle? bundle = null)
		{
			RedComponent = null;
			GreenComponent = null;
			BlueComponent = null;
			OpacityComponent = null;

			ColorSpace = null;

			HueComponent = null;
			SaturationComponent = null;
			BrightnessComponent = null;

			WhiteComponent = null;

			_ = CreateFromStringBundle (name, bundle == null ? IntPtr.Zero.ToPointer () : bundle.Handle.ToPointer ());  
        }

		#region DllImports
		// Initialisers
		[DllImport(SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorVyACSo7NSColorCcfC")]
		static extern IntPtr CreateFromNSColor (void* colorPointer);


		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_6bundleACSS_So8NSBundleCSgtcfC")]
		static extern IntPtr CreateFromStringBundle (
			[MarshalAs(UnmanagedType.LPWStr)] string str,
			void* bundlePointer);
        #endregion
    }
}
