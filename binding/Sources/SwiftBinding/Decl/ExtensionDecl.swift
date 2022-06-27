// based on https://github.com/SwiftDocOrg/SwiftSemantics/blob/6c42cdf1c016090bd09aef8968ba4c84bf4bf409/Sources/SwiftSemantics/Declarations/Extension.swift

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

/// An extension declaration.
public class ExtensionDecl: Decl, Derivable, HasMembers {
	public var members: [MemberDecl] = []

    var extendedTypeQualifiedName: String

    /**
     A list of protocol names inherited by the extended type.

     For example,
     the following extension on structure `S`
     has an `inheritance` of `["P", "Q"]`:

     ```swift
     struct S {}
     protocol P {}
     protocol Q {}
     extension S: P, Q {}
    ```
    */
	public var inheritance: [TypeDecl]

    /**
     The generic parameter requirements for the declaration.

     For example,
     the following conditional extension on structure S
     has a single requirement
     that its generic parameter identified as `"T"`
     conforms to the type identified as `"Hahable"`:

     ```swift
     struct S<T> {}
     extension S where T: Hashable {}
     ```
     */
	public let genericRequirements: [GenericRequirement]

    /// Creates an instance initialized with the given syntax node.
    public init(in context: Decl?, _ node: ExtensionDeclSyntax)
    {
		let name = node.extendedType.description.trim()
		extendedTypeQualifiedName = name.qualified(in: context)
		inheritance = node.inheritanceClause?.inheritedTypes(in: context) ?? []
		genericRequirements = GenericRequirement.genericRequirements(from: node.genericWhereClause?.requirementList)
		super.init(in: context, node.attributes, node.modifiers, name)
    }

    public func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?)
	{
		inheritance = inheritance.compactMap(resolve)
		for member in members {
			member.resolveTypes(resolve)
		}
	}
}
