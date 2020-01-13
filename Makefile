mkfile_path := $(abspath $(lastword $(MAKEFILE_LIST)))
current_dir := $(dir $(mkfile_path))
OUT := $(current_dir)out

all:
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target SwiftUIGlue
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj -target SwiftUIGlue.iOS
