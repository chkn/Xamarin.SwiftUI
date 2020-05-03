using System;
using System.Linq;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SwiftUI.Analyzers
{
	/// <summary>
	/// An analyzer that ensures that custom views declare a proper <c>Body</c> property.
	/// </summary>
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class ViewBodyAnalyzer : DiagnosticAnalyzer
	{
		public const string MissingBodyId = "SWUI001";
		public const string BodyReturnTypeId = "SWUI002";

		public static readonly DiagnosticDescriptor MissingBodyDiag = new DiagnosticDescriptor (
			MissingBodyId,
			"View Body",
			"Custom view '{0}' does not declare a 'Body' property",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor BodyReturnTypeDiag = new DiagnosticDescriptor (
			BodyReturnTypeId,
			"View Body Return Type",
			"'Body' property must use exact return type if declaring class is not partial",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create (MissingBodyDiag, BodyReturnTypeDiag);

		public override void Initialize (AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution ();
			context.RegisterSyntaxNodeAction (AnalyzeNode, SyntaxKind.ClassDeclaration);
		}

		static void AnalyzeNode (SyntaxNodeAnalysisContext ctx)
		{
			var decl = (ClassDeclarationSyntax)ctx.Node;
			var symbol = ctx.SemanticModel.GetDeclaredSymbol (decl, ctx.CancellationToken);
			if (symbol is null || !symbol.IsCustomView ())
				return;

			var body = symbol.GetBody ();
			if (body is null) {
				ctx.ReportDiagnostic (Diagnostic.Create (MissingBodyDiag, decl.Identifier.GetLocation (), decl.Identifier));
				return;
			}

			if (!decl.HasModifier (SyntaxKind.PartialKeyword) && body.Type.Is ("SwiftUI", "View", exact: true))
				ctx.ReportDiagnostic (Diagnostic.Create (BodyReturnTypeDiag, body.Locations.First (loc => loc.SourceTree == decl.SyntaxTree)));
		}
	}
}
