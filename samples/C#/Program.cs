using CSharpSamples;

using AppKit;
using SwiftUI;
using Foundation;
using CoreGraphics;

NSApplication.Init ();
NSApplication.SharedApplication.Delegate = new Delegate ();
NSApplication.Main (args);

class Delegate : NSApplicationDelegate {
    public override void DidFinishLaunching (NSNotification notif)
    {
        var wnd = new NSWindow(new CGRect(0, 0, 1000, 1000), NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Miniaturizable, NSBackingStore.Buffered, false);

        wnd.Title = "HI MOM!";
        wnd.ContentView = new NSHostingView (new HelloView ());

        wnd.MakeKeyAndOrderFront (this);
    }
}