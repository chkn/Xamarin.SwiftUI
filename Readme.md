# SwiftUI C# Binding

This is intended to be a "bare metal" binding to SwiftUI. The API is low-level; in places where we had a choice to make a more efficient API or an easier-to-use one, we chose efficiency.

**IMPORTANT**: This binding exposes some inner workings of the Swift memory model. You _must_ follow the guidelines below or you may leak memory or have corruption!

## Memory Management

To reduce overhead, types that are structs in SwiftUI are also exposed to C# as structs implementing `IDisposable`. To have proper memory management semantics, value ownership must be tracked, and any owned values must be ultimately disposed or they may leak memory.

The easiest way to handle this is to create all values with `using var`. For example:

```csharp
public static SwiftUI.Text MakeText (string str)
{
	using var swiftStr = new SwiftString (str);
	DoSomething (swiftStr.Copy ());
	return new Text (swiftStr.Copy ());
}
```

In the above example, `MakeText` owns `swiftStr` up until the end of the method until it is implicitly disposed because it was declared with `using var`. Copies of `swiftStr` are created that are owned by `DoSomething` and the `new Text` respectively. This is the safest pattern, but it might result in unnecessary copying.

Rather than keeping your ownership and ultimately disposing the value, it is also possible to transfer your ownership and move the value. In this example, we can transfer the ownership of `swiftStr` to `new Text` because we don't use it after that point:

```csharp
public static SwiftUI.Text MakeText (string str)
{
	var swiftStr = new SwiftString (str);
	DoSomething (swiftStr.Copy ());
	return new Text (swiftStr);
	// swiftStr is invalid after the above call because it was moved not Copied!
}
```
We still need the copy for `DoSomething` because we still need to use the value after that point. We also don't dispose `swiftStr` in the above case, because the value is no longer valid after it is passed to `new Text`.

**Note about Boxing**: Boxing operations on Swift values are implicitly move operations-- the original value should not be used after boxing.

## Swift ABI notes

### Calling Convention

Swift passes `this` argument last, followed by type metadata for each generic parameter, first for generic arguments on the declaring type, then for those on the method. If the generic parameter is constrained by a protocol, there is another argument for the conformance pointer following the type metadata.

