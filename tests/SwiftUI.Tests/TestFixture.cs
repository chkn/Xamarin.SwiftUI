using System;
namespace SwiftUI.Tests
{
	public class TestFixture : IDisposable
	{
		ThrowingTraceListener? throwForFailedAsserts = new ThrowingTraceListener ();

		public void Dispose ()
		{
			throwForFailedAsserts?.Dispose ();
			throwForFailedAsserts = null;
		}
	}
}
