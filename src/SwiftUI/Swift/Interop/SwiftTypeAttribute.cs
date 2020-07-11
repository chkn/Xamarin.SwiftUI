using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

namespace Swift.Interop
{
	/// <summary>
	/// Indicates the attributed managed type is bridged to or from Swift.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct)]
	public abstract class SwiftTypeAttribute : Attribute
	{
		/// <summary>
		/// Provides a <see cref="SwiftType"/> for the given attributed managed type.
		/// </summary>
		/// <param name="attributedType">The <see cref="Type"/> that is attributed by this instance.</param>
		/// <param name="typeArgs">The <see cref="SwiftType"/> for each generic argument or element type,
		///  or <c>null</c> if this type has no generic arguments or element types.</param>
		/// <returns>the <see cref="SwiftType"/> for the given attributed managed type, or <c>null</c>.</returns>
		/// <remarks>	
		/// This method should only be called by <see cref="SwiftType.Of(Type)"/>.
		///  Do not call this this method directly.
		/// </remarks>
		protected internal abstract SwiftType? GetSwiftType (Type attributedType, SwiftType []? typeArgs);
	}

	/// <summary>
	/// Indicates the attributed managed type is imported from a Swift library.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = false)]
	public class SwiftImportAttribute : SwiftTypeAttribute
	{
		readonly NativeLib lib;
		string? mangledName;

		public SwiftImportAttribute (string libraryPath, string? mangledName = null)
		{
			this.lib = NativeLib.Get (libraryPath);
			this.mangledName = mangledName;
		}

		protected internal unsafe override SwiftType GetSwiftType (Type attributedType, SwiftType []? typeArgs)
		{
			if (mangledName is null)
				mangledName = SwiftType.Mangle (attributedType);
			if (typeArgs is null)
				return new SwiftType (lib, mangledName, attributedType);

			// If this is generic, then it might be a couple different things.
			if (!mangledName.StartsWith ("$s", StringComparison.Ordinal))
				mangledName = "$s" + mangledName;
			if (mangledName.EndsWith ("MQ", StringComparison.Ordinal)) {
				// opaque type
				var args = stackalloc void* [typeArgs.Length];
				for (var i = 0; i < typeArgs.Length; i++)
					args [i] = typeArgs [i].Metadata;
				return new SwiftType (lib, SwiftCoreLib.GetOpaqueTypeMetadata (0, args, lib.RequireSymbol (mangledName), 0));
			} else {
				// get type from metadata accessor function
				if (!mangledName.EndsWith ("Mp", StringComparison.Ordinal))
					mangledName += "Ma";

				var ftnPtr = lib.RequireSymbol (mangledName);
				var args = new List<object> (typeArgs.Length * 2);
				args.Add ((long)0); // metadataReq arg

				// determine how many additional arguments based on number of generic parameters and constraints
				var tparams = attributedType.GetGenericTypeDefinition ().GetGenericArguments ();
				for (var i = 0; i < typeArgs.Length; i++) {
					args.Add ((IntPtr)typeArgs [i].Metadata);
					// also add any constraints
					var constraints = tparams [i].GetGenericParameterConstraints ();
					foreach (var constr in constraints) {
						var attr = constr.GetCustomAttribute<SwiftProtocolAttribute> (inherit: false);
						if (attr != null)
							args.Add ((IntPtr)typeArgs [i].GetProtocolConformance (attr.Descriptor));
					}
				}
				var del = MetadataReq.MakeDelegate (args.Count - 1, ftnPtr);
				return new SwiftType (lib, (IntPtr)del.DynamicInvoke (args.ToArray ()), null, attributedType, typeArgs);
			}
		}
	}
}
