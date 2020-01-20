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
	// HACK: Allocate 2 extra pointers after metadata for SwiftUIView.ThunkView generic metadata:
	//   type metadata for T
	//   View conformance for T (unused)
	// This works because View has no generic metadata of its own.
	[StructLayout (LayoutKind.Sequential)]
	unsafe struct CustomViewMetadata
	{
		public FullTypeMetadata Metadata;

		public const uint AdditionalPointers = 3;
		public TypeMetadata* ThunkViewU;
		public TypeMetadata* ThunkViewT;
		public ProtocolWitnessTable* ThunkViewTViewConformance;
	}

	unsafe class CustomViewType : ManagedSwiftType
	{
		ViewProtocolConformanceDescriptor* viewConformanceDesc;
		ProtocolWitnessTable* viewConformance;

		public override int NativeFieldsOffset => sizeof (CustomViewData);

		public override uint AdditionalMetadataPointers => CustomViewMetadata.AdditionalPointers;

		public ProtocolWitnessTable* ViewConformance {
			get {
				if (viewConformance == null)
					viewConformance = CreateViewConformance ();
				return viewConformance;
			}
		}

		public PropertyInfo BodyProperty { get; }

		internal CustomViewType (Type customViewType): base (customViewType, MetadataKinds.Struct)
		{
			try {
				BodyProperty = customViewType.GetProperty ("Body", BindingFlags.Public | BindingFlags.Instance);
				if (BodyProperty is null || !BodyProperty.CanRead || BodyProperty.CanWrite || !BodyProperty.PropertyType.IsSubclassOf (typeof (View)))
					throw new ArgumentException ($"View implementations must either override ViewType, or declare a public, read-only `Body` property returning a concrete type of `{nameof (View)}`");

				var thunkMetadata = (CustomViewMetadata*)fullMetadata;
				thunkMetadata->ThunkViewU = Metadata;
				thunkMetadata->ThunkViewT = SwiftType.Of (BodyProperty.PropertyType)!.Metadata;
				// Currently unused, so don't force allocation if it's a custom view
				//thunkMetadata->ThunkViewTViewConformance = swiftBodyType.ViewConformance;
				thunkMetadata->ThunkViewTViewConformance = null;
			} catch {
				// Ensure we don't leak allocated unmanaged memory
				Dispose (true);
				throw;
			}
		}

		protected override unsafe FullTypeMetadata* AllocFullTypeMetadata ()
			=> (FullTypeMetadata*)Marshal.AllocHGlobal (sizeof (CustomViewMetadata) + 4 * NativeFields.Count);

		protected override unsafe ValueWitnessTable* CreateValueWitnessTable ()
		{
			var vwt = base.CreateValueWitnessTable ();
			vwt->Flags = ValueWitnessFlags.IsNonPOD;
			return vwt;
		}

		public override ProtocolWitnessTable* GetProtocolConformance (ProtocolDescriptor* descriptor)
			=> descriptor == SwiftUILib.Types.View? ViewConformance : base.GetProtocolConformance (descriptor);

		protected virtual ProtocolWitnessTable* CreateViewConformance ()
		{
			viewConformanceDesc = (ViewProtocolConformanceDescriptor*)Marshal.AllocHGlobal (sizeof (ViewProtocolConformanceDescriptor));

			// zero everything first
			*viewConformanceDesc = default;
			viewConformanceDesc->Populate (Metadata->TypeDescriptor);

			var bodySwiftType = SwiftType.Of (BodyProperty.PropertyType)!;
			var bodyConformance = bodySwiftType.GetProtocolConformance (SwiftUILib.Types.View);
			var witnessTable = SwiftCoreLib.GetProtocolWitnessTable (&viewConformanceDesc->ConformanceDescriptor, Metadata, null);

			viewConformanceDesc->FixupAndRegister (
				witnessTable,
				bodyConformance,
				bodySwiftType.Metadata,
				SwiftGlueLib.Pointers.BodyProtocolWitness);

			return witnessTable;
		}

		protected internal override unsafe void Transfer (void* dest, void* src, TransferFuncType funcType)
		{
			// Manage ref counts
			if (funcType.IsCopy ())
				((CustomViewData*)src)->View.AddRef ();
			if (funcType.IsAssign ())
				((CustomViewData*)dest)->View.UnRef ();

			// This msut come after the above, since AddRef might modify the GCHandle
			//  that we're copying here..
			base.Transfer (dest, src, funcType);
		}

		protected internal override unsafe void Destroy (void* data)
		{
			base.Destroy (data);
			((CustomViewData*)data)->View.UnRef ();
		}

		// FIXME: View data appears to be passed in context register
		static void Body (void* dest, void* dataPtr)
		{
			var data = (CustomViewData*)dataPtr;
			var view = data->View;

			// HACK: Overwrite our data array with the given native data
			view.OverwriteNativeData (data);

			// Now, when we call Body, it will operate on the new data
			var body = (View)((CustomViewType)view.ViewType).BodyProperty.GetValue (view);

			// Copy the returned view into dest
			using (var handle = body.GetHandle ())
				body.ViewType.Transfer (dest, handle.Pointer, TransferFuncType.InitWithCopy);
		}
		static readonly PtrPtrFunc bodyFn = Body;

		static CustomViewType ()
		{
			SetBodyFn (bodyFn);
		}

		[DllImport (SwiftGlueLib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swiftui_ThunkView_setBodyFn")]
		static extern void SetBodyFn (PtrPtrFunc bodyFn);

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (viewConformanceDesc != null) {
				Marshal.FreeHGlobal ((IntPtr)viewConformanceDesc);
				viewConformanceDesc = null;
			}
		}
	}
}
