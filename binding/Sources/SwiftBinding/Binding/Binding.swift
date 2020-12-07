
public protocol Binding {
	var id: String { get }
	var children: [Binding] { get }
	func write(_ writer: Writer)
}
