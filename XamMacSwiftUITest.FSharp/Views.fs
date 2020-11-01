namespace XamMacSwiftUITest.FSharp

open SwiftUI

open type SwiftUI.Views

type ClickButton () =
    inherit View ()

    let counter = State (None)
    member __.Body =
        let text =
            match counter.Value with
            | None -> "Never been clicked"
            | Some i -> sprintf "Clicked %d times" i
        let button = Button(fun () -> counter.Value <- Some ((defaultArg counter.Value 0) + 1)) {
            Text(text)
            Text("2")
            Text("3")
            Text("4")
            Text("5")
            Text("6")
            Text("7")
            Text("8")
            Text("9")
            Text("10")
        }

        let colour = 
            match counter.Value with
            | None -> Color.Yellow
            | Some c -> if ((defaultArg counter.Value 0) % 2 = 0) then Color.Red else Color.Blue

        // Using string literals here as nameof() crashes
        let colourText = 
            match counter.Value with
            | None -> "Yellow"
            | Some ct -> if ((defaultArg counter.Value 0) % 2 = 0) then "Red" else "Blue"

        button.Background (Text(colourText).Background(colour))