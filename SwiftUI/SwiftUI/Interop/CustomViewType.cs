using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift.Interop;

namespace SwiftUI.Interop
{
	unsafe class CustomViewType : ViewType, IDisposable
	{
		public static CustomViewType Create (Type bodyType, Type? closureType = null)
		{
			var bodySwiftType = SwiftType.Of (bodyType);
			if (bodySwiftType is null)
				throw new ArgumentException ($"{bodyType.FullName} is not a Swift type");
		}

		CustomViewType (TypeMetadata* metadata) : base (metadata)
		{
			ViewConformance = CreateViewConformance ();
		}

		static TypeMetadata* CreateTypeMetadata (Type viewType)
		{
			TypeMetadata* metadata;

			// Pointer to ValueWitnessTable* is at offset - 1
			var valueWitnessTable = (ValueWitnessTable**)Marshal.AllocHGlobal (IntPtr.Size + sizeof (TypeMetadata));
			try {
				*valueWitnessTable = CreateValueWitnessTable (viewType);

				metadata = (TypeMetadata*)(valueWitnessTable + 1);
				metadata->Kind = MetadataKinds.Struct;
				metadata->TypeDescriptor = (NominalTypeDescriptor*)CreateTypeDescriptor (viewType);
			} catch {
				Marshal.FreeHGlobal ((IntPtr)valueWitnessTable);
				throw;
			}
			return metadata;
		}

		#region Value Witness Table

		static ValueWitnessTable* CreateValueWitnessTable (Type viewType)
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

				var sz = (IntPtr)View.GetNativeDataSize (viewType);
				vwt->Size = sz;
				vwt->Stride = sz;
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

		static StructDescriptor* CreateTypeDescriptor (Type viewType)
		{
			//FIXME: Fields
			// Need to alloc enough for name and fields relative to this ptr..
			var name = Encoding.ASCII.GetBytes (viewType.Name);
			var ns = Encoding.ASCII.GetBytes (viewType.Namespace);

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
				Debug.Assert (modDesc->Name == viewType.Namespace);

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
				Debug.Assert (desc->NominalType.Name == viewType.Name);

				desc->NumberOfFields = 0;
				desc->FieldOffsetVectorOffset = 0;
			} catch {
				Marshal.FreeHGlobal ((IntPtr)modDesc);
				throw;
			}
			return desc;
		}

		// FIXME: Share a single instance across all views?
		static ProtocolWitnessTable* CreateViewConformance ()
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
				Marshal.FreeHGlobal ((IntPtr)ViewConformance);
				Marshal.FreeHGlobal ((IntPtr)Metadata->TypeDescriptor->Context.Parent);
				Marshal.FreeHGlobal ((IntPtr)((ValueWitnessTable**)Metadata - 1));
				Metadata = null;
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
