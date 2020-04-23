using System;
using System.Linq;
using System.Diagnostics;

using Xunit;

namespace SwiftUI.Tests
{
	// Xunit doesn't hook Debug.Assert (https://github.com/xunit/xunit/issues/382)
	class ThrowingTraceListener : TraceListener
	{
		readonly TraceListener [] oldListeners;

		public ThrowingTraceListener ()
		{
			oldListeners = Trace.Listeners.Cast<TraceListener> ().ToArray ();
			Trace.Listeners.Clear ();
			Trace.Listeners.Add (this);
		}

		public override void Fail (string message, string detailMessage)
			=> throw new DebugAssertFailureException (message, detailMessage);

		public override void Write (string message)
		{
		}

		public override void WriteLine (string message)
		{
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				Trace.Listeners.Clear ();
				Trace.Listeners.AddRange (oldListeners);
			}
			base.Dispose (disposing);
		}
	}

	//https://github.com/dotnet/roslyn/pull/7896
	[Serializable]
	class DebugAssertFailureException : Exception
	{
		public DebugAssertFailureException (string message, string detailMessage)
			: base (message + Environment.NewLine + detailMessage)
		{
		}

		protected DebugAssertFailureException (
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base (info, context)
		{
		}
	}
}
