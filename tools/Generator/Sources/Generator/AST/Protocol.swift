// based on https://github.com/SwiftDocOrg/SwiftSemantics/blob/6c42cdf1c016090bd09aef8968ba4c84bf4bf409/Sources/SwiftSemantics/Declarations/Protocol.swift

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

/// A protocol declaration.
struct Protocol: DerivableType {
	/// The declaration attributes.
	var attributes: [Attribute]

	/// The declaration modifiers.
	var modifiers: [Modifier]

	/// The protocol name.
	var name: String

	var inheritance: [String]

	var typeCode: Character? { nil }

	var bindingMode: TypeBindingMode { .none }

	/// Creates an instance initialized with the given syntax node.
	public init(_ node: ProtocolDeclSyntax)
	{
		attributes = node.attributes?.compactMap { $0.as(AttributeSyntax.self) }.compactMap { Attribute(rawValue: $0.attributeName.text.trimmed) } ?? []
		modifiers = node.modifiers?.compactMap { Modifier(rawValue: $0.name.text.trimmed) } ?? []
		name = node.identifier.text.trimmed
		inheritance = node.inheritanceClause?.inheritedTypes ?? []
	}
}
