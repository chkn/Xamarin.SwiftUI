﻿using System;

using SwiftUI;

namespace SwiftUITestShared
{
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
}