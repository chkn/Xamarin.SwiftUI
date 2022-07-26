
import Foundation
import SwiftBinding

// root dir within our source tree
let rootPath = URL(fileURLWithPath: #file).deletingLastPathComponent().appendingPathComponent("../../..")

// Generated source outputs
let generatedCS = rootPath.appendingPathComponent("src/SwiftUI/Generated")
let generatedGlue = rootPath.appendingPathComponent("src/SwiftUIGlue")

let header = """
	// WARNING - AUTO-GENERATED - DO NOT EDIT!!!
	//
	// -> To regenerate, run `swift run` from binding directory.
	//

	"""

// Functions to create generated source files

func write(_ writable: Writable, as lang: (String) -> Writer?)
{
	lang(writable.id).flatMap(writable.write)
}

func csharp(_ name: String) -> Writer?
{
	FileWriter(generatedCS.appendingPathComponent(name + ".g.cs"), header: header)
}
