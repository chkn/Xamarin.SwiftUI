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

	// Pre-map some types onto managed types
	//  nil means erase the type or do not bind
	var typesByName: [String:Type?] = [
		// all managed types are hashable
		"Swift.Hashable": nil,

		// FIXME: Figure out how we want to expose this
		"SwiftUI.SubscriptionView": nil,

		"Swift.RandomAccessCollection": OtherType.managed(name: "System.Collections.Generic.IList)
	]

	var extensions: [ExtensionDeclSyntax] = []

	public init (_ swiftUI : Xcode)
	{
		self.swiftUI = swiftUI
	}

	func resolve(_ ty : Type) -> Type?
	{
		typesByName[ty.qualifiedName] ?? nil
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
				dty.inheritance.append(contentsOf: node.inheritanceClause?.inheritedTypes.map(OtherType.unresolved) ?? [])
				typesByName.updateValue(dty, forKey: typeName)
			}
		}

		// resolve all types'
		let names = typesByName.keys
		for name in names {
			if var ty = typesByName[name] as? HasTypesToResolve {
				ty.resolveTypes(resolve: resolve)
				typesByName.updateValue(ty as! Type, forKey: name)
			}
		}
	}

	func add(_ ty: Type)
	{
		if typesByName[ty.qualifiedName] == nil {
			typesByName.updateValue(ty, forKey: ty.qualifiedName)
		}
	}

	override func visit(_ node: StructDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(Struct(node))
		return .skipChildren
	}

	override func visit(_ node: ProtocolDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(Protocol(node))
		return .skipChildren
	}

	override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(node)
		return .skipChildren
	}
}
