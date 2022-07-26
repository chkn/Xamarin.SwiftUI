
import Darwin
import SwiftSyntax

public protocol HasTypesToResolve {
	mutating func resolveTypes(_ resolve: (TypeRef) -> TypeRef)
}

public protocol Derivable: HasTypesToResolve {
	var inheritance: [TypeRef] { get set }
}

public extension Derivable {
	mutating func resolveTypes(_ resolve: (TypeRef) -> TypeRef)
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

public protocol HasMembers: HasTypesToResolve {
	var members: [MemberDecl] { get set }
}

public extension HasMembers {
	mutating func resolveTypes(_ resolve: (TypeRef) -> TypeRef)
	{
		for member in members {
			member.resolveTypes(resolve)
		}
	}
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

	open func resolveTypes(_ resolve: (TypeRef) -> TypeRef) {
		for member in members {
			member.resolveTypes(resolve)
		}
		for ext in extensions {
			ext.resolveTypes(resolve)
		}
	}
}

open class GenericTypeDecl: NominalTypeDecl {
	public var genericParameters: [GenericParameter] = []

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter($0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}

	open override func resolveTypes(_ resolve: (TypeRef) -> TypeRef) {
		super.resolveTypes(resolve)
		for i in 0..<genericParameters.count {
			var gp = genericParameters[i]
			gp.resolveTypes(resolve)
			genericParameters[i] = gp
		}
	}
}
