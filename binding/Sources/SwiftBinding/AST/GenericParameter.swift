
import Foundation

import SwiftSyntax

public struct GenericParameter: HasTypesToResolve {
	/// The generic parameter attributes.
	public var attributes: [Attribute]

	/// The generic parameter name.
	public var name: String

	/// The generic parameter type, if any.
	public var type: Type?

	/// Creates an instance initialized with the given syntax node.
	public init(_ node: GenericParameterSyntax, _ whereClause: GenericWhereClauseSyntax?)
	{
		attributes = node.attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { Attribute(rawValue: $0.name) } ?? []

		name = node.name.text.trim()
		if let tyName = node.inheritedType?.name {
			type = UnresolvedType(tyName)
		}

		if let wc = whereClause {
			for req in wc.requirementList {
				if let node = ConformanceRequirementSyntax(req.body), node.leftTypeIdentifier.name == name {
					type = UnresolvedType(node.rightTypeIdentifier.name)
					break
				}
			}
		}
	}

	public mutating func resolveTypes(_ resolve: (Type) -> Type?)
	{
		if let ty = type {
			type = resolve(ty)
		}
	}
}
