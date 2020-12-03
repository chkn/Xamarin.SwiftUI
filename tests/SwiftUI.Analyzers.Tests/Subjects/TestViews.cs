using System;
using Swift.Interop;

public record View
{
}

[Obsolete]
public record CustomViewWithoutBody : SwiftUI.View, IDisposable
{
	public string Body => "foof";
}

public record CustomViewWithViewBody : SwiftUI.View
{
	public SwiftUI.View Body => new SwiftUI.Text ("HELLO");
}

public record NotCustomViewBaseClass : View
{
}

public record NotCustomViewBaseClassDerived : CustomViewWithViewBody
{
}

[SwiftImport ("foo")]
public record NotCustomViewAttribute : SwiftUI.View
{
}

