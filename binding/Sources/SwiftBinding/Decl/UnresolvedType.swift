
import Foundation

public class UnresolvedTypeDecl: TypeDecl {
	public init(in context: Decl?, name: String)
	{
		super.init(in: context, attributes: [], modifiers: [], name: name)
	}
}
