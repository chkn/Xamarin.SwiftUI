using System;
using System.Linq;
using System.Collections.Generic;

namespace Swift.Interop
{
	using static SwiftCoreLib;

	public unsafe class SwiftTupleType : SwiftType
	{
		readonly SwiftType [] elementTypes;

		public new TupleTypeMetadata* Metadata => (TupleTypeMetadata*)base.Metadata;

		internal override int MangledTypeTrailingPointers
			=> elementTypes.Sum (et => et.MangledTypeTrailingPointers);

		internal override int MangledTypeSizeInner
			=> elementTypes.Sum (et => et.MangledTypeSizeInner) + 2; // '_' (or 'y') and 't'

		SwiftTupleType (SwiftType [] elementTypes, TypeMetadata** elts, ushort len)
			: base (Lib, GetTupleType (0, new TupleTypeFlags (len), elts, null, null))
		{
			this.elementTypes = elementTypes ?? throw new ArgumentNullException (nameof (elementTypes));
		}

		public static SwiftType? Of (Type [] elementTypes, Nullability nullability = default)
		{
			var len = checked((ushort)elementTypes.Length);
			switch (len) {
			case 0:
				return new SwiftType (Lib, "yt");
			case 1:
				// Swift just treats 1-ples as a single value
				return Of (elementTypes [0], nullability [0]);
			default:
				var elems = new SwiftType [len];
				var elts = stackalloc TypeMetadata* [len];
				for (var i = 0; i < len; i++) {
					var el = Of (elementTypes [i], nullability [i]);
					if (el is null)
						return null;
					elems [i] = el;
					elts [i] = el.Metadata;
				}
				return new SwiftTupleType (elems, elts, len);
			}
		}

		internal override unsafe byte* WriteMangledType (byte* dest, void** tpBase, List<IntPtr> trailingPtrs)
		{
			switch (elementTypes.Length) {

			case 0:
				*dest = (byte)'y';
				dest++;
				break;
			default:
				dest = elementTypes [0].WriteMangledType (dest, tpBase, trailingPtrs);
				*dest = (byte)'_';
				dest++;
				for (var i = 1; i < elementTypes.Length; i++)
					dest = elementTypes [i].WriteMangledType (dest, tpBase, trailingPtrs);
				break;
			}
			*dest = (byte)'t';
			dest++;

			return dest;
		}
	}
}
