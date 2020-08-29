
// must only use stored properties due to Swift's poor reflection
open class Binding {
	public let name: String
	public let genericParameters: [GenericParameter]
	public let genericFullName: String
	public private(set) var genericParameterConstraints: [GenericParameter]

	public init(_ type: GenericTypeDecl)
	{
		self.name = type.name
		self.genericParameters = type.genericParameters
		self.genericFullName = genericParameters.isEmpty ? name : "\(name)<" + genericParameters.map({ $0.name }).joined(separator: ", ") + ">"
		self.genericParameterConstraints = type.genericParameters.filter({ !$0.types.isEmpty })
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
}
