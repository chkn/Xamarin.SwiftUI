namespace XamMacSwiftUITest.FSharp

open SwiftUI

type ClickButton () =
    inherit View ()

    let counter = State (0)
    member __.Body =
        let button = Button ((fun () -> counter.Value <- counter.Value + 1), Text (sprintf "Clicked %d times" counter.Value))
        button.Opacity (if (counter.Value % 2 = 0) then 0.5 else 1.0)