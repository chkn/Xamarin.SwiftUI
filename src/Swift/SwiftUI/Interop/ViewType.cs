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

		/// <summary>
		/// Not to be used except by <see cref="CustomViewType"/>
		/// </summary>
		internal ViewType (FullTypeMetadata* fullMetadata)
			: base (fullMetadata)
		{
		}

		public ViewType (IntPtr typeMetadata, Type? managedType = null)
			: base (typeMetadata, managedType)
		{
		}

		public ViewType (NativeLib lib, string name, Type? managedType = null)
			: this (lib, "SwiftUI", name, managedType)
		{
		}

		public ViewType (NativeLib lib, string module, string name, Type? managedType = null)
			: base (lib, module, name, SwiftTypeCode.Struct, managedType)
		{
		}

		public ViewType (NativeLib lib, Type managedType)
			: base (lib, managedType.Namespace, GetSwiftTypeName (managedType), SwiftTypeCode.Struct, managedType)
		{
		}

		/// <summary>
		/// Returns the <see cref="ViewType"/> of the given <see cref="Type"/>.
		/// </summary>
		/// <remarks>
		/// By convention, types that are exposed to Swift must have a public static SwiftType property.
		/// </remarks>
		public new static ViewType? Of (Type type) => SwiftType.Of (type) as ViewType;
	}
}
