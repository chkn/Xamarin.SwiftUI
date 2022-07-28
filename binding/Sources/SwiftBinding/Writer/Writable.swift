
public struct Indent: CustomStringConvertible {
	public let description: String

	public init(character: Character, count: Int)
	{
		self.description = String(repeating: character, count: count)
	}
}

public protocol LanguageState {
	var indent: Indent { get }
	func string(for type: TypeRef) -> String?
}

public struct CSharpState: LanguageState {
	public var indent: Indent = Indent(character: "\t", count: 1)
	public init()
	{
	}

	public func string(for type: TypeRef) -> String?
	{
		switch type {
		case .nint: return "nint"
		case .nuint: return "nuint"
		case .erased: return nil
		case .generic(let name): return name
		case .optional(let wrapped):
			guard let wrappedStr = string(for: wrapped) else { return nil }
			return "\(wrappedStr)?"
		case .array(let elementType):
			guard let elementTypeStr = string(for: elementType) else { return nil }
			return "\(elementTypeStr)[]"
		case .tuple(let types):
			if types.count == 1 { return string(for: types[0]) }
			let mappedTypes = types.compactMap(string(for:))
			guard mappedTypes.count == types.count else { return nil }
			return "(\(mappedTypes.joined(separator: ", ")))"
		case .function(_, let args, let returnType):
			let name = returnType.isVoid ? "Action" : "Func"
			let genericArgs = returnType.isVoid ? args : args + [returnType]
			return string(for: .managed("System", name, genericArgs))

		case .managed(let ns, let name, let genericArgs):
			// FIXME: respect usings
			let mappedTypes = genericArgs.compactMap(string(for:))
			guard mappedTypes.count == genericArgs.count else { return nil }
			let genericArgsStr = mappedTypes.isEmpty ? "" : "<\(mappedTypes.joined(separator: ", "))>"
			return "\(ns?.appending(".") ?? "")\(name)\(genericArgsStr)"

		default: return type.qualifiedName!
		}
	}
}

public struct FSharpState: LanguageState {
	public var indent: Indent = Indent(character: " ", count: 4)
	public init()
	{
	}

	public func string(for type: TypeRef) -> String?
	{
		switch type {
		case .erased: return nil
		case .nint: return "nativeint"
		case .nuint: return "unativeint"
		case .generic(let name): return "'\(name)"
		case .optional(let wrapped):
			guard let wrappedStr = string(for: wrapped) else { return nil }
			return "\(wrappedStr) voption"
		case .array(let elementType):
			guard let elementTypeStr = string(for: elementType) else { return nil }
			return "\(elementTypeStr)[]"
		case .tuple(let types):
			if types.count == 1 { return string(for: types[0]) }
			let mappedTypes = types.compactMap(string(for:))
			guard mappedTypes.count == types.count else { return nil }
			return "struct (\(mappedTypes.joined(separator: " * ")))"
		case .function(_, let args, let returnType):
			let name = returnType.isVoid ? "Action" : "Func"
			let genericArgs = returnType.isVoid ? args : args + [returnType]
			return string(for: .managed("System", name, genericArgs))

		case .managed(let ns, let name, let genericArgs):
			// FIXME: respect usings
			let mappedTypes = genericArgs.compactMap(string(for:))
			guard mappedTypes.count == genericArgs.count else { return nil }
			let genericArgsStr = mappedTypes.isEmpty ? "" : "<\(mappedTypes.joined(separator: ", "))>"
			return "\(ns?.appending(".") ?? "")\(name)\(genericArgsStr)"

		default: return type.qualifiedName!
		}
	}
}

public protocol CSharpWritable {
	func write(to: Writer, csharp: CSharpState)
}

public protocol FSharpWritable {
	func write(to: Writer, fsharp: FSharpState)
}

public struct Writable {
	public let id: String
	public let write: (Writer) -> Void

	public init<B>(_ binding: B, _ state: CSharpState) where B: Binding, B: CSharpWritable
	{
		self.id = binding.id
		self.write = { writer in binding.write(to: writer, csharp: state) }
	}

	public init<B>(_ binding: B, _ state: FSharpState) where B: Binding, B: FSharpWritable
	{
		self.id = binding.id
		self.write = { writer in binding.write(to: writer, fsharp: state) }
	}

	public init(id: String, write: @escaping (Writer) -> Void)
	{
		self.id = id
		self.write = write
	}
}
