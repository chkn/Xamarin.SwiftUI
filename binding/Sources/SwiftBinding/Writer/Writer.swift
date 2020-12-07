
public protocol Writer {
	func write(_ text: String)
	func write(child: Binding)
}

public extension Writer {
	func write(child: Binding)
	{
		child.write(self)
	}
}
