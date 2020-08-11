//
//  Decl.swift
//  Generator
//
//  Created by Alex Corrado on 7/20/20.
//

// the attributes we care about
enum Attribute: String {
	// FIXME: Maybe add @available as well?
	case frozen
}

// don't care about setter only- swiftUI interface doesn't declare these modifiers
enum Modifier: String {
	case `private`
	case `fileprivate`
	case `internal`
	case `public`
	case open
}

protocol Decl : CustomStringConvertible {
	var attributes: [Attribute] { get }
	var modifiers: [Modifier] { get }
	var name: String { get }
	var qualifiedName: String { get }
}

extension Decl {
	var qualifiedName: String { name.qualified }
	var description: String { qualifiedName }
}

