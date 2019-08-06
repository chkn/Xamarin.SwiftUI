//
//  Glue.swift
//  SwiftUIGlue
//
//  Created by Alex Corrado on 7/27/19.
//  Copyright © 2019 Alex Corrado. All rights reserved.
//

import Foundation

import SwiftUI

// The Swift calling convention is very similar to the C calling convention, but
//  uses extra registers for returning value types that are 3 or 4 pointers in size.
//  Thus, until mono supports this convention natively, we need some glue functions...

@_silgen_name("swiftui_text_verbatim")
public func NetUI_make_text(dest : UnsafeMutableRawPointer, verbatim : String) -> Void
{
    let txt = Text(verbatim: verbatim)
    dest.storeBytes(of: txt, as: Text.self)
}
