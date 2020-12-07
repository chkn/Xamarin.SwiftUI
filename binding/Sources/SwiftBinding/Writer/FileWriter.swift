
import Foundation

public struct FileWriter: Writer {
	let handle: FileHandle

	public init?(_ path: URL, header: String? = nil)
	{
		FileManager.default.createFile(atPath: path.path, contents: nil)
		guard let hndl = try? FileHandle(forWritingTo: path) else { return nil }
		self.handle = hndl

		if let str = header {
			write(str)
		}
	}

	public func write(_ text: String)
	{
		handle.write(text.data(using: .utf8)!)
	}
}
