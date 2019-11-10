using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

using Swift.Interop;

namespace SwiftUI.Interop
{
	unsafe class CustomViewType : ViewType, IDisposable
	{
		readonly Type bodyType;
		readonly Type [] stateFieldTypes;

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
			: base (AllocTypeMetadata ())
		{
			try {
				this.bodyType = bodyType;
				this.stateFieldTypes = stateFieldTypes;
				NativeDataSize = CalculateNativeDataSize ();

				var valueWitnessTable = (ValueWitnessTable**)Metadata - 1;
				*valueWitnessTable = CreateValueWitnessTable ();

				ViewConformance = CreateViewConformance ();

				Metadata->Kind = MetadataKinds.Struct;
				Metadata->TypeDescriptor = (NominalTypeDescriptor*)CreateTypeDescriptor ();
			} catch {
				// Ensure we don't leak allocated unmanaged memory
				Dispose (true);
				throw;
			}
		}

		static TypeMetadata* AllocTypeMetadata ()
		{
			// Pointer to ValueWitnessTable* is at offset - 1 from metadata
			return (TypeMetadata*)(Marshal.AllocHGlobal (IntPtr.Size + sizeof (TypeMetadata)) + IntPtr.Size);
		}

		int CalculateNativeDataSize ()
		{
			if (stateFieldTypes.Length == 0)
				return sizeof (View.Data);

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
			var view = ((View.Data*)src)->View;
			view.AddRef ();
			return InitWithTake (dest, src, typeMetadata);
		}
		static readonly TransferFunc InitWithCopyDel = InitWithCopy;

		//FIXME: MonoPInvokeCallback
		static void* AssignWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((View.Data*)src)->View;
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
			var view = ((View.Data*)obj)->View;
			view.Dispose ();
		}
		static readonly DestroyFunc DestoryDel = Destroy;

		#endregion

		StructDescriptor* CreateTypeDescriptor ()
		{
			//FIXME: Fields
			// FIXME: Is this a good way to name these?
			var name = Encoding.ASCII.GetBytes (Guid.NewGuid ().ToString ());
			var ns = Encoding.ASCII.GetBytes (Guid.NewGuid ().ToString ());

			StructDescriptor* desc;
			var modDesc = (ModuleDescriptor*)Marshal.AllocHGlobal
				(sizeof (ModuleDescriptor) + sizeof (StructDescriptor) + name.Length + ns.Length + 2);
			try {
				modDesc->Context = new ContextDescriptor {
					Flags = new ContextDescriptorFlags { Kind = ContextDescriptorKind.Module }
				};

				var nsPtr = (IntPtr)((byte*)modDesc + sizeof (ModuleDescriptor));
				Marshal.Copy (ns, 0, nsPtr, ns.Length);
				Marshal.WriteByte (nsPtr, ns.Length, 0);
				modDesc->NamePtr.Target = (void*)nsPtr;

				desc = (StructDescriptor*)((byte*)nsPtr + ns.Length + 1);
				desc->NominalType = default;
				desc->NominalType.Context.Flags = new ContextDescriptorFlags {
					Kind = ContextDescriptorKind.Struct,
					IsUnique = true
				};
				desc->NominalType.Context.ParentPtr.Target = modDesc;

				var namePtr = (IntPtr)((byte*)desc + sizeof (StructDescriptor));
				Marshal.Copy (name, 0, namePtr, name.Length);
				Marshal.WriteByte (namePtr, name.Length, 0);
				desc->NominalType.NamePtr.Target = (void*)namePtr;

				desc->NumberOfFields = 0;
				desc->FieldOffsetVectorOffset = 0;
			} catch {
				Marshal.FreeHGlobal ((IntPtr)modDesc);
				throw;
			}
			return desc;
		}

		ProtocolWitnessTable* CreateViewConformance ()
		{
			var vtable = (ProtocolWitnessTable*)Marshal.AllocHGlobal (sizeof (ProtocolWitnessTable) + IntPtr.Size);
			try {
				vtable->ProtocolDescriptor = (ProtocolConformanceDescriptor*)SwiftUILib.Types.View;
				Marshal.WriteIntPtr ((IntPtr)(vtable + 1), IntPtr.Zero); // FIXME
			} catch {
				Marshal.FreeHGlobal ((IntPtr)vtable);
				throw;
			}
			return vtable;
		}

		#region IDisposable

		protected virtual void Dispose (bool disposing)
		{
			if (Metadata != null) {
				Marshal.FreeHGlobal ((IntPtr)Metadata->TypeDescriptor->Context.Parent);
				Marshal.FreeHGlobal ((IntPtr)((ValueWitnessTable**)Metadata - 1));
				Metadata = null;
			}

			if (ViewConformance != null) {
				Marshal.FreeHGlobal ((IntPtr)ViewConformance);
				ViewConformance = null;
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
