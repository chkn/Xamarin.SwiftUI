using System;
using SwiftUI;
using static SwiftUI.Views;

namespace CSharpSamples
{
	public partial record HelloView : View
	{
		readonly State<int> clickCounter = new (0);

		public record ButtonModifier : ViewModifier<ModifiedBackground<Content, Color>>
		{
			public override ModifiedBackground<Content, Color> Body (Content content)
			{
				return content.Background (Color.Red);
			}
		}

		public ModifiedBackground<Button<Text>, ModifiedBackground<Text, Color>> Body {
			get {
				var button = new Button<Text> (
					() => {
						clickCounter.Value++;
					}, new Text (string.Format (clickCounter.Value > 0 ? "Clicked {0} times" : "Never been clicked", clickCounter.Value))
				);

				// TODO button.Modifier (new ButtonModifier ());

				var backgroundColour = clickCounter.Value > 0 ? clickCounter.Value % 2 == 0 ? Color.Red : Color.Blue : Color.Yellow;
				var textColour = clickCounter.Value > 0 ? clickCounter.Value % 2 == 0 ? nameof (Color.Red) : nameof (Color.Blue) : nameof (Color.Yellow);

				return button.Background (new Text (textColour).Background (backgroundColour));
			}
		}
	}
}
