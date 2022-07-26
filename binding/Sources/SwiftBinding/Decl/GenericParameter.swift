
import Foundation

import SwiftSyntax

public struct GenericParameter: HasTypesToResolve {
	/// The generic parameter attributes.
	public var attributes: [DeclAttribute]

	/// The generic parameter name.
	public var name: String

	/// The generic parameter types, if any.
	public var types: [TypeRef] = []

	/// Creates an instance initialized with the given syntax node.
	public init(_ node: GenericParameterSyntax, _ whereClause: GenericWhereClauseSyntax?)
	{
		attributes = node.attributes?.compactMap(DeclAttribute.parse) ?? []

		name = node.name.text.trim()
		if let ty = node.inheritedType {
			types.append(TypeRef.parse(ty))
		}

		if let wc = whereClause {
			for req in wc.requirementList {
				if let node = ConformanceRequirementSyntax(req.body), node.leftTypeIdentifier.name == name {
					types.append(TypeRef.parse(node.rightTypeIdentifier))
				}
			}
		}
	}

	public mutating func resolveTypes(_ resolve: (TypeRef) -> TypeRef)
	{
		types = types.map(resolve)
	}
}
