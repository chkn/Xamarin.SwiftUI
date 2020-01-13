namespace XamMacSwiftUITest.FSharp

open System
open Foundation
open AppKit
open CoreGraphics

open SwiftUI

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit NSApplicationDelegate()
    override self.ApplicationShouldTerminateAfterLastWindowClosed(_) = GC.Collect(); true
    override self.DidFinishLaunching(_) =
        let flags = NSWindowStyle.Titled ||| NSWindowStyle.Closable ||| NSWindowStyle.Miniaturizable ||| NSWindowStyle.Resizable ||| NSWindowStyle.FullSizeContentView

        let window = new NSWindow(CGRect(0., 0., 480., 300.), flags, NSBackingStore.Buffered, false)
        window.Center()

        window.ContentView <- NSHostingView.Create(ClickButton())
        window.MakeKeyAndOrderFront(self)