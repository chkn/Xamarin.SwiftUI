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
		public const string MissingBodyId = "SWUI001";
		public const string NotPartialClassId = "SWUI002";

		public static readonly DiagnosticDescriptor MissingBodyDiag = new DiagnosticDescriptor (
			MissingBodyId,
			"Custom View Body",
			"Custom view '{0}' does not declare a 'Body' property",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public static readonly DiagnosticDescriptor NotPartialClassDiag = new DiagnosticDescriptor (
			NotPartialClassId,
			"Custom View must be Partial Class",
			"Custom view '{0}' is not declared as a partial class",
			"SwiftUI", DiagnosticSeverity.Error, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
			=> ImmutableArray.Create (MissingBodyDiag, NotPartialClassDiag);

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

			if (symbol.GetBody () is null)
				ctx.ReportDiagnostic (Diagnostic.Create (MissingBodyDiag, decl.Identifier.GetLocation (), decl.Identifier));

			if (!decl.HasModifier (SyntaxKind.PartialKeyword))
				ctx.ReportDiagnostic (Diagnostic.Create (NotPartialClassDiag, decl.Identifier.GetLocation (), decl.Identifier));
		}
	}
}
