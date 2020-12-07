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
	var typesByName: [String:TypeDecl?] = [:]

	// diagnostics
	var currentFile: URL? = nil
	let diag: DiagnosticEngine = DiagnosticEngine()

	var types: [TypeDecl] { typesByName.values.compactMap({ $0 }) }

	public var diagnostics: [Diagnostic] { diag.diagnostics }

	public override init()
	{
		super.init()
		self.resetTypes()
	}

	/// Resets the types to the initial state of pre-mapped types
	open func resetTypes()
	{
		// Pre-map some types onto managed types
		//  nil means erase the type or do not bind
		typesByName = [
			// General .NET types..
			"Swift.Comparable": UnresolvedTypeDecl(in: nil, name: "IComparable"),
			// I think we don't care about these because all .net types can satisfy them..
			"Swift.Equatable": nil,
			"Swift.Hashable": nil,
			"Swift.CustomStringConvertible": nil,
			"Swift.CustomDebugStringConvertible": nil,

			// FIXME: Support these when we have an answer to Combine bindings
			"SwiftUI.SubscriptionView": nil,
			"Combine.ObservableObject": nil,

			// FIXME: Do we need any of these?
			"Swift.LosslessStringConvertible": nil,
			"Swift.BinaryFloatingPoint": nil,
			"Swift.RandomAccessCollection": nil,
			"Swift.OptionSet": nil,
			"Swift.Decodable": nil,
			"Swift.Identifiable": nil,
			"Swift.AdditiveArithmetic": nil,
			"Swift.ExpressibleByExtendedGraphemeClusterLiteral": nil,
			"Swift.Codable": nil,
			"Swift.SetAlgebra": nil,
			"Swift.ExpressibleByStringLiteral": nil,
			"Swift.CustomReflectable": nil,
			"Swift.RawRepresentable": nil,
			"Swift.Encodable": nil,
			"Swift.ExpressibleByStringInterpolation": nil,

			// These types aren't actually used in public API, but our binder still touches them
			// FIXME: This name is incorrectly qualified in the "SwiftUI" namespace somewhere
			"SwiftUI.AnyObject": nil,
			"SwiftUI._VariadicView.UnaryViewRoot": nil,

			// Add manually-bound and Xamarin-bound types as unresolved
			"AppKit.NSApplicationDelegate": UnresolvedTypeDecl(in: nil, name: "AppKit.INSApplicationDelegate"),
			"CoreData.NSFetchRequestResult": UnresolvedTypeDecl(in: nil, name: "CoreData.INSFetchRequestResult"),
			"ObjectiveC.NSObject": UnresolvedTypeDecl(in: nil, name: "Foundation.NSObject"),
			"QuartzCore.CALayer": UnresolvedTypeDecl(in: nil, name: "CoreAnimation.CALayer"),
			"SwiftUI.View": UnresolvedTypeDecl(in: nil, name: "SwiftUI.View"),
			"UIKit.UIApplicationDelegate": UnresolvedTypeDecl(in: nil, name: "UIKit.IUIApplicationDelegate"),

			// No plans to bind these for now..
			"SwiftUI.ViewBuilder": nil
		]
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
		if let ty = typesByName[qualifiedName] {
			return ty
		}
		diagnose(typeUnresolved(qualifiedName))
		return nil
	}

	open func run(_ framework: Framework, loadedLib: UnsafeMutableRawPointer? = nil) -> [Binding]?
	{
		currentFile = nil
		guard let file = framework.swiftinterface else {
			diagnose(swiftinterfaceNotFound(in: framework))
			return nil
		}
		currentFile = file

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

		var bindings: [Binding] = []
		for ty in types {
			bind(type: ty, into: &bindings)
		}
		return bindings
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

	open func bind(type: TypeDecl, into bindings: inout [Binding])
	{
		// don't bind internal/non-public types
		if type.name.hasPrefix("_") || !type.isPublic {
			return
		}

		if let sty = type as? StructDecl {
			// try to bind some known SwiftStruct types first
			//  must do "SwiftUI.Shape" first becuase it derives from View
			if let swiftStruct = tryBind(struct: sty, as: "SwiftUI.Shape") ?? tryBind(struct: sty, as: "SwiftUI.View") {
				bindings.append(swiftStruct)
				return
			}

			if let _ = tryBind(struct: sty, as: "SwiftUI.ViewModifier") {
				// FIXME: We need to support ViewModifiers
				return
			}

			// frozen POD structs -> blittableStruct
			if type.isFrozen && !type.isNonPOD {
				bindings.append(BlittableStructBinding(sty))
				return
			}

			// other structs just derive from SwiftStruct directly
			bindings.append(SwiftStructBinding(sty, "SwiftStruct"))
		}
	}

	open func diagnose(_ message: Diagnostic.Message)
	{
		var loc: SourceLocation? = nil
		if let file = currentFile {
			//FIXME: try to get actual lines and columns?
			loc = SourceLocation(line: 0, column: 0, offset: 0, file: file.path)
		}
		diag.diagnose(message, location: loc)
	}

	open override func visit(_ node: EnumDeclSyntax) -> SyntaxVisitorContinueKind
	{
		// FIXME
		return .skipChildren
	}

	open override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(ExtensionDecl(in: currentContext, node))
		return .skipChildren
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
}
