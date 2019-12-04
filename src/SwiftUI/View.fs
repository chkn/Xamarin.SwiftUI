[<AutoOpen>]
module SwiftUI.Views

open System

open SwiftUI

type ViewBuilder internal () =
    member inline __.Yield(v : #IView) = v
    //member inline __.Combine(view1 : #IView, view2 : #IView) = //TupleView
    // etc...

type CustomViewBuilder internal () =
    member inline __.Yield(v) = v

    [<CustomOperation("body")>]
    member inline __.Body(state : 'tstate, [<ProjectionParameter>] fn : 'tstate -> 'tbody) =
        { new CustomView<'tbody,'tstate>() with
            override __.Body = fn state
        }

let View = CustomViewBuilder()
