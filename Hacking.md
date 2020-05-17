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

### Using Hopper Dissasembler (https://www.hopperapp.com/) to work out the signature for a native PInvoke call
We've found that Hopper has proven useful in finding out what calls are happening under the hood for a particular native SwiftUI call.
What's worked for us...

- Within Xcode create a Swift project
- Create the simplest version of the API call you are trying to PInvoke
eg Swift Code for creating a Color via HSBO
public func CreateColourViaHSBO () -> Color
{
    let ColorHSBO = Color.init( hue: 0, saturation: 0, brightness: 0, opacity: 0)
    return ColorHSBO
}

- build the project
- Use Hopper to navigate to the executable or dylib and open it.
- Then do a search for `CreateColourViaHSBO()`
- In ASM Mode you should see something like...
call       imp___stubs__$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC ; SwiftUI.Color.init(hue: Swift.Double, saturation: Swift.Double, brightness: Swift.Double, opacity: Swift.Double) -> SwiftUI.Color
- SwiftUI signature tend to start with $ so form the above we see that the full PInvoke signature is `$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC`
- Also from the above you clearly see what types are expected for hue, saturation, brightness and opacity.
- You can also confirm this on the command line by executing the following command
echo '$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC' | swift demangle
- which should give you the following output
SwiftUI.Color.init(hue: Swift.Double, saturation: Swift.Double, brightness: Swift.Double, opacity: Swift.Double) -> SwiftUI.Color
- Then in .NET you can set this call up as
		[DllImport (SwiftUILib.Path,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "$s7SwiftUI5ColorV3hue10saturation10brightness7opacityACSd_S3dtcfC")]
		static extern IntPtr CreateFromHSBO (
			double hue,
			double saturation,
			double brightness,
			double opacity);
- Where the IntPtr returned holds the data for the newly created Color object, in this instance.


### Using Hopper Dissasembler to work out Glue parameters
We've also found that Hopper has proven invaluable in working out the order and expected parameters (hidden or otherwise) when PInvoking to Swift.

- As before, withing Xcode, create the simplest version of the API call you are trying to PInvoke via glue code
eg. Swift Code
public func CallSetViewBackground()
{
    SetViewBackground(view: Text("Stuff"), value: Color.red)
}

// A View can have ANY view as a background
public func SetViewBackground<TView: View, TBackground: View>(view : TView, value : TBackground)
{
}

- build the project
- Use Hopper to navigate to the executable or dylib and open it.
- Run the Parse Swift Metadata script Alex wrote (need to add instructions on how to integrate this into Hopper)
- Then do a search for `CallSetViewBackground()`
It should look similar to this...

- Then switch to the pseudo code view and uncheck "Remove potential dead code" and "Remove NOPs" at the top
- Find the actual call to SetViewBackground()
It should look similar to this...


- Notice the parameter order of `(&var_70, &var_78, rdx, rcx)`
    where var_70 is the result of the `SwiftUI.Text.init()` calls
    where var_78 is the result of the static call to the `SwiftUI.Color.red.getter`
    where rdx represents the `*type metadata for SwiftUI.Text`
    where rcs represents the `*type metadata for SwiftUI.Color`
Also worth noting that...
    r8 holds the `*protocol witness table for SwiftUI.Text`
    r9 holds the `*protocol witness table for SwiftUI.Color`
These are hidden parameters which we'll need later.

- With this information we can then create our glue function as...
@_silgen_name("swiftui_View_background")
public func SetViewBackground<TView: View, TBackground: View>(dest : UnsafeMutableRawPointer, view : TView, value : TBackground)
{
    let result = view.background(value)
    dest.initializeMemory(as: type(of: result), repeating: result, count: 1)
}

- So in terms of .NET, as per our PInvoke notes above, when we call our glue function this becomes....
ViewBackground (result.Pointer, viewHandle.Pointer, backgroundHandle.Pointer, viewType.Metadata, backgroundType.Metadata, viewType.GetProtocolConformance (SwiftUILib.ViewProtocol), backgroundType.GetProtocolConformance (SwiftUILib.ViewProtocol));
    - where result.Pointer is the pre-memory allocated pointer we'll use once the call above returns, which points to our newly created view.
    - where viewHandle.Pointer is the pointer to the View who's background we will change. This is equivalent to the Swift `SwiftUI.Text.init()` call above.
    - where backgroundHandle.Pointer is the pointer to the background we want to apply to the aforementioned View. This is equivalent to the Swift static call to the `SwiftUI.Color.red.getter` call above. Worth noting a Color, in SwiftUI is a View, so your background can be ANY View.
    - where viewType.Metadata is the equivalent  to the `*type metadata for SwiftUI.Text` stored in rdx above.
    - where backgroundType.Metadata is the equivalent  to the `*type metadata for SwiftUI.Color` stored in rcs above.
    - where viewType.GetProtocolConformance (SwiftUILib.ViewProtocol) is the equivalent  to the `*protocol witness table for SwiftUI.Text` stored in r8 above.
    - where backgroundType.Metadata is the equivalent  to the `*protocol witness table for SwiftUI.Color` stored in r9 above.
So we have 4 "hidden" parameters that need to be passed for SwiftUI to correctly marshal all the required information through our glue code.