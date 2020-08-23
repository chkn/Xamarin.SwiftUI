
import Darwin
import SwiftSyntax

public protocol HasTypesToResolve {
	mutating func resolveTypes(_ resolve: (Type) -> Type?)
}

public protocol Derivable: HasTypesToResolve {
	var inheritance: [Type] { get set }
}

public extension Derivable {
	mutating func resolveTypes(_ resolve: (Type) -> Type?)
	{
		inheritance = inheritance.compactMap(resolve)
	}
}

open class Type: Decl {
	public var typeCode: Character? { nil }
	public var isFrozen: Bool { attributes.contains(.frozen) }
}
