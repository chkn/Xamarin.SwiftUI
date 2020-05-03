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
	public class BodyReturnTypeCodeFixProvider : CodeFixProvider
	{
		public override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (ViewBodyAnalyzer.BodyReturnTypeId);

		public override FixAllProvider GetFixAllProvider () => WellKnownFixAllProviders.BatchFixer;

		public override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var diag = context.Diagnostics.First ();
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken);
			var decl = root!.FindToken (diag.Location.SourceSpan.Start).Parent!.AncestorsAndSelf ().OfType<PropertyDeclarationSyntax> ().First ();

			context.RegisterCodeFix (CodeAction.Create (
				"Make declaring class partial",
				cancel => MakePartialAsync (context.Document, decl.Ancestors ().OfType<ClassDeclarationSyntax> ().First (), cancel),
				equivalenceKey: $"{nameof (BodyReturnTypeCodeFixProvider)}.{nameof (MakePartialAsync)}"), diag);

			var returnExpr = decl.GetReturnExpression ();
			if (returnExpr != null) {
				var model = await context.Document.GetSemanticModelAsync (context.CancellationToken);
				var type = model.GetTypeInfo (returnExpr, context.CancellationToken).Type!;
				if (!(type is IErrorTypeSymbol)) {
					context.RegisterCodeFix (CodeAction.Create (
						"Use exact return type",
						cancel => MakeExactTypeAsync (context.Document, decl, type, cancel),
						equivalenceKey: $"{nameof (BodyReturnTypeCodeFixProvider)}.{nameof (MakeExactTypeAsync)}"), diag);
				}
			}
		}

		static async Task<Document> MakePartialAsync (Document document, ClassDeclarationSyntax decl, CancellationToken cancelToken)
		{
			var root = await document.GetSyntaxRootAsync (cancelToken);
			var newDecl = decl.AddModifiers (Token (SyntaxKind.PartialKeyword));
			var newRoot = root!.ReplaceNode (decl, newDecl);
			return document.WithSyntaxRoot (newRoot);
		}

		static async Task<Document> MakeExactTypeAsync (Document document, PropertyDeclarationSyntax decl, ITypeSymbol type, CancellationToken cancelToken)
		{
			var root = await document.GetSyntaxRootAsync (cancelToken);
			var model = await document.GetSemanticModelAsync (cancelToken);
			var typeSyntax = ParseTypeName (type.ToMinimalDisplayString (model!, decl.Type.GetLocation ().SourceSpan.Start));
			var newDecl = decl.WithType (typeSyntax.WithTriviaFrom (decl.Type));
			var newRoot = root!.ReplaceNode (decl, newDecl);
			return document.WithSyntaxRoot (newRoot);
		}
	}
}
