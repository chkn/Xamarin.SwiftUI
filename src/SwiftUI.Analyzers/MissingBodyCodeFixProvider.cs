using System;
using System.Linq;
using System.Threading;
using System.Composition;
using System.Threading.Tasks;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SwiftUI.Analyzers
{
	[ExportCodeFixProvider (LanguageNames.CSharp), Shared]
	public class MissingBodyCodeFixProvider : CodeFixProvider
	{
		static readonly PropertyDeclarationSyntax BodyPropertyDecl =
			PropertyDeclaration (
					QualifiedName (IdentifierName ("SwiftUI"), IdentifierName ("View")),
					Identifier ("Body"))
				.WithModifiers (TokenList (Token (SyntaxKind.PublicKeyword)))
				.WithExpressionBody (
					ArrowExpressionClause (
						ThrowExpression (
							ObjectCreationExpression (
								QualifiedName (IdentifierName ("System"), IdentifierName ("NotImplementedException")))
							.WithArgumentList (
								ArgumentList ()))))
				.WithSemicolonToken (Token (SyntaxKind.SemicolonToken))
				.WithAdditionalAnnotations (Simplifier.Annotation)
				.WithAdditionalAnnotations (Formatter.Annotation);

		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (ViewBodyAnalyzer.MissingBodyId);

		public override FixAllProvider GetFixAllProvider () => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var diag = context.Diagnostics.First ();
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken);
			var decl = root!.FindToken (diag.Location.SourceSpan.Start).Parent!.AncestorsAndSelf ().OfType<ClassDeclarationSyntax> ().First ();

			context.RegisterCodeFix (CodeAction.Create (
				"Implement Body property",
				cancel => ImplementBodyAsync (context.Document, decl, cancel),
				equivalenceKey: nameof (MissingBodyCodeFixProvider)), diag);
		}

		static async Task<Document> ImplementBodyAsync (Document document, ClassDeclarationSyntax decl, CancellationToken cancelToken)
		{
			var root = await document.GetSyntaxRootAsync (cancelToken);
			var newDecl =
				decl.AddMembers (BodyPropertyDecl)
				    .AddModifierIfMissing (SyntaxKind.PartialKeyword);
			var newRoot = root!.ReplaceNode (decl, newDecl);
			return document.WithSyntaxRoot (newRoot);
		}
	}
}
