
public protocol Writer {
	func write(_ text: String)
	func write(child: Writable)
	func write(enclosed: String, _ closing: String, _ writable: Writable)
}

public extension Writer {
	func write(child: Writable)
	{
		child.write(self)
	}

	func write(enclosed opening: String, _ closing: String, _ writable: Writable)
	{
		self.write(opening)
		writable.write(self)
		self.write(closing)
	}
}
