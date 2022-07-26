
import SwiftSyntax

public enum TypeRef {
	/// nint in C# and nativeint in F#
	case nint
	/// nuint in C# and unativeint in F#
	case nuint

	case erased
	case unresolved(name: String)

	// a generic parameter
	case generic(_ name: String)

	// managed types
	case managed(_ namespace: String?, _ name: String, _ genericArgs: [TypeRef])

	case decl(_ ty: TypeDecl)

	/// https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_tuple-type
	case tuple(_ types: [TypeRef])

	/// https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_array-type
	indirect case array(of: TypeRef)

	// https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_optional-type
	indirect case optional(_ ty: TypeRef)

	/// https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_function-type
	indirect case function(escaping: Bool, _ args: [TypeRef], _ return: TypeRef)
}

public extension TypeRef {
	static func parse(_ typeSyntax: TypeSyntax) -> TypeRef
	{
		func parseFunction(escaping: Bool, _ fty: FunctionTypeSyntax) -> TypeRef {
			let args = fty.arguments.map({ parse($0.type) })
			let returnType = parse(fty.returnType)
			return .function(escaping: escaping, args, returnType)
		}

		let syntax = Syntax(typeSyntax)
		if let aty = AttributedTypeSyntax(syntax) {
			if let fty = FunctionTypeSyntax(Syntax(aty.baseType)) {
				let escaping = aty.attributes?.contains(where: { $0.as(AttributeSyntax.self)?.name == "escaping" }) ?? false
				return parseFunction(escaping: escaping, fty)
			}
			return .unresolved(name: aty.baseType.name)

		} else if let fty = FunctionTypeSyntax(syntax) {
			return parseFunction(escaping: false, fty)

		} else if let tty = TupleTypeSyntax(syntax) {
			let types = tty.elements.map({ parse($0.type) })
			return .tuple(types)

		} else if let aty = ArrayTypeSyntax(syntax) {
			let elementType = parse(aty.elementType)
			return .array(of: elementType)

		} else if let oty = OptionalTypeSyntax(syntax) {
			let wrappedType = parse(oty.wrappedType)
			return .optional(wrappedType)

		} else if let mty = MetatypeTypeSyntax(syntax) {
			//print(mty.description)
			// FIXME?
			return .erased

		} else if let nty = TypeSyntax(syntax) {
			return .unresolved(name: nty.name)
		}
		fatalError("Unrecognized type syntax")
	}

	var qualifiedName: String? {
		switch self {
		case .decl(let decl): return decl.qualifiedName
		case .managed(let ns, let name, let genericArgs):
			if genericArgs.contains(where: { $0.qualifiedName == nil }) { return nil }
			let genericArgList = genericArgs.isEmpty ? "" : "<\(genericArgs.compactMap({ $0.qualifiedName }).joined(separator: ", "))>"
			return "\(ns?.appending(".") ?? "")\(name)\(genericArgList)"
		case .unresolved(let name), .generic(let name): return name
		default: return nil
		}
	}

	var isVoid: Bool {
		switch self {
		case .unresolved(let name) where name == "Swift.Void": return true
		case .managed(let ns, let name, _) where ns == "System" && name == "Void": return true
		default: return false
		}
	}
}

extension TypeRef: HasTypesToResolve {
	public mutating func resolveTypes(_ resolve: (TypeRef) -> TypeRef) {
		switch self {
		case .decl(let decl):
			if var d = decl as? HasTypesToResolve {
				d.resolveTypes(resolve)
			}
		case .managed(let ns, let name, let genericArgs):
			self = .managed(ns, name, genericArgs.map(resolve))
		case .tuple(let types):
			self = .tuple(types.map(resolve))
		case .array(of: let type):
			self = .array(of: resolve(type))
		case .function(let escaping, let args, let returnType):
			self = .function(escaping: escaping, args.map(resolve), resolve(returnType))

		default: return
		}
	}
}

extension TypeRef: CustomStringConvertible {
	public var description: String {
		switch self {
		case .nint: return "Int"
		case .nuint: return "UInt"
		case .erased: return "<erased>"
		case .decl(_), .unresolved(_), .generic(_): return self.qualifiedName!

		case .managed(let ns, let name, let genericArgs):
			let genericArgList = genericArgs.map({ $0.description }).joined(separator: ", ")
			return "\(ns?.appending(".") ?? "")\(name)\(genericArgList)"

		case .tuple(let types):
			let typeList = types.map({ $0.description }).joined(separator: ", ")
			return "(\(typeList))"

		case .array(of: let type):
			return "[\(type.description)]"

		case .function(let escaping, let args, let returnType):
			let argList = args.map({ $0.description }).joined(separator: ", ")
			return "\(escaping ? "@escaping " : "")(\(argList)) -> \(returnType.description))"

		case .optional(let type):
			return "\(type.description)?"
		}
	}
}
