
public enum Severity: String, Equatable {
	case info
	case warning
	case error
}

public struct Message: Identifiable, CustomStringConvertible {
	public var severity: Severity
	public var id: String
	public var text: String
	public var description: String { "\(severity) \(id): \(text)" }

	public static func typeUnresolved(_ name: String) -> Message
	{
		Message(severity: .warning, id: "SBIND001", text: "Unresolved type '\(name)'")
	}
}
