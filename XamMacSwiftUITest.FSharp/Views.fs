namespace XamMacSwiftUITest.FSharp

open SwiftUI

type HelloView() =
    inherit CustomView<HelloView>()
    let clicks = State(3)
    member __.Body =
        Button((fun () -> clicks.Value <- clicks.Value + 1), Text(sprintf "Clicked %d times" clicks.Value))
