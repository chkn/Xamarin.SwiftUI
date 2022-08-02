
open class SwiftUIViewBinding: SwiftStructBinding {

	open func prepare()
	{
		// For a view primary ctor, unwrap the @ViewBuilders
		if let pctor = primaryCtor {
			for i in 0..<pctor.parameters.count {
				let p = pctor.parameters[i]
				if p.attributes.contains(.ViewBuilder) {
					if case let .function(_, args, returnType) = p.type, args.isEmpty {
						pctor.parameters[i] = Parameter(attributes: p.attributes, firstName: p.firstName, secondName: p.secondName, type: returnType, variadic: p.variadic, defaultArgument: p.defaultArgument)
					}
				}
			}
		}
	}

	override open func write(to writer: Writer, csharp state: CSharpState)
	{
		self.prepare()
		super.write(to: writer, csharp: state)
		/*
		writer.write("\npartial class Views {\n")
		// FIXME: Non-primary ctors here
		writer.write("}\n")
		*/
	}
}
