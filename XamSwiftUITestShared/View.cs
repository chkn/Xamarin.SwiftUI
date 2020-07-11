using System;

using SwiftUI;

namespace SwiftUITestShared
{
	public partial class StackedView : View
	{
		public VStack<TupleView<(ClickButton, Text)>> Body =>
			new VStack<TupleView<(ClickButton, Text)>> (HorizontalAlignment.Trailing,
				new TupleView<(ClickButton, Text)> ((
					new ClickButton (),
					new Text ("Right text")
				)));
	}

	public partial class ClickButton : View
	{
		State<(string, bool)> state = new State<(string, bool)> (("Please click this button:", true));
		public VStack<TupleView<(Text, Button<Text>?)>> Body =>
			new VStack<TupleView<(Text, Button<Text>?)>> (HorizontalAlignment.Leading,
				new TupleView<(Text, Button<Text>?)> ((
					new Text (state.Value.Item1),
					state.Value.Item2 ? new Button<Text> (() => state.Value = ("Thanks!", false), new Text ("Click me!")) : null
				)));
	}

	/*	
	public partial class ClickButton : View
	{
		State<int?> counter = new State<int?> (null);

		public ModifiedBackground<Button<Text>, ModifiedBackground<Text, Color>> Body {
			get {
				Button<Text> button = null;
				button = new Button<Text> (
					() => {
						var value = counter.Value ?? 0;
						counter.Value = value + 1;
					}, new Text (string.Format (counter.Value.HasValue ? "Clicked {0} times" : "Never been clicked", counter.Value))
				);

				var colour = counter.Value.HasValue ? counter.Value % 2 == 0 ? Color.Red : Color.Blue : Color.Yellow;
				var colourText = counter.Value.HasValue ? counter.Value % 2 == 0 ? nameof (Color.Red) : nameof (Color.Blue) : nameof (Color.Yellow);

				return button.Background (new Text (colourText).Background (colour));
			}
		}
	}
	*/

}
 