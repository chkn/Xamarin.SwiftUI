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
	/// An analyzer to ensure custom views are partial and declare a proper <c>Body</c> property.
	/// </summary>
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class ViewAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create (Diagnostics.MissingBody, Diagnostics.NotPartialRecord);

		public override void Initialize (AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);
			context.EnableConcurrentExecution ();
			context.RegisterSyntaxNodeAction (AnalyzeNode, SyntaxKind.RecordDeclaration);
		}

		static void AnalyzeNode (SyntaxNodeAnalysisContext ctx)
		{
			var decl = (RecordDeclarationSyntax)ctx.Node;
			var symbol = ctx.SemanticModel.GetDeclaredSymbol (decl, ctx.CancellationToken);
			if (symbol is null || !symbol.IsCustomView ())
				return;

			if (symbol.GetBodyProperty () is null)
				ctx.ReportDiagnostic (Diagnostic.Create (Diagnostics.MissingBody, decl.Identifier.GetLocation (), decl.Identifier));

			if (!decl.HasModifier (SyntaxKind.PartialKeyword))
				ctx.ReportDiagnostic (Diagnostic.Create (Diagnostics.NotPartialRecord, decl.Identifier.GetLocation (), decl.Identifier));
		}
	}
}
