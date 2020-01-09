mkfile_path := $(abspath $(lastword $(MAKEFILE_LIST)))
current_dir := $(dir $(mkfile_path))
OUT := $(current_dir)out

all:
	xcodebuild -project src/SwiftUIGlue/SwiftUIGlue.xcodeproj
	msbuild

# iOS Only
ios:
	msbuild /p:XamiOSSwiftUITest=iOS

# Mac Only
mac:
	msbuild /p:XamMacSwiftUITest=Any
	msbuild /p:XamMacSwiftUITest_FSharp=Any

restore:
	msbuild /t:Restore /p:nugetInteractive=true

run:
	msbuild /t:Run

clean:
	msbuild /t:Clean
