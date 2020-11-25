using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public enum RGBColorSpace
	{
		sRGB,
		DisplayP3,
		sRGBLinear,
	}

	[SwiftImport (SwiftUILib.Path)]
	public unsafe partial record Color : View
	{
		// not a pointer; the actual Color data
		readonly IntPtr opaqueData;

		#region Static Colours
		public static Color Black => new Color (GetColorBlack ());

		public static Color Blue => new Color (GetColorBlue ());

		public static Color Clear => new Color (GetColorClear ());

		public static Color Gray =>  new Color (GetColorGray ());

		public static Color Green => new Color (GetColorGreen ());

		public static Color Orange => new Color (GetColorOrange ());

		public static Color Pink => new Color (GetColorPink ());

		public static Color Primary => new Color (GetColorPrimary ());

		public static Color Purple => new Color (GetColorPurple ());

		public static Color Red => new Color (GetColorRed ());

		public static Color Secondary => new Color (GetColorSecondary ());

		public static Color White = new Color (GetColorWhite ());

		public static Color Yellow => new Color (GetColorYellow ());
		#endregion

		protected override void InitNativeData (void* handle, Nullability nullability)
		{
			IntPtr* dest = (IntPtr*)handle;
			*dest = opaqueData;
		}

		#region Constructors
		internal Color (IntPtr data)
		{
			opaqueData = data;
		}

		public Color (double hue, double saturation, double brightness, double opacity)
		{
			opaqueData = CreateFromHSBO (hue, saturation, brightness, opacity);
		}

		public Color (RGBColorSpace colorSpace, double red, double green, double blue, double opacity)
		{
			var opaqueRBGColorspaceMetadata = SwiftType.Of (typeof (RGBColorSpace))!;
			var result = Marshal.AllocHGlobal (opaqueRBGColorspaceMetadata.NativeDataSize);
			try {
				GetSwiftUIColorSpace (colorSpace, result.ToPointer ());
				opaqueData = CreateFromRGBColorSpaceRedGreenBlueOpacity (result.ToPointer (), red, green, blue, opacity);
			} catch {
				Marshal.FreeHGlobal (result);
				throw;
			}
		}

		public Color (RGBColorSpace colorSpace, double white, double opacity)
		{
			var opaqueRBGColorspaceMetadata = SwiftType.Of (typeof (RGBColorSpace))!;
			var result = Marshal.AllocHGlobal (opaqueRBGColorspaceMetadata.NativeDataSize);
			try {
				GetSwiftUIColorSpace (colorSpace, result.ToPointer ());
				opaqueData = CreateFromRGBColorSpaceWhiteOpacity (result.ToPointer (), white, opacity); ;
			} catch {
				Marshal.FreeHGlobal (result);
				throw;
			}
		}

		static void GetSwiftUIColorSpace (RGBColorSpace colorSpace, void* result)
		{
			switch (colorSpace) {
				case RGBColorSpace.sRGB:
					GetRGBColorSpacesRGB (result);
					break;
				case RGBColorSpace.DisplayP3:
					GetRGBColorSpaceDisplayP3 (result);
					break;
				case RGBColorSpace.sRGBLinear:
					GetRGBColorSpacesRGBLinear (result);
					break;
				default:
					throw new NotSupportedException ();
			}
		}
		#endregion

		#region DllImports

		#region Initialisers
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC")]
		static extern IntPtr CreateFromHSBO (
			double hue,
			double saturation,
			double brightness,
			double opacity);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_3red5green4blue7opacityA2C13RGBColorSpaceO_S4dtcfC")]
		static extern IntPtr CreateFromRGBColorSpaceRedGreenBlueOpacity (
			void* swiftUIColourSpace,
			double red,
			double green,
			double blue,
			double opacity);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_5white7opacityA2C13RGBColorSpaceO_S2dtcfC")]
		static extern IntPtr CreateFromRGBColorSpaceWhiteOpacity (
			void* swiftUIColourSpace,
			double white,
			double opacity);
		#endregion

		#region RGBColorSpace
		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			// TODO Use Direct PInkoke rather than Glue call EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO9displayP3yA2EmFWC")]
			EntryPoint = "swiftui_RGBColorSpace_displayP3")]
		static extern void GetRGBColorSpaceDisplayP3 (void* resultPointer);

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_RGBColorSpace_sRGB")]
		static extern void GetRGBColorSpacesRGB (void* resultPointer);

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_RGBColorSpace_sRGBLinear")]
		static extern void GetRGBColorSpacesRGBLinear (void* resultPointer);
		#endregion

		#region Static Colours
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5blackACvgZ")]
		static extern IntPtr GetColorBlack ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4blueACvgZ")]
		static extern IntPtr GetColorBlue ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5clearACvgZ")]
		static extern IntPtr GetColorClear ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4grayACvgZ")]
		static extern IntPtr GetColorGray ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5greenACvgZ")]
		static extern IntPtr GetColorGreen ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6orangeACvgZ")]
		static extern IntPtr GetColorOrange ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4pinkACvgZ")]
		static extern IntPtr GetColorPink ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV7primaryACvgZ")]
		static extern IntPtr GetColorPrimary ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6purpleACvgZ")]
		static extern IntPtr GetColorPurple ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3redACvgZ")]
		static extern IntPtr GetColorRed ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV9secondaryACvgZ")]
		static extern IntPtr GetColorSecondary ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5whiteACvgZ")]
		static extern IntPtr GetColorWhite ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6yellowACvgZ")]
		static extern IntPtr GetColorYellow ();
		#endregion

		#endregion
	}
}