//
//  Parser.swift
//  Parser
//
//  Created by Alex Corrado on 7/20/20.
//

import Foundation
import SwiftSyntax

public enum TypeBindingMode: Equatable {
	/// Type is not bound.
	case none

	/// Type is bound as a subclass of the given base class that derives from SwiftStruct
	case swiftStructSubclass(baseClass: String)

	/// Type is bound as an ISwiftBlittableStruct
	case blittableStruct
}

open class Binder: SyntaxVisitor {
	var xcode: Xcode
	var sdk: SDK

	var currentContext: Decl? = nil
	var extensions: [Extension] = []

	// Pre-map some types onto managed types
	//  nil means erase the type or do not bind
	var typesByName: [String:Type?] = [
		// all managed types are hashable
		"Swift.Hashable": nil,

		// FIXME: Support this when we have an answer to Combine bindings
		"SwiftUI.SubscriptionView": nil
	]

	public var types: [Type] { typesByName.values.compactMap({ $0 }) }

	public init (_ xcode : Xcode, sdk: SDK)
	{
		self.xcode = xcode
		self.sdk = sdk
	}

	open func bindingMode(forType ty: Type) -> TypeBindingMode
	{
		// don't bind internal/non-public types
		if ty.name.hasPrefix("_") || !ty.isPublic {
			return .none
		}

		if let dty = ty as? Derivable {
			if dty.inheritance.contains(where: { $0.qualifiedName == "SwiftUI.Shape"}) {
				return .swiftStructSubclass(baseClass: "SwiftUI.Shape")
			}
			if dty.inheritance.contains(where: { $0.qualifiedName == "SwiftUI.View" }) {
				return .swiftStructSubclass(baseClass: "SwiftUI.View")
			}
		}

		// frozen POD structs -> blittableStruct
		if let vwt = valueWitnessTable(forType: ty), ty.isFrozen && !vwt.pointee.isNonPOD {
			return .blittableStruct
		}

		return .none
	}

	open func valueWitnessTable(forType ty: Type) -> UnsafePointer<ValueWitnessTable>?
	{
		/*
			guard let sym = metadataSymbolName else { return nil }
		return dlsym(swiftUILib, sym)?
			.advanced(by: -MemoryLayout<UnsafeRawPointer>.size)
			.assumingMemoryBound(to: UnsafePointer<ValueWitnessTable>.self)
			.pointee
		*/
		return nil
	}

	open func add(type ty: Type)
	{
		if typesByName[ty.qualifiedName] == nil {
			typesByName.updateValue(ty, forKey: ty.qualifiedName)
		}
	}

	open func resolve(type ty: Type) -> Type?
	{
		typesByName[ty.qualifiedName] ?? nil
	}

	open func run(_ framework: URL) throws
	{
		let file = try xcode.swiftinterfacePath(of: framework, forSdk: sdk)
		let tree = try SyntaxParser.parse(file!)

		currentContext = Module(in: nil, name: Xcode.name(of: framework))
		walk(tree)

		// merge type extensions into types
		for var ext in extensions {
			ext.resolveTypes(resolve)
			if var dty = ext.extendedType as? Derivable {
				dty.inheritance.append(contentsOf: ext.inheritance)
			}
		}

		// resolve all types'
		for el in typesByName {
			if var ty = el.value as? HasTypesToResolve {
				ty.resolveTypes(resolve)
			}
		}
	}

	open override func visit(_ node: StructDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(type: Struct(in: currentContext, node: node))
		return .skipChildren
	}

	open override func visit(_ node: ProtocolDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(type: Protocol(in: currentContext, node: node))
		return .skipChildren
	}

	open override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(Extension(in: currentContext, node: node))
		return .skipChildren
	}
}
