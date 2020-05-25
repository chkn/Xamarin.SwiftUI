# Hacking on Xamarin.SwiftUI

This document details some internals and useful information for working on the binding itself.

## Binding

Swift classes are reference counted. Swift structs may contain pointers to instances of classes--when they do, they are considered non-POD and care must be taken to call into Swift to copy or destroy them.

- If possible, a `@frozen` Swift POD struct should be bound as a managed value type implementing `ISwiftBlittableStruct`.
- Other Swift structs should be bound as managed classes deriving from `SwiftStruct`, or for SwiftUI views, `View`. See the documentation on those classes for more discussion.

## Calling into Swift

The Swift calling convention is based on the C calling convention with some modifications. This means that you may be able to P/Invoke directly to the Swift API if it falls into the subset that overlaps with the C calling convention.

### Direct P/Invoke

When P/Invoking a Swift function, you must keep in mind that Swift may pass some hidden arguments.

- In addition to the declared arguments in the Swift signature, Swift appends a `this` argument for instance members, followed by type metadata for each generic parameter, first for generic arguments on the declaring type, then for those on the method itself. If the generic parameter is constrained by a protocol, there is another argument for the conformance pointer following the type metadata.
- When passing more than 1 generic parameter, the order is *important*. The order must be Generic1Pointer, Generic2Pointer, Generic1Metadata, Generic2Metadata, Generic1Protocol, Generic2Protocol

### Glue Function

For cases where the Swift calling convention differs from the C calling convention, we must write a glue funtion in Swift to call that API. See `src/SwiftUIGlue/SwiftUIGlue/Glue.swift` for examples and explanations.

### Using [Hopper Dissasembler](https://www.hopperapp.com/) to work out the signature for a native PInvoke call
We've found that Hopper has proven useful in finding out what calls are happening under the hood for a particular native SwiftUI call.
What's worked for us...

- Within Xcode create a Swift project
- Create the simplest version of the API call you are trying to PInvoke

e.g. Swift Code for creating a Color via HSBO:

```swift
public func CreateColourViaHSBO () -> Color
{
	return Color.init(hue: 0, saturation: 0, brightness: 0, opacity: 0)
}
```

- build the project
- Use Hopper to navigate to the executable or dylib and open it.
- Then do a search for `CreateColourViaHSBO()`
- In ASM Mode you should see something like...

```
call imp___stubs__$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC ; SwiftUI.Color.init(hue: Swift.Double, saturation: Swift.Double, brightness: Swift.Double, opacity: Swift.Double) -> SwiftUI.Color
```

- SwiftUI signature tend to start with `$`, so from the above we see that the full PInvoke signature is `$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC`
- Also from the above you clearly see what types are expected for hue, saturation, brightness and opacity.
- You can also confirm this on the command line by executing the following command

```
echo '$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC' | swift demangle
```

- which should give you the following output

```
SwiftUI.Color.init(hue: Swift.Double, saturation: Swift.Double, brightness: Swift.Double, opacity: Swift.Double) -> SwiftUI.Color
```

- Then in .NET you can set this call up as

```csharp
[DllImport (SwiftUILib.Path,
        CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC")]
static extern IntPtr CreateFromHSBO (
        double hue,
        double saturation,
        double brightness,
        double opacity);
```

- Where the `IntPtr` returned holds the data for the newly created `Color` object, in this instance. It's important to note that `Color` is a struct and is returned by value; it just happens that a `Color` value is exactly 1 pointer in size, and we can depend on that because the struct is declared `@frozen`.
- For a list of all the SwiftUI manged APIs you can call, please refer to this [SwiftUI.Framework](https://github.com/xybp888/iOS-SDKs/blob/master/iPhoneOS13.0.sdk/System/Library/Frameworks/SwiftUI.framework/SwiftUI.tbd) document.


### Using Hopper Dissasembler to work out Glue parameters
We've also found that Hopper has proven invaluable in working out the order and expected parameters (hidden or otherwise) when PInvoking to Swift.

- As before, withing Xcode, create the simplest version of the API call you are trying to PInvoke via glue code

eg. Swift Code

```swift
public func CallSetViewBackground()
{
	SetViewBackground(view: Text("Stuff"), value: Color.red)
}

// A View can have ANY view as a background
public func SetViewBackground<TView: View, TBackground: View>(view : TView, value : TBackground)
{
}
```

- build the project
- Use Hopper to navigate to the executable or dylib and open it.
- Run the Parse Swift Metadata script from the `misc` directory (need to add instructions on how to integrate this into Hopper)
- Then do a search for `CallSetViewBackground()`
The beginning of the code in ASM mode should look similar to this...

```assembly
_$s7testLib21CallSetViewBackgroundyyF:        // testLib.CallSetViewBackground() -> ()
00000000000039d0         push       rbp
00000000000039d1         mov        rbp, rsp
00000000000039d4         sub        rsp, 0xf0
00000000000039db         lea        rdi, qword [aStuff]                         ; "Stuff"
00000000000039e2         mov        esi, 0x5
00000000000039e7         mov        edx, 0x1
00000000000039ec         call       imp___stubs__$sSS21_builtinStringLiteral17utf8CodeUnitCount7isASCIISSBp_BwBi1_tcfC ; Swift.String.init(_builtinStringLiteral: Builtin.RawPointer, utf8CodeUnitCount: Builtin.Word, isASCII: Builtin.Int1) -> Swift.String
00000000000039f1         mov        rdi, rax
00000000000039f4         mov        rsi, rdx
00000000000039f7         call       imp___stubs__$s7SwiftUI18LocalizedStringKeyV13stringLiteralACSS_tcfC ; SwiftUI.LocalizedStringKey.init(stringLiteral: Swift.String) -> SwiftUI.LocalizedStringKey
00000000000039fc         mov        qword [rbp+var_80], rax
0000000000003a00         mov        qword [rbp+var_88], rdx
0000000000003a07         mov        byte [rbp+var_89], cl
0000000000003a0d         mov        qword [rbp+var_98], r8
0000000000003a14         call       _$s7SwiftUI4TextV_9tableName6bundle7commentAcA18LocalizedStringKeyV_SSSgSo8NSBundleCSgs06StaticI0VSgtcfcfA0_ ; default argument 1 of SwiftUI.Text.init(_: SwiftUI.LocalizedStringKey, tableName: Swift.String?, bundle: __C.NSBundle?, comment: Swift.StaticString?) -> SwiftUI.Text
0000000000003a19         mov        qword [rbp+var_A0], rax
0000000000003a20         mov        qword [rbp+var_A8], rdx
0000000000003a27         call       _$s7SwiftUI4TextV_9tableName6bundle7commentAcA18LocalizedStringKeyV_SSSgSo8NSBundleCSgs06StaticI0VSgtcfcfA1_ ; default argument 2 of SwiftUI.Text.init(_: SwiftUI.LocalizedStringKey, tableName: Swift.String?, bundle: __C.NSBundle?, comment: Swift.StaticString?) -> SwiftUI.Text
0000000000003a2c         mov        qword [rbp+var_B0], rax
0000000000003a33         call       _$s7SwiftUI4TextV_9tableName6bundle7commentAcA18LocalizedStringKeyV_SSSgSo8NSBundleCSgs06StaticI0VSgtcfcfA2_ ; default argument 3 of SwiftUI.Text.init(_: SwiftUI.LocalizedStringKey, tableName: Swift.String?, bundle: __C.NSBundle?, comment: Swift.StaticString?) -> SwiftUI.Text
```

- Now switch to the pseudo code view and uncheck "Remove potential dead code" and "Remove NOPs" at the top. This sometimes hides crucial information.
- Now in the psuedo code scroll down until you see

```swift
testLib.SetViewBackground<A, B where A: SwiftUI.View, B: SwiftUI.View>
```

Notice the parameter order of `(&var_70, &var_78, rdx, rcx)`:

* where var_70 is the result of the `SwiftUI.Text.init()` calls
* where var_78 is the result of the static call to the `SwiftUI.Color.red.getter`

As mentioned above in the section on Direct P/Invoking, there are also some hidden parameters due to the generic signature:

* rdx holds the `*type metadata for SwiftUI.Text`
* rcx holds the `*type metadata for SwiftUI.Color`
* r8 holds the `*protocol witness table for SwiftUI.Text`
* r9 holds the `*protocol witness table for SwiftUI.Color`

These are hidden parameters which we'll need later.

For more information about Swift Registers please refer to the [64-Bit Architecture Register Usage](https://github.com/apple/swift/blob/master/docs/ABI/RegisterUsage.md) document

- With the above information we can then create our glue function as...

```swift
@_silgen_name("swiftui_View_background")
public func SetViewBackground<TView: View, TBackground: View>(dest : UnsafeMutableRawPointer, view : TView, value : TBackground)
{
	let result = view.background(value)
	dest.initializeMemory(as: type(of: result), repeating: result, count: 1)
}
```

So in terms of .NET, as per our PInvoke notes above, when we call our glue function this becomes:

```csharp
ViewBackground (result.Pointer, viewHandle.Pointer, backgroundHandle.Pointer, viewType.Metadata, backgroundType.Metadata, viewType.GetProtocolConformance (SwiftUILib.ViewProtocol), backgroundType.GetProtocolConformance (SwiftUILib.ViewProtocol));
```
Where...
* `result.Pointer` is the pre-memory allocated pointer we'll use once the call above returns, which points to our newly created view.
* `viewHandle.Pointer` is the pointer to the View who's background we will change. This is equivalent to the Swift `SwiftUI.Text.init()` call above.
* `backgroundHandle.Pointer` is the pointer to the background we want to apply to the aforementioned View. This is equivalent to the Swift static call to the `SwiftUI.Color.red.getter` call above. Worth noting a `SwiftUI.Color`  conforms to `View`, so your background can be ANY `View`.
* `viewType.Metadata` is the equivalent  to the `*type metadata for SwiftUI.Text` stored in rdx above.
* `backgroundType.Metadata` is the equivalent  to the `*type metadata for SwiftUI.Color` stored in rcx above.
* `viewType.GetProtocolConformance (SwiftUILib.ViewProtocol)` is the equivalent  to the `*protocol witness table for SwiftUI.Text` stored in r8 above.
* `backgroundType.Metadata` is the equivalent  to the `*protocol witness table for SwiftUI.Color` stored in r9 above.

So we have 4 "hidden" parameters that need to be passed for SwiftUI to correctly marshal all the required information through our glue code.