// based on https://github.com/SwiftDocOrg/SwiftSemantics/blob/6c42cdf1c016090bd09aef8968ba4c84bf4bf409/Sources/SwiftSemantics/Declarations/Structure.swift

//Copyright 2019 Read Evaluate Press, LLC
//
//Permission is hereby granted, free of charge, to any person obtaining a
//copy of this software and associated documentation files (the "Software"),
//to deal in the Software without restriction, including without limitation
//the rights to use, copy, modify, merge, publish, distribute, sublicense,
//and/or sell copies of the Software, and to permit persons to whom the
//Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//DEALINGS IN THE SOFTWARE.

import SwiftSyntax

/// A structure declaration.
struct Struct: DerivableType {
	/// The declaration attributes.
	var attributes: [Attribute]

	/// The declaration modifiers.
	var modifiers: [Modifier]

	/// The structure name.
	var name: String

	var inheritance: [String]

	var genericParameters: [GenericParameter]

	var typeCode: Character? { "V" }

	var bindingMode: TypeBindingMode {
		if name.hasPrefix("_") || !modifiers.contains(.public) {
			return .none
		}

		if inheritance.contains("SwiftUI.View") {
			return .swiftStructSubclass(baseClass: "SwiftUI.View")
		}

		if inheritance.contains("SwiftUI.Shape") {
			return .swiftStructSubclass(baseClass: "SwiftUI.Shape")
		}

		// frozen POD structs -> blittableStruct
		if let vwt = valueWitnessTable, isFrozen && !vwt.pointee.isNonPOD {
			return .blittableStruct
		}

		return .none
	}

	public init(_ node: StructDeclSyntax)
	{
		attributes = node.attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { Attribute(rawValue: $0.name) } ?? []
		modifiers = node.modifiers?.compactMap { Modifier(rawValue: $0.name.text.trimmed) } ?? []
		name = node.identifier.text.trimmed
		inheritance = node.inheritanceClause?.inheritedTypes ?? []
		genericParameters = node.genericParameterClause?.genericParameterList.map { GenericParameter($0, node.genericWhereClause) } ?? []
	}
}
