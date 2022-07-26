
open class PrimaryCtorBinding: MethodBinding {

	public init(_ decl: InitializerDecl)
	{
		super.init(decl)
	}

	override open func write(to writer: Writer, csharp state: CSharpState)
	{
		writeParameterList(to: writer, state)
	}

	override open func write(to writer: Writer, fsharp state: FSharpState)
	{
		writeParameterList(to: writer, state)
	}
}
