
import Darwin
import SwiftSyntax

public protocol HasTypesToResolve {
	mutating func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?)
}

public protocol Derivable: HasTypesToResolve {
	var inheritance: [TypeDecl] { get set }
}

public extension Derivable {
	mutating func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?)
	{
		inheritance = inheritance.compactMap(resolve)
	}

	func inherits(from qualifiedName: String) -> Bool
	{
		self.inheritance.contains(where: { $0.qualifiedName == qualifiedName })
	}
}

public protocol Extendable {
	var extensions: [ExtensionDecl] { get set }
}

public extension Extendable {
	func extensionInherits(from qualifiedName: String) -> ExtensionDecl?
	{
		self.extensions.first(where: { $0.inherits(from: qualifiedName) })
	}
}



open class TypeDecl: Decl {
	public var typeCode: Character? { nil }
	public var isFrozen: Bool { attributes.contains(.frozen) }
}

open class GenericTypeDecl: TypeDecl {
	public var genericParameters: [GenericParameter] = []

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter(in: context, $0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}
}
