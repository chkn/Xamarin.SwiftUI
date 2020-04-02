using System;
using System.Reflection;

namespace Swift
{
	// There are a couple things we must reflect:
	//  1) Special handling for FSharp.Core types,
	//      as we don't want to force a dep on FSharp.Core
	//  2) Nullable reference types, as those are not detectable
	//      without reflection and `System.Runtime.CompilerServices.NullableAttribute`
	//      is detected by name only (may be embedded by the compiler)
	static class Nullability
	{
		/// <summary>
		/// Given a type that is already known to be either:
		///   a nullable reference or value type,
		///   or an F# Option or ValueOption,
		/// returns the underlying type.
		/// </summary>
		public static Type GetNullableUnderlyingType (this Type ty)
		{
			if (ty.IsNullable ()) {
				// Nullable<_>, FSharpOption<_>, or FSharpValueOption<_>
				return ty.GenericTypeArguments [0];
			}
			// nullable reference type is its own underlying type
			return ty;
		}

		/// <summary>
		/// Returns <c>true</c> if the given type represents System.Nullable,
		///  or either F# Option or ValueOption types.
		/// </summary>
		public static bool IsNullable (this Type ty)
		{
			if (ty.IsGenericType) {
				var gty = ty.GetGenericTypeDefinition ();

				if (ReferenceEquals (gty, typeof (Nullable<>)))
					return true;

				switch (gty.FullName) {
					case "Microsoft.FSharp.Core.FSharpOption`1":
					case "Microsoft.FSharp.Core.FSharpValueOption`1":
						return ty.Assembly.IsFSharpCore ();
				}
			}
			return false;
		}

		public static bool IsFSharpCore (this Assembly asm)
			=> asm.GetName ().Name == "FSharp.Core";
	}
}
