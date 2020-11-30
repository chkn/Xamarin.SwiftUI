# Xamarin.SwiftUI

A managed binding to SwiftUI.

## Getting Started with Master Builds

[![Build Status](https://alcorra.visualstudio.com/Xamarin.SwiftUI/_apis/build/status/Xamarin.SwiftUI?branchName=master)](https://alcorra.visualstudio.com/Xamarin.SwiftUI/_build/latest?definitionId=1&branchName=master)

### Requirements

This binding is designed to provide a decent API from either C# 9 or F# 5.

- C# 9

  The NuGet package currently pulls in and enables the correct C# compiler and language version (see below). No manual configuration should be needed, but watch out for any settings in your csproj that might conflict.

- F# 5

  If you are using VS Mac, you must currently install [this Mono build](https://github.com/mono/mono/pull/20511#issuecomment-717160671) to get the F# 5 compiler for Xamarin.

### Install the NuGet Package

Until the project is ready for use in real apps, there are no official packages. For now, you can get a sneak peek by installing the latest package from the master feed:

1. Add this feed as a NuGet source: `https://pkgs.dev.azure.com/alcorra/Xamarin.SwiftUI/_packaging/master/nuget/v3/index.json`
2. Add the `Xamarin.SwiftUI` package to your project. If you do not see the package, ensure pre-release packages are enabled.

Adding the above package will make the following changes to your project:
- Set LangVersion to `preview` to enable source generators.
- Use a version of the Roslyn C# compiler that supports source generators.
- Enable C# reference type nullability, as passing null values to Swift is only supported with proper nullability annotations.

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
- .NET 5 SDK
- Xamarin toolchain
- Xcode 11 or newer
- Xcode [command line](https://developer.apple.com/library/archive/technotes/tn2339/_index.html) tools

### Build Everything and NuGet Package

```
msbuild build.proj
```

If you need to make changes to the SwiftUIGlue native glue library during development, you can rebuild just those bits by running:

```
msbuild build.proj /t:SwiftUIGlue /p:Configuration=Debug
```

#### Packaging

The major and minor version of nuget packages created by the CI pipeline is controlled by the `name:` element in `azure-pipelines.yaml`. This should be bumped for each release.

For local development, the version of the package produced can be overridden:

```
msbuild build.proj /p:Version=X.X.XXX
```

#### Troubleshooting

You may receive the following errors on the first couple builds:

- `error CS1617: Invalid option '9.0' for /langversion`
- `error CS0012: The type '...' is defined in an assembly that is not referenced. You must add a reference to assembly 'netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51'`

If you receive either of the above failures, simply run the command again.

### Visual Studio for Mac

Open `Xamarin.SwiftUI.sln`

For more information about the internals and guidance on developing the binding itself, see the document titled [Hacking](Hacking.md).
