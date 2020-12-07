
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

func write(_ binding: Binding, as lang: (String) -> Writer?)
{
	lang(binding.id).flatMap({ $0.write(child: binding) })
}

func csharp(_ name: String) -> Writer?
{
	FileWriter(generatedCS.appendingPathComponent(name + ".g.cs"), header: header)
}
