
import Foundation

open class MethodBinding: Binding {
	public let decl: FunctionDecl

	public var id: String { decl.qualifiedName }

	public init(_ decl: FunctionDecl)
	{
		self.decl = decl
	}

	open func write(_ writer: Writer)
	{
		writeAccessibilityAndName(writer)
		writeParameterList(writer)
		writeBody(writer)
	}

	open func writeAccessibilityAndName(_ writer: Writer)
	{
		// FIXME
	}

	open func writeParameterList(_ writer: Writer)
	{
		writer.write("(")

		var hadPrev = false
		for p in decl.parameters {
			if hadPrev {
				writer.write(", ")
			}
			writeParameter(p, to: writer)
			hadPrev = true
		}

		writer.write(")")
	}

	open func writeParameter(_ p: Parameter, to writer: Writer)
	{
		writer.write("\(p.type!.qualifiedName) \((p.secondName ?? p.firstName)!)")
	}

	open func writeBody(_ writer: Writer)
	{
		// FIXME
	}
}
