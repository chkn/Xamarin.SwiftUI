using System;
using System.Linq;
using System.Diagnostics;

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
		[Fact]
		public void AllSwiftTypesCanBeCreated ()
		{
			var types = typeof (SwiftCoreLib)
				.Assembly
				.GetTypes ()
				.Where (ty => !ty.IsAbstract && Attribute.IsDefined (ty, typeof (SwiftTypeAttribute), true))
				.Select (ty => ty.IsGenericTypeDefinition? ty.MakeGenericType (Array.ConvertAll (ty.GetGenericArguments (), _ => typeof (Text))) : ty);
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
	}
}
