using System;
using System.Runtime.InteropServices;

namespace Swift.Interop
{
	// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/ABI/MetadataValues.h#L1091
	/// Kinds of context descriptor.
	public enum ContextDescriptorKind : byte
	{
		/// This context descriptor represents a module.
		Module = 0,

		/// This context descriptor represents an extension.
		Extension = 1,

		/// This context descriptor represents an anonymous possibly-generic context
		/// such as a function body.
		Anonymous = 2,

		/// This context descriptor represents a protocol context.
		Protocol = 3,

		/// This context descriptor represents an opaque type alias.
		OpaqueType = 4,

		/// First kind that represents a type of any sort.
		Type_First = 16,

		/// This context descriptor represents a class.
		Class = Type_First,

		/// This context descriptor represents a struct.
		Struct = Type_First + 1,

		/// This context descriptor represents an enum.
		Enum = Type_First + 2,

		/// Last kind that represents a type of any sort.
		Type_Last = 31,
	};

	[StructLayout (LayoutKind.Sequential)]
	public struct ContextDescriptorFlags
	{
		public uint Value;

		/// <summary>
		/// The kind of context this descriptor describes.
		/// </summary>
		public ContextDescriptorKind Kind {
			get => (ContextDescriptorKind)(Value & 0x1Fu);
			set => Value = (Value & 0xFFFFFFE0u) | (byte)value;
		}

		/// <summary>
		/// Whether this is a unique record describing the referenced context.
		/// </summary>
		public bool IsUnique {
			get => (Value & 0x40u) != 0;
			set => Value = (Value & 0xFFFFFFBFu) | (value ? 0x40u : 0);
		}

		/// <summary>
		/// Whether the context being described is generic.
		/// </summary>
		public bool IsGeneric {
			get => (Value & 0x80u) != 0;
			set => Value = (Value & 0xFFFFFF7Fu) | (value? 0x80u : 0);
		}

#if DEBUG
		public override string ToString ()
			=> $"{{Kind = {Kind}, IsUnique = {IsUnique}, IsGeneric = {IsGeneric}}}";
#endif
	}
}
