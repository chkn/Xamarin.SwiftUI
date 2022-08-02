
open class MethodBinding: Binding, CSharpWritable, FSharpWritable {
	public let decl: FunctionDecl

	public var parameters: [Parameter]

	public var id: String { decl.qualifiedName }

	public init(_ decl: FunctionDecl)
	{
		self.decl = decl
		self.parameters = decl.parameters
	}

	open func writeAccessibilityAndName(to writer: Writer, csharp: CSharpState)
	{
		// FIXME
	}

	open func writeAccessibilityAndName(to writer: Writer, fsharp: FSharpState)
	{
		// FIXME
	}

	open func parameterString(_ p: Parameter, csharp: CSharpState) -> String?
	{
		guard let typeStr = csharp.string(for: p.type!) else { return nil }
		return "\(typeStr) \((p.secondName ?? p.firstName)!)"
	}

	open func parameterString(_ p: Parameter, fsharp: FSharpState) -> String?
	{
		guard let typeStr = fsharp.string(for: p.type!) else { return nil }
		return "\((p.secondName ?? p.firstName)!): \(typeStr)"
	}

	open func writeParameterList<S: LanguageState>(to writer: Writer, _ state: S)
	{
		var hadPrev = false
		for p in parameters {
			var maybeStr: String? = nil
			if let csharp = state as? CSharpState {
				maybeStr = parameterString(p, csharp: csharp)
			} else if let fsharp = state as? FSharpState {
				maybeStr = parameterString(p, fsharp: fsharp)
			}
			if let str = maybeStr {
				if hadPrev {
					writer.write(", " + str)
				} else {
					writer.write("(")
					writer.write(str)
					hadPrev = true
				}
			} else {
				// if the parameter type is erased, erase the containing declaration unless the parameter is optional
				if p.defaultArgument == nil {
					// FIXME
					fatalError("erase me!")
				}
			}
		}

		if hadPrev {
			writer.write(")")
		} else {
			writer.write("()")
		}
	}

	open func writeBody(to writer: Writer, csharp: CSharpState)
	{
		// FIXME
	}

	open func writeBody(to writer: Writer, fsharp: FSharpState)
	{
		// FIXME
	}

	open func write(to writer: Writer, csharp state: CSharpState)
	{
		writeAccessibilityAndName(to: writer, csharp: state)
		writeParameterList(to: writer, state)
		writeBody(to: writer, csharp: state)
	}

	open func write(to writer: Writer, fsharp state: FSharpState)
	{
		writeAccessibilityAndName(to: writer, fsharp: state)
		writeParameterList(to: writer, state)
		writeBody(to: writer, fsharp: state)
	}
}
