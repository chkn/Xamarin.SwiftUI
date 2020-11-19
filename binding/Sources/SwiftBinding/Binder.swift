//
//  Parser.swift
//  Parser
//
//  Created by Alex Corrado on 7/20/20.
//

import Foundation
import SwiftSyntax

open class Binder: SyntaxVisitor {
	var currentContext: Decl? = nil
	var extensions: [ExtensionDecl] = []

	let diag: DiagnosticEngine = DiagnosticEngine()

	// Pre-map some types onto managed types
	//  nil means erase the type or do not bind
	var typesByName: [String:TypeDecl?] = [
		// General .NET types..
		"Swift.Comparable": UnresolvedTypeDecl(in: nil, name: "IComparable"),
		// I think we don't care about these because all .net types can satisfy them..
		"Swift.Equatable": nil,
		"Swift.Hashable": nil,
		"Swift.CustomStringConvertible": nil,

		// Add manually-bound and Xamarin-bound types as unresolved
		"QuartzCore.CALayer": UnresolvedTypeDecl(in: nil, name: "CoreAnimation.CALayer"),
		"SwiftUI.View": UnresolvedTypeDecl(in: nil, name: "SwiftUI.View"),

		// FIXME: Support these when we have an answer to Combine bindings
		"SwiftUI.SubscriptionView": nil,
		"Combine.ObservableObject": nil
	]

	public var types: [TypeDecl] { typesByName.values.compactMap({ $0 }) }
	public var diagnostics: [Diagnostic] { diag.diagnostics }
	public private(set) var bindings: [Binding] = []

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
		if let ty = typesByName[qualifiedName] {
			return ty
		}
		diag.diagnose(typeUnresolved(qualifiedName))
		return nil
	}

	open func run(_ framework: Framework, loadedLib: UnsafeMutableRawPointer? = nil)
	{
		guard let file = framework.swiftinterface else {
			diag.diagnose(swiftinterfaceNotFound(in: framework))
			return
		}

		// FIXME: Parsing swiftinterface currently results in a lot of syntax errors,
		//  so don't pass diagnosticEngine for now.
		let tree = try! SyntaxParser.parse(file /*, diagnosticEngine: diag */)
		currentContext = ModuleDecl(in: framework, loadedLib: loadedLib)
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

			if let _ = tryBind(struct: sty, as: "SwiftUI.ViewModifier") {
				// FIXME: We need to support ViewModifiers
				return nil
			}

			// frozen POD structs -> blittableStruct
			if type.isFrozen && !type.isNonPOD {
				return BlittableStructBinding(sty)
			}
		}

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
