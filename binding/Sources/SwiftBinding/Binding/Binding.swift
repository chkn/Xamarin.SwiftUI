
public protocol Binding {
	var id: String { get }
	func write(_ writer: Writer)
}
