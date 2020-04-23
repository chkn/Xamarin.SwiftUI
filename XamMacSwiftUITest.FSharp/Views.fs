namespace XamMacSwiftUITest.FSharp

open SwiftUI

type ClickButton () =
    inherit View ()

    let counter = State (None)
    member __.Body =
        let text =
            match counter.Value with
            | None -> "Never been clicked"
            | Some i -> sprintf "Clicked %d times" i
        let button = Button ((fun () -> counter.Value <- Some ((defaultArg counter.Value 0) + 1)), Text (text))
        button.Opacity (if ((defaultArg counter.Value 0) % 2 = 0) then 0.5 else 1.0)