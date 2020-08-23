//
//  SDK.swift
//  Generator
//
//  Created by Alex Corrado on 7/18/20.
//

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

	public func runtimeRoot(_ sdk: SDK) -> URL
	{
		developerPath.appendingPathComponent(sdk.runtimeRoot)
	}

	public func sdkRoot(_ sdk: SDK) -> URL
	{
		developerPath.appendingPathComponent(sdk.sdkRoot)
	}

	public static func name(of framework: URL) -> String
	{
		framework.deletingPathExtension().lastPathComponent
	}

	public func binaryPath(of framework: URL, forSdk: SDK) -> URL
	{
		runtimeRoot(forSdk).appendingPathComponent(framework.path).appendingPathComponent(Xcode.name(of: framework))
	}

	public func swiftinterfacePath(of framework: URL, forSdk: SDK) throws -> URL?
	{
		let path = sdkRoot(forSdk).appendingPathComponent(framework.path).appendingPathComponent("Modules/\(Xcode.name(of: framework)).swiftmodule")
		return try FileManager.default.contentsOfDirectory(at: path, includingPropertiesForKeys: nil).first(where: { $0.pathExtension == "swiftinterface" })
	}

	public init(developerPath: URL)
	{
		self.developerPath = developerPath
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
