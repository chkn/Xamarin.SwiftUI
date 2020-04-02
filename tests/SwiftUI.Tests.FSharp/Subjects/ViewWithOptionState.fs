namespace SwiftUI.Tests.FSharp

open SwiftUI

type ViewWithOptionState() =
    inherit View()
    let count = State(Some 0)
    member __.Body = Text((defaultArg count.Value 0).ToString())
