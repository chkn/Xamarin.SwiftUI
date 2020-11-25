using System;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public sealed record EmptyView : View
	{
		protected override unsafe void InitNativeData (void* handle, Nullability nullability)
		{
		}
	}
}
