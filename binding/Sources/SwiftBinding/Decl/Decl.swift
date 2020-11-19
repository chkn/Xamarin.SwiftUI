import SwiftSyntax

open class Decl: CustomStringConvertible {
	public let context: Decl?
	public let attributes: [DeclAttribute]
	public let modifiers: [DeclModifier]
	public let name: String

	open var metadataSymbolName: String? { nil }
	open var module: ModuleDecl { context!.module }

	public var isPublic: Bool { modifiers.contains(.public) }
	public var qualifiedName: String { name.qualified(with: context?.qualifiedName) }
	public var description: String { qualifiedName }

	public init(in context: Decl?, attributes: [DeclAttribute], modifiers: [DeclModifier], name: String)
	{
		self.context = context
		self.attributes = attributes
		self.modifiers = modifiers
		self.name = name
	}

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String)
	{
		self.context = context
		self.attributes = attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { DeclAttribute(rawValue: $0.name) } ?? []
		self.modifiers = modifiers?.compactMap { DeclModifier(rawValue: $0.name.text.trim()) } ?? []
		self.name = name
	}
}

