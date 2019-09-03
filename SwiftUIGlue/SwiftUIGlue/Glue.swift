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
//  varies in a couple ways. Thus, until mono supports this convention natively,
//  we need some glue functions...


//
// Extra registers are used for returning value types that are 3 or 4 pointers in size.
//
@_silgen_name("swiftui_Text_verbatim")
public func Text_verbatim(dest : UnsafeMutablePointer<Text>, verbatim : String) -> Void
{
    dest.initialize(to: Text(verbatim: verbatim))
}

//
// Class methods: Context register is used for pointer to type metadata
//
@_silgen_name("swiftui_NSHostingView_rootView")
public func NSHostingView_rootView<T: View>(root : T) -> NSHostingView<T>
{
    return NSHostingView(rootView: root)
}
