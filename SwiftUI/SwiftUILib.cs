using System;
using System.Runtime.InteropServices;

using SwiftUI.Interop;

namespace SwiftUI
{
	public class SwiftUILib : NativeLib
	{
		public const string Path = "/System/Library/Frameworks/SwiftUI.framework/SwiftUI";

		public static SwiftUILib Types { get; } = new SwiftUILib ();

		SwiftUILib () : base (Path)
		{
		}

		#region Types

		ViewType<Text> _text;
		public ViewType<Text> Text => _text ?? (_text = new ViewType<Text> (this, "Text"));

		#endregion
	}
}
