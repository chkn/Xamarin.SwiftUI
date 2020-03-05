//
//  Glue.swift
//  SwiftUIGlue
//
//  Created by Alex Corrado on 7/27/19.
//  Copyright © 2019 Alex Corrado. All rights reserved.

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
// Indirectly returned struct: return pointer is passed in rax.
// Closure contexts passed in r13
//
@_silgen_name("swiftui_Button_action_label")
public func Button_action_label<T: View>(dest : UnsafeMutablePointer<Button<T>>, action : Delegate, label : T)
{
    let del = DelegateBox(action)
    dest.initialize(to: Button<T>(action: del.invoke, label: { label }))
}

//
// Indirectly returned struct: return pointer is passed in rax.
//
@_silgen_name("swiftui_State_initialValue")
public func State_initialValue<T>(dest : UnsafeMutablePointer<State<T>>, initialValue : T)
{
    dest.initialize(to: State<T>(initialValue: initialValue))
}

//
// Non-static sized struct: pointer passed in r13
//
// HACK: This doesn't quite do what we need, so we alias a helper function
//  that the Swift compiler generates to swiftui_State_wrappedValue_setter
//  NOTE: Must be built in Release to get that helper emitted by the compiler!
//   -> In the helper, "value" is passed guaranteed, not owned
public func State_wrappedValue_setter<T>(state : __owned State<T>, value : __owned T)
{
    state.wrappedValue = value
}

//
// Non-static sized struct: pointer passed in r13
//
@_silgen_name("swiftui_State_wrappedValue_getter")
public func State_wrappedValue_getter<T>(dest : UnsafeMutablePointer<T>, state : UnsafePointer<State<T>>)
{
    dest.initialize(to: state.pointee.wrappedValue)
}

//
// Class methods: Context register is used for pointer to type metadata
//
#if os(iOS) || os(tvOS)

@_silgen_name("swiftui_UIHostingController_rootView")
public func UIHostingController_rootView<T: View>(root : T) -> UIHostingController<T>
{
    return UIHostingController(rootView: root)
}
#endif

#if os(macOS)
@_silgen_name("swiftui_NSHostingView_rootView")
public func NSHostingView_rootView<T: View>(root : T) -> NSHostingView<T>
{
    return NSHostingView(rootView: root)
}
#endif

#if os(watchOS)
@_silgen_name("swiftui_WKHostingController_rootView")
public func WKHostingController_rootView<T: View>() -> WKHostingController<T>
{
    return WKHostingController()
}
#endif

//
// Protocol witness: gets self in context register
//
public struct ThunkView<U, T: View>: View {
    
    let viewData: U

    public var body: T {
        let resultPtr = UnsafeMutablePointer<T>.allocate(capacity: 1)
        defer { resultPtr.deallocate() }
        withUnsafePointer(to: viewData, { viewBodyFn!(UnsafeRawPointer(resultPtr), UnsafeRawPointer($0)) })
        return resultPtr.move()
    }
}

public typealias ViewBodyFn = @convention(c) (UnsafeRawPointer, UnsafeRawPointer) -> Void // (TBody*, CustomViewData*) -> Void
var viewBodyFn: ViewBodyFn?

@_silgen_name("swiftui_ThunkView_setViewBodyFn")
public func SetViewBodyFn(value : @escaping ViewBodyFn)
{
    viewBodyFn = value
}

//
// Protocol witness: gets self in context register
//
public struct ThunkViewModifier<U, T: ViewModifier>: ViewModifier {
    let viewModifierData: U

    public func body(content: _ViewModifier_Content<ThunkViewModifier<U, T>>) -> some View {
        let resultPtr = UnsafeMutablePointer<T>.allocate(capacity: 1)
        defer { resultPtr.deallocate() }
        withUnsafePointer(to: viewModifierData, { viewModifierBodyFn!(UnsafeRawPointer(resultPtr), UnsafeRawPointer($0)) })
        _ = resultPtr.move()
        return content
    }
}

public typealias ViewModifierBodyFn = @convention(c) (UnsafeRawPointer, UnsafeRawPointer) -> Void // (TBody*, CustomViewData*) -> Void
var viewModifierBodyFn: ViewModifierBodyFn?

@_silgen_name("swiftui_ThunkViewModifier_setViewModifierBodyFn")
public func SetViewModifierBodyFn(value : @escaping ViewModifierBodyFn)
{
    viewModifierBodyFn = value
}
