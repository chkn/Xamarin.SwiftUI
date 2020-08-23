
import Foundation

public class Module: Decl {
	public init (in context: Decl?, name: String)
	{
		super.init(in: context, attributes: [], modifiers: [], name: name)
	}
}
