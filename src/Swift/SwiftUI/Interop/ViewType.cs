using System;

using Swift.Interop;

namespace SwiftUI.Interop
{
	/// <summary>
	/// A type that conforms to <c>SwiftUI.View</c>
	/// </summary>
	public unsafe class ViewType : SwiftType
	{
		ProtocolWitnessTable* _viewConformance;
		public ProtocolWitnessTable* ViewConformance
			=> _viewConformance == null ? (_viewConformance = CreateViewConformance ()) : _viewConformance;

		public override ProtocolWitnessTable* GetProtocolConformance (ProtocolDescriptor* descriptor)
			=> descriptor == SwiftUILib.Types.View ? ViewConformance : base.GetProtocolConformance (descriptor);

		protected virtual ProtocolWitnessTable* CreateViewConformance ()
			=> base.GetProtocolConformance (SwiftUILib.Types.View);

		public ViewType (NativeLib lib, string name, Type? managedType = null)
			: this (lib, "SwiftUI", name, managedType)
		{
		}

		public ViewType (NativeLib lib, string module, string name, Type? managedType = null)
			: base (lib, module, name, managedType)
		{
		}

		unsafe private protected ViewType (FullTypeMetadata* fullMetadata)
			: base (fullMetadata)
		{
		}
	}
}
