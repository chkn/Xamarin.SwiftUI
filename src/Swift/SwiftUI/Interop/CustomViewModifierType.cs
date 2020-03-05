using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift;
using Swift.Interop;

namespace SwiftUI.Interop
{
	// HACK: Allocate 2 extra pointers after metadata for SwiftUIView.ThunkViewModifier generic metadata:
	//   type metadata for T
	//   View conformance for T (unused)
	// This works because View has no generic metadata of its own.
	[StructLayout(LayoutKind.Sequential)]
	unsafe struct CustomViewModifierMetadata
	{
		public FullTypeMetadata Metadata;

		public const uint AdditionalPointers = 3;
		public TypeMetadata* ThunkViewModifierU;
		public TypeMetadata* ThunkViewModifierT;
		public ProtocolWitnessTable* ThunkViewTViewConformance;
	}

	unsafe class CustomViewModifierType : ManagedSwiftType
	{
		ViewModifierProtocolConformanceDescriptor* viewModifierConformanceDesc;
		ProtocolWitnessTable* viewConformance;

		public override int NativeFieldsOffset => sizeof(CustomViewModifierData);

		public override uint AdditionalMetadataPointers => CustomViewModifierMetadata.AdditionalPointers;

		public ProtocolWitnessTable* ViewConformance
		{
			get
			{
				if (viewConformance == null)
					viewConformance = CreateViewModifierConformance();
				return viewConformance;
			}
		}

		public PropertyInfo BodyProperty { get; }

		internal CustomViewModifierType(Type customViewModifierType) : base(customViewModifierType, MetadataKinds.Struct)
		{
			try
			{
				BodyProperty = customViewModifierType.GetProperty("Body", BindingFlags.Public | BindingFlags.Instance);
				if (BodyProperty is null || !BodyProperty.CanRead || BodyProperty.CanWrite || !BodyProperty.PropertyType.IsSubclassOf(typeof(View)))
					throw new ArgumentException($"View implementations must either override ViewType, or declare a public, read-only `Body` property returning a concrete type of `{nameof(View)}`");

				var thunkMetadata = (CustomViewModifierMetadata*)fullMetadata;
				thunkMetadata->ThunkViewModifierU = Metadata;
				thunkMetadata->ThunkViewModifierT = SwiftType.Of(BodyProperty.PropertyType)!.Metadata;
				// Currently unused, so don't force allocation if it's a custom view
				//thunkMetadata->ThunkViewTViewConformance = swiftBodyType.ViewConformance;
				thunkMetadata->ThunkViewTViewConformance = null;
			}
			catch
			{
				// Ensure we don't leak allocated unmanaged memory
				Dispose(true);
				throw;
			}
		}

		protected override unsafe FullTypeMetadata* AllocFullTypeMetadata()
			=> (FullTypeMetadata*)Marshal.AllocHGlobal(sizeof(CustomViewModifierMetadata) + 4 * NativeFields.Count);

		protected override unsafe ValueWitnessTable* CreateValueWitnessTable()
		{
			var vwt = base.CreateValueWitnessTable();
			vwt->Flags = ValueWitnessFlags.IsNonPOD;
			return vwt;
		}

		public override ProtocolWitnessTable* GetProtocolConformance(ProtocolDescriptor* descriptor)
			=> descriptor == SwiftUILib.Types.ViewModifier ? ViewConformance : base.GetProtocolConformance(descriptor);

		protected virtual ProtocolWitnessTable* CreateViewModifierConformance()
		{
			viewModifierConformanceDesc = (ViewModifierProtocolConformanceDescriptor*)Marshal.AllocHGlobal(sizeof(ViewModifierProtocolConformanceDescriptor));

			// zero everything first
			*viewModifierConformanceDesc = default;
			viewModifierConformanceDesc->Populate(Metadata->TypeDescriptor);

			var bodySwiftType = SwiftType.Of(BodyProperty.PropertyType)!;
			var bodyConformance = bodySwiftType.GetProtocolConformance(SwiftUILib.Types.ViewModifier);
			var witnessTable = SwiftCoreLib.GetProtocolWitnessTable(&viewModifierConformanceDesc->ConformanceDescriptor, Metadata, null);

			viewModifierConformanceDesc->FixupAndRegister(
				witnessTable,
				bodyConformance,
				bodySwiftType.Metadata,
				SwiftGlueLib.Pointers.ViewModifierBodyProtocolWitness);

			return witnessTable;
		}

		protected internal override unsafe void Transfer(void* dest, void* src, TransferFuncType funcType)
		{
			// Manage ref counts
			if (funcType.IsCopy())
				((CustomViewModifierData*)src)->View.AddRef();
			if (funcType.IsAssign())
				((CustomViewModifierData*)dest)->View.UnRef();

			// This msut come after the above, since AddRef might modify the GCHandle
			//  that we're copying here..
			base.Transfer(dest, src, funcType);
		}

		protected internal override unsafe void Destroy(void* data)
		{
			base.Destroy(data);
			((CustomViewModifierData*)data)->View.UnRef();
		}

		// FIXME: View data appears to be passed in context register
		static void Body(void* dest, void* dataPtr)
		{
			var data = (CustomViewModifierData*)dataPtr;
			var view = data->View;

			// HACK: Overwrite our data array with the given native data
			view.OverwriteNativeData(data);

			// Now, when we call Body, it will operate on the new data
			var body = (ViewModifier)((CustomViewModifierType)view.ViewType).BodyProperty.GetValue(view);

			// Copy the returned view into dest
			using (var handle = body.GetHandle())
				body.ViewType.Transfer(dest, handle.Pointer, TransferFuncType.InitWithCopy);
		}
		static readonly PtrPtrFunc bodyFn = Body;

		static CustomViewModifierType()
		{
			SetViewModifierBodyFn(bodyFn);
		}

		[DllImport(SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_ThunkViewModifier_setViewModifierBodyFn")]
		static extern void SetViewModifierBodyFn(PtrPtrFunc bodyFn);

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (viewModifierConformanceDesc != null)
			{
				Marshal.FreeHGlobal((IntPtr)viewModifierConformanceDesc);
				viewModifierConformanceDesc = null;
			}
		}
	}
}
