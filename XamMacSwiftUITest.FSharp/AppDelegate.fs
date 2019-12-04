namespace XamMacSwiftUITest.FSharp

open System
open Foundation
open AppKit
open CoreGraphics

open SwiftUI
open Views

[<Register("AppDelegate")>]
type AppDelegate() =
    inherit NSApplicationDelegate()
    override self.DidFinishLaunching(_) =
        let flags = NSWindowStyle.Titled ||| NSWindowStyle.Closable ||| NSWindowStyle.Miniaturizable ||| NSWindowStyle.Resizable ||| NSWindowStyle.FullSizeContentView
        let window = new NSWindow(CGRect(0., 0., 480., 300.), flags, NSBackingStore.Buffered, false)
        window.Center()

        window.ContentView <- NSHostingView.Create(Button((fun () -> printfn "CLICKED!"), Text("Click me")))
        window.MakeKeyAndOrderFront(self)
