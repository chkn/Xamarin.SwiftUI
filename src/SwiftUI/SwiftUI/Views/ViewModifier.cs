using System;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public abstract unsafe record ViewModifier<T> : View
	{
		public abstract T Body (Content content);
	}
}