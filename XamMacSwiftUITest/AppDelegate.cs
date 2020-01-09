using System;

using AppKit;
using Foundation;
using CoreGraphics;

using SwiftUI;

namespace XamMacSwiftUITest
{
	public class AppDelegate : NSApplicationDelegate
	{
		public override void DidFinishLaunching (NSNotification notification)
		{
			var window = new NSWindow (new CGRect (x: 0, y: 0, width: 480, height: 300),
				NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable | NSWindowStyle.Resizable | NSWindowStyle.FullSizeContentView,
				NSBackingStore.Buffered, deferCreation: false);

			window.Center ();

			window.ContentView = NSHostingView.Create (new ClickButton());

			window.MakeKeyAndOrderFront (null);
		}
	}
}
