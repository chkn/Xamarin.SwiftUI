using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	/// <summary>
	/// See https://github.com/apple/swift/blob/master/docs/ABI/Mangling.rst#types
	/// </summary>
	public enum SwiftTypeCode {
		Class = 'C',     // nominal class type
		Enum = 'O',      // nominal enum type
		Struct = 'V',    // nominal struct type
	};

	public unsafe class SwiftType
	{
		readonly NativeLib? lib;

		// Delegates from the value witness table...
		DestroyFunc? _destroy;
		TransferFunc? _copyInit;
		//TransferFunc? _moveInit;

		public TypeMetadata* Metadata { get; protected set; }

		public ValueWitnessTable* ValueWitnessTable => *((ValueWitnessTable**)Metadata - 1);

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type from a native library.
		/// </summary>
		public SwiftType (NativeLib lib, string mangledName, Type? managedType = null)
		{
			this.lib = lib;
			Metadata = (TypeMetadata*)lib.RequireSymbol ("$s" + mangledName + "N");

			// Assert assumed invariants..
			Debug.Assert (!ValueWitnessTable->IsNonBitwiseTakable, $"expected bitwise movable: {mangledName}");
			if (managedType is null)
				return;
			checked {
				Debug.Assert (Metadata->TypeDescriptor->Name == managedType.Name, $"unexpected name: {Metadata->TypeDescriptor->Name}");
				Debug.Assert (Metadata->Kind == MetadataKind.OfType (managedType), $"unexpected kind: {Metadata->Kind}");
				Debug.Assert ((int)ValueWitnessTable->Size == Marshal.SizeOf (managedType), $"unexpected size: {ValueWitnessTable->Size}");
			}
		}

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type with a simple mangling from a native library.
		/// </summary>
		public SwiftType (NativeLib lib, string module, string name, Type? managedType = null,
						  SwiftTypeCode code = SwiftTypeCode.Struct)
			: this (lib, MangleTypeName (module, name) + ((char)code), managedType)
		{
		}

		/// <summary>
		/// Returns the <see cref="SwiftType"/> of the given <see cref="Type"/>.
		/// </summary>
		/// <remarks>
		/// By convention, types that are exposed to Swift must have a public static SwiftType property.
		/// </remarks>
		public static SwiftType? Of (Type type)
			=> type.GetProperty ("SwiftType", BindingFlags.Public | BindingFlags.Static)?.GetValue (null) as SwiftType;

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> for a managed type.
		/// </summary>
		private protected SwiftType (TypeMetadata* metadata)
		{
			Metadata = metadata;
		}

		internal static string MangleTypeName (string module, string name)
			=> (module == "Swift" ? "s" : module.Length + module) + name.Length + name;

		public virtual ProtocolWitnessTable* GetProtocolConformance (IntPtr descriptor)
		{
			if (lib is null || descriptor == IntPtr.Zero)
				return null;

			var conformance = SwiftCoreLib.GetProtocolConformance (Metadata, descriptor);
			Debug.Assert (conformance->ProtocolDescriptor == (ProtocolConformanceDescriptor*)descriptor);
			return conformance;
		}

#if DEBUG
		public override string ToString ()
			=> Metadata->ToString ();
#endif

		internal void Copy (void* dest, void* src)
		{
			var witness = ValueWitnessTable;
			if (witness->IsNonPOD) {
				// In this case, one or more fields is a reference-counted reference,
				//  so we need to make sure the proper references are incremented
				if (_copyInit is null)
					_copyInit = Marshal.GetDelegateForFunctionPointer<TransferFunc> (witness->InitWithCopy);
				_copyInit (dest, src, Metadata);
			} else {
				var bytes = (long)witness->Size;
				Buffer.MemoryCopy (src, dest, bytes, bytes);
			}
		}
		internal T Copy<T> (in T src) where T : unmanaged
		{
			T result;
			if (ValueWitnessTable->IsNonPOD) {
				fixed (void* srcPtr = &src)
					Copy (&result, srcPtr);
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
