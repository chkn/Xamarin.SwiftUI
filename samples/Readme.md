# Samples

To build the samples from source, you must first run `msbuild build.proj` in the root of this repo to generate a nuget package.

Samples are categorized first by language, and then into 2 different styles:

1. **App-style** - A single project for all platforms, using `SwiftUI.App`. This is the preferred style for new apps targeting iOS 14 and macOS 11 or newer.
2. **Classic** - Classic Xamarin project-per-platform configuration. For integrating SwiftUI into existing Xamarin apps, or if you must target iOS 13 or macOS 10.15 or newer.

The sample SwiftUI views themselves are shared across both styles and are located at the root of the language directory.