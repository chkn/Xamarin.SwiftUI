﻿namespace XamMacSwiftUITest.FSharp

open System
open Foundation
open AppKit
open CoreGraphics

open SwiftUI

[<Register("AppDelegate")>]
type AppDelegate () =
    inherit NSApplicationDelegate ()

    override __.ApplicationShouldTerminateAfterLastWindowClosed (_) = 
        GC.Collect (); 
        false

    override __.DidFinishLaunching (_) =
        let flags = NSWindowStyle.Titled ||| NSWindowStyle.Closable ||| NSWindowStyle.Miniaturizable ||| NSWindowStyle.Resizable ||| NSWindowStyle.FullSizeContentView

        let window = new NSWindow (CGRect (0., 0., 480., 300.), flags, NSBackingStore.Buffered, false)
        window.WillClose.Add (fun _ -> 
            window.ContentView <- NSTextField.CreateLabel ("CLOSING") 
            GC.Collect ()
        )
        window.Center ()

        window.ContentView <- new NSHostingView (ClickButton ())
        window.MakeKeyAndOrderFront (__)