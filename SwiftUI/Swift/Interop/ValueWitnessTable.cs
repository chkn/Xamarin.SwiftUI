using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	[Flags]
	public enum ValueWitnessFlags
	{
		AlignmentMask = 0x0000FFFF,
		IsNonPOD = 0x00010000,
		IsNonInline = 0x00020000,
		HasExtraInhabitants = 0x00040000,
		HasSpareBits = 0x00080000,
		IsNonBitwiseTakable = 0x00100000,
		HasEnumWitnesses = 0x00200000
	}

	/// <summary>
	/// See https://github.com/apple/swift/blob/master/include/swift/ABI/ValueWitness.def
	/// </summary>
	[StructLayout (LayoutKind.Sequential)]
	public struct ValueWitnessTable
	{
		///   T *(*initializeBufferWithCopyOfBuffer)(B *dest, B *src, M *self);
		/// Given an invalid buffer, initialize it as a copy of the
		/// object in the source buffer.
		public IntPtr InitBufferWithCopy;

		///   void (*destroy)(T *object, witness_t *self);
		///
		/// Given a valid object of this type, destroy it, leaving it as an
		/// invalid object.  This is useful when generically destroying
		/// an object which has been allocated in-line, such as an array,
		/// struct, or tuple element.
		public IntPtr Destroy;

		///   T *(*initializeWithCopy)(T *dest, T *src, M *self);
		///
		/// Given an invalid object of this type, initialize it as a copy of
		/// the source object.  Returns the dest object.
		public IntPtr InitWithCopy;

		///   T *(*assignWithCopy)(T *dest, T *src, M *self);
		///
		/// Given a valid object of this type, change it to be a copy of the
		/// source object.  Returns the dest object.
		public IntPtr AssignWithCopy;

		///   T *(*initializeWithTake)(T *dest, T *src, M *self);
		///
		/// Given an invalid object of this type, initialize it by taking
		/// the value of the source object.  The source object becomes
		/// invalid.  Returns the dest object.
		public IntPtr InitWithTake;

		///   T *(*assignWithTake)(T *dest, T *src, M *self);
		///
		/// Given a valid object of this type, change it to be a copy of the
		/// source object.  The source object becomes invalid.  Returns the
		/// dest object.
		public IntPtr AssignWithTake;

		/// unsigned (*getEnumTagSinglePayload)(const T* enum, UINT_TYPE emptyCases)
		/// Given an instance of valid single payload enum with a payload of this
		/// witness table's type (e.g Optional&lt;ThisType&gt;) , get the tag of the enum.
		public IntPtr GetEnumTagSinglePayload;

		/// void (*storeEnumTagSinglePayload)(T* enum, UINT_TYPE whichCase,
		///                                   UINT_TYPE emptyCases)
		/// Given uninitialized memory for an instance of a single payload enum with a
		/// payload of this witness table's type (e.g Optional&lt;ThisType&gt;), store the
		/// tag.
		public IntPtr StoreEnumTagSinglePayload;

		///   SIZE_TYPE size;
		///
		/// The required storage size of a single object of this type.
		public IntPtr Size;

		///   SIZE_TYPE stride;
		///
		/// The required size per element of an array of this type. It is at least
		/// one, even for zero-sized types, like the empty tuple.
		public IntPtr Stride;

		public ValueWitnessFlags Flags;

		public int Alignment => (int)((Flags & ValueWitnessFlags.AlignmentMask) + 1);

		public bool IsNonPOD => Flags.HasFlag (ValueWitnessFlags.IsNonPOD);

		public bool IsNonBitwiseTakable => Flags.HasFlag (ValueWitnessFlags.IsNonBitwiseTakable);
	}
}
