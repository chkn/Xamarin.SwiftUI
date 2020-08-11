//
//  UnresolvedType.swift
//  Generator
//
//  Created by Alex Corrado on 7/25/20.
//

import Foundation

// not Swift types
enum OtherType {
	case managed(name : String)
	case unresolved(name : String)
}

extension OtherType : Type {
	var typeCode: Character? { nil }
	var bindingMode: TypeBindingMode { .none }
	var attributes: [Attribute] { [] }
	var modifiers: [Modifier] { [] }
	var name: String {
		switch self {
		case .managed(let name), .unresolved(let name): return name
		}
	}
}
