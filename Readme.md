# Xamarin.SwiftUI

A managed binding to SwiftUI.

## Build Status
[![.github/workflows/build.yml](https://github.com/chkn/Xamarin.SwiftUI/actions/workflows/build.yml/badge.svg)](https://github.com/chkn/Xamarin.SwiftUI/actions/workflows/build.yml)

## Project Status:
✅ **Active**.

Xamarin.SwiftUI provides a managed binding to Apple's next-generation [SwiftUI](https://developer.apple.com/documentation/swiftui) toolkit. Using your favourite .NET language, you should be able to create SwiftUI apps. However, only a handful of APIs are currently bound. [Work is ongoing](https://github.com/chkn/Xamarin.SwiftUI/tree/swift-parser2) on a binding generator to automatically cover the entire API surface.

<!--
## Nuget Status 
[![Version](https://img.shields.io/nuget/v/SwiftUI.NET.svg)](https://nuget.org/packages/SwiftUI.NET)
[![Downloads](https://img.shields.io/nuget/dt/SwiftUI.NET.svg)](https://nuget.org/packages/SwiftUI.NET)


## Nuget Download
📦 [NuGet](https://nuget.org/packages/SwiftUI.NET): `dotnet add package SwiftUI.NET`
-->

## Example

A simple custom view with state:

```csharp
using SwiftUI;
using static SwiftUI.Views;

public partial record ClickView : View
{
	readonly State<int> clicks = new State<int> (0);
	public View Body
		=> Button ($"Clicked {clicks.Value} times", () => clicks.Value++);
}
```

## Building from Source

### Prerequisites

- .NET 6 SDK
- Xcode 13 or newer

### Building

```
dotnet msbuild /restore build.proj
```

If you need to make changes to the SwiftUIGlue native glue library during development, you can rebuild just those bits by running:

```
dotnet msbuild build.proj /t:SwiftUIGlue
```

#### Packaging

The major and minor version of nuget packages created by the CI pipeline is controlled by the `VERSION` file. This does not need to be bumped for patch releases.

For local development, the version of the package produced can be overridden:

```
dotnet msbuild /restore build.proj /t:Pack /p:Version=X.X.XXX
```

---

For more information about the internals and guidance on developing the binding itself, see the document titled [Hacking](Hacking.md).
