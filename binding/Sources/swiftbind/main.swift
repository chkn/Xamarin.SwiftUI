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
case 2 where CommandLine.arguments[1] != "--help":
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

// FIXME: Iter SDKs and/or take as argument
let sdk = SDK.macOS
let framework = xcode.framework(at: URL(fileURLWithPath: frameworkPath), for: sdk)

var binder = Binder()
binder.run(framework, loadedLib: dlopen(framework.binary.path, 0))

for ty in binder.bindings {
	var templateName: String
	switch ty {

	case is SwiftStructBinding:
		templateName = "SwiftStruct.cs"

	case is BlittableStructBinding:
		templateName = "BlittableStruct.cs"
	default:
		continue
	}

	let rendered = try env.renderTemplate(name: templateName, context: [ "type": ty ])
	try rendered.write(to: generatedCS.appendingPathComponent(ty.name + ".g.cs"), atomically: false, encoding: .utf8)
}

for msg in binder.diagnostics {
	print(msg)
}
