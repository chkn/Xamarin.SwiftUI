import SwiftSyntax

func swiftinterfaceNotFound(in framework: Framework) -> Diagnostic.Message
{
	Diagnostic.Message(.error, "No swiftinterface found for '\(framework.name)' at '\(framework.swiftinterface?.path ?? "nil")'")
}

func swiftinterfaceParseError(message: String) -> Diagnostic.Message
{
	Diagnostic.Message(.error, "Error parsing swiftinterface: \(message)")
}

func typeUnresolved(_ name: String) -> Diagnostic.Message
{
	Diagnostic.Message(.warning, "Unresolved type '\(name)'")
}
