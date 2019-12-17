namespace XamMacSwiftUITest.FSharp

open SwiftUI

type ClickButton() =
    inherit CustomView<ClickButton>()

    let clicks = State(0)
    member __.Body =
        Button((fun () -> clicks.Value <- clicks.Value + 1), Text(sprintf "Clicked %d times" clicks.Value))


