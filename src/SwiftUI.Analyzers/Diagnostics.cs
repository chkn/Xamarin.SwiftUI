using System;
using Microsoft.CodeAnalysis;

namespace SwiftUI.Analyzers
{
	static class Diagnostics
	{
		public static readonly DiagnosticDescriptor MissingBody = new DiagnosticDescriptor (
			"SWUI001",
			"Custom View Body Missing",
			"Custom view '{0}' does not declare a 'Body' property",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor NotPartialClass = new DiagnosticDescriptor (
			"SWUI002",
			"Custom View Must be Partial Class",
			"Custom view '{0}' is not declared as a partial class",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor BodyReturnType = new DiagnosticDescriptor (
			"SWUI003",
			"Custom View Body Returns Multiple Types",
			"'Body': all code paths do not return the same type",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor ExpectedLambda = new DiagnosticDescriptor (
			"SWUI004",
			"Expected Lambda Expression",
			"Expected lambda expression",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);
	}
}
