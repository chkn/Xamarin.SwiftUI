using System;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public interface IView : ISwiftValue
	{
		new ViewType SwiftType { get; }
	}

	public interface IView<T> : IView, ISwiftValue<T>
		where T : unmanaged, IView<T>
	{
	}
}
