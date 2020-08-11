//
//  main.swift
//  generator
//
//  Created by Alex Corrado on 7/12/20.
//

import Darwin
import Foundation

import PathKit
import Stencil
import SwiftSyntax

// Compute some paths..

// tools/Generator within our source tree
let generatorPath = URL(fileURLWithPath: #file).deletingLastPathComponent().appendingPathComponent("../..")

// Generated source outputs
let generatedCS = generatorPath.appendingPathComponent("../../src/SwiftUI/Generated")
let generatedGlue = generatorPath.appendingPathComponent("../../src/SwiftUIGlue")

// Templates within our source tree
var env = Environment(loader: FileSystemLoader(paths: [Path(generatorPath.appendingPathComponent("Templates").path)]))

// SwiftUI binaries and swiftinterface files within Xcode
var swiftUI : Xcode
switch CommandLine.arguments.count {

case 1:
	swiftUI = Xcode.default
case 2:
	swiftUI = Xcode(developerPath: URL(fileURLWithPath: CommandLine.arguments[1]))
default:
	print("Usage: \(ProcessInfo.processInfo.processName) [Developer path]")
	exit(1)
}

// FIXME: Iter SDKs
let sdk = SDK.allCases.first!

var parser = Parser(swiftUI)
try parser.run(sdk)

for ty in parser.typesByName {
	var templateName: String
	var context: [String:Any] = [
		"className": ty.value.name
	]

	switch ty.value.bindingMode {

    case .swiftStructSubclass(let baseClass):
		let strct = ty.value as! Struct
		context.updateValue(baseClass, forKey: "baseClass")
		context.updateValue(strct.genericParameters.map({ $0.name }), forKey: "genericParamNames")
		context.updateValue(strct.genericParameters.filter({ $0.type != nil }), forKey: "genericParamWhere")
		templateName = "SwiftStruct.cs"
    default:
		continue
	}

	let rendered = try env.renderTemplate(name: templateName, context: context)
	try rendered.write(to: generatedCS.appendingPathComponent(ty.value.name + ".g.cs"), atomically: false, encoding: .utf8)
}
