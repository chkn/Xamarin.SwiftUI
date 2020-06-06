#I "/Users/alex/.nuget/packages/fparsec/1.1.1/lib/netstandard2.0"

#r "FParsecCS.dll"
#r "FParsec.dll"
#r "bin/Debug/netstandard2.0/Swift.Parser.dll"

open System.Text
open FParsec
open Swift.Parser

let path = "/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk/System/Library/Frameworks/SwiftUI.framework/Modules/SwiftUI.swiftmodule/x86_64-apple-macos.swiftinterface"

runParserOnFile file () path Encoding.UTF8
