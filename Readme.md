# Xamarin.SwiftUI

A managed binding to SwiftUI.

## Getting Started

Ensure you have the XCode [command line](https://developer.apple.com/library/archive/technotes/tn2339/_index.html) tools installed before starting.

For the time being you'll need to run **make** from the root directory of the repo.
This will use Xcode to build SwiftUIGlue dynamic lib which is referenced from both CSharp and FSharp projects.
It will also build those 2 projects, just to make sure everything is building correctly. 

## Hacking

For information about the internals and guidance on developing the binding itself, see the document titled [Hacking](Hacking.md).
