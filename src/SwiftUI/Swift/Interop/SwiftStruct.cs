using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	unsafe interface ISwiftFieldExposable : ISwiftValue
	{
		/// <summary>
		/// Initializes the native data for this Swift type at the given location.
		/// </summary>
		void InitNativeData (void* handle);
	}

	/// <summary>
	/// A base class used to implement a Swift struct as a reference type.
	/// </summary>
	/// <remarks>
	/// It is possible to bind a Swift struct as a managed value type, but this
	///  is only safe in the case of POD types. Otherwise, we need to ensure that
	///  ownership is handled correctly, which we can only do reliably with a
	///  class that is finalizable, such as this one.
	/// <para/>
	/// There are other cases where Swift structs must be reflected as a managed class,
	///  such as when dealing with generic Swift structs that are not statically sized,
	///  when fields must be exposed in Swift metadata, or when the Swift struct is
	///  non-movable (<see cref="ValueWitnessTable.IsNonBitwiseTakable"/>).
	/// <para/>
	/// An instance of this class can own a single copy of the Swift struct data, but other
	///  copies may be created when passing the value to Swift. It is also possible for an
	///  instance of this class to not own its own data. This happens when the instance
	///  represents a field in a larger struct. In that case, the <see cref="SwiftStruct"/>
	///  instance representing the outer struct owns the data.
	/// <para/>
	/// When an instance of this class does own its data, the data is deinitialized and
	///  deallocated when this instance is disposed or finalized.
	/// </remarks>
	public unsafe abstract class SwiftStruct : ISwiftFieldExposable
	{
		protected virtual SwiftType SwiftType
			=> SwiftType.Of (GetType ()) ?? throw new UnknownSwiftTypeException (GetType ());

		// This is a tagged pointer that indicates whether we allocated the memory or not.
		private protected TaggedPointer data;

		protected bool NativeDataInitialized => data != null;

		// NOTE: If we ever change this implementation such that the returned SwiftHandle
		//  needs to be disposed, we must update NSHostingView and UIHostingView constructors.
		public SwiftHandle GetSwiftHandle ()
		{
			if (data == null) {
				data = TaggedPointer.AllocHGlobal (SwiftType.NativeDataSize);
				try {
					InitNativeData (data.Pointer);
				} catch {
					data = default;
					throw;
				}
			}
			return new SwiftHandle (data.Pointer, SwiftType);
		}

		void ISwiftFieldExposable.InitNativeData (void* handle)
		{
			if (NativeDataInitialized)
				throw new InvalidOperationException ();
			data = new TaggedPointer (handle, owned: false);
			InitNativeData (handle);
		}

		protected abstract void InitNativeData (void* handle);

		// HACK: When Swift calls us back (e.g. View.Body), we overwrite our
		//  native data array so we are operating on the new data...
		internal void OverwriteNativeData (void* newData)
		{
			var dest = data.Pointer;
			Debug.Assert (dest != null);
			SwiftType.Transfer (dest, newData, TransferFuncType.AssignWithCopy);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (data.IsOwned)
				SwiftType.Destroy (data.Pointer);
			data.Dispose ();
			GC.SuppressFinalize (this);
		}

		public void Dispose () => Dispose (true);
		~SwiftStruct () => Dispose (false);
	}
}
