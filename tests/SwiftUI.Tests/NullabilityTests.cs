using System;
using System.Reflection;
using Swift.Interop;
using Xunit;

namespace SwiftUI.Tests
{
	public class NullabilityTests : TestFixture
	{
		class A<T> { }

		A<string> a1;
		A<string?> a2;
		A<string?>? a3;
		A<A<string>> a4;
		A<A<string?>> a5;
		A<A<string?>?> a6;
		A<A<string?>?>? a7;

		[Fact]
		public void NullabilityOfFieldsA ()
		{
			AssertField (nameof (a1), new Nullability (false));
			AssertField (nameof (a1), new Nullability (false, new [] { new Nullability (false) }));
			AssertField (nameof (a2), new Nullability (false, new[] { new Nullability (true) }));
			AssertField (nameof (a3), new Nullability (true, new[] { new Nullability (true) }));
			AssertField (nameof (a4), new Nullability (false));
			AssertField (nameof (a4), new Nullability (false, new [] { new Nullability (false, new [] { new Nullability (false) }) }));
			AssertField (nameof (a5), new Nullability (false, new[] { new Nullability (false, new[] { new Nullability (true) }) }));
			AssertField (nameof (a6), new Nullability (false, new [] { new Nullability (true, new [] { new Nullability (true) }) }));
			AssertField (nameof (a7), new Nullability (true, new[] { new Nullability (true, new [] { new Nullability (true) }) }));
		}

		class B<T, U> { }

		B<int, string> b1;
		B<int?, string> b2;
		B<int, string?> b3;
		B<int?, string?> b4;
		B<int, string>? b5;
		B<int?, string?>? b6;
		B<A<int?>, A<string?>> b7;
		B<A<int?>?, A<string?>?> b8;
		B<A<int?>?, A<string?>?>? b9;

		[Fact]
		public void NullabilityOfFieldsB ()
		{
			AssertField (nameof (b1), new Nullability (false, new [] { new Nullability (false), new Nullability (false) }));
			AssertField (nameof (b2), new Nullability (false, new [] { new Nullability (true), new Nullability (false) }));
			AssertField (nameof (b3), new Nullability (false, new [] { new Nullability (false), new Nullability (true) }));
			AssertField (nameof (b4), new Nullability (false, new [] { new Nullability (true), new Nullability (true) }));
			AssertField (nameof (b5), new Nullability (true, new [] { new Nullability (false), new Nullability (false) }));
			AssertField (nameof (b6), new Nullability (true, new [] { new Nullability (true), new Nullability (true) }));
			AssertField (nameof (b7), new Nullability (false, new [] { new Nullability (false, new [] { new Nullability (true) }), new Nullability (false, new [] { new Nullability (true) }) }));
			AssertField (nameof (b8), new Nullability (false, new [] { new Nullability (true, new [] { new Nullability (true) }), new Nullability (true, new [] { new Nullability (true) }) }));
			AssertField (nameof (b9), new Nullability (true, new [] { new Nullability (true, new [] { new Nullability (true) }), new Nullability (true, new [] { new Nullability (true) }) }));
		}

		struct C<T> { }

		C<int> c1;
		C<int?> c2;
		C<int?>? c3;
		C<string> c4;
		C<string?> c5;
		C<string?>? c6;

		[Fact]
		public void NullabilityOfFieldsC ()
		{
			AssertField (nameof (c1), new Nullability (false));
			AssertField (nameof (c1), new Nullability (false, new [] { new Nullability (false) }));
			AssertField (nameof (c2), new Nullability (false, new [] { new Nullability (true) }));
			AssertField (nameof (c3), new Nullability (true, new [] { new Nullability (true) }));

			AssertField (nameof (c4), new Nullability (false));
			AssertField (nameof (c4), new Nullability (false, new [] { new Nullability (false) }));
			AssertField (nameof (c5), new Nullability (false, new [] { new Nullability (true) }));
			AssertField (nameof (c6), new Nullability (true, new [] { new Nullability (true) }));
		}

		Tuple<int?> d1;
		Tuple<int, int?> d2;
		Tuple<int, int, int?> d3;
		Tuple<int, int, int, int, int, int, int?> d4;
		Tuple<int, int, int, int, int, int, int?, Tuple<int?>> d5;
		Tuple<int, int, int, int, int, int, int?, Tuple<int, int?>> d6;
		ValueTuple<int?> d7;
		ValueTuple<int, int?> d8;
		ValueTuple<int, int, int?> d9;
		ValueTuple<int, int, int, int, int, int, int?> d10;
		ValueTuple<int?, int, int, int, int, int, int?, ValueTuple<int?>> d11;
		ValueTuple<int?, int, int, int, int, int, int?, ValueTuple<int, int?>> d12;

		[Theory]
		[InlineData (nameof (d1))]
		[InlineData (nameof (d2))]
		[InlineData (nameof (d3))]
		[InlineData (nameof (d4))]
		[InlineData (nameof (d5))]
		[InlineData (nameof (d6))]
		[InlineData (nameof (d7))]
		[InlineData (nameof (d8))]
		[InlineData (nameof (d9))]
		[InlineData (nameof (d10))]
		[InlineData (nameof (d11))]
		[InlineData (nameof (d12))]
		public void NullabilityOfTupleFields (string fieldName)
			=> AssertFieldType (fieldName);

		static FieldInfo GetField (string fieldName)
			=> typeof (NullabilityTests).GetField (fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;

		static void AssertField (string fieldName, Nullability expected)
		{
			var fld = GetField (fieldName);
			Assert.True (Nullability.Of (fld) == expected, fieldName);
		}

		static void AssertFieldType (string fieldName)
		{
			var fld = GetField (fieldName);
			Assert.NotNull (SwiftType.Of (fld.FieldType, Nullability.Of (fld)));
		}
	}
}
