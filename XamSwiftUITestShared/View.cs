using System;

using SwiftUI;

namespace XamSwiftUITestShared
{
    public class ClickButton : View
    {
        State<int> counter = new State<int> (0);
        public Button<Text> Body =>
            new Button<Text> (() => counter.Value += 1, new Text (string.Format("Clicked {0} times", counter.Value)));
    }
}