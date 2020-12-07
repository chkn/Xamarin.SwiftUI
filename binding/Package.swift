// swift-tools-version:5.1
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
  name: "SwiftBinding",
  platforms: [
    .macOS(.v10_15),
  ],
  products: [
    .executable(name: "swiftbind", targets: ["swiftbind"]),
    .library(name: "SwiftBinding", targets: ["SwiftBinding"]),
  ],
  dependencies: [
    // requires Xcode 12.x
    .package(url: "https://github.com/apple/swift-syntax.git", .exact("0.50300.0")),
  ],
  targets: [
    // Targets are the basic building blocks of a package. A target can define a module or a test suite.
    // Targets can depend on other targets in this package, and on products in packages which this package depends on.
    .target(
      name: "swiftbind",
      dependencies: ["SwiftBinding"]),

    .target(
	  name: "SwiftBinding",
	  dependencies: ["SwiftSyntax"])
	]
)
