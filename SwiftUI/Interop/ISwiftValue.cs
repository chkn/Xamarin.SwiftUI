using System;

namespace SwiftUI.Interop
{
	public interface ISwiftValue<T> : IDisposable
		where T : unmanaged, ISwiftValue<T>
	{
		SwiftType<T> SwiftType { get; }
		T Copy ();
	}
}
