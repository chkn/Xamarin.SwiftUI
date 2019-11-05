using System;

using Swift.Interop;

namespace SwiftUI.Interop
{
	/// <summary>
	/// A type that conforms to <c>SwiftUI.View</c>
	/// </summary>
	public unsafe class ViewType : SwiftType
	{
		public ProtocolWitnessTable* ViewConformance {
			get {
				if (_viewConformance == null)
					_viewConformance = base.GetProtocolConformance (SwiftUILib.Types.View);
				return _viewConformance;
			}
			protected set {
				_viewConformance = value;
			}
		}
		ProtocolWitnessTable* _viewConformance;

		public override ProtocolWitnessTable* GetProtocolConformance (IntPtr descriptor)
		{
			if (descriptor == SwiftUILib.Types.View)
				return ViewConformance;

			return base.GetProtocolConformance (descriptor);
		}

		public ViewType (NativeLib lib, string name, Type? managedType = null)
			: this (lib, "SwiftUI", name, managedType)
		{
		}

		public ViewType (NativeLib lib, string module, string name, Type? managedType = null)
			: base (lib, module, name, managedType)
		{
		}

		unsafe private protected ViewType (TypeMetadata* metadata) : base (metadata)
		{
		}
	}
}
