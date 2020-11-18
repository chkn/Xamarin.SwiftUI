
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

	public lazy var valueWitnessTable: ValueWitnessTable? =
	{
		guard let tc = typeCode, let bin = module.binary else { return nil }

		// FIXME: This search is pretty fuzzy and probably error prone
		var offs = bin.findSymbol({ $0.hasSuffix("\(name.count)\(name)\(tc)N") })
		if offs == 0 {
			return nil
		}

		offs -= UInt64(MemoryLayout<UnsafeRawPointer>.size)

		bin.reader.seek(to: offs)
		let vwt: ValueWitnessTable = bin.reader.read()

		return vwt
	}()
}

open class GenericTypeDecl: TypeDecl {
	public var genericParameters: [GenericParameter] = []

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter(in: context, $0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}
}
