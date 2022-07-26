//
//  Extensions.swift
//  Generator
//
//  Created by Alex Corrado on 7/20/20.
//

import SwiftSyntax

extension String {
	func trim() -> String { self.trimmingCharacters(in: .whitespacesAndNewlines) }

	func qualified(with prefix: String?) -> String
	{
		if let pfx = prefix {
			return self.contains(".") ? self : "\(pfx).\(self)"
		} else {
			return self
		}
	}

	func qualified(in context: Decl?) -> String
	{
		self.qualified(with: context?.qualifiedName)
	}
}

extension TypeSyntax {
	var name: String { description.trim() }
}

extension AttributeSyntax {
	var name: String { attributeName.text.trim() }
}

extension CustomAttributeSyntax {
	var name: String { attributeName.name.trim() }
}


extension TypeInheritanceClauseSyntax {
	var inheritedTypeNames: [String] { inheritedTypeCollection.map { $0.typeName.name } }

	func inheritedTypes(in context: Decl?) -> [TypeRef]
	{
		self.inheritedTypeNames.map({ .unresolved(name: $0) })
	}
}

