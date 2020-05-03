# Hacking on Xamarin.SwiftUI

This document details some internals and useful information for working on the binding itself.

## Binding

Swift classes are reference counted. Swift structs may contain pointers to instances of classes--when they do, they are considered non-POD and care must be taken to call into Swift to copy or destroy them.

- If possible, a Swift POD struct should be bound as a managed value type implementing `ISwiftBlittableStruct`.
- Other Swift structs should be bound as managed classes deriving from `SwiftStruct`, or for SwiftUI views, `View`. See the documentation on those classes for more discussion.

## Calling into Swift

The Swift calling convention is based on the C calling convention with some modifications. This means that you may be able to P/Invoke directly to the Swift API if it falls into the subset that overlaps with the C calling convention.

### Direct P/Invoke

When P/Invoking a Swift function, you must keep in mind that Swift may pass some hidden arguments.

- In addition to the declared arguments in the Swift signature, Swift appends a `this` argument for instance members, followed by type metadata for each generic parameter, first for generic arguments on the declaring type, then for those on the method itself. If the generic parameter is constrained by a protocol, there is another argument for the conformance pointer following the type metadata.
- When passing more than 1 generic parameter, the order is *important*. The order must be Generic1Pointer, Generic2Pointer, Generic1Metadata, Generic2Metadata, Generic1Protocol, Generic2Protocol

### Glue Function

For cases where the Swift calling convention differs from the C calling convention, we must write a glue funtion in Swift to call that API. See `src/SwiftUIGlue/SwiftUIGlue/Glue.swift` for examples and explanations.

