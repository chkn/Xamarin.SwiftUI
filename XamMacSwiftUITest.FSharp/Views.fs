namespace XamMacSwiftUITest.FSharp

open SwiftUI

type HelloView() =
    inherit CustomView<HelloView>()
    member __.Body = Button((fun () -> printfn "CLICKED"), Text("HELLO WORLD"))
