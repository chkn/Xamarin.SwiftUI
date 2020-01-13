using System;
using ObjCRuntime;
using SwiftUI;

namespace XamiOSSwiftUITest
{
    public class ClickButton : View
    {
        State<int> counter = new State<int>(0);
        public Button<Text> Body =>
            new Button<Text>(() => counter.Value++, new Text(string.Format("Clicked {0} times", counter.Value)));
    }
}