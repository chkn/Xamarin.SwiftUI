// WARNING - AUTO-GENERATED - DO NOT EDIT!!!
//
// -> To regenerate, run `swift run` from tools/Generator
//
using System;
using System.Runtime.InteropServices;

using Swift;
using Swift.Interop;

namespace SwiftUI
{
	[SwiftImport (SwiftUILib.Path)]
	public unsafe sealed class {{className}}{% if genericParamNames.count > 0 %}<{{ genericParamNames|join:", " }}>{% endif %} : {{baseClass}}{% for gp in genericParamWhere %}
		where {{ gp.name }} : {{ gp.type }}{% endfor %}
	{

	}

	public partial class Views
	{

	}
}
