using System;
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SwiftUI.Analyzers
{
	[ExportCodeFixProvider (LanguageNames.CSharp), Shared]
	public class ViewCodeFixProvider : CodeFixProvider
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
			=> ImmutableArray.Create (ViewAnalyzer.MissingBodyId, ViewAnalyzer.NotPartialClassId);

		public override FixAllProvider GetFixAllProvider () => WellKnownFixAllProviders.BatchFixer;

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var diag = context.Diagnostics.First ();
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken);

			// Find the class declaration that our diagnostic is on
			var decl = root?.FindToken (diag.Location.SourceSpan.Start)
			                .Parent?
			                .AncestorsAndSelf ()
			                .OfType<ClassDeclarationSyntax> ()
			                .FirstOrDefault ();
			if (decl is null)
				return;

			// The code fix itself
			Task<Document> Fix (CancellationToken cancel)
			{
				var newDecl = decl!;
				var diags = context.Diagnostics;

				if (diags.ContainsId (ViewAnalyzer.MissingBodyId))
					newDecl = newDecl.AddMembers (BodyPropertyDecl);

				if (diags.ContainsId (ViewAnalyzer.NotPartialClassId))
					newDecl = newDecl.AddModifiers (Token (SyntaxKind.PartialKeyword));

				var newDoc = context.Document;
				if (newDecl != decl) {
					var newRoot = root!.ReplaceNode (decl, newDecl);
					newDoc = context.Document.WithSyntaxRoot (newRoot);
				}
				return Task.FromResult (newDoc);
			}

			var action = CodeAction.Create ("Implement custom view", Fix, nameof (ViewCodeFixProvider));
			context.RegisterCodeFix (action, diag);
		}
	}
}
