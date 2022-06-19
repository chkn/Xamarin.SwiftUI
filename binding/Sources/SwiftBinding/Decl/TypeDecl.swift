
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

public protocol HasMembers {
	var members: [MemberDecl] { get set }
}

public extension HasMembers where Self: Extendable {
	var membersIncludingExtensions: [MemberDecl] {
		var result = members
		result.append(contentsOf: extensions.filter({ $0.genericRequirements.isEmpty }).flatMap { $0.members })
		return result
	}
}

open class TypeDecl: Decl {
	public var typeCode: Character? { nil }
	public var isFrozen: Bool { attributes.contains(.frozen) }
	public var isNonPOD: Bool { valueWitnessTable?.pointee.isNonPOD ?? true }

	override public var metadataSymbolName: String? {
		guard let ctx = context?.metadataSymbolName, let tc = typeCode else { return nil }
		return "\(ctx)\(name.count)\(name)\(tc)N"
	}

	lazy var valueWitnessTable: UnsafePointer<ValueWitnessTable>? =
	{
		guard let sym = metadataSymbolName else { return nil }
		return dlsym(module.lib, sym)?
			.advanced(by: -MemoryLayout<UnsafeRawPointer>.size)
			.assumingMemoryBound(to: UnsafePointer<ValueWitnessTable>.self)
			.pointee
	}()
}

open class NominalTypeDecl: TypeDecl, HasMembers, Extendable {
	public var members: [MemberDecl] = []
	public var extensions: [ExtensionDecl] = []
}

open class GenericTypeDecl: NominalTypeDecl {
	public var genericParameters: [GenericParameter] = []

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter(in: context, $0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}
}
