
/// A struct bound as a managed value type implementing ISwiftBlittableStruct
open class BlittableStructBinding: TypeBinding {

	public init(_ type: StructDecl)
	{
		super.init(type)
	}

	override open func writeType(to writer: Writer, csharp: CSharpState)
	{
		writer.write("""
[StructLayout (LayoutKind.Sequential)]
public readonly struct \(genericFullName) : ISwiftBlittableStruct<\(genericFullName)>
{

}
""")
	}
}
