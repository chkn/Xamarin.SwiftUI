import Foundation

public struct PrimaryCtorBinding: Binding {
	public let initializer: InitializerDecl

	public var id: String { "\(initializer.context?.qualifiedName ?? "").ctor" }

	public var children: [Binding] { [] }

	public init(_ initializer: InitializerDecl)
	{
		self.initializer = initializer
	}

	public func write(_ writer: Writer)
	{
		writer.write("(")

		var hadPrev = false
		for child in children {
			if hadPrev {
				writer.write(", ")
			}
			writer.write(child: child)
			hadPrev = true
		}

		writer.write(")")
	}
}

