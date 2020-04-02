using System;

using SwiftUI;

namespace SwiftUI.Tests
{
	public class ViewWithNullableReferenceState : View
	{
		readonly State<string?> message = new State<string?> ("Hello World");

		public Text Body => new Text (message.Value ?? "No message");
	}

	public class ViewWithNullableReferenceState2 {
		readonly State<State<string?>> message = new State<State<string?>> (new State<string?> ("hello"));

		//public Text Body => new Text (message.Value ?? "No message");
	}

	public class ViewWithNullableValueState : View
	{
		readonly State<int?> count = new State<int?> (0);

		public Text Body => new Text ((count.Value ?? 0).ToString ());
	}
}
