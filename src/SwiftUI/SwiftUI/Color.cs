using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace SwiftUI
{
	using static Color;

	public enum RGBColorSpace
	{
		DisplayP3,
		RGB,
		RGBLinear,
	};

	[SwiftImport (SwiftUILib.Path)]
	public unsafe partial class Color : View
	{
		public IntPtr Data { get; private set; }

		#region Static Colour Spaces
		public static IntPtr ColorSpaceDisplayP3 {
			get {
				var fullMeta = RGBColorSpaceMetadata (0);
				var meta = &fullMeta->Metadata;
				return GetColorSpaceDisplayP3 (meta);
			}
		}

		public static IntPtr ColorSpaceRGB => GetColorSpaceRGB ();
		public static IntPtr ColorSpaceRGBLinear => GetColorSpaceRGBLinear ();
		#endregion

		#region Static Colours
		// TODO: Most of these should be cached
		public static Color Black => new Color (GetColorBlack (0));
		public static Color Blue => new Color (GetColorBlue (0));
		public static Color Clear => new Color (GetColorClear (0));
		public static Color Gray => new Color (GetColorGray (0));
		public static Color Green => new Color (GetColorGreen (0));
		public static Color Orange => new Color (GetColorOrange (0));
		public static Color Pink => new Color (GetColorPink (0));
		public static Color Primary => new Color (GetColorPrimary (0));
		public static Color Purple => new Color (GetColorPurple (0));
		public static Color Red => new Color (GetColorRed (0));
		public static Color Secondary => new Color (GetColorSecondary (0));
		public static Color White => new Color (GetColorWhite (0));
		public static Color Yellow => new Color (GetColorYellow (0));
		#endregion

		protected override void InitNativeData (void* handle)
		{
		}

		#region Constructors
		internal Color (IntPtr data)
		{
			Data = data;
		}

		public Color (RGBColorSpace colorSpace, double red, double green, double blue, double opacity)
		{
			Data = CreateFromRGBColorSpaceRedGreenBlueOpacity ((ulong)colorSpace, red, green, blue, opacity);
		}

		public Color (RGBColorSpace colorSpace, double white, double opacity)
		{
			Data = CreateFromRGBColorSpaceWhiteOpacity ((ulong)colorSpace, white, opacity);
        }

        public Color (double hue, double saturation, double brightness, double opacity)
		{
			Data = CreateFromHSBO (hue, saturation, brightness, opacity);
		}
		#endregion

		#region DllImports

		#region Initialisers
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_3red5green4blue7opacityA2C13RGBColorSpaceO_S4dtcfC")]
		static extern IntPtr CreateFromRGBColorSpaceRedGreenBlueOpacity (
			ulong colourSpace,
			double red,
			double green,
			double blue,
			double opacity);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV_5white7opacityA2C13RGBColorSpaceO_S2dtcfC")]
		static extern IntPtr CreateFromRGBColorSpaceWhiteOpacity (
			ulong colourSpace,
			double white,
			double opacity);


		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC")]
		static extern IntPtr CreateFromHSBO (
			double hue,
			double saturation,
			double brightness,
			double opacity);
		#endregion

		#region RGBColorSpace
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO9displayP3yA2EmFWC")]
		static extern IntPtr GetColorSpaceDisplayP3 (TypeMetadata* colorSpaceMetadata);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO4sRGByA2EmFWC")]
		static extern IntPtr GetColorSpaceRGB ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO10sRGBLinearyA2EmFWC")]
		static extern IntPtr GetColorSpaceRGBLinear ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceOMa")]
		static extern FullTypeMetadata* RGBColorSpaceMetadata (long metadataReq);
		#endregion

		#region Static Colours
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5blackACvgZ")]
		static extern IntPtr GetColorBlack (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4blueACvgZ")]
		static extern IntPtr GetColorBlue (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5clearACvgZ")]
		static extern IntPtr GetColorClear (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4grayACvgZ")]
		static extern IntPtr GetColorGray (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5greenACvgZ")]
		static extern IntPtr GetColorGreen (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6orangeACvgZ")]
		static extern IntPtr GetColorOrange (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4pinkACvgZ")]
		static extern IntPtr GetColorPink (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV7primaryACvgZ")]
		static extern IntPtr GetColorPrimary (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6purpleACvgZ")]
		static extern IntPtr GetColorPurple (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3redACvgZ")]
		static extern IntPtr GetColorRed (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV9secondaryACvgZ")]
		static extern IntPtr GetColorSecondary (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5whiteACvgZ")]
		static extern IntPtr GetColorWhite (long metadataReq);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6yellowACvgZ")]
		static extern IntPtr GetColorYellow (long metadataReq);
		#endregion

		#endregion
	}
}
