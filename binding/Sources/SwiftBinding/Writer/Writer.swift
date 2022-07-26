
public protocol Writer {
	func write(_ text: String)
	func write(child: Writable)
}

public extension Writer {
	func write(child: Writable)
	{
		child.write(self)
	}
}
