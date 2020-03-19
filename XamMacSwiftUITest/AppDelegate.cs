using System;

using AppKit;
using Foundation;
using CoreGraphics;

using SwiftUI;

namespace XamMacSwiftUITest
{
	[Register("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
			GC.Collect();
			return true;
		}

        public override void DidFinishLaunching (NSNotification notification)
		{
			var flags = NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Resizable | NSWindowStyle.FullSizeContentView;
			var window = new NSWindow (new CGRect (x: 0, y: 0, width: 480, height: 300), flags, NSBackingStore.Buffered, deferCreation: false);
			window.WillClose += delegate {
				window.ContentView = NSTextField.CreateLabel ("CLOSING");
				GC.Collect ();
			};
			window.Center ();

			var clickButton = new ClickButton();

			//clickButton.Modifier (new ClickModifier ());
			window.ContentView = NSHostingView.Create (clickButton);

			window.MakeKeyAndOrderFront (this);
		}
    }

	public class ClickButton : View
	{
		State<int> counter = new State<int>(0);
       // Color colour = new Color();
		public Button<Text> Body =>
			new Button<Text>(() => {
                counter.Value += 1;
				var val = counter.Value;
				this.Opacity (counter.Value % 2 == 0 ? 0.5 : 1.0);
			},
            new Text(string.Format("Clicked {0} times", counter.Value)));
	}
}
