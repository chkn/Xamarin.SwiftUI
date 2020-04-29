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
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			GC.Collect ();
			return false;
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

			window.ContentView = new NSHostingView (new ClickButton ());

			window.MakeKeyAndOrderFront (this);
		}
    }

	public class ClickButton : View
	{
		State<int?> counter = new State<int?> (null);
		public ModifiedBackground<Button<Text>, Color> Body {
		//public ModifiedOpacity<Button<Text>> Body {
			get {
				Button<Text> button = null;
				button = new Button<Text>(
					() =>
					{
						var value = counter.Value ?? 0;
						counter.Value = value + 1;
					}, new Text (string.Format (counter.Value.HasValue ? "Clicked {0} times" : "Never been clicked", counter.Value))
				);
				var colour = counter.Value % 2 == 0 ? Color.Blue : Color.Red;

				return button.Background (colour);
				//return button.Opacity (counter.Value % 2 == 0 ? 0.5 : 1.0)
			}
		}
	}
}
