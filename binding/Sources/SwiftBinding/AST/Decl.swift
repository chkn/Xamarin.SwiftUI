import SwiftSyntax

// the attributes we care about
public enum Attribute: String {
	// FIXME: Maybe add @available as well?
	case frozen
}

// don't care about setter only- swiftUI interface doesn't declare these modifiers
public enum Modifier: String {
	case `private`
	case `fileprivate`
	case `internal`
	case `public`
	case open
}

open class Decl: CustomStringConvertible {
	public let context: Decl?
	public let attributes: [Attribute]
	public let modifiers: [Modifier]
	public let name: String

	public var isPublic: Bool { modifiers.contains(.public) }
	public var qualifiedName: String { name.qualified(with: context?.qualifiedName) }
	public var description: String { qualifiedName }

	public init(in context: Decl?, attributes: [Attribute], modifiers: [Modifier], name: String)
	{
		self.context = context
		self.attributes = attributes
		self.modifiers = modifiers
		self.name = name
	}

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String)
	{
		self.context = context
		self.attributes = attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { Attribute(rawValue: $0.name) } ?? []
		self.modifiers = modifiers?.compactMap { Modifier(rawValue: $0.name.text.trim()) } ?? []
		self.name = name
	}
}

