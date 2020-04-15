﻿using System;
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
	

	[StructLayout (LayoutKind.Sequential, Size = 8)]
	public unsafe partial struct Color : ISwiftBlittableStruct<Color>, IDisposable
	{
		public static SwiftType SwiftType => SwiftUILib.Types.Color;
		SwiftType ISwiftValue.SwiftType => SwiftUILib.Types.Color;

		public static Color Empty => default;

		public Color Copy () => SwiftType.Transfer (in this, TransferFuncType.InitWithCopy);

		public void Dispose () => SwiftType.Destroy (in this);

		public IntPtr Data { get; private set; }

		#region Static Colour Spaces
		public static IntPtr ColorSpaceDisplayP3 => GetColorSpaceDisplayP3 (0, RGBColorSpaceMetadata ());
		public static IntPtr ColorSpaceRGB => GetColorSpaceRGB ();
		public static IntPtr ColorSpaceRGBLinear => GetColorSpaceRGBLinear ();
		#endregion

		#region Static Colours
		// TODO: Most of these should be cached
		public static Color Black => new Color (GetColorBlack (0, SwiftType.Metadata));
		public static Color Blue => new Color (GetColorBlue (0, SwiftType.Metadata));
		public static Color Clear => new Color (GetColorClear (0, SwiftType.Metadata));
		public static Color Gray => new Color (GetColorGray (0, SwiftType.Metadata));
		public static Color Green => new Color (GetColorGreen (0, SwiftType.Metadata));
		public static Color Orange => new Color (GetColorOrange (0, SwiftType.Metadata));
		public static Color Pink => new Color (GetColorPink (0, SwiftType.Metadata));
		public static Color Primary => new Color (GetColorPrimary (0, SwiftType.Metadata));
		public static Color Purple => new Color (GetColorPurple (0, SwiftType.Metadata));
		public static Color Red => new Color (GetColorRed (0, SwiftType.Metadata));
		public static Color Secondary => new Color (GetColorSecondary (0, SwiftType.Metadata));
		public static Color White => new Color (GetColorWhite (0, SwiftType.Metadata));
		public static Color Yellow => new Color (GetColorYellow (0, SwiftType.Metadata));
		#endregion

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

		// FIXME: Remove when this is fixed: https://github.com/mono/mono/issues/17869
		MemoryHandle ISwiftValue.GetHandle ()
		{
			var gch = GCHandle.Alloc (this, GCHandleType.Pinned);
			return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
		}

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
		static extern IntPtr GetColorSpaceDisplayP3 (long metadataReq, IntPtr colorSpaceMetadata);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO4sRGByA2EmFWC")]
		static extern IntPtr GetColorSpaceRGB ();

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceO10sRGBLinearyA2EmFWC")]
		static extern IntPtr GetColorSpaceRGBLinear ();
		#endregion

		#region Static Colours
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5blackACvgZ")]
		static extern IntPtr GetColorBlack (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5blueACvgZ")]
		static extern IntPtr GetColorBlue (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5clearACvgZ")]
		static extern IntPtr GetColorClear (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4grayACvgZ")]
		static extern IntPtr GetColorGray (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5greenACvgZ")]
		static extern IntPtr GetColorGreen (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6orangeACvgZ")]
		static extern IntPtr GetColorOrange (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV4pinkACvgZ")]
		static extern IntPtr GetColorPink (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV7primaryACvgZ")]
		static extern IntPtr GetColorPrimary (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV6purpleACvgZ")]
		static extern IntPtr GetColorPurple (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3redACvgZ")]
		static extern IntPtr GetColorRed (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV9secondaryACvgZ")]
		static extern IntPtr GetColorSecondary (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5whiteACvgZ")]
		static extern IntPtr GetColorWhite (long metadataReq, TypeMetadata* valueType);

		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV5yellowACvgZ")]
		static extern IntPtr GetColorYellow (long metadataReq, TypeMetadata* valueType);
		#endregion

		#region RGBColorSpace
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV13RGBColorSpaceOMa")]
		static extern IntPtr RGBColorSpaceMetadata ();
		#endregion

		#endregion
	}
}
