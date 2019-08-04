using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SwiftUI.Interop
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
		readonly NativeLib lib;

		public TypeMetadata* Metadata { get; protected set; }

		public ValueWitnessTable* ValueWitnessTable => *((ValueWitnessTable**)Metadata - 1);

		public SwiftType (NativeLib lib, string module, string name, SwiftTypeCode code = SwiftTypeCode.Struct)
			: this (lib, MangleTypeName (module, name) + ((char)code))
		{
		}

		public SwiftType (NativeLib lib, string mangledName)
		{
			this.lib = lib;
			Metadata = (TypeMetadata*)lib.RequireSymbol ("$s" + mangledName + "N");

			#if DEBUG
			#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
			AssertAssumedInvariants ();
			#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
			#endif
		}

		protected SwiftType (TypeMetadata* metadata)
		{
			Metadata = metadata;
		}

		#if DEBUG
		protected virtual void AssertAssumedInvariants ()
		{
			// FIXME: Support non-bitwise-takable types? (See comments in Move and Copy methods below..)
			Debug.Assert (!ValueWitnessTable->IsNonBitwiseTakable, "expected bitwise movable");
		}
		#endif

		public virtual IntPtr GetProtocolConformance (string module, string name)
		{
			var descriptor = lib.TryGetSymbol ("$s" + MangleTypeName (module, name) + "Mp");
			if (descriptor == IntPtr.Zero)
				return IntPtr.Zero;

			return SwiftLib.GetProtocolConformance (Metadata, descriptor);
		}

		internal static string MangleTypeName (string module, string name)
			=> (module == "Swift" ? "s" : module.Length + module) + name.Length + name;
	}

	public unsafe class SwiftType<T> : SwiftType, IDisposable
		where T : unmanaged, ISwiftValue<T>
	{
		DestroyFunc _destroy;
		TransferFunc _copyInit;
		//TransferFunc _moveInit;

		public SwiftType (NativeLib lib, string module, string name, SwiftTypeCode code = SwiftTypeCode.Struct)
			: base (lib, module, name, code)
		{
		}

		public SwiftType (NativeLib lib, string mangledName)
			: base (lib, mangledName)
		{
		}
		/*
		public SwiftType ()
		{

		}
		*/
		#if DEBUG
		protected override void AssertAssumedInvariants ()
		{
			base.AssertAssumedInvariants ();
			var ty = typeof (T);
			checked {
				Debug.Assert (Metadata->TypeDescriptor->Name == ty.Name); //, $"unexpected name: {Metadata->TypeDescriptor->Name}");
				Debug.Assert (Metadata->Kind == MetadataKind.OfType (ty)); //, $"unexpected kind: {Metadata->Kind}");
				Debug.Assert ((int)ValueWitnessTable->Size == Marshal.SizeOf<T> ()); //, $"unexpected size: {ValueWitnessTable->Size}");
			}
		}
		#endif

		// FIXME: Can't return the value for non-bitwise-takable data (implicit move here, see below)
		internal T Copy (in T src)
		{
			T result;
			var witness = ValueWitnessTable;
			if (witness->IsNonPOD) {
				// In this case, one or more fields of `data` is a reference-counted reference,
				//  so we need to make sure the proper references are incremented
				if (_copyInit is null)
					_copyInit = Marshal.GetDelegateForFunctionPointer<TransferFunc> (witness->InitWithCopy);
				fixed (void* srcPtr = &src)
					_copyInit (&result, srcPtr, Metadata);
			} else {
				result = src;
			}
			return result;
		}

		// FIXME: If we ever need to support non-bitwise-takable types,
		//  we'll need something like this Move method, and we'd need to
		//  verify that all non-bitwise-takable data is pinned!!!
		/*
		internal void Move (in T src, out T dest)
		{
			var witness = ValueWitnessTable;

			if (witness->IsNonBitwiseTakable) {
				// In this case, Swift has some pointer somewhere pointing to this data,
				//  so we need to tell it where we're moving it..
				if (_moveInit is null)
					_moveInit = Marshal.GetDelegateForFunctionPointer<TransferFunc> (witness->InitWithTake);
				fixed (void* srcPtr = &src)
				fixed (void* destPtr = &dest)
					_moveInit (destPtr, srcPtr, Metadata);
			} else {
				// Bitwise takable is a simple assignment
				dest = src;
			}
		}
		*/

		internal void Destroy (in T data)
		{
			var witness = ValueWitnessTable;
			if (witness->IsNonPOD) {
				// In this case, one or more fields of `data` is a reference counted reference
				//  so we need to make sure the proper references are decremented
				if (_destroy is null)
					_destroy = Marshal.GetDelegateForFunctionPointer<DestroyFunc> (witness->Destroy);
				fixed (void* ptr = &data)
					_destroy (ptr, Metadata);
			}
			// (no action needed for POD)
		}

		#region IDisposable Support
		// This is for releasing allocated type metadata for Swift types declared in managed code.

		protected virtual void Dispose (bool disposing)
		{
			_destroy = null;
			_copyInit = null;
			if (disposing) {
				// TODO: dispose managed state (managed objects).
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~SwiftType()
		// {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose ()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose (true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
