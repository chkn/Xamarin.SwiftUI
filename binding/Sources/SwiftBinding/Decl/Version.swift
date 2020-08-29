
import Foundation

public struct Version: RawRepresentable, CustomStringConvertible, ExpressibleByStringLiteral {
	public var major: Int
	public var minor: Int?
	public var revision: Int?

	public var rawValue: String {
		if let min = minor, let rev = revision {
			return "\(major).\(min).\(rev)"
		} else if let min = minor {
			return "\(major).\(min)"
		} else {
			return "\(major)"
		}
	}

	public var description: String { rawValue }

	public init(stringLiteral: String)
	{
		self.init(rawValue: stringLiteral)!
	}

	public init?(rawValue: String)
	{
		let scanner = Scanner(string: rawValue)
		guard let maj = scanner.scanInt() else { return nil }

		self.major = maj
		if let dot = scanner.scanCharacter(), dot == "." {
			self.minor = scanner.scanInt()
		}
		if let dot = scanner.scanCharacter(), dot == "." {
			self.revision = scanner.scanInt()
		}
	}

	public init(_ major: Int, _ minor: Int? = nil, _ revision: Int? = nil)
	{
		self.major = major
		self.minor = minor
		self.revision = revision
	}
}
