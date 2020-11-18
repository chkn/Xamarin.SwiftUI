
import Foundation

public class ModuleDecl: Decl {
	//FIXME: Just using the first one.. does it matter?
	let binary: MachOReader.Slice?

	override public var module: ModuleDecl { self }

	public init(in framework: Framework)
	{
		self.binary = MachOReader.slices(in: framework.binary)?.first
		super.init(in: nil, attributes: [], modifiers: [], name: framework.name)
	}

	public init(name: String, binary: URL)
	{
		self.binary = MachOReader.slices(in: binary)?.first
		super.init(in: nil, attributes: [], modifiers: [], name: name)
	}
}
