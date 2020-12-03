using System;

using SwiftUI;

namespace SwiftUI.Tests
{
	public record ViewWithNullableReferenceState : View
	{
		readonly State<string?> message = new State<string?> ("Hello World");

		public Text Body => new Text (message.Value ?? "No message");
	}

	public record ViewWithNullableValueState : View
	{
		readonly State<int?> count = new State<int?> (0);

		public Text Body => new Text ((count.Value ?? 0).ToString ());
	}
}
