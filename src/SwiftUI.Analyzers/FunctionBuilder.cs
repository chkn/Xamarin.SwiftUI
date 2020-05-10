using System;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SwiftUI.Analyzers
{
	/// <summary>
	/// Implements the syntax transform for function builders.
	/// </summary>
	public class FunctionBuilder : CSharpSyntaxRewriter
	{
		readonly TypeSyntax builderType;

		const string BuildBlock = "BuildBlock";
		const string BuildOptional = "BuildOptional";
		const string BuildEitherTrue = "BuildEitherTrue";
		const string BuildEitherFalse = "BuildEitherFalse";

		public FunctionBuilder (TypeSyntax builderType)
		{
			this.builderType = builderType ?? throw new ArgumentNullException (nameof (builderType));
		}

		public override SyntaxNode? VisitBlock (BlockSyntax node)
		{
			var invoke = Invoke (BuildBlock);

			if (node.Statements.Any ()) {
				var args = node.Statements
				               .Select (Visit)
				               .Cast<ExpressionSyntax> ();
				invoke = invoke.WithArgs (args);
			}

			return invoke;
		}

		public override SyntaxNode? VisitIfStatement (IfStatementSyntax node)
			=> node.Else is null? VisitOptional (node) : VisitEither (node);

		SyntaxNode? VisitOptional (IfStatementSyntax node)
			=> Invoke (BuildOptional).WithArgs (
				ConditionalExpression (node.Condition, (ExpressionSyntax)Visit (node.Statement),
					LiteralExpression (SyntaxKind.NullLiteralExpression)));

		SyntaxNode? VisitEither (IfStatementSyntax node)
			=> ConditionalExpression (node.Condition,
				whenTrue: Invoke (BuildEitherTrue).WithArgs ((ExpressionSyntax)Visit (node.Statement)),
				whenFalse: Invoke (BuildEitherFalse).WithArgs ((ExpressionSyntax)Visit (node.Else!.Statement)));

		public override SyntaxNode? VisitExpressionStatement (ExpressionStatementSyntax node)
			=> node.Expression.WithoutTrivia ();

		InvocationExpressionSyntax Invoke (string methodName)
			=> InvocationExpression (
				MemberAccessExpression (
					SyntaxKind.SimpleMemberAccessExpression,
					builderType,
					IdentifierName (methodName)));
	}
}
