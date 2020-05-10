using System;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public class EmptyView : View
	{
		protected override unsafe void InitNativeData (void* handle)
		{
		}
	}
}
