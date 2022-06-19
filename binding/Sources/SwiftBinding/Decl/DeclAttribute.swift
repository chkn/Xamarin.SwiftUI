

public enum Availability {
	case availableOn(platform: String, version: Version)
	case unavailableOn(platform: String)
	case star
}

// the Swift attributes we care about
public enum DeclAttribute: String {
	//case available(availability: Availability) // FIXME
	case frozen
	case _disfavoredOverload
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
