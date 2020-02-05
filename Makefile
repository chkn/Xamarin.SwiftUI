mkfile_path := $(abspath $(lastword $(MAKEFILE_LIST)))
current_dir := $(dir $(mkfile_path))
OUT := $(current_dir)out

all:

# SwiftGlue
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj clean

# iOS

# - Simulator
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue iOS" -arch x86_64 -sdk iphonesimulator

# - Hardware
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue iOS"

# TODO lipo these 2 together


# macOS
# - Hardware
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue macOS"


# tvOS
# - Simulator
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue tvOS" -arch x86_64 -sdk appletvsimulator

# - Hardware
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue tvOS"

# TODO lipo these 2 together


# watchOS
# - Simulator
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue watchOS" -arch x86_64 -sdk watchsimulator

# - Hardware
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target "SwiftUIGlue watchOS"

# TODO lipo these 2 together

clean:
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj clean