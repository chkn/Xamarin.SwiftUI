using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Swift.Interop
{
	[StructLayout (LayoutKind.Auto)]
	public readonly struct Nullability : IEquatable<Nullability>
	{
		// There are a couple things we must reflect:
		//  1) Special handling for FSharp.Core types,
		//      as we don't want to force a dep on FSharp.Core
		const string FSharpCoreAssemblyName = "FSharp.Core";
		const string FSharpOptionTypeFullName = "Microsoft.FSharp.Core.FSharpOption`1";
		const string FSharpOptionModuleQualifiedName = "Microsoft.FSharp.Core.OptionModule, " + FSharpCoreAssemblyName;
		const string FSharpValueOptionTypeFullName = "Microsoft.FSharp.Core.FSharpValueOption`1";
		const string FSharpValueOptionModuleQualifiedName = "Microsoft.FSharp.Core.ValueOption, " + FSharpCoreAssemblyName;

		//  2) Nullable reference types, as those are not detectable
		//      without reflection and `System.Runtime.CompilerServices.NullableAttribute`
		//      is detected by name only (may be embedded by the compiler)
		const string NullableAttributeTypeFullName = "System.Runtime.CompilerServices.NullableAttribute";
		const string NullableContextAttributeTypeFullName = "System.Runtime.CompilerServices.NullableContextAttribute";

		public readonly bool IsNullable;
		readonly Nullability []? elements;

		public Nullability this [int index]
			=> elements? [index] ?? default;

		public Nullability (bool isNullable, Nullability []? elements = null)
		{
			this.IsNullable = isNullable;
			this.elements = elements;
		}

		public static bool operator == (Nullability a, Nullability b)
		{
			if (a.IsNullable != b.IsNullable)
				return false;
			var len = Math.Max (a.elements?.Length ?? 0, b.elements?.Length ?? 0);
			for (var i = 0; i < len; i++) {
				if (a [i] != b [i])
					return false;
			}
			return true;
		}
		public static bool operator != (Nullability a, Nullability b) => !(a == b);
		public override bool Equals (object other) => Equals ((Nullability)other);
		public bool Equals (Nullability other) => this == other;
		public override int GetHashCode () => throw new NotImplementedException ();

		/// <summary>
		/// Returns a new <see cref="Nullability"/> with the toplevel value set to non-nullable.
		/// </summary>
		public readonly Nullability Strip ()
			=> new Nullability (false, elements);

		public static Nullability Of (FieldInfo field)
			=> Of (field.FieldType, GetAttributedNullability (field));

		internal static Nullability Of (Type type, ReadOnlySpan<byte> attributedNullability = default)
			=> Of (type, ref attributedNullability);

		static Nullability Of (Type type, ref ReadOnlySpan<byte> attributedNullability)
		{
			Nullability []? elements = null;

			var isNullable = IsReifiedNullable (type);
			if (isNullable) {
				// If it's reified nullability, unwrap the underlying type
				type = type.GenericTypeArguments [0];
			}
			if (attributedNullability.Length > 0 && (!type.IsValueType || type.IsGenericType)) {
				// If it's not a non-generic value type, check for attributed nullability
				if (attributedNullability [0] == NullableFlag)
					isNullable = true;
				// If there are more than one, then there is an entry for each
				//  generic arg/element type. Otherwise, the first entry applies to all..
				if (attributedNullability.Length > 1)
					attributedNullability = attributedNullability.Slice (1);
			}

			// Account for generic arg reified nullability
			var genericArgs = type.GenericTypeArguments;
			if (genericArgs.Length > 0) {
				elements = new Nullability [genericArgs.Length];
				for (var i = 0; i < elements.Length; i++)
					elements [i] = Nullability.Of (genericArgs [i], ref attributedNullability);
			}

			//FIXME: Array element type

			return new Nullability (isNullable, elements);
		}

		// https://github.com/dotnet/roslyn/blob/master/docs/features/nullable-metadata.md
		const byte NullableFlag = 2;
		static byte []? GetAttributedNullability (MemberInfo member)
		{
			foreach (var attr in member.GetCustomAttributes (false)) {
				var attrType = attr.GetType ();
				if (attrType.FullName != NullableAttributeTypeFullName)
					continue;

				var flagsField = attrType.GetField ("NullableFlags");
				var flags = flagsField?.GetValue (attr) as byte [];
				if (flags != null)
					return flags;
			}
			// Also check member's declaring type for a NullableContextAttribute
			foreach (var attr in member.DeclaringType.CustomAttributes) {
				if (attr.AttributeType.FullName == NullableContextAttributeTypeFullName)
					return new[] { (byte)attr.ConstructorArguments[0].Value };
			}
			return null;
		}

		/// <summary>
		/// Returns <c>true</c> if the given type represents System.Nullable,
		///  or either F# Option or ValueOption types.
		/// </summary>
		public static bool IsReifiedNullable (Type ty)
		{
			if (ty.IsGenericType) {
				var gty = ty.GetGenericTypeDefinition ();

				if (ReferenceEquals (gty, typeof (Nullable<>)))
					return true;

				switch (gty.FullName) {
					case FSharpOptionTypeFullName:
					case FSharpValueOptionTypeFullName:
						return IsFSharpCore (ty.Assembly);
				}
			}
			return false;
		}

		// sync with IsReifiedNullable
		static bool IsFSharpValueOption (Type ty)
			=> ty.IsGenericType &&
			   ty.GetGenericTypeDefinition ().FullName == FSharpValueOptionTypeFullName &&
			   IsFSharpCore (ty.Assembly);

		public static bool IsNull ([NotNullWhen (returnValue: false)] object? value)
		{
			// Handles nullable reference and values types, and FSharpOption
			if (value is null)
				return true;

			// Handle FSharpValueOption
			var ty = value.GetType ();
			if (IsFSharpValueOption (ty))
				return ty.GetProperty ("Tag")?.GetValue (value) is int tag && tag == 0;

			return false;
		}

		public static object Unwrap (object value)
		{
			var type = value.GetType ();
			if (!IsReifiedNullable (type)) {
				// Nullable reference or value type
				return value;
			}
			var prop = type.GetProperty ("Value");
			return prop.GetValue (value);
		}

		public static TNullable Wrap<TNullable> (object? value)
		{
			var ty = typeof (TNullable);
			if (value == null && !ty.IsValueType)
				return default!;
			if (IsReifiedNullable (ty)) {
				MethodInfo? transform = null;
				var underlyingType = ty.GenericTypeArguments [0];
				if (underlyingType.IsValueType) {
					var nty = typeof (Nullable<>).MakeGenericType (underlyingType);
					value = Activator.CreateInstance (nty, value == null ? Array.Empty<object> () : new[] { value });
					transform = GetFSharpOptionModuleFunc (ty, "OfNullable");
				} else {
					transform = GetFSharpOptionModuleFunc (ty, "OfObj");
				}
				if (!(transform is null))
					value = transform.Invoke (null, new[] { value });
			}
			return (TNullable)value!;
		}

		/// <summary>
		/// Given a type that is a nullable value type, C# nullable reference type,
		/// or an F# Option or ValueOption, returns the underlying type.
		/// </summary>
		// Sync with Wrap and Of above
		public static Type GetUnderlyingType (Type type)
			=> IsReifiedNullable (type)? type.GenericTypeArguments [0] : type;

		static MethodInfo? GetFSharpOptionModuleFunc (Type ty, string funcName)
		{
			var moduleTy = ty.GetGenericTypeDefinition ().FullName switch
			{
				FSharpOptionTypeFullName => Type.GetType (FSharpOptionModuleQualifiedName),
				FSharpValueOptionTypeFullName => Type.GetType (FSharpValueOptionModuleQualifiedName),
				_ => null
			};
			return moduleTy?
				.GetMethod (funcName, BindingFlags.Public | BindingFlags.Static)?
				.MakeGenericMethod (ty.GenericTypeArguments);
		}

		static bool IsFSharpCore (Assembly asm)
			=> asm.GetName ().Name == FSharpCoreAssemblyName;
	}
}
