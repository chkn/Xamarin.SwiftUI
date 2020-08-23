// swift-tools-version:5.1
// The swift-tools-version declares the minimum version of Swift required to build this package.

import PackageDescription

let package = Package(
  name: "SwiftBinding",
  platforms: [
    .macOS(.v10_13),
  ],
  products: [
    .executable(name: "swiftbind", targets: ["swiftbind"]),
    .library(name: "SwiftBinding", targets: ["SwiftBinding"]),
  ],
  dependencies: [
    // requires Xcode 11.4
    .package(url: "https://github.com/apple/swift-syntax.git", .exact("0.50200.0")),
    .package(url: "https://github.com/stencilproject/Stencil", .branch("master")),
  ],
  targets: [
    // Targets are the basic building blocks of a package. A target can define a module or a test suite.
    // Targets can depend on other targets in this package, and on products in packages which this package depends on.
    .target(
      name: "swiftbind",
      dependencies: ["SwiftBinding", "Stencil"]),

    .target(
	  name: "SwiftBinding",
	  dependencies: ["SwiftSyntax"])
	]
)
