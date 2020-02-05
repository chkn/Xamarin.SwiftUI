using System;

using AppKit;
using Foundation;
using CoreGraphics;

using SwiftUI;
using XamSwiftUITestShared;

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

            window.ContentView = NSHostingView.Create (new ClickButton ());

			window.MakeKeyAndOrderFront (this);
		}
    }
}
