import Foundation

public struct Framework {
	public var url: URL
	public var sdkRoot: URL?
	public var runtimeRoot: URL?

	public var name: String { url.deletingPathExtension().lastPathComponent }

	/// The path to this Framework's binary
	public var binary: URL { path(from: runtimeRoot).appendingPathComponent(name) }

	/// The path to this Framework's swiftinterface file
	public var swiftinterface: URL? {
		let dir = path(from: sdkRoot).appendingPathComponent("Modules/\(name).swiftmodule")
		let contents = try? FileManager.default.contentsOfDirectory(at: dir, includingPropertiesForKeys: nil)
		return contents?.first(where: { $0.pathExtension == "swiftinterface" })
	}

	public init(_ url: URL, _ sdkRoot: URL? = nil, _ runtimeRoot: URL? = nil)
	{
		self.url = url
		self.sdkRoot = sdkRoot
		self.runtimeRoot = runtimeRoot
	}

	func path(from root: URL?) -> URL
	{
		root?.appendingPathComponent(url.path) ?? url
	}
}
