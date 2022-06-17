# Xamarin.SwiftUI

A managed binding to SwiftUI.

## Build Status
[![.github/workflows/build.yml](https://github.com/chkn/Xamarin.SwiftUI/actions/workflows/build.yml/badge.svg)](https://github.com/chkn/Xamarin.SwiftUI/actions/workflows/build.yml)

## Project Status:
✅ **Active**.

Xamarin.SwiftUI provides a managed binding SwiftUI binding to Apple's [SwiftUI](https://developer.apple.com/documentation/swiftui?language=objc). Using your favourite .NET language, you should be able to create SwiftUI Apps.

## Nuget Status 
[![Version](https://img.shields.io/nuget/v/Xamarin.SwiftUI.svg)](https://nuget.org/packages/Xamarin.SwiftUI)
[![Downloads](https://img.shields.io/nuget/dt/Xamarin.SwiftUI.svg)](https://nuget.org/packages/Xamarin.SwiftUI)

## Nuget Download
📦 [NuGet](https://nuget.org/packages/Xamarin.SwiftUI): `dotnet add package Xamarin.SwiftUI`

## Getting Started with Master Builds

### Requirements

.NET 6

### Install the NuGet Package

Until the project is ready for use in real apps, there are no official packages. For now, you can get a sneak peek by installing the latest package from the master feed:

1. Add this feed as a NuGet source: `https://pkgs.dev.azure.com/alcorra/Xamarin.SwiftUI/_packaging/master/nuget/v3/index.json`
2. Add the `SwiftUI.NET` package to your project. If you do not see the package, ensure pre-release packages are enabled.

### Example

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

### Build Everything and NuGet Package

```
dotnet msbuild /restore build.proj
```

If you need to make changes to the SwiftUIGlue native glue library during development, you can rebuild just those bits by running:

```
dotnet msbuild build.proj /t:SwiftUIGlue /p:Configuration=Debug
```

#### Packaging

The major and minor version of nuget packages created by the CI pipeline is controlled by the `name:` element in `azure-pipelines.yaml`. This should be bumped for each release.

For local development, the version of the package produced can be overridden:

```
dotnet msbuild /restore build.proj /p:Version=X.X.XXX
```

---

For more information about the internals and guidance on developing the binding itself, see the document titled [Hacking](Hacking.md).
