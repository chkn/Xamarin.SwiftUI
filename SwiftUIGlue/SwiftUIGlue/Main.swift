//
//  Main.swift
//  SwiftUIBackend
//
//  Created by Alex Corrado on 7/23/19.
//  Copyright © 2019 Alex Corrado. All rights reserved.
//

import Foundation

import Cocoa
import SwiftUI

@_silgen_name("_netui_main")
public func NetUI_main<T: View>(_ view: UnsafePointer<T>) -> Int32
{
    let del = AppDelegate(content: view.pointee)
    NSApplication.shared.delegate = del
    return NSApplicationMain(CommandLine.argc, CommandLine.unsafeArgv)
}
