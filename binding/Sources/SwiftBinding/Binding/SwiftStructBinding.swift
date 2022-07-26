
/// A struct bound as a managed reference type deriving from SwiftStruct
open class SwiftStructBinding: TypeBinding {
	public let baseClass: String
	public var primaryCtor: PrimaryCtorBinding? = nil

	public init(_ type: StructDecl, _ baseClass: String)
	{
		self.baseClass = baseClass
		super.init(type)
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
