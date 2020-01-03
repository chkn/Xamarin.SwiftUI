using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using SwiftUI.Interop;

namespace Swift.Interop
{
	using static TransferFuncType;

	/// <summary>
	/// See https://github.com/apple/swift/blob/master/docs/ABI/Mangling.rst#types
	/// </summary>
	public enum SwiftTypeCode
	{
		Class = 'C',     // nominal class type
		Enum = 'O',      // nominal enum type
		Struct = 'V',    // nominal struct type
	};

	public enum TransferFuncType
	{
		InitWithCopy,
		AssignWithCopy,
		InitWithTake,
		AssignWithTake
	};

	public unsafe class SwiftType
	{
		protected FullTypeMetadata* fullMetadata;

		// non-null for external types where we have a simple (non-generic) mangled name
		readonly string? mangledName;

		readonly SwiftType []? genericArgs; // null if empty

		// Delegates from the value witness table...
		DestroyFunc? _destroy;
		TransferFunc? _copyInit;
		TransferFunc? _moveInit;

		TransferFunc GetTransferFunc (TransferFuncType type)
			=> type switch
			{
				InitWithCopy => _copyInit ??= Marshal.GetDelegateForFunctionPointer<TransferFunc> (ValueWitnessTable->InitWithCopy),
				InitWithTake => _moveInit ??= Marshal.GetDelegateForFunctionPointer<TransferFunc> (ValueWitnessTable->InitWithTake),
				_ => throw new NotImplementedException (type.ToString ())
			};

		public TypeMetadata* Metadata => &fullMetadata->Metadata;

		public ValueWitnessTable* ValueWitnessTable => fullMetadata->ValueWitnessTable;

		public virtual int NativeDataSize => checked ((int)ValueWitnessTable->Size);

		internal int MangledTypeTrailingPointers =>
			(mangledName is null ? 1 : 0) + (genericArgs?.Sum (ga => ga.MangledTypeTrailingPointers) ?? 0);

		// size without null termination or trailing pointers
		int MangledTypeSizeInner =>
			(mangledName?.Length ?? sizeof (SymbolicReference)) +
			(genericArgs?.Sum (ga => ga.MangledTypeSizeInner) + 2 ?? 0); // +2 for 'y' and 'G'

		/// <summary>
		/// Gets the number of bytes in the mangled type of this <see cref="SwiftType"/>.
		///  This may include trailing pointers.
		/// </summary>
		public int MangledTypeSize =>
			MangledTypeSizeInner +
			IntPtr.Size * MangledTypeTrailingPointers + // FIXME: Make sure trailing ptrs are aligned?
			1; // null terminated

		byte* WriteMangledType (byte* dest, void** tpBase, List<IntPtr> trailingPtrs)
		{
			if (mangledName is null) {
				var symRef = (SymbolicReference*)dest;
				symRef->Kind = SymbolicReferenceKind.IndirectContext;
				symRef->Pointer.Target = tpBase + trailingPtrs.Count;
				trailingPtrs.Add ((IntPtr)Metadata->TypeDescriptor);
				dest += sizeof (SymbolicReference);
			} else {
				fixed (char* chars = mangledName)
					dest += Encoding.ASCII.GetBytes (chars, mangledName.Length, dest, mangledName.Length);
			}
			if (genericArgs != null) {
				*dest = (byte)'y';
				dest++;
				foreach (var genArg in genericArgs) 
					dest = genArg.WriteMangledType (dest, tpBase, trailingPtrs);
				*dest = (byte)'G';
				dest++;
			}
			return dest;
		}

		/// <summary>
		/// Writes the mangled type of this <see cref="SwiftType"/> to the given destination.
		/// </summary>
		/// <remarks>
		/// The given destination must be allocated with at least <see cref="MangledTypeSize"/>
		///  in bytes.
		/// </remarks>
		public byte* WriteMangledType (byte* dest)
		{
			var trailingPtrs = new List<IntPtr> ();
			var tpBase = dest + MangledTypeSizeInner + 1; // +1 for null

			dest = WriteMangledType (dest, (void**)tpBase, trailingPtrs);
			*dest = 0; // null terminated
			dest++;
			Debug.Assert (dest == tpBase);

			// Write trailing pointers
			var ptr = (IntPtr)dest;
			foreach (var tp in trailingPtrs) {
				Marshal.WriteIntPtr (ptr, tp);
				ptr += IntPtr.Size;
			}

			return (byte*)ptr;
		}

		// Only used by CustomViewType
		private protected SwiftType ()
		{
		}

		public SwiftType (IntPtr typeMetadata, Type? managedType = null, SwiftType []? genericArgs = null)
		{
			this.fullMetadata = (FullTypeMetadata*)(typeMetadata - IntPtr.Size);
			this.genericArgs = genericArgs;

			// Assert assumed invariants..
			Debug.Assert (!ValueWitnessTable->IsNonBitwiseTakable, $"expected bitwise movable: {managedType?.Name}");
			if (managedType is null)
				return;
			checked {
				Debug.Assert (Metadata->TypeDescriptor->Name == GetSwiftTypeName (managedType), $"unexpected name: {Metadata->TypeDescriptor->Name}");
				Debug.Assert (Metadata->Kind == MetadataKind.OfType (managedType), $"unexpected kind: {Metadata->Kind}");
				Debug.Assert (!managedType.IsValueType || (int)ValueWitnessTable->Size == Marshal.SizeOf (managedType), $"unexpected size: {ValueWitnessTable->Size}");
			}
		}

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type from a native library.
		/// </summary>
		public SwiftType (NativeLib lib, string mangledName, Type? managedType = null)
			: this (lib.RequireSymbol ("$s" + mangledName + "N"), managedType)
		{
			this.mangledName = mangledName;
		}

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type with a simple mangling from a native library.
		/// </summary>
		public SwiftType (NativeLib lib, string module, string name, SwiftTypeCode code, Type? managedType = null)
			: this (lib, Mangle (module, name, code), managedType)
		{
		}

		public SwiftType (NativeLib lib, Type managedType)
			: this (lib, managedType.Namespace, GetSwiftTypeName (managedType), GetSwiftTypeCode (managedType), managedType)
		{
		}

		/// <summary>
		/// Returns the <see cref="SwiftType"/> of the given <see cref="Type"/>.
		/// </summary>
		/// <remarks>
		/// By convention, types that are exposed to Swift must have a public static SwiftType property.
		/// </remarks>
		//
		// Sync with SwiftValue.ToSwiftValue
		public static SwiftType? Of (Type type)
			=> Type.GetTypeCode (type) switch
			{
				TypeCode.String => SwiftCoreLib.Types.String,
				TypeCode.Byte => SwiftCoreLib.Types.Int8,
				TypeCode.Int16 => SwiftCoreLib.Types.Int16,
				TypeCode.Int32 => SwiftCoreLib.Types.Int32,
				// FIXME: ...
				_ => type.GetProperty ("SwiftType", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue (null) as SwiftType
				  ?? CustomViewType.Of (type)
			};

		internal static string Mangle (string module, string name)
			=> (module == "Swift" ? "s" : module.Length + module) + name.Length + name;

		internal static string Mangle (string module, string name, SwiftTypeCode code)
			=> Mangle (module, name) + ((char) code);

		internal static string GetSwiftTypeName (Type ty)
		{
			if (ty.IsConstructedGenericType)
				ty = ty.GetGenericTypeDefinition ();
			if (ty.IsGenericTypeDefinition) {
				// strip off the "`N" from the end of the type name
				var name = ty.Name;
				return name.Substring (0, name.Length - 2);
			}
			return ty.Name;
		}

		internal static SwiftTypeCode GetSwiftTypeCode (Type ty)
		{
			if (ty.IsClass) return SwiftTypeCode.Class;
			if (ty.IsValueType) return SwiftTypeCode.Struct;
			throw new NotSupportedException (ty.FullName);
		}

		public virtual ProtocolWitnessTable* GetProtocolConformance (ProtocolDescriptor* descriptor)
		{
			if (descriptor == null)
				return null;

			return SwiftCoreLib.GetProtocolConformance (Metadata, descriptor);
		}

#if DEBUG_TOSTRING
		public override string ToString ()
			=> Metadata->ToString ();
#endif

		internal virtual void Transfer (void* dest, void* src, TransferFuncType funcType)
		{
			var witness = ValueWitnessTable;
			if (witness->IsNonPOD) {
				// In this case, one or more fields is a reference-counted reference,
				//  so we need to make sure the proper references are incremented
				var func = GetTransferFunc (funcType);
				func (dest, src, Metadata);
			} else {
				var bytes = (long)witness->Size;
				Buffer.MemoryCopy (src, dest, bytes, bytes);
			}
		}
		internal T Transfer<T> (in T src, TransferFuncType funcType) where T : unmanaged
		{
			T result;
			if (ValueWitnessTable->IsNonPOD) {
				fixed (void* srcPtr = &src)
					Transfer (&result, srcPtr, funcType);
			} else {
				result = src;
			}
			return result;
		}

		internal void Destroy (void* data)
		{
			var witness = ValueWitnessTable;
			if (witness->IsNonPOD) {
				// In this case, one or more fields of `data` is a reference counted reference
				//  so we need to make sure the proper references are decremented
				if (_destroy is null)
					_destroy = Marshal.GetDelegateForFunctionPointer<DestroyFunc> (witness->Destroy);
				_destroy (data, Metadata);
			}
			// (no action needed for POD)
		}

		internal void Destroy<T> (in T data) where T : unmanaged
		{
			fixed (void* ptr = &data)
				Destroy (ptr);
		}
	}
}
