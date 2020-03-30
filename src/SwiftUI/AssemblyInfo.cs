using System;
using System.Runtime.CompilerServices;

#if __UNIFIED__
using ObjCRuntime;
#endif

[assembly: InternalsVisibleTo ("SwiftUI.Tests")]

#if __MACOS__
[assembly: LinkWith ("libSwiftUIGlue.dylib", Dlsym = DlsymOption.Disabled)]
#elif __UNIFIED__
[assembly: LinkWith ("SwiftUIGlue.framework", Dlsym = DlsymOption.Disabled)]
#endif
