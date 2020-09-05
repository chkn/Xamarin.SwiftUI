
import Foundation

public enum SDK: CaseIterable {
	case iOS
	case tvOS
	case macOS
	case watchOS

	/// Runtime root for this SDK relative to the Xcode Developer path.
	public var runtimeRoot: String {
		switch self {
		case .iOS:
			return "Platforms/iPhoneOS.platform/Library/Developer/CoreSimulator/Profiles/Runtimes/iOS.simruntime/Contents/Resources/RuntimeRoot"
		case .tvOS:
			abort()
		case .macOS:
			abort()
		case .watchOS:
			abort()
		}
	}

	/// SDK root for this SDK relative to the Xcode Developer path.
	public var sdkRoot: String {
		switch self {
		case .iOS:
			return "Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk"
		case .tvOS:
			abort()
		case .macOS:
			abort()
		case .watchOS:
			abort()
		}
	}
}

public struct Xcode {
	public var developerPath: URL

	public func runtimeRoot(for sdk: SDK) -> URL
	{
		developerPath.appendingPathComponent(sdk.runtimeRoot)
	}

	public func sdkRoot(for sdk: SDK) -> URL
	{
		developerPath.appendingPathComponent(sdk.sdkRoot)
	}

	public init(developerPath: URL)
	{
		self.developerPath = developerPath
	}

	public func framework(at path: URL, for sdk: SDK) -> Framework
	{
		Framework(path, sdkRoot(for: sdk), runtimeRoot(for: sdk))
	}

	public static var `default` : Xcode
	{
		let sdkPath = URL(fileURLWithPath: xcode_select(["-p"]))
		return Xcode(developerPath: sdkPath)
	}

	static func xcode_select(_ args : [String]) -> String
	{
		let proc = Process()
		proc.executableURL = URL(fileURLWithPath: "/usr/bin/xcode-select")
		proc.arguments = args

		let stdout = Pipe()
		proc.standardOutput = stdout
		try! proc.run()

		return String(decoding: stdout.fileHandleForReading.readDataToEndOfFile(), as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines)
	}
}
