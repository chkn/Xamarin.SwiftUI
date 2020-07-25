//
//  SDK.swift
//  Generator
//
//  Created by Alex Corrado on 7/18/20.
//

import Foundation

enum SDK : CaseIterable {
	case iOS
	case tvOS
	case macOS
	case watchOS
}

struct Xcode {
	var developerPath: URL

	public func binary(forSDK : SDK) -> URL
	{
		switch forSDK {
		case .iOS:
			return developerPath.appendingPathComponent("Platforms/iPhoneOS.platform/Library/Developer/CoreSimulator/Profiles/Runtimes/iOS.simruntime/Contents/Resources/RuntimeRoot/System/Library/Frameworks/SwiftUI.framework/SwiftUI")
		case .tvOS:
			return developerPath
		case .macOS:
			return developerPath
		case .watchOS:
			return developerPath
		}
	}

	public func swiftinterface(forSDK : SDK) -> URL
	{
		switch forSDK {
		case .iOS: return developerPath.appendingPathComponent("Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS.sdk/System/Library/Frameworks/SwiftUI.framework/Modules/SwiftUI.swiftmodule/arm64-apple-ios.swiftinterface")
		case .tvOS:
			return developerPath
		case .macOS:
			return developerPath
		case .watchOS:
			return developerPath
		}
	}

	static var `default` : Xcode
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
