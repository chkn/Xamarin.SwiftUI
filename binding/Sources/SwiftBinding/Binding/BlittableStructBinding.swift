
/// A struct bound as a managed value type implementing ISwiftBlittableStruct
public class BlittableStructBinding: TypeBinding {

	public init(_ type: StructDecl)
	{
		super.init(type)
	}

	override public func writeType(_ writer: Writer)
	{
		writer.write("""
[StructLayout (LayoutKind.Sequential)]
public readonly struct \(genericFullName) : ISwiftBlittableStruct<\(genericFullName)>
{

}
""")
	}
}
