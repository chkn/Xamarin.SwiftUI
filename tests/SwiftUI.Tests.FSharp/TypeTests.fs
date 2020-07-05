module SwiftUI.Tests.FSharp.TypeTests

open System

open FSharp.NativeInterop

open Xunit

open Swift.Interop

[<Fact>]
let ``Nullability.IsReifiedNullable`` () =
    Assert.True (Nullability.IsReifiedNullable (Some("foo").GetType()))
    Assert.True (Nullability.IsReifiedNullable (typeof<int option>))
    Assert.True (Nullability.IsReifiedNullable (typedefof<option<_>>))
    Assert.True (Nullability.IsReifiedNullable (ValueSome("foo").GetType()))
    Assert.True (Nullability.IsReifiedNullable (typeof<int voption>))
    Assert.True (Nullability.IsReifiedNullable (typedefof<voption<_>>))

[<Fact>]
let ``Nullability.IsNull`` () =
    Assert.True (Nullability.IsNull (None))
    Assert.True (Nullability.IsNull (ValueNone))
    Assert.False (Nullability.IsNull (Some 0))
    Assert.False (Nullability.IsNull (ValueSome 0))
    Assert.False (Nullability.IsNull (Some null))
    Assert.False (Nullability.IsNull (ValueSome null))

#nowarn "9" // unverifiable code
let assertRoundtrip (v : 'a) =
    use hnd = v.GetSwiftHandle()
    let ptr =
        hnd.Pointer
        |> NativePtr.ofVoidPtr<int>
        |> NativePtr.toNativeInt
    SwiftValue.FromNative(ptr, typeof<'a>)
    |> unbox
    |> (=) v
    |> Assert.True

[<Fact>]
let ``SwiftValue roundtrips`` () =
    assertRoundtrip 5
    assertRoundtrip -5
    assertRoundtrip 5u
    assertRoundtrip 5y
    assertRoundtrip -5y
    assertRoundtrip 5uy
    assertRoundtrip 5s
    assertRoundtrip -5s
    assertRoundtrip 5us
    assertRoundtrip 3.14f
    assertRoundtrip -3.14f
    assertRoundtrip 3.14
    assertRoundtrip -3.14
    assertRoundtrip "hello"
    assertRoundtrip (Nullable<_>(5))
    assertRoundtrip (Nullable<int>())
    assertRoundtrip (Some "hello")
    assertRoundtrip (None : string option)
    assertRoundtrip (ValueSome 5)
    assertRoundtrip (ValueNone : int voption)
