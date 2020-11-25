using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SwiftUI.Analyzers
{
	[Generator]
	public class ViewBodySourceGenerator : ISourceGenerator
	{
		public void Initialize (GeneratorInitializationContext context)
		{
		}

		public void Execute (GeneratorExecutionContext context)
		{
			foreach (var syntaxTree in context.Compilation.SyntaxTrees) {
				if (context.CancellationToken.IsCancellationRequested)
					return;

				var root = syntaxTree.GetCompilationUnitRoot (context.CancellationToken);
				var model = context.Compilation.GetSemanticModel (syntaxTree);
				var walker = new CompilationUnitWalker (context, model);
				walker.Visit (root);

				var result = walker.Result;
				if (result != null) {
					result = result.WithUsings (root.Usings).NormalizeWhitespace ();
					var fileName = Path.ChangeExtension (Path.GetFileName (syntaxTree.FilePath), ".g.cs");
					var sourceText = result.GetText (Encoding.UTF8);
					//Console.WriteLine (sourceText.ToString ());
					context.AddSource (fileName, sourceText);
				}
			}
		}

		sealed class CompilationUnitWalker : CSharpSyntaxWalker
		{
			GeneratorExecutionContext context;
			SemanticModel model;
			PropertyRewriter rewriter;

			public CompilationUnitSyntax? Result { get; private set; }

			public CompilationUnitWalker (GeneratorExecutionContext context, SemanticModel model)
			{
				this.context = context;
				this.model = model;
				this.rewriter = new PropertyRewriter (context, model);
			}

			public override void VisitStructDeclaration (StructDeclarationSyntax node)
			{
				// Don't visit children
			}

			public override void VisitClassDeclaration (ClassDeclarationSyntax node)
			{
				if (!node.HasModifier (SyntaxKind.PartialKeyword))
					return;

				var symbol = model.GetDeclaredSymbol (node, context.CancellationToken);
				if (symbol is null || !symbol.IsCustomView ())
					return;

				base.VisitClassDeclaration (node);
			}

			public override void VisitMethodDeclaration (MethodDeclarationSyntax node)
			{
				// Don't visit children
			}

			public override void VisitPropertyDeclaration (PropertyDeclarationSyntax node)
			{
				var classDecl = node.Parent as ClassDeclarationSyntax;
				if (classDecl is null)
					return;

				var symbol = model.GetDeclaredSymbol (node, context.CancellationToken);
				if (symbol is null || !symbol.IsBodyProperty ())
					return;

				// First, rewrite the existing node and replace it into a new syntax tree
				//  We need to track it so we can get the equivalent node in the new tree
				var newNode = (PropertyDeclarationSyntax)rewriter.Visit (node)
					.WithAdditionalAnnotations (new SyntaxAnnotation ("newNode"));
				var newRoot = node.SyntaxTree.GetRoot (context.CancellationToken).ReplaceNode (node, newNode);
				var newCompilation = model.Compilation.ReplaceSyntaxTree (node.SyntaxTree, newRoot.SyntaxTree);
				var newModel = newCompilation.GetSemanticModel (newRoot.SyntaxTree);
				newNode = newCompilation.SyntaxTrees
					.SelectMany (t => t.GetRoot (context.CancellationToken).DescendantNodes ())
					.OfType<PropertyDeclarationSyntax> ()
					.First (n => n.GetAnnotations ("newNode").Any ());

				// Then, figure out the actual returned type of the property
				var type = newNode.GetDerivedReturnType (newModel, context.CancellationToken);
				if (type is null) {
					context.ReportDiagnostic (Diagnostic.Create (Diagnostics.BodyReturnType, node.Identifier.GetLocation ()));
					return;
				}
				if (type is IErrorTypeSymbol)
					return;

				newNode =
					PropertyDeclaration (ParseTypeName (type.ToDisplayString ()), "Body__")
						.WithExpressionBody (newNode.ExpressionBody)
						.WithAccessorList (newNode.AccessorList)
						.WithSemicolonToken (Token (SyntaxKind.SemicolonToken));

				var newClassDecl =
					ClassDeclaration (classDecl.Identifier)
						.WithModifiers (classDecl.Modifiers)
						.AddMembers (newNode);

				var container = classDecl.Parent is NamespaceDeclarationSyntax nsDecl
					? NamespaceDeclaration (nsDecl.Name).AddMembers (newClassDecl)
					: (MemberDeclarationSyntax)newClassDecl;

				Result = (Result ?? CompilationUnit()).AddMembers (container);
			}
		}
		sealed class PropertyRewriter : CSharpSyntaxRewriter
		{
			GeneratorExecutionContext context;
			SemanticModel model;

			public PropertyRewriter (GeneratorExecutionContext context, SemanticModel model)
			{
				this.context = context;
				this.model = model;
			}

			public override SyntaxNode? VisitInvocationExpression (InvocationExpressionSyntax node)
			{
				var mthd = model.GetSymbolInfo (node, context.CancellationToken).Symbol as IMethodSymbol;
				if (mthd is null)
					return node;

				var newInvoke = node;
				for (var i = 0; i < mthd.Parameters.Length; i++) {
					INamedTypeSymbol? attrClass = null;
					foreach (var attr in mthd.Parameters [i].GetAttributes ()) {
						if (attr.AttributeClass.IsFunctionBuilderAttribute ()) {
							attrClass = attr.AttributeClass;
							break;
						}
					}
					if (attrClass is null)
						continue;

					// FIXME: Handle when different number of args are passed or they are passed in different order
					//  e.g. named and optional arguments
					var arg = node.ArgumentList.Arguments [i];
					var del = arg.Expression as AnonymousFunctionExpressionSyntax;
					if (del is null) {
						context.ReportDiagnostic (Diagnostic.Create (Diagnostics.ExpectedLambda, arg.GetLocation ()));
						return newInvoke;
					}

					var builder = new FunctionBuilder (ParseTypeName (attrClass.ToDisplayString ()));
					var newArgs =
						newInvoke.ArgumentList.Arguments
							.RemoveAt (i)
							.Insert (i, Argument ((ExpressionSyntax)builder.Visit (del.Body)));

					newInvoke = newInvoke.WithArgumentList (newInvoke.ArgumentList.WithArguments (newArgs));
				}
				return newInvoke;
			}
		}
	}
}
