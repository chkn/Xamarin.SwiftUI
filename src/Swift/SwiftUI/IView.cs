using System;

using Swift.Interop;
using SwiftUI.Interop;

namespace SwiftUI
{
	public interface IView : ISwiftValue
	{
		new ViewType SwiftType { get; }
		SwiftType ISwiftValue.SwiftType => SwiftType;
	}

	public interface IBlittableView<T> : IView, ISwiftBlittableStruct<T>
		where T : unmanaged, IBlittableView<T>
	{
	}
}
