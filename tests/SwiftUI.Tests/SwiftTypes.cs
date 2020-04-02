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
	public class SwiftTypes
	{
		[Theory]
		[InlineData (typeof (SwiftCoreLib))]
		[InlineData (typeof (SwiftUILib))]
		public void AllNonGenericTypesCanBeCreated (Type libType)
		{
			var lib = Activator.CreateInstance (libType, nonPublic: true);
			Assert.NotNull (lib);

			var props = libType.GetProperties ()
			                   .Where (prop => typeof (SwiftType).IsAssignableFrom (prop.PropertyType));

			using (new ThrowingTraceListener ())
				Assert.All (props, prop => Assert.NotNull (prop.GetValue (lib)));
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

			var gargs = sty.NativeFields [0].SwiftType.GenericArguments;
			Assert.NotNull (gargs);
			Assert.Equal (1, gargs!.Count);

			unsafe {
				Assert.Equal ("Optional", gargs [0].Metadata->TypeDescriptor->Name);
			}
		}
	}
}
