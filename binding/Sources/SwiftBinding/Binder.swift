//
//  Parser.swift
//  Parser
//
//  Created by Alex Corrado on 7/20/20.
//

import Foundation
import SwiftSyntax

open class Binder: SyntaxVisitor {
	var xcode: Xcode
	var sdk: SDK

	var currentContext: Decl? = nil
	var extensions: [ExtensionDecl] = []

	// Pre-map some types onto managed types
	//  nil means erase the type or do not bind
	var typesByName: [String:TypeDecl?] = [
		// FIXME: Support this when we have an answer to Combine bindings
		"SwiftUI.SubscriptionView": nil
	]

	public var types: [TypeDecl] { typesByName.values.compactMap({ $0 }) }
	public private(set) var bindings: [Binding] = []

	public init (_ xcode : Xcode, sdk: SDK)
	{
		self.xcode = xcode
		self.sdk = sdk
	}

	open func valueWitnessTable(for type: TypeDecl) -> UnsafePointer<ValueWitnessTable>?
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

	open func add(type ty: TypeDecl)
	{
		if typesByName[ty.qualifiedName] == nil {
			typesByName.updateValue(ty, forKey: ty.qualifiedName)
		}
	}

	open func resolve(type ty: TypeDecl) -> TypeDecl?
	{
		resolve(ty.qualifiedName)
	}

	open func resolve(_ qualifiedName: String) -> TypeDecl?
	{
		typesByName[qualifiedName] ?? nil
	}

	open func run(_ framework: URL) throws
	{
		let file = try xcode.swiftinterfacePath(of: framework, forSdk: sdk)
		let tree = try SyntaxParser.parse(file!)

		currentContext = ModuleDecl(in: nil, name: Xcode.name(of: framework))
		walk(tree)

		// resolve all types
		for el in typesByName {
			if var ty = el.value as? HasTypesToResolve {
				ty.resolveTypes(resolve)
			}
		}

		// resolve extensions and attach to types
		for var ext in extensions {
			ext.resolveTypes(resolve)
			if var extendedType = typesByName[ext.extendedTypeQualifiedName] as? Extendable {
				extendedType.extensions.append(ext)
			}
		}

		currentContext = nil
		extensions = []
		bindings = types.compactMap(binding)
	}

	func tryBind(struct type: StructDecl, as baseClass: String) -> SwiftStructBinding?
	{
		if type.inherits(from: baseClass) {
			return SwiftStructBinding(type, baseClass)
		}
		if let ext = type.extensionInherits(from: baseClass) {
			let binding = SwiftStructBinding(type, baseClass)
			binding.apply(ext, resolve)
			return binding
		}
		return nil
	}

	open func binding(for type: TypeDecl) -> Binding?
	{
		// don't bind internal/non-public types
		if type.name.hasPrefix("_") || !type.isPublic {
			return nil
		}

		// try to bind some known SwiftStruct types first
		//  must do "SwiftUI.Shape" first becuase it derives from View
		if let sty = type as? StructDecl {
			if let swiftStruct = tryBind(struct: sty, as: "SwiftUI.Shape") ?? tryBind(struct: sty, as: "SwiftUI.View") {
				return swiftStruct
			}
		}

		// frozen POD structs -> blittableStruct
//		if let vwt = valueWitnessTable(for: type), type.isFrozen && !vwt.pointee.isNonPOD {
//			return .blittableStruct
//		}

		return nil
	}

	open override func visit(_ node: StructDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(type: StructDecl(in: currentContext, node))
		return .skipChildren
	}

	open override func visit(_ node: ProtocolDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(type: ProtocolDecl(in: currentContext, node))
		return .skipChildren
	}

	open override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(ExtensionDecl(in: currentContext, node))
		return .skipChildren
	}
}
