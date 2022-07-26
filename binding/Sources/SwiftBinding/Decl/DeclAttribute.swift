
import SwiftSyntax

public enum AvailabilityPlatform: String {
	case iOS
	case OSX
	case tvOS
	case watchOS
	//case swift // indicates required Swift version
}

public enum Availability: Equatable {
	case available(on: AvailabilityPlatform, version: Version)
	case unavailable(on: AvailabilityPlatform)
	case star
}

// the Swift attributes we care about
public enum DeclAttribute: Equatable {
	case _disfavoredOverload
	case available(availability: [Availability])
	case frozen
	case ViewBuilder
}

// don't care about setter only- swiftUI interface doesn't declare these modifiers
public enum DeclModifier: String {
	case `private`
	case `fileprivate`
	case `internal`
	case `public`
	case open
}

public enum ThrowSpec: String {
	case `throws`
	case `rethrows`
}

public extension Availability {
	static func parse(_ syntax: AvailabilityArgumentSyntax) -> Availability?
	{
		let entry = syntax.entry
		if let versionRestriction = AvailabilityVersionRestrictionSyntax(entry) {
			guard let platform = AvailabilityPlatform(rawValue: versionRestriction.platform.text.trim()) else { return nil }
			guard let version = Version(rawValue: versionRestriction.version.description.trim()) else { return nil }
			return .available(on: platform, version: version)
		}
		if entry.description.trim() == "*" {
			return .star
		}
		return nil
	}
}

public extension DeclAttribute {
	static func parse(_ syntax: Syntax) -> DeclAttribute?
	{
		if let attr = AttributeSyntax(syntax) {
			switch attr.name {
			case "_disfavoredOverload": return ._disfavoredOverload
			case "available":
				guard let args = attr.argument?.as(AvailabilitySpecListSyntax.self) else { return nil }
				if args.description.trim().hasSuffix(", unavailable") {
					guard let platform = AvailabilityPlatform(rawValue: args.first!.description.trim().trimmingCharacters(in: [","])) else { return nil }
					return .available(availability: [.unavailable(on: platform)])
				}
				let availability = args.compactMap(Availability.parse)
				return availability.isEmpty ? nil : .available(availability: availability)
			case "frozen": return .frozen
			default: return nil
			}
		}

		if let attr = CustomAttributeSyntax(syntax) {
			switch attr.name {
			case "SwiftUI.ViewBuilder": return .ViewBuilder
			default: return nil
			}
		}

		return nil
	}
}
