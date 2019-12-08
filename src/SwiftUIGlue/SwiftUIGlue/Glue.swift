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
public func Text_verbatim(dest : UnsafeMutablePointer<Text>, verbatim : String)
{
	dest.initialize(to: Text(verbatim: verbatim))
}

//
// Holds a function pointer and context pointer and allows for managed clean up when Swift
//  no longer needs them.
// Aside from clean up, this is also needed because closure contexts are passed in r13.
//
@frozen
public struct Delegate {
	let invoke: @convention(c) (UnsafeRawPointer) -> Void
	let dispose: @convention(c) (UnsafeRawPointer) -> Void
	let ctx: UnsafeRawPointer
}
fileprivate class DelegateBox {
	private let del : Delegate

	init(_ del : Delegate)
	{
		self.del = del
	}

	public func invoke()
	{
		del.invoke(del.ctx)
	}

	deinit {
		del.dispose(del.ctx)
	}
}

//
// Struct return pointer is passed in rax.
// Closure contexts passed in r13
//
@_silgen_name("swiftui_Button_action_label")
public func Button_action_label<T: View>(dest : UnsafeMutablePointer<Button<T>>, action : Delegate, label : __owned T)
{
	let del = DelegateBox(action)
	dest.initialize(to: Button<T>(action: del.invoke, label: { label }))
}

//
// Class methods: Context register is used for pointer to type metadata
//
@_silgen_name("swiftui_NSHostingView_rootView")
public func NSHostingView_rootView<T: View>( root : __owned T) -> NSHostingView<T>
{
	return NSHostingView(rootView: root)
}

//
// Protocol witness gets self in context register
//
public struct ThunkView<T: View>: View {
	let gcHandle: UnsafeRawPointer

	public var body: T {
		let resultPtr = UnsafeMutablePointer<T>.allocate(capacity: 1)
		defer { resultPtr.deallocate() }
		bodyFn!(UnsafeRawPointer(resultPtr), gcHandle)
		return resultPtr.move()
	}
}

public typealias BodyFn = @convention(c) (UnsafeRawPointer, UnsafeRawPointer) -> Void // (TBody*, GCHandle*) -> Void
var bodyFn: BodyFn?

@_silgen_name("swiftui_ThunkView_setBodyFn")
public func SetBodyFn(value : @escaping BodyFn)
{
	bodyFn = value
}
