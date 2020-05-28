using System;
using Swift.Interop;

public class View
{
}

[Obsolete]
public class CustomViewWithoutBody : SwiftUI.View, IDisposable
{
	public string Body => "foof";
}

public class CustomViewWithViewBody : SwiftUI.View
{
	public SwiftUI.View Body => new SwiftUI.Text ("HELLO");
}

public class NotCustomViewBaseClass : View
{
}

public class NotCustomViewBaseClassDerived : CustomViewWithViewBody
{
}

[SwiftImport ("foo")]
public class NotCustomViewAttribute : SwiftUI.View
{
}

