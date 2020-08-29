// based on https://github.com/SwiftDocOrg/SwiftSemantics/blob/6c42cdf1c016090bd09aef8968ba4c84bf4bf409/Sources/SwiftSemantics/Supporting%20Types/GenericRequirement.swift

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
 A generic requirement.

 A generic type or function declaration may specifying one or more requirements
 in a generic where clause before the opening curly brace (`{`) its body.
 Each generic requirement establishes a relation between two type identifiers.

 For example,
 the following declaration specifies two generic requirements:

 ```swift
 func difference<C1: Collection, C2: Collection>(between lhs: C1, and rhs: C2) -> [C1.Element]
    where C1.Element: Equatable, C1.Element == C2.Element
 ```

 - The first generic requirement establishes a `conformance` relation
   between the generic types identified by `"C1.Element"` and `"Equatable"`
 - The second generic requirement establsihes a `sameType` relation
   between the generic types identified by `"C1.Element"` and `"C2.Element"`
 */
public struct GenericRequirement: Hashable, Codable {
    /**
     A relation between the two types identified
     in the generic requirement.

     For example,
     the declaration `struct S<T: Equatable>`
     has a single generic requirement
     that the type identified by `"T"`
     conforms to the type identified by `"Equatable"`.
     */
    public enum Relation: String, Hashable, Codable {
        /**
         The type identified on the left-hand side is equivalent to
         the type identified on the right-hand side of the generic requirement.
         */
        case sameType

        /**
         The type identified on the left-hand side conforms to
         the type identified on the right-hand side of the generic requirement.
        */
        case conformance
    }

    /// The relation between the two identified types.
    public let relation: Relation

    /// The identifier for the left-hand side type.
    public let leftTypeIdentifier: String

    /// The identifier for the right-hand side type.
    public let rightTypeIdentifier: String

    /**
     Creates and returns generic requirements initialized from a
     generic requirement list syntax node.

     - Parameter from: The generic requirement list syntax node, or `nil`.
     - Returns: An array of generic requirements, or `nil` if the node is `nil`.
     */
    public static func genericRequirements(from node: GenericRequirementListSyntax?) -> [GenericRequirement] {
        guard let node = node else { return [] }
        return node.compactMap { GenericRequirement($0) }
    }

    private init?(_ node: GenericRequirementSyntax) {
        if let node = SameTypeRequirementSyntax(node.body) {
            self.relation = .sameType
            self.leftTypeIdentifier = node.leftTypeIdentifier.description.trim()
            self.rightTypeIdentifier = node.rightTypeIdentifier.description.trim()
        } else if let node = ConformanceRequirementSyntax(node.body) {
            self.relation = .conformance
            self.leftTypeIdentifier = node.leftTypeIdentifier.description.trim()
            self.rightTypeIdentifier = node.rightTypeIdentifier.description.trim()
        } else {
            return nil
        }
    }
}

// MARK: - CustomStringConvertible

extension GenericRequirement: CustomStringConvertible {
    public var description: String {
        switch relation {
        case .sameType:
            return "\(leftTypeIdentifier) == \(rightTypeIdentifier)"
        case .conformance:
            return "\(leftTypeIdentifier): \(rightTypeIdentifier)"
        }
    }
}
