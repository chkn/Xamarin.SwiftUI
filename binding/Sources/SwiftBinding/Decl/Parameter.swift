// from https://github.com/SwiftDocOrg/SwiftSemantics/blob/db025ec43c8cb3588c7e1787732584e74e0a160f/Sources/SwiftSemantics/Declarations/Function.swift

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

/**
 A function parameter.
 This type can also be used to represent
 initializer parameters and associated values for enumeration cases.
 */
public struct Parameter: HasTypesToResolve {
	/// The declaration attributes.
	public var attributes: [DeclAttribute]

	/**
	 The first, external name of the parameter.
	 For example,
	 given the following function declaration,
	 the first parameter has a `firstName` equal to `nil`,
	 and the second parameter has a `firstName` equal to `"by"`:
	 ```swift
	 func increment(_ number: Int, by amount: Int = 1)
	 ```
	 */
	public let firstName: String?

	/**
	 The second, internal name of the parameter.
	 For example,
	 given the following function declaration,
	 the first parameter has a `secondName` equal to `"number"`,
	 and the second parameter has a `secondName` equal to `"amount"`:
	 ```swift
	 func increment(_ number: Int, by amount: Int = 1)
	 ```
	*/
	public let secondName: String?

	/**
	 The type identified by the parameter.
	 For example,
	 given the following function declaration,
	 the first parameter has a `type` equal to `"Person"`,
	 and the second parameter has a `type` equal to `"String"`:
	 ```swift
	 func greet(_ person: Person, with phrases: String...)
	 ```
	*/
	public var type: TypeDecl?

	/**
	 Whether the parameter accepts a variadic argument.
	 For example,
	 given the following function declaration,
	 the second parameter is variadic:
	 ```swift
	 func greet(_ person: Person, with phrases: String...)
	 ```
	*/
	public let variadic: Bool

	/**
	 The default argument of the parameter.
	 For example,
	 given the following function declaration,
	 the second parameter has a default argument equal to `"1"`.
	 ```swift
	 func increment(_ number: Int, by amount: Int = 1)
	 ```
	 */
	public let defaultArgument: String?

	public init(_ node: FunctionParameterSyntax)
	{
        self.attributes = node.attributes?.compactMap(DeclAttribute.parse) ?? []
        firstName = node.firstName?.text.trim()
        secondName = node.secondName?.text.trim()
        if let nty = node.type {
			// SwiftSyntax doesn't treat @escaping as an attribute, but we want to
			var name = nty.name
			if name.hasPrefix("@escaping ") {
				self.attributes.append(.escaping)
				name = String(name.suffix(from: name.index(name.startIndex, offsetBy: 10)))
			}
			type = UnresolvedTypeDecl(in: nil, name: name)
		}
        variadic = node.ellipsis != nil
        defaultArgument = node.defaultArgument?.value.description.trim()
    }

	public mutating func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?) {
		if let ty = type {
			type = resolve(ty)
		}
	}
}
