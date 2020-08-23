//
//  Extensions.swift
//  Generator
//
//  Created by Alex Corrado on 7/20/20.
//

import SwiftSyntax

extension String {
	func trim() -> String { self.trimmingCharacters(in: .whitespacesAndNewlines) }

	func qualified(with prefix: String?) -> String {
		if let pfx = prefix {
			return self.hasPrefix(pfx) ? self : "\(pfx).\(self)"
		} else {
			return self
		}
	}
}

extension TypeSyntax {
	var name: String { description.trim() }
}

extension AttributeSyntax {
	var name: String { attributeName.text.trim() }
}

extension TypeInheritanceClauseSyntax {
	var inheritedTypes: [String] { inheritedTypeCollection.map { $0.typeName.name } }
}

