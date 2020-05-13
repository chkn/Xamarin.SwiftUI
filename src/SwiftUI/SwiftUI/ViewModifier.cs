using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public abstract unsafe class ViewModifier<T> : View
	{
		public abstract T Body (Content content);
	}

	[SwiftImport (SwiftUILib.Path)]
	public unsafe class Content : View
	{

	}
}