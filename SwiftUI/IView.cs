using System;

using SwiftUI.Interop;

namespace SwiftUI
{
	public interface IView<T> : ISwiftValue<T>
		where T : unmanaged, IView<T>
	{
		new ViewType<T> SwiftType { get; }
	}
}
