module SwiftUI.Tests.FSharp.ColorTests

open System
open System.Reflection

open Xunit

open Swift.Interop

open SwiftUI

[<Fact>]
let ``AllStaticColorCallsWork`` () =
    let t = typeof<Color>;
    let properties = t.GetProperties ()
    for item in properties do
        let colour = item.GetValue (null);
        Assert.NotNull (colour);

[<Fact>]
let ``ColorHSBOCallWorks`` () =
    let colour = new Color(0.0, 0.6, 0.0, 0.5)
    Assert.NotNull(colour)

[<Fact>]
let ``ColorColorSpaceRGBOCallWorks`` () =
    let colour = new Color(RGBColorSpace.DisplayP3, 0.0, 0.6, 0.0, 0.5)
    Assert.NotNull(colour)

[<Fact>]
let ``ColorColorSpaceWOCallWorks`` () =
    let colour = new Color(RGBColorSpace.sRGB, 0.0, 0.6)
    Assert.NotNull(colour)