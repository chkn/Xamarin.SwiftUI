using System;

using SwiftUI.Interop;

namespace SwiftUI
{
	/// <summary>
	/// A type that conforms to <c>SwiftUI.View</c>
	/// </summary>
	public class ViewType<T> : SwiftType<T> where T : unmanaged, IView<T>
	{
		public IntPtr ViewConformance {
			get {
				if (_viewConformance == IntPtr.Zero)
					_viewConformance = GetProtocolConformance ("SwiftUI", "View");
				return _viewConformance;
			}
		}
		IntPtr _viewConformance;

		public ViewType (NativeLib lib, string name)
			: this (lib, "SwiftUI", name)
		{
		}

		public ViewType (NativeLib lib, string module, string name)
			: base (lib, module, name)
		{
		}
	}
}
