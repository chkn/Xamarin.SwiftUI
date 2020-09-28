/// This module enables a SwiftUI-style syntax for constructing view hierarchies in F#.
///  Computation Expressions are used to achieve this, taking advantage of implicit yield
///  syntax added in F# 4.7.
///
/// Note that all methods and functions in this module are `inline`. When used properly,
///  no references to this assembly are emitted by the F# compiler and the consuming assembly
///  will not depend on this assembly at runtime.
[<AutoOpen>]
module SwiftUI.FSharpExtensions

/// A struct type that can be used in place of unit `()`
// Using this type instead of unit (a reference type) facilitates overload resolution.
//  Alternatively, we could've used the regular unit type and struct tuples, but the
//  F# compiler doesn't inline those as well as reference tuples, resulting in
//  less efficient code gen.
[<Struct; RequireQualifiedAccess>]
type N = A

[<RequireQualifiedAccess>]
type TupleConvert = ToStructTuple with
    static member inline ($) (ToStructTuple, (a, b)) = struct (a, b)
    static member inline ($) (ToStructTuple, (a, b, c)) = struct (a, b, c)

// These functions wrap the intermediate values used in the Computation Expressions. The wrapped value is
//  either a single view, or multiple views wrapped in a TupleView. The wrapper is a tuple type 'a * 'b,
//  where 'a is the type of the child view-- either #View or TupleView<_>. In the latter case, 'b is a
//  tuple that will be wrapped in the TupleView<_> type denoted by 'a.
let inline private view (view : #View) = (view, N.A)
let inline private tuple<'t,'u> (tuple : 'u) : TupleView<'t> * 'u = (Unchecked.defaultof<_>, tuple)
let inline private untuple<'v, ^t, ^u when ^t : not struct and (TupleConvert or  ^t) : (static member ( $ ) : TupleConvert *  ^t ->  ^u)> (_ : 'v, tuple : ^t) : 'v =
    unbox (TupleView<_>(TupleConvert.ToStructTuple $ tuple))

type View with
    member inline __.Zero() = ()
    member inline __.Delay(fn) = fn()
    member inline __.Yield(x) = view x
    member inline __.Combine((a : 'a, N.A), (b : 'b, N.A)) = tuple<struct ('a * 'b), _> (a, b)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c))) = tuple<struct ('a * 'b * 'c), _> (a, b, c)

// The following to be added for each view that has a ViewBuilder:

#nowarn "44" // setters marked with ObsoleteAttribute

type Button<'TLabel when 'TLabel :> View> with
    member inline this.Run((view, N.A)) = this.Label <- view; this
    member inline this.Run(tuple) = this.Label <- untuple tuple; this
