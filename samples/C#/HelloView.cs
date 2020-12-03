using System;
using SwiftUI;
using static SwiftUI.Views;

namespace CSharpSamples
{
	public partial record HelloView : View
	{
		readonly State<int> clicks = new (0);

		public View Body =>
			Button ($"Clicked {clicks.Value} times", () => clicks.Value++);
	}
}
