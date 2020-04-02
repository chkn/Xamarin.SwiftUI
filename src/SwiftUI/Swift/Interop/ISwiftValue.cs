using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// We bind Swift values in a couple ways:
	//  1. ISwiftValue represents any wrapper type that can get a Handle (pointer) to the data.
	//  2. ISwiftBlittableStruct<T> is an ISwiftValue that represents a managed struct of
	//      type T that is itself the native data.

	/// <summary>
	/// A Swift struct or class type.
	/// </summary>
	public interface ISwiftValue
	{
		// by convention:
		//  FIXME: Make an actual requirement when/if we get
		//   https://github.com/Partydonk/roslyn/tree/dev/abock/asim/asim-playground
		// public static SwiftType SwiftType { get; }

		// should be implemented explicitly:
		SwiftType SwiftType { get; }

		/// <summary>
		/// Gets a <see cref="MemoryHandle"/> to the native data.
		/// </summary>
		/// <remarks>
		/// The returned <see cref="MemoryHandle"/> must be disposed when no longer needed.
		/// </remarks>
		MemoryHandle GetHandle ();

		/// <summary>
		/// Decrements any references to reference-counted data held by this
		///  <see cref="ISwiftValue"/>.
		/// </summary>
		/// <remarks>
		/// This interface doesn't implement <see cref="IDisposable"/> because that
		///  would cause F# to require the use of the <c>new</c> operator, which
		///  harshes our DSL. In cases where it is important, a finalizer will be
		///  provided that will automatically dispose of the object if you do not call
		///  <see cref="Dispose"/>. 
		/// </remarks>
		void Dispose ();
	}

	/// <summary>
	/// A Swift struct that is fully represented as a blittable managed struct.
	/// </summary>
	/// <typeparam name="T">The blittable managed struct type.
	///	  This must be the type that implements this interface.</typeparam>
	public interface ISwiftBlittableStruct<T> : ISwiftValue
		where T : unmanaged, ISwiftBlittableStruct<T>
	{
		/// <remarks>
		/// This is a fallback that should be avoided because it causes boxing.
		///  Methods that deal with Swift values should provide a generic overload
		///  for <see cref="ISwiftBlittableStruct{T}"/> that takes a pointer to the value
		///  directly (e.g. using a <c>fixed</c> statement), rather than calling
		///  this property.
		/// </remarks>
		unsafe MemoryHandle ISwiftValue.GetHandle ()
		{
			var gch = GCHandle.Alloc (this, GCHandleType.Pinned);
			return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
		}

		// Default implementation provided for POD only.
		void ISwiftValue.Dispose ()
		{
		}
	}

	// Sync with SwiftType.Of
	public static class SwiftValue
	{
		/// <summary>
		/// Returns an <see cref="ISwiftValue"/> representing the given object.
		/// </summary>
		/// <param name="obj">The managed object to bridge to Swift</param>
		/// <param name="asOptional"><c>true</c> to bridge the value to Swift
		///  as an Optional value. This is done regardless if <typeparamref name="T"/> is
		///  identified as a known nullable wrapper, such as <see cref="Nullable"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is
		///	 <c>null</c>, <typeparamref name="T"/> is not a known nullable wrapper type,
		///	 and <paramref name="asOptional"/> is <c>false</c>.</exception>
		/// <exception cref="ArgumentException">Thrown when the type <typeparamref name="T"/> cannot
		///  be directly bridged to Swift</exception>
		public static ISwiftValue ToSwiftValue<T> (this T obj, bool asOptional = false)
		{
			switch (obj) {
			case ISwiftValue swiftValue: return swiftValue;

			// FIXME: This might leak; we should maybe box this into a custom box that
			//  has a finalizer?
			case string val: return new Swift.String (val);
			}

			var type = typeof (T);
			var swiftType = SwiftType.Of (type);
			if (swiftType is null)
				throw new ArgumentException ($"Type '{type}' cannot be bridged to Swift");

			// Nullable types are bridged to Swift optionals
			if (asOptional || type.IsNullable ()) {
				var underlyingType = type.GetNullableUnderlyingType ();
				var underlyingSwiftType = SwiftType.Of (underlyingType);
				if (underlyingSwiftType is null)
					throw new ArgumentException ($"Type '{underlyingType}' cannot be bridged to Swift");

				// FIXME: Handle non-POD types
				return new OptionalPOD (obj!, swiftType, underlyingSwiftType);
			} else if (obj is null) {
				throw new ArgumentNullException (nameof (obj));
			}

			return new POD (obj, swiftType);
		}

		public static TSwiftValue FromNative<TSwiftValue> (IntPtr ptr)
		{
			// FIXME: Handle nullables

			var ty = typeof (TSwiftValue);
			if (ty.IsValueType)
				return Marshal.PtrToStructure<TSwiftValue> (ptr);

			return (TSwiftValue)Activator.CreateInstance (typeof (TSwiftValue), ptr);
		}

		/// <summary>
		/// A wrapper for POD types to expose them as <see cref="ISwiftValue"/>s.
		/// </summary>
		class POD : ISwiftValue
		{
			public SwiftType SwiftType { get; }

			object value;

			public unsafe MemoryHandle GetHandle ()
			{
				var gch = GCHandle.Alloc (value, GCHandleType.Pinned);
				return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
			}

			public POD (object value, SwiftType swiftType)
			{
				this.value = value;
				SwiftType = swiftType;
				unsafe {
					Debug.Assert (!swiftType.ValueWitnessTable->IsNonPOD);
				}
			}

			public void Dispose ()
			{
				// nop, since this is POD
			}
		}

		unsafe class OptionalPOD : ISwiftValue
		{
			public SwiftType SwiftType { get; }

			byte [] data;

			public MemoryHandle GetHandle ()
			{
				var gch = GCHandle.Alloc (data, GCHandleType.Pinned);
				return new MemoryHandle ((void*)gch.AddrOfPinnedObject (), gch);
			}

			public OptionalPOD (object value, SwiftType swiftType, SwiftType wrappedSwiftType)
			{
				SwiftType = swiftType;
				Debug.Assert (!swiftType.ValueWitnessTable->IsNonPOD);

				data = new byte [swiftType.NativeDataSize];
				fixed (void* ptr = &data[0]) {
					var tag = 1; // nil
					if (value != null) {
						tag = 0;
						Marshal.StructureToPtr (value, (IntPtr)ptr, false);
					}
					wrappedSwiftType.StoreEnumTagSinglePayload (ptr, tag, 1);
				}
			}

			public void Dispose ()
			{
				// nop, since this is POD
			}
		}
	}
}
