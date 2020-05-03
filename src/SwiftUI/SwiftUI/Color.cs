using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace SwiftUI
{
	using static Color;

	public enum RGBColorSpace
	{
		sRGB,
		DisplayP3,
		sRGBLinear,
	};

	[SwiftImport (SwiftUILib.Path)]
	public unsafe partial class Color : View
	{
		internal IntPtr Data { get; private set; }

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
		static Color? black = null;
		public static Color Black => black ??= new Color (GetColorBlack ());

		static Color? blue = null;
		public static Color Blue => blue ??= new Color (GetColorBlue ());

		public static Color Clear => new Color (GetColorClear ());

		static Color? gray = null;
		public static Color Gray => gray ??= new Color (GetColorGray ());

		static Color? green = null;
		public static Color Green => green ??= new Color (GetColorGreen ());

		static Color? orange = null;
		public static Color Orange => orange ??= new Color (GetColorOrange ());

		static Color? pink = null;
		public static Color Pink => pink ??= new Color (GetColorPink ());

		public static Color Primary => new Color (GetColorPrimary ());

		static Color? purple = null;
		public static Color Purple => purple ??= new Color (GetColorPurple ());

		static Color? red = null;
		public static Color Red => red ??= new Color (GetColorRed ());

		public static Color Secondary => new Color (GetColorSecondary ());

		static Color? white = null;
		public static Color White => white ??= new Color (GetColorWhite ());

		static Color? yellow = null;
		public static Color Yellow => yellow ??= new Color (GetColorYellow ());
		#endregion

		protected override void InitNativeData (void* handle)
		{
			// Using TransferFuncType.InitWithTake here in case we leak.
			using (var dataHandle = Data.GetSwiftHandle ())
				SwiftType.Transfer (handle, dataHandle.Pointer, TransferFuncType.InitWithTake);
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
