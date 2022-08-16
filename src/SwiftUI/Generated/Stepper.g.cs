// WARNING - AUTO-GENERATED - DO NOT EDIT!!!
//
// -> To regenerate, run `swift run` from binding directory.
//
using System;
using System.Runtime.InteropServices;

namespace SwiftUI;

[Swift.Interop.SwiftImport ("/System/Library/Frameworks/SwiftUI.framework/SwiftUI")]
public unsafe sealed record Stepper<Label>(System.Action? onIncrement, System.Action? onDecrement, System.Action<System.Boolean> onEditingChanged, Label label) : SwiftUI.View
	where Label : SwiftUI.View
{
}

partial class Views {

}