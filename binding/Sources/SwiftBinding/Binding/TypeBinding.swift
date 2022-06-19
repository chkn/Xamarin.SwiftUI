
open class TypeBinding: Binding {
	public let type: GenericTypeDecl

	public var name: String { type.name }
	public var qualifier: String? { type.context?.qualifiedName }
	public var libPath: String { type.module.runtimeLibPath }
	public var genericParameters: [GenericParameter] { type.genericParameters }

	public let genericFullName: String
	public private(set) var genericParameterConstraints: [GenericParameter]

	public let children: [Binding]
	public var id: String { "\(qualifier?.appending(".") ?? "")\(name)" }

	public init(_ type: GenericTypeDecl)
	{
		self.type = type
		self.genericFullName = type.genericParameters.isEmpty ? type.name : "\(type.name)<" + type.genericParameters.map({ $0.name }).sorted().joined(separator: ", ") + ">"
		self.genericParameterConstraints = type.genericParameters.filter({ !$0.types.isEmpty })
		self.children = []
	}

	/// Applies the generic parameter constraints from the given extension
	func apply(_ ext: ExtensionDecl, _ resolve: (String) -> TypeDecl?)
	{
		for req in ext.genericRequirements {
			if req.relation == .conformance {
				if let i = genericParameterConstraints.firstIndex(where: { $0.name == req.leftTypeIdentifier }) {
					var gp = genericParameterConstraints[i]
					if let ty = resolve(req.rightTypeIdentifier) {
						gp.types.append(ty)
					}
					genericParameterConstraints[i] = gp
					continue
				}
				if var gp = genericParameters.first(where: { $0.name == req.leftTypeIdentifier }) {
					if let ty = resolve(req.rightTypeIdentifier) {
						gp.types.append(ty)
					}
					genericParameterConstraints.append(gp)
				}
			}
		}
	}

	open func write(_ writer: Writer)
	{
		writer.write("using System;\nusing System.Runtime.InteropServices;\n\n")

		if let ns = qualifier {
			writer.write("namespace \(ns);\n\n")
		}
		writer.write("[Swift.Interop.SwiftImport (\"\(libPath)\")]\n")
		writeType(writer)
	}

	open func writeType(_ writer: Writer)
	{
	}
}
