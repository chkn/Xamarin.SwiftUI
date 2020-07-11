using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Swift.Interop;

namespace Swift
{
	static unsafe class Optional
	{
		const int EmptyCases = 1;

		public enum Tag
		{
			Some = 0,
			None = 1
		}

		public static void StoreTag (this SwiftType wrappedType, void* ptr, Tag tag)
			=> wrappedType.StoreEnumTagSinglePayload (ptr, (int)tag, EmptyCases);

		internal static SwiftHandle Wrap (void* src, SwiftType optionalType, SwiftType wrappedType)
		{
			var data = new byte [optionalType.NativeDataSize];
			fixed (void* dest = &data [0]) {
				var tag = Tag.None;
				if (src != null) {
					tag = Tag.Some;
					wrappedType.Transfer (dest, src, TransferFuncType.InitWithCopy);
				}
				wrappedType.StoreTag (dest, tag);
			}
			return new SwiftHandle (data, optionalType, destroyOnDispose: true);
		}

		/// <summary>
		/// Represents a <c>Swift.Optional</c> value of <typeparamref name="T"/>
		///  where the binary representation of type <typeparamref name="T"/>
		///  contains an extra inhabitant and therefore the native size of
		///  <c>Optional&lt;<typeparamref name="T"/>&gt;</c> is the same as
		///  the native size of <typeparamref name="T"/>.
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public readonly struct Packed<T> : ISwiftBlittableStruct<Packed<T>>
			where T : unmanaged
		{
			readonly T value;

			// FIXME: Account for the case where T might have nullable type arguments?
			static SwiftType UnderlyingSwiftType => SwiftType.Of (typeof (T))!;

			public static Packed<T> None {
				get {
					var packed = default (Packed<T>);
					UnderlyingSwiftType.StoreTag (&packed, Tag.None);
					return packed;
				}
			}

			public static Packed<T> Some (T value)
			{
				var packed = new Packed<T> (value);
				UnderlyingSwiftType.StoreTag (&packed, Tag.Some);
				return packed;
			}

			Packed (T value) => this.value = value;

			public static implicit operator Packed<T> (T? value)
				=> value.HasValue? Some (value.Value) : None;

			#if DEBUG
			static Packed ()
			{
				unsafe {
					Debug.Assert (UnderlyingSwiftType.ValueWitnessTable->HasExtraInhabitants,
						$"Type {nameof (T)} has no extra inhabitants; use {nameof (Unpacked<T>)} instead");
					Debug.Assert ((int)SwiftType.Of (typeof (T), new Nullability (true))!.ValueWitnessTable->Size == Marshal.SizeOf<T> ());
				}
			}
			#endif
		}

		/// <summary>
		/// Represents a <c>Swift.Optional</c> value of <typeparamref name="T"/>
		///  where the binary representation of type <typeparamref name="T"/>
		///  contains no extra inhabitants, and therefore extra bits are added
		///  for the tag.
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public readonly struct Unpacked<T> : ISwiftBlittableStruct<Unpacked<T>>
			where T : unmanaged
		{
			readonly T value;
			readonly byte extraBits;

			// FIXME: Account for the case where T might have nullable type arguments?
			static SwiftType UnderlyingSwiftType => SwiftType.Of (typeof (T))!;

			public static Unpacked<T> None {
				get {
					var unpacked = default (Unpacked<T>);
					UnderlyingSwiftType.StoreTag (&unpacked, Tag.None);
					return unpacked;
				}
			}

			public static Unpacked<T> Some (T value)
			{
				var unpacked = new Unpacked<T> (value);
				UnderlyingSwiftType.StoreTag (&unpacked, Tag.Some);
				return unpacked;
			}

			Unpacked (T value)
			{
				this.value = value;
				this.extraBits = 0;
			}

			public static implicit operator Unpacked<T> (T? value)
				=> value.HasValue? Some (value.Value) : None;

			#if DEBUG
			static Unpacked ()
			{
				unsafe {
					Debug.Assert (!UnderlyingSwiftType.ValueWitnessTable->HasExtraInhabitants,
						$"Type {nameof (T)} has extra inhabitants; use {nameof (Packed<T>)} instead");
					Debug.Assert ((int)SwiftType.Of (typeof (T), new Nullability (true))!.ValueWitnessTable->Size == Marshal.SizeOf<T> () + 1);
				}
			}
			#endif
		}
	}
}
