
import Foundation

public class ModuleDecl: Decl {
	let lib: UnsafeMutableRawPointer?

	override public var metadataSymbolName: String? { "$s\(name.count)\(name)" }
	override public var module: ModuleDecl { self }

	public init(in framework: Framework, loadedLib: UnsafeMutableRawPointer? = nil)
	{
		self.lib = loadedLib ?? dlopen(framework.binary.path, 0)
		super.init(in: nil, attributes: [], modifiers: [], name: framework.name)
	}

	public init(name: String, loadedLib: UnsafeMutableRawPointer? = nil)
	{
		self.lib = loadedLib
		super.init(in: nil, attributes: [], modifiers: [], name: name)
	}
}
