
import Foundation

public class ModuleDecl: Decl {
	let lib: UnsafeMutableRawPointer?

	/// The path to use in the SwiftImport attribute
	public let runtimeLibPath: String

	override public var metadataSymbolName: String? { "$s\(name.count)\(name)" }
	override public var module: ModuleDecl { self }

	public init(in framework: Framework, loadedLib: UnsafeMutableRawPointer? = nil)
	{
		self.lib = loadedLib ?? dlopen(framework.binary.path, 0)
		self.runtimeLibPath = framework.url.appendingPathComponent(framework.name).path
		super.init(in: nil, attributes: [], modifiers: [], name: framework.name)
	}

	public init(name: String, runtimeLibPath: String, loadedLib: UnsafeMutableRawPointer? = nil)
	{
		self.lib = loadedLib
		self.runtimeLibPath = runtimeLibPath
		super.init(in: nil, attributes: [], modifiers: [], name: name)
	}
}
