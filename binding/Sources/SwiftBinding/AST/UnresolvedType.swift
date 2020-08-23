
import Foundation

public class UnresolvedType: Type {
	public init(_ name: String)
	{
		super.init(in: nil, attributes: [], modifiers: [], name: name)
	}
}
