
import Foundation

import SwiftSyntax

public struct GenericParameter: HasTypesToResolve {
	/// The generic parameter attributes.
	public var attributes: [DeclAttribute]

	/// The generic parameter name.
	public var name: String

	/// The generic parameter types, if any.
	public var types: [TypeDecl] = []

	/// Creates an instance initialized with the given syntax node.
	public init(in context: Decl?, _ node: GenericParameterSyntax, _ whereClause: GenericWhereClauseSyntax?)
	{
		attributes = node.attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { DeclAttribute(rawValue: $0.name) } ?? []

		name = node.name.text.trim()
		if let tyName = node.inheritedType?.name {
			types.append(UnresolvedTypeDecl(in: context, name: tyName))
		}

		if let wc = whereClause {
			for req in wc.requirementList {
				if let node = ConformanceRequirementSyntax(req.body), node.leftTypeIdentifier.name == name {
					types.append(UnresolvedTypeDecl(in: context, name: node.rightTypeIdentifier.name))
				}
			}
		}
	}

	public mutating func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?)
	{
		types = types.compactMap(resolve)
	}
}
