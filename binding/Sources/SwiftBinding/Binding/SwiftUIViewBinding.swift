
open class SwiftUIViewBinding: SwiftStructBinding {

	override open func write(to writer: Writer, csharp state: CSharpState)
	{
		super.write(to: writer, csharp: state)
		writer.write(enclosed: "\npartial class Views {\n", "\n}", Writable(id: self.id + "_Views", write: { wr in
			// FIXME: write stuff here
		}))
	}
}
