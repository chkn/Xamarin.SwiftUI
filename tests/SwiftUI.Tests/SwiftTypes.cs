using System;
using System.Linq;
using System.Diagnostics;

using Xunit;

using Swift;
using SwiftUI;
using Swift.Interop;

namespace SwiftUI.Tests
{
	public class SwiftTypes
	{
		[Theory]
		[InlineData (typeof (SwiftCoreLib))]
		[InlineData (typeof (SwiftUILib))]
		public void CreateAllNonGenericTypes (Type libType)
		{
			var lib = Activator.CreateInstance (libType, nonPublic: true);
			Assert.NotNull (lib);

			var props = libType.GetProperties ()
			                   .Where (prop => typeof (SwiftType).IsAssignableFrom (prop.PropertyType));

			// These use Debug.Assert, which Xunit doesn't hook by default (https://github.com/xunit/xunit/issues/382)
			//  So we need to hook this ourselves...
			// FIXME: Better way? Hook at the test fixture level? etc..
			var oldListeners = Trace.Listeners.Cast<TraceListener> ().ToArray ();
			try {
				Trace.Listeners.Clear ();
				Trace.Listeners.Add (new ThrowingTraceListener ());
				Assert.All (props, prop => Assert.NotNull (prop.GetValue (lib)));
			} finally {
				Trace.Listeners.Clear ();
				Trace.Listeners.AddRange (oldListeners);
			}
		}
	}
}
