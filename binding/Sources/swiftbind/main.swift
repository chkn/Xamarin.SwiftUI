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

import SwiftBinding

// Compute some paths..

// binding dir within our source tree
let bindingPath = URL(fileURLWithPath: #file).deletingLastPathComponent().appendingPathComponent("../..")

// Generated source outputs
let generatedCS = bindingPath.appendingPathComponent("../src/SwiftUI/Generated")
let generatedGlue = bindingPath.appendingPathComponent("../src/SwiftUIGlue")

// Templates within our source tree
var env = Environment(loader: FileSystemLoader(paths: [Path(bindingPath.appendingPathComponent("Templates").path)]))

// SwiftUI binaries and swiftinterface files within Xcode
var xcode: Xcode
var frameworkPath = "/System/Library/Frameworks/SwiftUI.framework"
switch CommandLine.arguments.count {
case 1:
	xcode = Xcode.default
case 2:
	xcode = Xcode.default
	frameworkPath = CommandLine.arguments[1]
case 4 where CommandLine.arguments[1] == "--developerPath":
	frameworkPath = CommandLine.arguments[3]
	fallthrough
case 3 where CommandLine.arguments[1] == "--developerPath":
	xcode = Xcode(developerPath: URL(fileURLWithPath: CommandLine.arguments[2]))
default:
	print("Usage: \(ProcessInfo.processInfo.processName) [options] [Framework path]")
	print()
	print("Options:")
	print("  --developerPath [Developer path]  - Sets the Developer path to use (defaults to xcode-select -p)")
	print()
	exit(1)
}

// FIXME: Iter SDKs
let sdk = SDK.allCases.first!

var binder = Binder(xcode, sdk: sdk)
try binder.run(URL(fileURLWithPath: frameworkPath))

for ty in binder.types {
	var templateName: String
	var context: [String:Any] = [
		"className": ty.name
	]

	switch binder.bindingMode(forType: ty) {

    case .swiftStructSubclass(let baseClass):
		if let strct = ty as? Struct { // could also be protocol here
			context.updateValue(baseClass, forKey: "baseClass")
			context.updateValue(strct.genericParameters.map({ $0.name }), forKey: "genericParamNames")
			context.updateValue(strct.genericParameters.filter({ $0.type != nil }), forKey: "genericParamWhere")
			templateName = "SwiftStruct.cs"
		} else {
			continue
		}
    default:
		continue
	}

	let rendered = try env.renderTemplate(name: templateName, context: context)
	try rendered.write(to: generatedCS.appendingPathComponent(ty.name + ".g.cs"), atomically: false, encoding: .utf8)
}
