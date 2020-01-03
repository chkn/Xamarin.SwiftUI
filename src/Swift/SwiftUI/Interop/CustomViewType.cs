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
	[StructLayout (LayoutKind.Sequential)]
	unsafe struct CustomViewMetadata
	{
		public const int FieldOffsetVectorOffset = 5;
		public FullTypeMetadata Metadata;
		public TypeMetadata* ThunkViewU;
		public TypeMetadata* ThunkViewT;
		public ProtocolWitnessTable* ThunkViewTViewConformance;
		// field offset vector follows..
	}

	readonly struct SwiftFieldInfo
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

	// FIXME: Ultimately, we should emit all metadata into one large allocation,
	//  to make sharing a module and relative pointers easier.
	unsafe class CustomViewType : ViewType, IDisposable
	{
		// lock!
		static readonly ConditionalWeakTable<Type,CustomViewType> registry = new ConditionalWeakTable<Type,CustomViewType> ();

		ViewProtocolConformanceDescriptor* viewConformanceDesc;

		public override int NativeDataSize { get; }

		public PropertyInfo BodyProperty { get; }

		readonly SwiftFieldInfo [] swiftFields;

		public static new CustomViewType? Of (Type customViewType)
		{
			if (!customViewType.IsSubclassOf (typeof (View)))
				return null;

			CustomViewType result;
			lock (registry) {
				if (!registry.TryGetValue (customViewType, out result)) {
					result = new CustomViewType (customViewType);
					registry.Add (customViewType, result);
				}
			}
			return result;
		}

		CustomViewType (Type customViewType)
		{
			BodyProperty = customViewType.GetProperty ("Body", BindingFlags.Public | BindingFlags.Instance);
			if (BodyProperty is null || !BodyProperty.CanRead || BodyProperty.CanWrite || !BodyProperty.PropertyType.IsSubclassOf (typeof (View)))
				throw new ArgumentException ($"View implementations must either override ViewType, or declare a public, read-only `Body` property returning a concrete type of `{nameof (View)}`");

			// Determine fields to expose to Swift
			var offset = sizeof (CustomViewData);
			var fieldList = new List<SwiftFieldInfo> ();
			foreach (var fld in customViewType.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
				if (!typeof (ISwiftFieldExposable).IsAssignableFrom (fld.FieldType))
					continue;
				var swiftType = SwiftType.Of (fld.FieldType);
				Debug.Assert (swiftType != null);
				fieldList.Add (new SwiftFieldInfo (fld, swiftType, offset));
				offset += swiftType.NativeDataSize; // FIXME: alignment?
			}
			swiftFields = fieldList.ToArray ();

			try {
				fullMetadata = (FullTypeMetadata*)Marshal.AllocHGlobal (sizeof (CustomViewMetadata) + 4 * swiftFields.Length);
				NativeDataSize = sizeof (CustomViewData) + swiftFields.Sum (fld => fld.SwiftType.NativeDataSize); // FIXME: alignment?

				fullMetadata->ValueWitnessTable = CreateValueWitnessTable ();

				Metadata->Kind = MetadataKinds.Struct;
				Metadata->TypeDescriptor = (NominalTypeDescriptor*)CreateTypeDescriptor (customViewType);

				var thunkMetadata = (CustomViewMetadata*)fullMetadata;
				thunkMetadata->ThunkViewU = Metadata;
				thunkMetadata->ThunkViewT = ViewType.Of (BodyProperty.PropertyType)!.Metadata;
				// Currently unused, so don't force allocation if it's a custom view
				//thunkMetadata->ThunkViewTViewConformance = swiftBodyType.ViewConformance;
				thunkMetadata->ThunkViewTViewConformance = null;

				var fieldOffsetVector = (int*)(thunkMetadata + 1);
				for (var i = 0; i < swiftFields.Length; i++)
					fieldOffsetVector [i] = swiftFields [i].Offset;
			} catch {
				// Ensure we don't leak allocated unmanaged memory
				Dispose (true);
				throw;
			}
		}

		public void InitNativeFields (object customView, void* data)
		{
			foreach (var fldInfo in swiftFields) {
				var fld = (ISwiftFieldExposable)fldInfo.Field.GetValue (customView);
				fld.InitNativeData ((byte*)data + fldInfo.Offset);
			}
		}

		public void DestroyNativeFields (object customView, void* data)
		{
			foreach (var fldInfo in swiftFields) {
				var fld = (ISwiftFieldExposable)fldInfo.Field.GetValue (customView);
				fld.DestroyNativeData ((byte*)data + fldInfo.Offset);
			}
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

		internal override unsafe void Transfer (void* dest, void* src, TransferFuncType funcType)
		{
			// Add ref if needed
			switch (funcType) {
			case TransferFuncType.InitWithCopy:
			case TransferFuncType.AssignWithCopy:
				((CustomViewData*)src)->View.AddRef ();
				break;
			}

			// Copy the native data
			var sz = NativeDataSize;
			Buffer.MemoryCopy (src, dest, sz, sz);

			// Transfer the fields
			foreach (var fld in swiftFields)
				fld.SwiftType.Transfer ((byte*)dest + fld.Offset, (byte*)src + fld.Offset, funcType);
		}

		//FIXME: MonoPInvokeCallback
		static void* InitWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.ViewType.Transfer (dest, src, TransferFuncType.InitWithCopy);
			return dest;
		}
		static readonly TransferFunc InitWithCopyDel = InitWithCopy;

		//FIXME: MonoPInvokeCallback
		static void* AssignWithCopy (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.ViewType.Transfer (dest, src, TransferFuncType.AssignWithCopy);
			return dest;
		}
		static readonly TransferFunc AssignWithCopyDel = AssignWithCopy;

		//FIXME: MonoPInvokeCallback
		// FIXME: Swift core lib probably provides a function for this trivial case?
		static void* InitWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.ViewType.Transfer (dest, src, TransferFuncType.InitWithTake);
			return dest;
		}
		static readonly TransferFunc InitWithTakeDel = InitWithTake;

		//FIXME: MonoPInvokeCallback
		// FIXME: Swift core lib probably provides a function for this trivial case?
		static void* AssignWithTake (void* dest, void* src, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)src)->View;
			view.ViewType.Transfer (dest, src, TransferFuncType.AssignWithTake);
			return dest;
		}
		static readonly TransferFunc AssignWithTakeDel = AssignWithTake;

		//FIXME: MonoPInvokeCallback
		static void Destroy (void* obj, TypeMetadata* typeMetadata)
		{
			var view = ((CustomViewData*)obj)->View;
			view.DestroyNativeData (obj);
		}
		static readonly DestroyFunc DestoryDel = Destroy;

		#endregion

		#region Type Descriptor

		static string Sanitize (string name)
			=> Regex.Replace (name, @"[^a-zA-Z0-9]", "");

		StructDescriptor* CreateTypeDescriptor (Type customViewType)
		{
			var name = Encoding.ASCII.GetBytes (Sanitize (customViewType.Name));
			var ns = Encoding.ASCII.GetBytes (Sanitize (customViewType.Namespace));

			StructDescriptor* desc;
			var modDesc = (ModuleDescriptor*)Marshal.AllocHGlobal (
				sizeof (ModuleDescriptor) +
				sizeof (StructDescriptor) +
				sizeof (FieldDescriptor) +
				sizeof (FieldRecord) * swiftFields.Length +
				swiftFields.Sum (fld => fld.SwiftType.MangledTypeSize) +
				swiftFields.Sum (fld => fld.Field.Name.Length + 1) +
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

				desc->NumberOfFields = swiftFields.Length;
				desc->FieldOffsetVectorOffset = CustomViewMetadata.FieldOffsetVectorOffset;

				// Even if there are no fields, the presence of the field descriptor
				//  pointer is used to determine if the type is "reflectable"
				var fldDesc = (FieldDescriptor*)((byte*)namePtr + name.Length + 1);
				desc->NominalType.FieldsPtr.Target = (void*)fldDesc;

				fldDesc->MangledTypeNamePtr = default; // FIXME:?
				fldDesc->SuperclassPtr = default;
				fldDesc->Kind = FieldDescriptorKind.Struct;
				fldDesc->FieldRecordSize = checked ((ushort)sizeof (FieldRecord));
				fldDesc->NumFields = (uint)swiftFields.Length;

				var fldRecs = (FieldRecord*)(fldDesc + 1);
				var fldType = (byte*)(fldRecs + swiftFields.Length);
				for (var i = 0; i < swiftFields.Length; i++) {
					fldRecs [i].Flags = swiftFields [i].Field.IsInitOnly? (FieldRecordFlags)0 : FieldRecordFlags.IsVar;
					fldRecs [i].MangledTypeNamePtr.Target = fldType;
					fldType = swiftFields [i].SwiftType.WriteMangledType (fldType);
				}
				var fldNamePtr = (IntPtr)fldType;
				for (var i = 0; i < swiftFields.Length; i++) {
					var fldName = Encoding.UTF8.GetBytes (swiftFields [i].Field.Name);
					fldRecs [i].FieldNamePtr.Target = (void*)fldNamePtr;
					Marshal.Copy (fldName, 0, fldNamePtr, fldName.Length);
					Marshal.WriteByte (fldNamePtr, fldName.Length, 0);
					fldNamePtr += fldName.Length + 1;
				}
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

			var bodySwiftType = ViewType.Of (BodyProperty.PropertyType)!;
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
		static void Body (void* dest, void* dataPtr)
		{
			var data = (CustomViewData*)dataPtr;
			var view = (View)GCHandle.FromIntPtr (data->GcHandleToView).Target;

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
