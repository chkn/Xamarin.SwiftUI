using System;
using System.Buffers;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[StructLayout(LayoutKind.Sequential, Size=8)]
	public unsafe struct Color : ISwiftBlittableStruct<Color>, IDisposable
	{
		public static SwiftType SwiftType => SwiftUILib.Types.Color;
		SwiftType ISwiftValue.SwiftType => SwiftUILib.Types.Color;

		public static Color Empty => default;

		public Color Copy () => SwiftType.Transfer (in this, TransferFuncType.InitWithCopy);

		public void Dispose () => SwiftType.Destroy (in this);

		#region Static Colours
		public static Color Black => new Color();
		public static Color Blue => new Color();
		public static Color Clear => new Color();
		public static Color Gray => new Color();
		public static Color Green => new Color();
		public static Color Orange => new Color();
		public static Color Pink => new Color();
		public static Color Primary => new Color();
		public static Color Purple => new Color();
		public static Color Red => new Color();
		public static Color Secondary => new Color();
		public static Color White => new Color();
		public static Color Yellow => new Color();
		#endregion

		// FIXME: Remove when this is fixed: https://github.com/mono/mono/issues/17869
		MemoryHandle ISwiftValue.GetHandle()
		{
			var gch = GCHandle.Alloc (this, GCHandleType.Pinned);
			return new MemoryHandle ((void*)gch.AddrOfPinnedObject(), gch);
		}
	}
}
