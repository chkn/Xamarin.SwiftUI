
import Darwin
import Foundation

import SwiftSyntax
import SwiftBinding

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

// HACK: To access the value witness table, we dlopen the mac framework
//   and assume that these ABI details (e.g. @frozen) are the same across all platforms
let frameworkURL = URL(fileURLWithPath: frameworkPath)
let macFramework = xcode.framework(at: frameworkURL, for: SDK.macOS)
let loadedLib = dlopen(macFramework.binary.path, 0)

// Run the binding for every SDK, conditionalizing the generated code for any differences
let binder = Binder()
var bindings : [String:[(SDK,Binding)]] = [:]

var sdks = Set(SDK.allCases)
for sdk in SDK.allCases {
	let framework = xcode.framework(at: frameworkURL, for: sdk)

	binder.resetTypes()
	guard let result = binder.run(framework, loadedLib: loadedLib) else {
		sdks.remove(sdk)
		continue
	}
	for binding in result {
		var list = bindings[binding.id] ?? []
		list.append((sdk, binding))
		bindings.updateValue(list, forKey: binding.id)
	}
}

let cw = ConditionalWriter(sdks)
let csharp = CSharpState()

for kv in bindings {
	let binding = kv.value.first!.1
	switch binding {
	case let tb as TypeBinding:
		if let writer = csharp(tb.name) {
			cw.write(bindings: kv.value.compactMap({
				if let writable = $0.1 as? CSharpWritable {
					return ($0.0, Writable(id: $0.1.id, write: { writer in writable.write(to: writer, csharp: csharp) }))
				}
				return nil
			} ), to: writer)
		} else {
			binder.diagnose(Diagnostic.Message(.error, "Cannot open file for writing"))
		}
	default:
		print("Don't know which Writer to use for \(binding)")
	}
}

// De-dupe the messages and print them out
let diags = binder.diagnostics.map({ ($0.location?.file, Set([$0.debugDescription])) })
let msgs: [String?:Set<String>] = Dictionary(diags, uniquingKeysWith: { $0.union($1) })
for kv in msgs {
	print("In file \(kv.key ?? "<unknown>"):")
	for msg in kv.value {
		print("\t\(msg)")
	}
}
exit(Int32(diags.count))

