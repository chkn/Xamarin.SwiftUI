# Xamarin.SwiftUI

A managed binding to SwiftUI.

## Master Builds

[![Build Status](https://alcorra.visualstudio.com/Xamarin.SwiftUI/_apis/build/status/Xamarin.SwiftUI?branchName=master)](https://alcorra.visualstudio.com/Xamarin.SwiftUI/_build/latest?definitionId=1&branchName=master)

Until the project is ready for use in real apps, there are no official packages. For now, you can get a sneak peek by installing the latest package from the master feed:

1. Add this feed as a NuGet source: `https://pkgs.dev.azure.com/alcorra/Xamarin.SwiftUI/_packaging/master/nuget/v3/index.json`
2. Add the `Xamarin.SwiftUI` package to your project.

## Building from Source

### Prerequisites
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

```
msbuild build.proj /p:Version=X.X.XXX
```

The major and minor version of nuget packages created by the CI pipeline is controlled by the `name:` element in `azure-pipelines.yaml`. This should be bumped for each release.

### Visual Studio for Mac

Open `Xamarin.SwiftUI.sln`

For more information about the internals and guidance on developing the binding itself, see the document titled [Hacking](Hacking.md).
