
open class TypeBinding: Binding, CSharpWritable {
	public let type: GenericTypeDecl

	public var name: String { type.name }
	public var qualifier: String? { type.context?.qualifiedName }
	public var libPath: String { type.module.runtimeLibPath }
	public var genericParameters: [GenericParameter] { type.genericParameters }

	public let genericFullName: String
	public private(set) var genericParameterConstraints: [GenericParameter]

	public var id: String { "\(qualifier?.appending(".") ?? "")\(name)" }

	public init(_ type: GenericTypeDecl)
	{
		self.type = type
		self.genericFullName = type.genericParameters.isEmpty ? type.name : "\(type.name)<" + type.genericParameters.map({ $0.name }).sorted().joined(separator: ", ") + ">"
		self.genericParameterConstraints = type.genericParameters.filter({ !$0.types.isEmpty })
	}

	/// Applies the generic parameter constraints from the given extension
	func apply(_ ext: ExtensionDecl)
	{
		for req in ext.genericRequirements {
			if req.relation == .conformance {
				if let i = genericParameterConstraints.firstIndex(where: { $0.name == req.leftType.qualifiedName }) {
					var gp = genericParameterConstraints[i]
					gp.types.append(req.rightType)
					genericParameterConstraints[i] = gp
					continue
				}
				if var gp = genericParameters.first(where: { $0.name == req.leftType.qualifiedName }) {
					gp.types.append(req.rightType)
					genericParameterConstraints.append(gp)
				}
			}
		}
	}

	open func writePrelude(to writer: Writer, csharp: CSharpState)
	{
		// FIXME: get usings from state?
		writer.write("using System;\nusing System.Runtime.InteropServices;\n\n")

		if let ns = qualifier {
			writer.write("namespace \(ns);\n\n")
		}
		writer.write("[Swift.Interop.SwiftImport (\"\(libPath)\")]\n")
	}

	open func writeType(to writer: Writer, csharp: CSharpState)
	{
	}

	open func write(to writer: Writer, csharp state: CSharpState) {
		writePrelude(to: writer, csharp: state)
		writeType(to: writer, csharp: state)
	}
}
