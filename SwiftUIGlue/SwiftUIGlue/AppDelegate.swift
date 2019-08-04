//
//  AppDelegate.swift
//  SwiftUIBackend
//
//  Created by Alex Corrado on 7/23/19.
//  Copyright © 2019 Alex Corrado. All rights reserved.
//

import Cocoa
import SwiftUI

class AppDelegate<Content>: NSObject, NSApplicationDelegate where Content : View
{
    let content: Content
    var window: NSWindow!

    init(content: Content) {
        self.content = content
    }

    func applicationDidFinishLaunching(_ aNotification: Notification) {
        // Insert code here to initialize your application
        window = NSWindow(
            contentRect: NSRect(x: 0, y: 0, width: 480, height: 300),
            styleMask: [.titled, .closable, .miniaturizable, .resizable, .fullSizeContentView],
            backing: .buffered, defer: false)
        window.center()

        window.contentView = NSHostingView(rootView: content)

        window.makeKeyAndOrderFront(nil)
    }
}
