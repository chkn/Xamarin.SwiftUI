using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

using Swift;
using Swift.Interop;

namespace SwiftUI.Interop
{
	// HACK: Allocate 2 extra pointers after metadata for SwiftUIView.ThunkView generic metadata:
	//   type metadata for T
	//   View conformance for T (unused)
	[StructLayout (LayoutKind.Sequential)]
	unsafe struct CustomViewMetadata
	{
		public FullTypeMetadata Metadata;
		public TypeMetadata* ThunkViewT;
		public ProtocolWitnessTable* ThunkViewTViewConformance;
	}

	unsafe class CustomViewType : ViewType, IDisposable
	{
		readonly Type bodyType;
		readonly Type [] stateFieldTypes;
		ViewProtocolConformanceDescriptor* viewConformanceDesc;

		internal int NativeDataSize { get; }

		internal static CustomViewType For (Type bodyType, Type stateType)
		{
			Type [] stateFieldTypes;

			if (stateType == typeof (Unit))
				stateFieldTypes = Array.Empty<Type> ();
			else if (typeof (ITuple).IsAssignableFrom (stateType))
				stateFieldTypes = FSharpType.GetTupleElements (stateType);
			else
				stateFieldTypes = new[] { stateType };

			return new CustomViewType (bodyType, stateFieldTypes);
		}

		CustomViewType (Type bodyType, Type [] stateFieldTypes)
			: base (AllocFullTypeMetadata ())
		{
			try {
				this.bodyType = bodyType;
				this.stateFieldTypes = stateFieldTypes;

				NativeDataSize = CalculateNativeDataSize ();
				fullMetadata->ValueWitnessTable = CreateValueWitnessTable ();

				Metadata->Kind = MetadataKinds.Struct;
				Metadata->TypeDescriptor = (NominalTypeDescriptor*)CreateTypeDescriptor ();

				var swiftBodyType = SwiftType.Of (bodyType) as ViewType;
				if (swiftBodyType is null)
					throw new ArgumentException ("Expected Swift View type", nameof (bodyType));

				var thunkMetadata = (CustomViewMetadata*)fullMetadata;
				thunkMetadata->ThunkViewT = swiftBodyType.Metadata;
				// Currently unused, so don't force allocation if it's a custom view
				//thunkMetadata->ThunkViewTViewConformance = swiftBodyType.ViewConformance;
				thunkMetadata->ThunkViewTViewConformance = null;

			} catch {
				// Ensure we don't leak allocated unmanaged memory
				Dispose (true);
				throw;
			}
		}

		static FullTypeMetadata* AllocFullTypeMetadata ()
			=> (FullTypeMetadata*)Marshal.AllocHGlobal (sizeof (CustomViewMetadata));

		int CalculateNativeDataSize ()
		{
			if (stateFieldTypes.Length == 0)
				return sizeof (CustomViewData);

			throw new NotImplementedException ();
		}

		#region Value Witness Table

		ValueWitnessTable* CreateValueWitnessTable ()
		{
			var vwt = (ValueWitnessTable*)Marshal.AllocHGlobal (sizeof (ValueWitnessTable));
			try {
				var initWithCopy = Marshal.GetFunctionPointerForDelegate (InitWithCopyDel);
				vwt->InitBufferWithCopy = initWithCopy;
				vwt->Destroy = Marshal.GetFunctionPointerForDelegate (DestoryDel);
				vwt->InitWithCopy = initWithCopy;
				vwt->AssignWithCopy = Marshal.GetFunctionPointerForDelegate (AssignWithCopyDel);
				vwt->InitWithTake = Marshal.GetFunctionPointerForDelegate (InitWithTakeDel);
				vwt->AssignWithTake = Marshal.GetFunctionPointerForDelegate (AssignWithTakeDel);

				vwt->Size = (IntPtr)NativeDataSize;
				vwt->Stride = (IntPtr)NativeDataSize; // FIXME: alignment
				vwt->Flags = ValueWitnessFlags.IsNonPOD;
			} catch {
				Marshal.FreeHGlobal ((IntPtr)vwt);
				throw;
			}
			return vwt;
		}

		//FIXME: MonoPInvokeCallback
		static void* InitWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.AddRef ();
			return InitWithTake (dest, src, typeMetadata);
		}
		static readonly TransferFunc InitWithCopyDel = InitWithCopy;

		//FIXME: MonoPInvokeCallback
		static void* AssignWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.AddRef ();
			Destroy (dest, typeMetadata);
			return InitWithTake (dest, src, typeMetadata);
		}
		static readonly TransferFunc AssignWithCopyDel = AssignWithCopy;

		//FIXME: MonoPInvokeCallback
		// FIXME: Swift core lib probably provides a function for this trivial case?
		static void* InitWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var vwt = *((ValueWitnessTable**)typeMetadata - 1);
			var sz = (long)vwt->Size;
			Buffer.MemoryCopy (src, dest, sz, sz);
			return dest;
		}
		static readonly TransferFunc InitWithTakeDel = InitWithTake;

		//FIXME: MonoPInvokeCallback
		// FIXME: Swift core lib probably provides a function for this trivial case?
		static void* AssignWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			Destroy (dest, typeMetadata);
			return InitWithTake (dest, src, typeMetadata);
		}
		static readonly TransferFunc AssignWithTakeDel = AssignWithTake;

		//FIXME: MonoPInvokeCallback
		static void Destroy (void* obj, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)obj)->View;
			view.Dispose ();
		}
		static readonly DestroyFunc DestoryDel = Destroy;

		#endregion

		#region Type Descriptor

		StructDescriptor* CreateTypeDescriptor ()
		{
			// FIXME: Name these somehow..
			//  Maybe have a single module desc that we cache?
			var name = Encoding.ASCII.GetBytes ("HELLO");
			var ns = Encoding.ASCII.GetBytes ("WORLD");

			StructDescriptor* desc;
			var modDesc = (ModuleDescriptor*)Marshal.AllocHGlobal (
				sizeof (ModuleDescriptor) +
				sizeof (StructDescriptor) +
				sizeof (FieldDescriptor) +
				// FIXME: plus size of field vector
				name.Length + ns.Length + 2 // (for nulls)
			);
			try {
				modDesc->Context = new ContextDescriptor {
					Flags = new ContextDescriptorFlags { Kind = ContextDescriptorKind.Module }
				};

				var nsPtr = (IntPtr)((byte*)modDesc + sizeof (ModuleDescriptor));
				Marshal.Copy (ns, 0, nsPtr, ns.Length);
				Marshal.WriteByte (nsPtr, ns.Length, 0);
				modDesc->NamePtr.Target = (void*)nsPtr;

				desc = (StructDescriptor*)((byte*)nsPtr + ns.Length + 1);
				desc->NominalType.Context.Flags = new ContextDescriptorFlags {
					Kind = ContextDescriptorKind.Struct,
					IsUnique = true
				};
				desc->NominalType.Context.ParentPtr.Target = modDesc;

				var namePtr = (IntPtr)((byte*)desc + sizeof (StructDescriptor));
				Marshal.Copy (name, 0, namePtr, name.Length);
				Marshal.WriteByte (namePtr, name.Length, 0);
				desc->NominalType.NamePtr.Target = (void*)namePtr;

				//FIXME: Can't implement this for now because Marshal.GetFunctionPointerForDelegate
				//  always gives us a pointer that overflows the Int32 offset value.
				desc->NominalType.AccessFunctionPtr.Target = null;

				//FIXME
				desc->NumberOfFields = 0;
				desc->FieldOffsetVectorOffset = 2;

				// Even if there are no fields, the presence of the field descriptor
				//  pointer is used to determine if the type is "reflectable"
				var fldDesc = (FieldDescriptor*)((byte*)namePtr + name.Length + 1);
				desc->NominalType.FieldsPtr.Target = (void*)fldDesc;

				// FIXME: ?
				fldDesc->MangledTypeNamePtr = default;
				fldDesc->SuperclassPtr = default;
				fldDesc->Kind = FieldDescriptorKind.Struct;
				fldDesc->FieldRecordSize = 12;
				fldDesc->NumFields = 0; //FIXME
			} catch {
				Marshal.FreeHGlobal ((IntPtr)modDesc);
				throw;
			}
			return desc;
		}

		#endregion

		protected override ProtocolWitnessTable* CreateViewConformance ()
		{
			viewConformanceDesc = (ViewProtocolConformanceDescriptor*)Marshal.AllocHGlobal (sizeof (ViewProtocolConformanceDescriptor));

			// zero everything first
			*viewConformanceDesc = default;
			viewConformanceDesc->Populate (Metadata->TypeDescriptor);

			var bodySwiftType = (ViewType)Of (bodyType)!;
			var bodyConformance = bodySwiftType.ViewConformance;
			var witnessTable = SwiftCoreLib.GetProtocolWitnessTable (&viewConformanceDesc->ConformanceDescriptor, Metadata, null);

			viewConformanceDesc->FixupAndRegister (
				witnessTable,
				bodyConformance,
				bodySwiftType.Metadata,
				SwiftGlueLib.Pointers.BodyProtocolWitness);

			return witnessTable;
		}

		// FIXME: View data appears to be passed in context register
		static void Body (void* dest, void* gcHandle)
		{
			var view = (ICustomView)GCHandle.FromIntPtr ((IntPtr)gcHandle).Target;
			var body = view.Body;
			var swiftType = body.SwiftType;
			using (var handle = body.Handle)
				swiftType.Transfer (dest, handle.Pointer, swiftType.MoveInitFunc);
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

		#region IDisposable

		protected virtual void Dispose (bool disposing)
		{
			if (fullMetadata != null) {
				var vwt = fullMetadata->ValueWitnessTable;
				if (vwt != null) {
					fullMetadata->ValueWitnessTable = null;
					Marshal.FreeHGlobal ((IntPtr)vwt);
				}

				Marshal.FreeHGlobal ((IntPtr)fullMetadata);
				fullMetadata = null;
			}

			if (viewConformanceDesc != null) {
				Marshal.FreeHGlobal ((IntPtr)viewConformanceDesc);
				viewConformanceDesc = null;
			}
		}

		~CustomViewType () => Dispose (false);
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		#endregion
	}
}
