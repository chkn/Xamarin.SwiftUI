using System;
using System.Diagnostics;

using Xunit;

namespace SwiftUI.Tests
{
	public class ThrowingTraceListener : TraceListener
	{
		public override void Fail (string message, string detailMessage)
		{
			throw new DebugAssertFailureException (message, detailMessage);
		}

		public override void Write (string message)
		{
		}

		public override void WriteLine (string message)
		{
		}
	}

	//https://github.com/dotnet/roslyn/pull/7896
	[Serializable]
	public class DebugAssertFailureException : Exception
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
