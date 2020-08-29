// WARNING - AUTO-GENERATED - DO NOT EDIT!!!
//
// -> To regenerate, run `swift run` from binding directory.
//
using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public unsafe sealed class {{type.genericFullName}} : {{type.baseClass}}{% for gp in type.genericParameterConstraints %}
		where {{ gp.name }} : {{ gp.types|join:", " }}{% endfor %}
	{

	}

	public partial class Views
	{

	}
}
