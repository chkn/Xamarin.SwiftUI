module Views

open SwiftUI


let HelloView() =
    View {
        body (
            Button((fun () -> printfn "CLICKED"), Text("HELLO WORLD"))
        )
    }
