
/// A struct bound as a managed reference type deriving from SwiftStruct
open class SwiftStructBinding: TypeBinding {
	public let baseClass: String
	public var primaryCtor: PrimaryCtorBinding? = nil

	public required init(_ type: StructDecl, _ baseClass: String)
	{
		self.baseClass = baseClass
		super.init(type)
	}

	open func prepareForWriting()
	{
		// For a primary ctor, unwrap the @ViewBuilders
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
		self.prepareForWriting()
		super.write(to: writer, csharp: state)
	}

	override open func writeType(to writer: Writer, csharp: CSharpState)
	{
		writer.write("public unsafe sealed record \(genericFullName)")
		if let pctor = primaryCtor {
			writer.write(child: Writable(pctor, csharp))
		}
		writer.write(" : \(baseClass)\n")
		for gp in genericParameterConstraints {
			let types = gp.types.compactMap({ $0.qualifiedName }).sorted()
			if types.isEmpty { continue }
			writer.write("\twhere \(gp.name) : \(types.joined(separator: ", "))\n")
		}
		writer.write("{\n")

		writer.write("}\n")
	}
}
