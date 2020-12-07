
/// A struct bound as a managed reference type deriving from SwiftStruct
public class SwiftStructBinding: TypeBinding {
	public let baseClass: String

	public init(_ type: StructDecl, _ baseClass: String)
	{
		self.baseClass = baseClass
		super.init(type)
	}

	override public func writeType(_ writer: Writer)
	{
		writer.write("\tpublic unsafe sealed class \(genericFullName) : \(baseClass)\n")
		for gp in genericParameterConstraints {
			writer.write("\t\twhere \(gp.name) : \(gp.types.map({ $0.qualifiedName }).sorted().joined(separator: ", "))\n")
		}
		writer.write("\t{\n")

		writer.write("\t}\n")
	}
}
