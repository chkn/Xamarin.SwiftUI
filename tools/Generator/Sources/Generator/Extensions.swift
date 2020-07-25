//
//  Extensions.swift
//  Generator
//
//  Created by Alex Corrado on 7/20/20.
//

import SwiftSyntax

extension String {
	var trimmed: String { self.trimmingCharacters(in: .whitespacesAndNewlines) }

	var qualified: String {
		self.hasPrefix("SwiftUI.") ? self : "SwiftUI.\(self)"
	}
}

extension TypeSyntax {
	var name: String { description.trimmed }
}

extension AttributeSyntax {
	var name: String { attributeName.text.trimmed }
}

extension TypeInheritanceClauseSyntax {
	var inheritedTypes: [String] { inheritedTypeCollection.map { $0.typeName.name } }
}
