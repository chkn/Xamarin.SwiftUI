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
public class ProtocolDecl: TypeDecl, Derivable, Extendable {
	public var inheritance: [TypeDecl]
	public var extensions: [ExtensionDecl] = []

	/// Creates an instance initialized with the given syntax node.
	public init(in context: Decl?, _ node: ProtocolDeclSyntax)
	{
		inheritance = node.inheritanceClause?.inheritedTypes(in: context) ?? []
		super.init(in: context, node.attributes, node.modifiers, node.identifier.text.trim())
	}
}
