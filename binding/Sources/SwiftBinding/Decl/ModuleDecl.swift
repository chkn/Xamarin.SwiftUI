
import Foundation

public class ModuleDecl: Decl {
	public init(in context: Decl?, name: String)
	{
		super.init(in: context, attributes: [], modifiers: [], name: name)
	}
}
