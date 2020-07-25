//
//  Parser.swift
//  Parser
//
//  Created by Alex Corrado on 7/20/20.
//

import Foundation
import SwiftSyntax

class Parser: SyntaxVisitor {
	var swiftUI : Xcode

	var typesByName: [String:Type] = [:]

	var extensions: [ExtensionDeclSyntax] = []

	public init (_ swiftUI : Xcode)
	{
		self.swiftUI = swiftUI
	}

	func run(_ sdk : SDK) throws
	{
		let file = swiftUI.swiftinterface(forSDK: sdk)
		let tree = try SyntaxParser.parse(file)
		walk(tree)

		// merge type extensions into types
		for node in extensions {
			let typeName = node.extendedType.name.qualified
			guard let ty = typesByName[typeName] else { continue }

			if var dty = ty as? DerivableType {
				dty.inheritance.append(contentsOf: node.inheritanceClause?.inheritedTypes ?? [])
				typesByName.updateValue(dty, forKey: typeName)
			}
		}
	}

	override func visit(_ node: StructDeclSyntax) -> SyntaxVisitorContinueKind
	{
		let ty = Struct(node)
		typesByName.updateValue(ty, forKey: ty.name.qualified)
		return .skipChildren
	}

	override func visit(_ node: ProtocolDeclSyntax) -> SyntaxVisitorContinueKind
	{
		let ty = Protocol(node)
		typesByName.updateValue(ty, forKey: ty.name.qualified)
		return .skipChildren
	}

	override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(node)
		return .skipChildren
	}
}
