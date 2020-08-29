
/// A struct bound as a managed reference type deriving from SwiftStruct
public class SwiftStructBinding: Binding {
	public let baseClass: String

	public init(_ type: StructDecl, _ baseClass: String)
	{
		self.baseClass = baseClass
		super.init(type)
	}
}
