
import Foundation

public enum SDK: CaseIterable, Hashable {
	case macOS
	case iOS
	case tvOS
	//case watchOS

	public var conditionalDefine: String {
		switch self {
		case .macOS: return "__MACOS__"
		case .iOS: return "__IOS__"
		case .tvOS: return "__TVOS__"
		}
	}

	/// Runtime root for this SDK
	public func runtimeRoot(_ developerPath: URL) -> URL
	{
		switch self {
		case .macOS:
			return URL(fileURLWithPath: "/")
		case .iOS:
			return developerPath.appendingPathComponent("Platforms/iPhoneOS.platform/Library/Developer/CoreSimulator/Profiles/Runtimes/iOS.simruntime/Contents/Resources/RuntimeRoot")
		case .tvOS:
			return developerPath.appendingPathComponent("Platforms/AppleTVOS.platform/Library/Developer/CoreSimulator/Profiles/Runtimes/tvOS.simruntime/Contents/Resources/RuntimeRoot")
//		case .watchOS:
//			return developerPath.appendingPathComponent("Platforms/WatchOS.platform/Library/Developer/CoreSimulator/Profiles/Runtimes/watchOS.simruntime/Contents/Resources/RuntimeRoot")
		}
	}

	/// SDK root for this SDK
	public func sdkRoot(_ developerPath: URL) -> URL
	{
		switch self {
		case .macOS:
			return developerPath.appendingPathComponent("Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk")
		case .iOS:
			return developerPath.appendingPathComponent("Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk")
		case .tvOS:
			return developerPath.appendingPathComponent("Platforms/AppleTVOS.platform/Developer/SDKs/AppleTVOS.sdk")
//		case .watchOS:
//			return developerPath.appendingPathComponent("Platforms/WatchOS.platform/Developer/SDKs/WatchOS.sdk")
		}
	}
}

public struct Xcode {
	public var developerPath: URL

	public init(developerPath: URL)
	{
		self.developerPath = developerPath
	}

	public func framework(at path: URL, for sdk: SDK) -> Framework
	{
		Framework(path, sdk.sdkRoot(developerPath), sdk.runtimeRoot(developerPath))
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
