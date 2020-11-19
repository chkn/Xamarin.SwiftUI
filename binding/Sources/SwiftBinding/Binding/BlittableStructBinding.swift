
/// A struct bound as a managed value type implementing ISwiftBlittableStruct
public class BlittableStructBinding: Binding {

	public init(_ type: StructDecl)
	{
		super.init(type)
	}
}
