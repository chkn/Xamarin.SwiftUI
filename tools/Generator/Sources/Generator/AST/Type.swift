
import Darwin
import SwiftSyntax

enum TypeBindingMode : Equatable {
	case none
	case swiftStructSubclass(baseClass : String)
	case blittableStruct
}

struct GenericParameter {
	/// The generic parameter attributes.
	var attributes: [Attribute]

	/// The generic parameter name.
	var name: String

	/// The generic parameter type, if any.
	var type: String?

	/// Creates an instance initialized with the given syntax node.
	public init(_ node: GenericParameterSyntax, _ whereClause: GenericWhereClauseSyntax?)
	{
		attributes = node.attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { Attribute(rawValue: $0.name) } ?? []

		name = node.name.text.trimmed
		type = node.inheritedType?.name

		if let wc = whereClause {
			for req in wc.requirementList {
				if let node = ConformanceRequirementSyntax(req.body), node.leftTypeIdentifier.name == name {
					type = node.rightTypeIdentifier.name
				}
			}
		}

		// Prefix name with "T" to match .NET convention
		name = "T" + name
	}
}

protocol Type: Decl {
	var typeCode: Character? { get }
	var bindingMode: TypeBindingMode { get }
}

protocol DerivableType: Type {
	var inheritance: [String] { get set }
}

extension Type {
	var isFrozen: Bool { attributes.contains(.frozen) }

	// applies to non-generic class, struct and enum types
	var metadataSymbolName: String? {
		typeCode.map { "$s7SwiftUI\(name.count)\(name)\($0)N" }
	}

	var valueWitnessTable: UnsafePointer<ValueWitnessTable>? {
		nil
		/*
		guard let sym = metadataSymbolName else { return nil }
		return dlsym(swiftUILib, sym)?
			.advanced(by: -MemoryLayout<UnsafeRawPointer>.size)
			.assumingMemoryBound(to: UnsafePointer<ValueWitnessTable>.self)
			.pointee
		*/
	}
}
