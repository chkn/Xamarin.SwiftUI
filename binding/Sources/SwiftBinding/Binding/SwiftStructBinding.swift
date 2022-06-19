
/// A struct bound as a managed reference type deriving from SwiftStruct
public class SwiftStructBinding: TypeBinding {
	public let baseClass: String

	public var primaryCtor: PrimaryCtorBinding? = nil

	public init(_ type: StructDecl, _ baseClass: String)
	{
		self.baseClass = baseClass
		super.init(type)
	}

	override public func writeType(_ writer: Writer)
	{
		writer.write("public unsafe sealed record \(genericFullName)")
		if let pctor = primaryCtor {
			writer.write(child: pctor)
		}
		writer.write(" : \(baseClass)\n")
		for gp in genericParameterConstraints {
			writer.write("\twhere \(gp.name) : \(gp.types.map({ $0.qualifiedName }).sorted().joined(separator: ", "))\n")
		}
		writer.write("{\n")

		writer.write("}\n")
	}
}
