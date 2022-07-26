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
	var typesByName: [String:TypeRef] = [:]

	// diagnostics
	var currentFile: URL? = nil
	let diag: DiagnosticEngine = DiagnosticEngine()

	var types: [TypeDecl] {
		typesByName.values.compactMap({ tyr in
			switch tyr {
			case .decl(let decl): return decl
			default: return nil
			}
		})
	}

	public var diagnostics: [Diagnostic] { diag.diagnostics }

	public override init()
	{
		super.init()
		self.resetTypes()
	}

	public func resetTypes()
	{
		self.resetTypes(&typesByName)
	}

	/// Resets the types to the initial state of pre-mapped types
	open func resetTypes(_ typesByName: inout [String:TypeRef])
	{
		// Pre-map some types
		typesByName = [
			// General .NET types..
			"Swift.Bool": .managed("System", "Boolean", []),
			"Swift.Comparable": .managed("System", "IComparable", []),
			"Swift.Int": .nint,
			"Swift.Int8": .managed("System", "SByte", []),
			"Swift.UInt": .nuint,
			"Swift.UInt32": .managed("System", "UInt32", []),
			"Swift.Void": .managed("System", "Void", []),

			// I think we don't care about these because all .net types can satisfy them..
			"Swift.Equatable": .erased,
			"Swift.Hashable": .erased,
			"Swift.CustomStringConvertible": .erased,
			"Swift.CustomDebugStringConvertible": .erased,

			// FIXME: Support these when we have an answer to Combine bindings
			"SwiftUI.SubscriptionView": .erased,
			"Combine.ObservableObject": .erased,

			// FIXME: Do we need any of these?
			"Swift.LosslessStringConvertible": .erased,
			"Swift.BinaryFloatingPoint": .erased,
			"Swift.RandomAccessCollection": .erased,
			"Swift.OptionSet": .erased,
			"Swift.Decodable": .erased,
			"Swift.Identifiable": .erased,
			"Swift.AdditiveArithmetic": .erased,
			"Swift.ExpressibleByExtendedGraphemeClusterLiteral": .erased,
			"Swift.Codable": .erased,
			"Swift.SetAlgebra": .erased,
			"Swift.ExpressibleByStringLiteral": .erased,
			"Swift.CustomReflectable": .erased,
			"Swift.RawRepresentable": .erased,
			"Swift.Encodable": .erased,
			"Swift.ExpressibleByStringInterpolation": .erased,

			// These types aren't actually used in public API, but our binder still touches them
			// FIXME: This name is incorrectly qualified in the "SwiftUI" namespace somewhere
			"SwiftUI.AnyObject": .erased,
			"SwiftUI._VariadicView.UnaryViewRoot": .erased,

			// Add manually-bound and Xamarin-bound types
			"AppKit.NSApplicationDelegate": .managed("AppKit", "INSApplicationDelegate", []),
			"CoreData.NSFetchRequestResult": .managed("CoreData", "INSFetchRequestResult", []),
			"CoreGraphics.CGFloat": .managed("System.Runtime.InteropServices", "NFloat", []),
			"CoreGraphics.CGLineCap": .managed("CoreGraphics", "CGLineCap", []),
			"CoreGraphics.CGRect": .managed("CoreGraphics", "CGRect", []),
			"ObjectiveC.NSObject": .managed("Foundation", "NSObject", []),
			"QuartzCore.CALayer": .managed("CoreAnimation", "CALayer", []),
			"Swift.String": .managed("Swift", "String", []),
			"SwiftUI.View": .managed("SwiftUI", "View", []),
			"UIKit.UIApplicationDelegate": .managed("UIKit", "IUIApplicationDelegate", []),

			// No plans to bind these for now..
			"SwiftUI.ViewBuilder": .erased
		]
	}

	open func add(type ty: TypeDecl)
	{
		if typesByName[ty.qualifiedName] == nil {
			typesByName.updateValue(.decl(ty), forKey: ty.qualifiedName)
		}
	}

	open func resolve(type tyr: TypeRef) -> TypeRef
	{
		// create mutable copy
		var tyr2 = tyr
		tyr2.resolveTypes(resolve)

		// If we already have a mapping for the type, just go with that
		if let qualifiedName = tyr2.qualifiedName, let ty = typesByName[qualifiedName] {
			return ty
		}

		return tyr2
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
		typesByName = typesByName.mapValues(resolve)

		// resolve extensions and attach to types
		for ext in extensions {
			ext.resolveTypes(resolve)
			if case let .decl(decl) = typesByName[ext.extendedTypeQualifiedName], var extendedType = decl as? Extendable {
				extendedType.extensions.append(ext)
			}
		}

		// if we still have some unresolved types, create diagnostics
		for var el in typesByName {
			el.value.resolveTypes({ tyr in
				if case let .unresolved(name) = tyr {
					diagnose(typeUnresolved(name))
				}
				return tyr
			})
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
		var binding: SwiftStructBinding? = nil
		if type.inherits(from: baseClass) {
			binding = SwiftStructBinding(type, baseClass)
		} else if let ext = type.extensionInherits(from: baseClass) {
			let b = SwiftStructBinding(type, baseClass)
			b.apply(ext)
			binding = b
		}
		return binding
	}

	open func bind(type: TypeDecl, into bindings: inout [Binding])
	{
		// don't bind internal/non-public types, or types without any children
		if !type.isPublic || type.name.hasPrefix("_") {
			return
		}

		if let mty = type as? HasMembers, mty.members.isEmpty {
			return
		}

		var typeBinding: Binding? = nil

		if let sty = type as? StructDecl {
			// try to bind some known SwiftStruct types first
			//  must do "SwiftUI.Shape" first becuase it derives from View
			if let swiftStruct = tryBind(struct: sty, as: "SwiftUI.Shape") ?? tryBind(struct: sty, as: "SwiftUI.View") {
				typeBinding = swiftStruct
				bindings.append(swiftStruct)
			}

			else if let _ = tryBind(struct: sty, as: "SwiftUI.ViewModifier") {
				// FIXME: We need to support ViewModifiers
			}

			// frozen POD structs -> blittableStruct
			else if type.isFrozen && !type.isNonPOD {
				let binding = BlittableStructBinding(sty)
				typeBinding = binding
				bindings.append(binding)
			}

			else {
				// other structs just derive from SwiftStruct directly
				let binding = SwiftStructBinding(sty, "SwiftStruct")
				typeBinding = binding
				bindings.append(binding)
			}
		}

		if let tyb = typeBinding, let nom = type as? NominalTypeDecl {
			for member in nom.membersIncludingExtensions {
				bind(member: member, for: tyb, into: &bindings)
			}
		}
	}

	open func bind(member: MemberDecl, for binding: Binding, into bindings: inout [Binding])
	{
		// don't bind disfavored overloads for now
		if member.attributes.contains(._disfavoredOverload) { return }
		guard let type = member.context as? NominalTypeDecl else { return }

		if let ctor = member as? InitializerDecl, ctor.isPublic {
			if let sty = binding as? SwiftStructBinding {
				// try to identify a primary ctor
				func isPrimaryCandidate(_ d: InitializerDecl) -> Bool
				{
					d.isPublic && !d.optional && !d.attributes.contains(._disfavoredOverload) && d.genericParameters.isEmpty
				}

				// this one is primary if it meets all the requirements and there is no other ctor that does
				let isPrimary = isPrimaryCandidate(ctor) && !type.membersIncludingExtensions.contains(where: {
					if let d = $0 as? InitializerDecl, d !== ctor && isPrimaryCandidate(d) { return true } else { return false } })

				if isPrimary {
					sty.primaryCtor = PrimaryCtorBinding(ctor)
				}

				// FIXME: Bind optional and generic ctors as static Create methods
			}
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

	open override func visit(_ node: ClassDeclSyntax) -> SyntaxVisitorContinueKind
	{
		// FIXME
		return .skipChildren
	}

	open override func visit(_ node: ExtensionDeclSyntax) -> SyntaxVisitorContinueKind
	{
		extensions.append(ExtensionDecl(in: currentContext, node))
		return .visitChildren
	}

	open override func visit(_ node: ProtocolDeclSyntax) -> SyntaxVisitorContinueKind
	{
		add(type: ProtocolDecl(in: currentContext, node))
		return .skipChildren
	}

	open override func visit(_ node: StructDeclSyntax) -> SyntaxVisitorContinueKind
	{
		let decl = StructDecl(in: currentContext, node)
		add(type: decl)
		currentContext = decl
		return .visitChildren
	}

	open override func visitPost(_ node: StructDeclSyntax) {
		currentContext = currentContext?.context
	}

	open override func visit(_ node: InitializerDeclSyntax) -> SyntaxVisitorContinueKind
	{
		if var ty = currentContext as? HasMembers {
			ty.members.append(InitializerDecl(in: currentContext, node))
		}
		return .skipChildren
	}
}
