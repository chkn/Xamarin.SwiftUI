// based on https://raw.githubusercontent.com/SwiftDocOrg/SwiftSemantics/db025ec43c8cb3588c7e1787732584e74e0a160f/Sources/SwiftSemantics/Declarations/Initializer.swift

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

/// An initializer declaration.
public class InitializerDecl: FunctionDecl {

    /// Whether the initializer is optional.
    public var optional: Bool

    /// The `throws` or `rethrows` keyword, if any.
    public var throwsOrRethrows: ThrowSpec? = nil

    /// Creates an instance initialized with the given syntax node.
    public init(in context: Decl?, _ node: InitializerDeclSyntax)
    {
        optional = node.optionalMark != nil
        if let keyword = node.throwsOrRethrowsKeyword {
			throwsOrRethrows = ThrowSpec(rawValue: keyword.description.trim())
		}
		// FIXME: name should be full selector?
        super.init(in: context, node.attributes, node.modifiers, "init", node.genericParameterClause, node.parameters, node.genericWhereClause)
    }
}
