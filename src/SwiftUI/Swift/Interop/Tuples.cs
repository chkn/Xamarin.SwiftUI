using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Swift.Interop
{
	static class Tuples
	{
		/// <summary>
		/// Index of the TRest generic arg in tuple types.
		/// </summary>
		public const int TRestIndex = 7;

		/// <summary>
		/// Gets the element types from the given managed tuple type.
		/// </summary>
		/// <remarks>
		/// Has logic to recursively unpack TRest
		/// </remarks>
		public static Type [] GetElementTypes (Type type)
		{
			Debug.Assert (typeof (ITuple).IsAssignableFrom (type));
			var args = type.GetGenericArguments ();

			if (args.Length == TRestIndex + 1) {
				var rest = GetElementTypes (args [TRestIndex]);
				var len = rest.Length;
				Array.Resize (ref args, TRestIndex + len);
				Array.Copy (rest, 0, args, TRestIndex, len);
			}

			return args;
		}

		/// <summary>
		/// Flattens the given <see cref="Nullability"/> for the given tuple type.
		/// </summary>
		/// <remarks>
		/// Has logic to recursively unpack TRest
		/// </remarks>
		public static Nullability FlattenNullability (Type tupleType, Nullability nullability)
		{
			Debug.Assert (typeof (ITuple).IsAssignableFrom (tupleType));
			var args = tupleType.GetGenericArguments ();

			if (args.Length <= TRestIndex)
				return nullability;

			return nullability.AppendingElements (FlattenNullability (args [TRestIndex], nullability [TRestIndex]));
		}

		// Assumes tupleType has a constructor that takes all the elements
		public static object CreateTuple (Type tupleType, params object [] args)
		{
			if (args.Length > TRestIndex) {
				args [TRestIndex] = CreateTuple (tupleType.GetGenericArguments () [TRestIndex], args [TRestIndex..]);
				Array.Resize (ref args, TRestIndex + 1);
			}
			return Activator.CreateInstance (tupleType, args)!;
		}
	}
}
