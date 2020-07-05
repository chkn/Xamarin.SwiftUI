using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Xunit;

using Swift;
using SwiftUI;
using Swift.Interop;
using SwiftUI.Interop;
using SwiftUI.Tests.FSharp;

namespace SwiftUI.Tests
{
	public class TypeTests : TestFixture
	{
		static Type GetTypeForTest (Type ty)
		{
			if (!ty.IsGenericTypeDefinition)
				return ty;

			var gargs = ty.GetGenericArguments ();
			for (var i = 0; i < gargs.Length; i++) {
				var constr = gargs [i].GetGenericParameterConstraints ().SingleOrDefault ();
				if (constr == typeof (ITuple))
					gargs [i] = typeof (ValueTuple<Text,Text>);
				else
					gargs [i] = typeof (Text);
			}

			return ty.MakeGenericType (gargs);
		}

		[Fact]
		public void AllSwiftTypesCanBeCreated ()
		{
			var types = typeof (SwiftCoreLib)
				.Assembly
				.GetTypes ()
				.Where (ty => !ty.IsAbstract && Attribute.IsDefined (ty, typeof (SwiftTypeAttribute), true))
				.Select (GetTypeForTest);
			Assert.All (types, ty => Assert.NotNull (SwiftType.Of (ty)));
		}

		[Theory]
		[InlineData (typeof (ViewWithNullableReferenceState))]
		[InlineData (typeof (ViewWithNullableValueState))]
		[InlineData (typeof (ViewWithOptionState))]
		public void NullableFieldConvertsToSwiftOptional (Type viewType)
		{
			var sty = SwiftType.Of (viewType) as CustomViewType;
			Assert.NotNull (sty);

			Assert.Equal (1, sty!.NativeFields.Count);
			Assert.False (sty.NativeFields [0].Nullability.IsNullable);
			Assert.True (sty.NativeFields [0].Nullability [0].IsNullable);

			var gargs = sty.NativeFields [0].SwiftType.GenericArguments;
			Assert.NotNull (gargs);
			Assert.Equal (1, gargs!.Count);

			unsafe {
				Assert.Equal ("Optional", gargs [0].Metadata->TypeDescriptor->Name);
			}
		}

		[SkippableTheory]
		[InlineData (typeof (Optional.Packed<>), typeof (Swift.String))]
		[InlineData (typeof (Optional.Packed<>), typeof (IntPtr))]
		[InlineData (typeof (Optional.Unpacked<>), typeof (byte))]
		[InlineData (typeof (Optional.Unpacked<>), typeof (sbyte))]
		[InlineData (typeof (Optional.Unpacked<>), typeof (int))]
		[InlineData (typeof (Optional.Unpacked<>), typeof (long))]
		// NOTE: For Double? we seem to match the in-memory representation, but the
		//  calling convention differs.
		[InlineData (typeof (Optional.Unpacked<>), typeof (double))]
		[InlineData (typeof (Optional.Unpacked<>), typeof (float))]
		public void OptionalTypes (Type optionalType, Type wrappedType)
		{
			// static constructor has asserts, but it is only included for DEBUG builds..
			var cctor = optionalType.MakeGenericType (wrappedType).TypeInitializer;
			Skip.If (cctor is null, "Asserts only compiled in debug builds");
			cctor!.Invoke (null, null);
		}
	}
}
