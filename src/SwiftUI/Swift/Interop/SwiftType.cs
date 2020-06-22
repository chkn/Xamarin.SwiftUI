using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

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

	public static class TransferFuncTypeEx
	{
		public static bool IsCopy (this TransferFuncType tft)
		{
			switch (tft) {
			case InitWithCopy:
			case AssignWithCopy:
				return true;
			}
			return false;
		}

		public static bool IsAssign (this TransferFuncType tft)
		{
			switch (tft) {
			case AssignWithCopy:
			case AssignWithTake:
				return true;
			}
			return false;
		}
	}

	public unsafe class SwiftType
	{
		protected FullTypeMetadata* fullMetadata;

		// non-null for external types where we have a simple (non-generic) mangled name
		readonly string? mangledName;

		readonly SwiftType []? genericArgs; // null if empty

		// only holding on to this to keep it from being finalized
		readonly NativeLib? lib;

		// Delegates from the value witness table...
		DestroyFunc? _destroy;
		TransferFunc? _copyInit;
		TransferFunc? _moveInit;
		TransferFunc? _copyAssign;
		GetEnumTagSinglePayloadFunc? _getEnumTagSinglePayload;
		StoreEnumTagSinglePayloadFunc? _storeEnumTagSinglePayload;

		TransferFunc GetTransferFunc (TransferFuncType type)
			=> type switch
			{
				InitWithCopy => _copyInit ??= Marshal.GetDelegateForFunctionPointer<TransferFunc> (ValueWitnessTable->InitWithCopy),
				InitWithTake => _moveInit ??= Marshal.GetDelegateForFunctionPointer<TransferFunc> (ValueWitnessTable->InitWithTake),
				AssignWithCopy => _copyAssign ??= Marshal.GetDelegateForFunctionPointer<TransferFunc> (ValueWitnessTable->AssignWithCopy),
				_ => throw new NotImplementedException (type.ToString ())
			};

		public TypeMetadata* Metadata => &fullMetadata->Metadata;

		public ValueWitnessTable* ValueWitnessTable => fullMetadata->ValueWitnessTable;

		public virtual int NativeDataSize => checked ((int)ValueWitnessTable->Size);

		internal IReadOnlyList<SwiftType>? GenericArguments => genericArgs;

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

		// Only used by ManagedSwiftType
		private protected SwiftType ()
		{
		}

		// Using IntPtr typeMetadata arg here instead of FullTypeMetadata* so it's easier
		//  to just pass the result of lib.RequireSymbol
		public SwiftType (NativeLib? lib, IntPtr typeMetadata, Type? managedType = null, SwiftType []? genericArgs = null)
		{
			this.lib = lib;
			this.fullMetadata = (FullTypeMetadata*)(typeMetadata - IntPtr.Size);
			this.genericArgs = genericArgs;

			// Assert assumed invariants..
			Debug.Assert (!ValueWitnessTable->IsNonBitwiseTakable, $"expected bitwise movable: {managedType?.Name}");
			SanityCheck (managedType);
		}

		[Conditional ("DEBUG")]
		internal void SanityCheck (Type? managedType)
		{
			if (managedType is null)
				return;
			checked {
				Debug.Assert (Metadata->TypeDescriptor->Name == GetSwiftTypeName (managedType), $"unexpected name: {Metadata->TypeDescriptor->Name}");
				Debug.Assert (Metadata->Kind == MetadataKind.OfType (managedType), $"unexpected kind: {Metadata->Kind}");
				Debug.Assert (!managedType.IsValueType || (int)ValueWitnessTable->Size == Marshal.SizeOf (managedType), $"unexpected size: {ValueWitnessTable->Size}");
				// We should think hard before making a non-POD Swift struct a public C# struct
				//  (Swift.String is internal and we are careful to manage its lifetime)
				Debug.Assert (!managedType.IsValueType || !managedType.IsPublic || !ValueWitnessTable->IsNonPOD, "expected POD");
			}
		}

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type from a native library.
		/// </summary>
		/// <param name="mangledName">The mangled name of the Swift type.</param>
		public SwiftType (NativeLib lib, string mangledName, Type? managedType = null)
			: this (lib, lib.RequireSymbol ("$s" + NormalizeMangledName (mangledName) + "N"), managedType)
		{
			this.mangledName = NormalizeMangledName (mangledName);
		}

		/// <summary>
		/// Creates a new <see cref="SwiftType"/> that references a type with a simple mangling from a native library.
		/// </summary>
		public SwiftType (NativeLib lib, string module, string name, SwiftTypeCode code, Type? managedType = null)
			: this (lib, Mangle (module, name, code), managedType)
		{
		}

		public SwiftType (NativeLib lib, Type managedType)
			: this (lib, Mangle (managedType), managedType)
		{
		}

		// lock!
		static readonly ConditionalWeakTable<Type, SwiftType> registry = new ConditionalWeakTable<Type, SwiftType> ();

		/// <summary>
		/// Returns the <see cref="SwiftType"/> of the given <see cref="Type"/>.
		/// </summary>
		/// <remarks>
		/// Types that are exposed to Swift are attributed with a <see cref="SwiftTypeAttribute"/>.
		/// </remarks>
		//
		// Sync with SwiftValue.ToSwiftValue
		public static SwiftType? Of (Type type, Nullability? givenNullability = default)
		{
			SwiftType? result;
			var nullability = givenNullability ?? Nullability.Of (type);
			lock (registry) {
				if (!registry.TryGetValue (type, out result)) {
					// First see if it is a core type
					result = SwiftCoreLib.GetSwiftType (type);

					// Otherwise, see if the type has an attribute that can provide a SwiftType
					if (result == null) {
						// First pass inherit: false to get a SwiftImport that overrides a different
						//  type of inherited attribute (e.g. CustomViewAttribute)
						var attr = type.GetCustomAttribute<SwiftTypeAttribute> (inherit: false) ??
						           type.GetCustomAttribute<SwiftTypeAttribute> (inherit: true);
						if (attr != null) {
							SwiftType []? typeArgs = null;
							if (type.IsGenericType) {
								var genericArgs = type.GenericTypeArguments;
								if (genericArgs.Length == 0)
									throw new ArgumentException ("Type is a generic type definition", nameof (type));

								typeArgs = new SwiftType [genericArgs.Length];
								for (var i = 0; i < genericArgs.Length; i++) {
									var argType = genericArgs [i];
									typeArgs [i] = Of (argType, nullability [i]) ??
										throw new UnknownSwiftTypeException (argType);
								}
							}
							result = attr.GetSwiftType (type, typeArgs);
						}
					}

					// Otherwise, see if there is some special handling for this type
					if (result == null) {
						// Special handling for tuples
						// FIXME: Treat F# Unit as 0-element tuple?
						if (typeof (ITuple).IsAssignableFrom (type)) {
							var args = type.GetGenericArguments ();
							result = SwiftCoreLib.GetTupleType (args, nullability);
						}

						// If it's a nullable type, try to unwrap it
						//  Only handle reified nullables here because we are caching this result
						else if (Nullability.IsReifiedNullable (type)) {
							//  Nullable types -> Swift optional
							var underlyingType = Nullability.GetUnderlyingType (type);
							var underlyingSwiftType = SwiftType.Of (underlyingType, nullability.Strip ());
							if (underlyingSwiftType != null)
								result = SwiftCoreLib.GetOptionalType (underlyingSwiftType);
						}
					}
					if (result != null)
						registry.Add (type, result);
				}
			}
			// If it's a non-reified nullable, also wrap the type in an Optional
			//  (but note that we won't cache this result)
			if (result != null && nullability.IsNullable && !Nullability.IsReifiedNullable (type)) {
				result = SwiftCoreLib.GetOptionalType (result);
			}
			return result;
		}

		internal static string Mangle (Type managedType)
			=> Mangle (managedType.Namespace, GetSwiftTypeName (managedType), GetSwiftTypeCode (managedType));

		internal static string Mangle (string module, string name)
			=> (module == "Swift" ? "s" : module.Length + module) + name.Length + name;

		internal static string Mangle (string module, string name, SwiftTypeCode code)
			=> Mangle (module, name) + ((char) code);

		static string NormalizeMangledName (string mangledName)
		{
			var ln1 = mangledName.Length - 1;
			var len = (mangledName [ln1] == 'N') ? ln1 : mangledName.Length;
			var offs = mangledName.StartsWith ("$s", StringComparison.Ordinal) ? 2 : 0;
			return mangledName.Substring (offs, len);
		}

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
			=> MetadataKind.OfType (ty) switch
			{
				MetadataKinds.Class => SwiftTypeCode.Class,
				MetadataKinds.Struct => SwiftTypeCode.Struct,
				_ => throw new NotSupportedException (ty.FullName)
			};

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

		protected internal virtual void Transfer (void* dest, void* src, TransferFuncType funcType)
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

		/// <summary>
		/// Gets the tag for an enum that has a single payload of this type.
		/// </summary>
		internal int GetEnumTagSinglePayload (void* ptr, int emptyCases)
		{
			if (_getEnumTagSinglePayload is null)
				_getEnumTagSinglePayload = Marshal.GetDelegateForFunctionPointer<GetEnumTagSinglePayloadFunc> (ValueWitnessTable->GetEnumTagSinglePayload);
			checked {
				return (int)_getEnumTagSinglePayload (ptr, (uint)emptyCases, Metadata);
			}
		}

		/// <summary>
		/// Stores the tag for an enum that has a single payload of this type.
		/// </summary>
		internal void StoreEnumTagSinglePayload (void* dest, int whichCase, int emptyCases)
		{
			if (_storeEnumTagSinglePayload is null)
				_storeEnumTagSinglePayload = Marshal.GetDelegateForFunctionPointer<StoreEnumTagSinglePayloadFunc> (ValueWitnessTable->StoreEnumTagSinglePayload);
			checked {
				_storeEnumTagSinglePayload (dest, (uint)whichCase, (uint)emptyCases, Metadata);
			}
		}

		protected internal virtual void Destroy (void* data)
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
