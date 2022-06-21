/// This module enables a SwiftUI-style syntax for constructing view hierarchies in F#.
///  Computation Expressions are used to achieve this, taking advantage of implicit yield
///  syntax added in F# 4.7.
///
/// Note that all methods and functions in this module are `inline`. When used properly,
///  no references to this assembly are emitted by the F# compiler (for Release builds),
///  and the consuming assembly will not depend on this assembly at runtime.
[<AutoOpen>]
module SwiftUI.FSharpExtensions

open System.Runtime.CompilerServices

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
    static member inline ($) (ToStructTuple, (a, b, c, d)) = struct (a, b, c, d)
    static member inline ($) (ToStructTuple, (a, b, c, d, e)) = struct (a, b, c, d, e)
    static member inline ($) (ToStructTuple, (a, b, c, d, e, f)) = struct (a, b, c, d, e, f)
    static member inline ($) (ToStructTuple, (a, b, c, d, e, f, g)) = struct (a, b, c, d, e, f, g)
    static member inline ($) (ToStructTuple, (a, b, c, d, e, f, g, h)) = struct (a, b, c, d, e, f, g, h)
    static member inline ($) (ToStructTuple, (a, b, c, d, e, f, g, h, i)) = struct (a, b, c, d, e, f, g, h, i)
    static member inline ($) (ToStructTuple, (a, b, c, d, e, f, g, h, i, j)) = struct (a, b, c, d, e, f, g, h, i, j)

// These functions wrap the intermediate values used in the Computation Expressions. The wrapped value is
//  either a single view, or multiple views wrapped in a TupleView. The wrapper is a tuple type 'a * 'b,
//  where 'a is the type of the child view-- either #View or TupleView<_>. In the latter case, 'b is a
//  tuple that will be wrapped in the TupleView<_> type denoted by 'a.
let inline private view (view : #View) = (view, N.A)
let inline private tuple<'t,'u when 't :> ITuple> (tuple : 'u) : TupleView<'t> * 'u = (Unchecked.defaultof<_>, tuple)
let inline private untuple<'v, ^t, ^u when ^u :> ITuple and ^t : not struct and (TupleConvert or  ^t) : (static member ( $ ) : TupleConvert *  ^t ->  ^u)> (_ : 'v, tuple : ^t) : 'v =
    unbox (TupleView<_>(TupleConvert.ToStructTuple $ tuple))

type View with
    member inline __.Zero() = ()
    member inline __.Delay(fn) = fn()
    member inline __.Yield(x) = view x
    member inline __.Combine((a : 'a, N.A), (b : 'b, N.A)) = tuple<struct ('a * 'b), _> (a, b)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c))) = tuple<struct ('a * 'b * 'c), _> (a, b, c)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd))) = tuple<struct ('a * 'b * 'c * 'd), _> (a, b, c, d)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e))) = tuple<struct ('a * 'b * 'c * 'd * 'e), _> (a, b, c, d, e)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e, f : 'f))) = tuple<struct ('a * 'b * 'c * 'd * 'e * 'f), _> (a, b, c, d, e, f)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e, f : 'f, g : 'g))) = tuple<struct ('a * 'b * 'c * 'd * 'e * 'f * 'g), _> (a, b, c, d, e, f, g)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e, f : 'f, g : 'g, h : 'h))) = tuple<struct ('a * 'b * 'c * 'd * 'e * 'f * 'g * 'h), _> (a, b, c, d, e, f, g, h)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e, f : 'f, g : 'g, h : 'h, i : 'i))) = tuple<struct ('a * 'b * 'c * 'd * 'e * 'f * 'g * 'h * 'i), _> (a, b, c, d, e, f, g, h, i)
    member inline __.Combine((a : 'a, N.A), (_, (b : 'b, c : 'c, d : 'd, e : 'e, f : 'f, g : 'g, h : 'h, i : 'i, j : 'j))) = tuple<struct ('a * 'b * 'c * 'd * 'e * 'f * 'g * 'h * 'i * 'j), _> (a, b, c, d, e, f, g, h, i, j)


// The following to be added for each view that has a ViewBuilder:

#nowarn "44" // setters marked with ObsoleteAttribute

type Button<'TLabel when 'TLabel :> View> with
    member inline this.Run((view, N.A)) = this.Label <- view; this
    member inline this.Run(tuple) = this.Label <- untuple tuple; this

type SwiftUI.Views with
    static member inline Button(action) = Button(action, Unchecked.defaultof<_>)
