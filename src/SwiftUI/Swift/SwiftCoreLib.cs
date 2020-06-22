using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Swift.Interop;

namespace Swift
{
	public unsafe static class SwiftCoreLib
	{
		public const string Path = "/usr/lib/swift/libswiftCore.dylib";

		// convenience
		static NativeLib Lib => NativeLib.Get (Path);

		#region Types
		// Special types like String, Double require the mangling from https://github.com/apple/swift/blob/master/docs/ABI/Mangling.rst#types
		// This usually take the form of starting with S with the KNOWN-TYPE-KIND letter from the above link. eg String is SS, Float64/Double is Sd

		internal static SwiftType? GetSwiftType (Type type)
		{
			if (type == typeof (IntPtr))
				return new SwiftType (Lib, "SV");
			return Type.GetTypeCode (type) switch {
				TypeCode.String => SwiftType.Of (typeof (Swift.String)),
				TypeCode.Byte => new SwiftType (Lib, "Swift", "UInt8", SwiftTypeCode.Struct),
				TypeCode.SByte => new SwiftType (Lib, "Swift", "Int8", SwiftTypeCode.Struct),
				TypeCode.Int16 => new SwiftType (Lib, "Swift", "Int16", SwiftTypeCode.Struct, typeof (Int16)),
				TypeCode.UInt16 => new SwiftType (Lib, "Swift", "UInt16", SwiftTypeCode.Struct, typeof (UInt16)),
				TypeCode.Int32 => new SwiftType (Lib, "Swift", "Int32", SwiftTypeCode.Struct, typeof (Int32)),
				TypeCode.UInt32 => new SwiftType (Lib, "Swift", "UInt32", SwiftTypeCode.Struct, typeof (UInt32)),
				TypeCode.Int64 => new SwiftType (Lib, "Swift", "Int64", SwiftTypeCode.Struct, typeof (Int64)),
				TypeCode.UInt64 => new SwiftType (Lib, "Swift", "UInt64", SwiftTypeCode.Struct, typeof (UInt64)),
				// Double is actually a type alias for Float64, hence why we use Sd for mangling here
				TypeCode.Double => new SwiftType (Lib, "Sd", typeof (Double)),
				TypeCode.Single => new SwiftType (Lib, "Sf"),
				TypeCode.Boolean => throw new NotImplementedException (),
				TypeCode.Char => throw new NotImplementedException (),
				TypeCode.DateTime => throw new NotImplementedException (),
				TypeCode.Decimal => throw new NotImplementedException (),
				_ => null,
			};
		}

		internal static SwiftType GetOptionalType (SwiftType wrapped)
			=> new SwiftType (Lib, GetOptionalType (0, wrapped.Metadata), genericArgs: new[] { wrapped });

		internal static SwiftType? GetTupleType (Type [] args, Nullability nullability = default)
		{
			var len = checked((ushort)args.Length);
			switch (len) {
			case 0:
				return new SwiftType (Lib, "yt");
			case 1:
				// Swift just treats 1-ples as a single value
				return SwiftType.Of (args [0], nullability [0]);
			default:
				var flags = new TupleTypeFlags (len);
				var elts = stackalloc TypeMetadata* [len];
				for (var i = 0; i < len; i++) {
					var elTy = SwiftType.Of (args [i], nullability [i]);
					if (elTy is null)
						return null;
					elts [i] = elTy.Metadata;
				}
				return new SwiftType (Lib, GetTupleType (0, flags, elts, null, null));
			}
		}

		#endregion

		#region Functions

		// https://github.com/apple/swift/blob/ebcbaca9681816b9ebaa7ba31ef97729e707db93/include/swift/Runtime/Casting.h#L233
		[DllImport (Path, EntryPoint = "swift_conformsToProtocol")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolConformance (TypeMetadata* typeMetadata, ProtocolDescriptor* protocolDescriptor);

		// https://github.com/apple/swift/blob/10aac5696a3948b8580d188921786301328eb3a3/include/swift/Runtime/Metadata.h#L353
		[DllImport (Path, EntryPoint = "swift_getWitnessTable")]
		internal unsafe static extern ProtocolWitnessTable* GetProtocolWitnessTable (ProtocolConformanceDescriptor* conformanceDescriptor, TypeMetadata* typeMetadata, void* instantiationArgs);

		#endregion

		#region Metadata Accessors
		//  For values for the first arg, see https://github.com/apple/swift/blob/ffc0f6f783a53573eb79440f16584e0422378b16/include/swift/ABI/MetadataValues.h#L1594
		//  (generally we pass 0 for complete metadata)

		[DllImport (Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swift_getOpaqueTypeMetadata")]
		internal static extern IntPtr GetOpaqueTypeMetadata (long metadataReq, void** arguments, IntPtr descriptor, uint index);

		[DllImport (Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$sSqMa")]
		static extern IntPtr GetOptionalType (long metadataReq, TypeMetadata* wrappedType);

		//https://github.com/apple/swift/blob/c925ce502b65ddca05ee8639d0d6ad0ab83d60a0/include/swift/Runtime/Metadata.h#L481
		[DllImport (Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "swift_getTupleTypeMetadata")]
		static extern IntPtr GetTupleType (long metadataReq, TupleTypeFlags flags, TypeMetadata** elements, string? labels, ValueWitnessTable* proposedWitnesses);

		#endregion
	}
}
