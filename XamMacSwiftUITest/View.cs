using System;
using SwiftUI;


namespace XamMacSwiftUITest
{
    public class ClickButton : View
    {
        State<int> counter = new State<int> (0);
        Text clicketyClick;

        public ClickButton()
        {
            this.clicketyClick = new Text(string.Format("Clicked {0} times", this.counter.Value))
            {
                Modifier = new ClickModifier(),
            };
            
        }

        public Button<Text> Body =>
            new Button<Text> (() => this.counter.Value++, this.clicketyClick);
    }
}
