module SwiftUI.Tests.FSharp.TypeTests

open Xunit

open Swift

[<Fact>]
let ``ReflectionExtensions.IsFSharpOption`` () =
    Assert.True (Nullability.IsNullable (Some("foo").GetType()))
    Assert.True (Nullability.IsNullable (typeof<int option>))
    Assert.True (Nullability.IsNullable (typedefof<option<_>>))
    Assert.True (Nullability.IsNullable (ValueSome("foo").GetType()))
    Assert.True (Nullability.IsNullable (typeof<int voption>))
    Assert.True (Nullability.IsNullable (typedefof<voption<_>>))