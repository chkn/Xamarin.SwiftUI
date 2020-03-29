using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Swift.Interop
{
	public readonly struct SwiftFieldInfo
	{
		public readonly FieldInfo Field;
		public readonly SwiftType SwiftType;
		public readonly int Offset;

		public SwiftFieldInfo (FieldInfo field, SwiftType swiftType, int offset)
		{
			Field = field;
			SwiftType = swiftType;
			Offset = offset;
		}
	}

	/// <summary>
	/// A <see cref="SwiftType"/> that is declared and implemented in managed code.
	/// </summary>
	/// <typeparam name="TNativeData">
	/// A blittable struct type representing the native data payload that comes before
	///  any <see cref="ISwiftFieldExposable"/> fields in the data structure exposed to Swift.
	/// </typeparam>
	// FIXME: Ultimately, we should emit all metadata into one large allocation,
	//  to make sharing a module and relative pointers easier.
	public unsafe class ManagedSwiftType : SwiftType, IDisposable
	{
		GCHandle gch;

		public Type ManagedType { get; }

		public sealed override int NativeDataSize { get; }

		public virtual int NativeFieldsOffset => 0;

		public IReadOnlyList<SwiftFieldInfo> NativeFields { get; }

		/// <summary>
		/// The number of pointer-sized words immediately following the FullTypeMetadata
		///  in the type metadata. For structs, the pointer-sized words accounted for here are then
		///  followed by a <see cref="GCHandle"/> pointer and then the field offset vector.
		/// </summary>
		public virtual uint AdditionalMetadataPointers => 0;

		/// <summary>
		/// When added to <see cref="AdditionalMetadataPointers"/>, forms the offset
		///  in pointer-sized words of the <see cref="GCHandle"/> pointer.
		/// </summary>
		const uint GCHandleOffsetBase = 2;

		uint GCHandleOffset => GCHandleOffsetBase + AdditionalMetadataPointers;
		uint FieldOffsetVectorOffset => GCHandleOffset + 1;

		public ManagedSwiftType (Type managedType, MetadataKinds? metadataKind = null)
		{
			// DO NOT re-order these initialization steps
			ManagedType = managedType;
			NativeFields = DetermineNativeFields ();
			NativeDataSize = CalculateNativeDataSize ();

			try {
				fullMetadata = AllocFullTypeMetadata ();
				fullMetadata->ValueWitnessTable = CreateValueWitnessTable ();

				var kind = metadataKind ?? MetadataKind.OfType (managedType);
				Metadata->Kind = kind;
				Metadata->TypeDescriptor = CreateTypeDescriptor (managedType);

				if (kind == MetadataKinds.Struct) {
					// Store a GCHandle so we can recover our ManagedSwiftType from a TypeMetadata*
					gch = GCHandle.Alloc (this, GCHandleType.WeakTrackResurrection);
					var ptr = GCHandle.ToIntPtr (gch);
					var loc = (IntPtr*)Metadata + GCHandleOffset;
					*loc = ptr;

					// Populate field offset vector
					var fieldOffsetVector = (int*)(loc + 1);
					for (var i = 0; i < NativeFields.Count; i++)
						fieldOffsetVector [i] = NativeFields [i].Offset;
				}
			} catch {
				// Ensure we don't leak allocated unmanaged memory
				Dispose (true);
				throw;
			}
		}

		/// <summary>
		/// Calculates the size of the Swift native data.
		/// </summary>
		/// <remarks>
		/// This method is called from the constructor. The only instance state that
		///  is guaranteed to be initialized before this method is called are <see cref="ManagedType"/>
		///  and <see cref="NativeFields"/>.
		/// </remarks>
		protected virtual unsafe int CalculateNativeDataSize ()
			=> NativeFieldsOffset + NativeFields.Sum (fld => fld.SwiftType.NativeDataSize); // FIXME: alignment?

		#region Native Fields

		/// <summary>
		/// Determines the fields to expose to Swift.
		/// </summary>
		/// <remarks>
		/// This method is called from the constructor. The only instance state that
		///  is guaranteed to be initialized before this method is called is <see cref="ManagedType"/>.
		/// </remarks>
		protected virtual unsafe IReadOnlyList<SwiftFieldInfo> DetermineNativeFields ()
		{
			var offset = NativeFieldsOffset;
			var managedFields = ManagedType.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			var swiftFields = new List<SwiftFieldInfo> (managedFields.Length);
			foreach (var fld in managedFields) {
				if (!typeof (ISwiftFieldExposable).IsAssignableFrom (fld.FieldType))
					continue;
				var swiftType = SwiftType.Of (fld.FieldType);
				Debug.Assert (swiftType != null, "ISwiftFieldExposable types must be SwiftTypes");
				// FIXME: '!' shouldn't be needed as we have Debug.Assert
				swiftFields.Add (new SwiftFieldInfo (fld, swiftType!, offset));
				offset += swiftType!.NativeDataSize; // FIXME: alignment?
			}
			return swiftFields.ToArray ();
		}

		public void InitNativeFields (object instance, void* data)
		{
			foreach (var fldInfo in NativeFields) {
				var fld = (ISwiftFieldExposable)fldInfo.Field.GetValue (instance);
				fld.InitNativeData ((byte*)data + fldInfo.Offset);
			}
		}

		#endregion

		/// <summary>
		/// Allocates the <see cref="FullTypeMetadata"/> for this type.
		/// </summary>
		/// <remarks>
		/// This method is called from the constructor. The only instance state that
		///  is guaranteed to be initialized before this method is called are <see cref="ManagedType"/>,
		///  <see cref="NativeFields"/> and <see cref="NativeDataSize"/>.
		/// </remarks>
		protected virtual FullTypeMetadata* AllocFullTypeMetadata ()
			=> (FullTypeMetadata*)Marshal.AllocHGlobal (sizeof (FullTypeMetadata) + 4 * NativeFields.Count);

		#region Value Witness Table

		/// <summary>
		/// Computes the <see cref="ValueWitnessTable"/> for this type.
		/// </summary>
		/// <remarks>
		/// This method is called from the constructor.The only instance state that
		///  is guaranteed to be initialized before this method is called are <see cref="ManagedType"/>,
		///  <see cref="NativeFields"/> and <see cref="NativeDataSize"/>.
		/// </remarks>
		protected virtual ValueWitnessTable* CreateValueWitnessTable ()
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
				vwt->Flags = NativeFields.Any (fld => fld.SwiftType.ValueWitnessTable->IsNonPOD)? ValueWitnessFlags.IsNonPOD : 0;
			} catch {
				Marshal.FreeHGlobal ((IntPtr)vwt);
				throw;
			}
			return vwt;
		}

		protected internal override unsafe void Transfer (void* dest, void* src, TransferFuncType funcType)
		{
			// Copy the native data
			var sz = NativeDataSize;
			Buffer.MemoryCopy (src, dest, sz, sz);

			// Transfer the fields
			foreach (var fld in NativeFields)
				fld.SwiftType.Transfer ((byte*)dest + fld.Offset, (byte*)src + fld.Offset, funcType);
		}

		protected internal override unsafe void Destroy (void* data)
		{
			// Destroy the fields
			foreach (var fld in NativeFields)
				fld.SwiftType.Destroy ((byte*)data + fld.Offset);
		}

		static ManagedSwiftType GetManagedSwiftType (TypeMetadata* metadata)
		{
			Debug.Assert (metadata->Kind == MetadataKinds.Struct);
			// subtract 1 because GCHandle is right before field offset vector
			var offs = ((StructDescriptor*)metadata->TypeDescriptor)->FieldOffsetVectorOffset - 1;
			var loc = (IntPtr*)metadata + offs;
			var ptr = *loc;
			return (ManagedSwiftType)GCHandle.FromIntPtr (ptr).Target;
		}

		//FIXME: MonoPInvokeCallback
		static void* InitWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var swiftType = GetManagedSwiftType (typeMetadata);
			swiftType.Transfer (dest, src, TransferFuncType.InitWithCopy);
			return dest;
		}
		static readonly TransferFunc InitWithCopyDel = InitWithCopy;

		//FIXME: MonoPInvokeCallback
		static void* AssignWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var swiftType = GetManagedSwiftType (typeMetadata);
			swiftType.Transfer (dest, src, TransferFuncType.AssignWithCopy);
			return dest;
		}
		static readonly TransferFunc AssignWithCopyDel = AssignWithCopy;

		//FIXME: MonoPInvokeCallback
		static void* InitWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var swiftType = GetManagedSwiftType (typeMetadata);
			swiftType.Transfer (dest, src, TransferFuncType.InitWithTake);
			return dest;
		}
		static readonly TransferFunc InitWithTakeDel = InitWithTake;

		//FIXME: MonoPInvokeCallback
		static void* AssignWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var swiftType = GetManagedSwiftType (typeMetadata);
			swiftType.Transfer (dest, src, TransferFuncType.AssignWithTake);
			return dest;
		}
		static readonly TransferFunc AssignWithTakeDel = AssignWithTake;

		//FIXME: MonoPInvokeCallback
		static void Destroy (void* obj, TypeMetadata* typeMetadata)
		{
			var swiftType = GetManagedSwiftType (typeMetadata);
			swiftType.Destroy (obj);
		}
		static readonly DestroyFunc DestoryDel = Destroy;

		#endregion

		#region Type Descriptor

		static string Sanitize (string name)
			=> Regex.Replace (name, @"[^a-zA-Z0-9]", "");

		protected virtual NominalTypeDescriptor* CreateTypeDescriptor (Type managedType)
		{
			if (Metadata->Kind != MetadataKinds.Struct)
				throw new NotImplementedException (Metadata->Kind.ToString ());

			var name = Encoding.ASCII.GetBytes (Sanitize (managedType.Name));
			var ns = Encoding.ASCII.GetBytes (Sanitize (managedType.Namespace));

			StructDescriptor* desc;
			var modDesc = (ModuleDescriptor*)Marshal.AllocHGlobal (
				sizeof (ModuleDescriptor) +
				sizeof (StructDescriptor) +
				sizeof (FieldDescriptor) +
				sizeof (FieldRecord) * NativeFields.Count +
				NativeFields.Sum (fld => fld.SwiftType.MangledTypeSize) +
				NativeFields.Sum (fld => fld.Field.Name.Length + 1) +
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

				desc->NumberOfFields = NativeFields.Count;
				desc->FieldOffsetVectorOffset = FieldOffsetVectorOffset;

				// Even if there are no fields, the presence of the field descriptor
				//  pointer is used to determine if the type is "reflectable"
				var fldDesc = (FieldDescriptor*)((byte*)namePtr + name.Length + 1);
				desc->NominalType.FieldsPtr.Target = (void*)fldDesc;

				fldDesc->MangledTypeNamePtr = default; // FIXME:?
				fldDesc->SuperclassPtr = default;
				fldDesc->Kind = FieldDescriptorKind.Struct;
				fldDesc->FieldRecordSize = checked((ushort)sizeof (FieldRecord));
				fldDesc->NumFields = (uint)NativeFields.Count;

				var fldRecs = (FieldRecord*)(fldDesc + 1);
				var fldType = (byte*)(fldRecs + NativeFields.Count);
				for (var i = 0; i < NativeFields.Count; i++) {
					fldRecs [i].Flags = NativeFields [i].Field.IsInitOnly ? (FieldRecordFlags)0 : FieldRecordFlags.IsVar;
					fldRecs [i].MangledTypeNamePtr.Target = fldType;
					fldType = NativeFields [i].SwiftType.WriteMangledType (fldType);
				}
				var fldNamePtr = (IntPtr)fldType;
				for (var i = 0; i < NativeFields.Count; i++) {
					var fldName = Encoding.UTF8.GetBytes (NativeFields [i].Field.Name);
					fldRecs [i].FieldNamePtr.Target = (void*)fldNamePtr;
					Marshal.Copy (fldName, 0, fldNamePtr, fldName.Length);
					Marshal.WriteByte (fldNamePtr, fldName.Length, 0);
					fldNamePtr += fldName.Length + 1;
				}
			} catch {
				Marshal.FreeHGlobal ((IntPtr)modDesc);
				throw;
			}
			return (NominalTypeDescriptor*)desc;
		}

		#endregion

		#region IDisposable

		protected virtual void Dispose (bool disposing)
		{
			if (gch.IsAllocated)
				gch.Free ();
			if (fullMetadata != null) {
				var vwt = fullMetadata->ValueWitnessTable;
				if (vwt != null) {
					fullMetadata->ValueWitnessTable = null;
					Marshal.FreeHGlobal ((IntPtr)vwt);
				}

				Marshal.FreeHGlobal ((IntPtr)fullMetadata);
				fullMetadata = null;
			}
		}

		~ManagedSwiftType () => Dispose (false);
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		#endregion
	}
}
