using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SwiftUI.Analyzers
{
	/// <summary>
	/// SwiftUI extensions for Roslyn
	/// </summary>
	public static class SwiftUIExtensions
	{
		/// <summary>
		/// Gets the <c>Body</c> property of the given <c>SwiftUI.View</c>, or <c>null</c>.
		/// </summary>
		public static IPropertySymbol? GetBodyProperty (this INamedTypeSymbol viewType)
			=> viewType.GetMembers ()
			           .OfType<IPropertySymbol> ()
			           .SingleOrDefault (IsBodyProperty);

		public static bool IsBodyProperty (this IPropertySymbol prop)
			=> prop.Name == "Body" && prop.Type.Is ("SwiftUI", "View");

		public static bool IsCustomView (this INamedTypeSymbol symbol)
		{
			// Direct base class must be SwiftUI.View
			if (!symbol.BaseType.Is ("SwiftUI", "View", exact: true))
				return false;

			// Must not have a SwiftTypeAttribute on it
			foreach (var attr in symbol.GetAttributes ()) {
				if (attr.AttributeClass.Is ("Interop", "SwiftTypeAttribute"))
					return false;
			}

			return true;
		}

		public static bool IsFunctionBuilderAttribute (this INamedTypeSymbol? symbol)
			=> symbol != null && symbol.BaseType.Is ("Swift", "FunctionBuilderAttribute");

		// FIXME: Walk all return statements and try to unify the types
		internal static ITypeSymbol? GetDerivedReturnType (this PropertyDeclarationSyntax prop, SemanticModel model, CancellationToken cancel = default)
		{
			var expr = prop.ExpressionBody?.Expression;
			if (expr is null) {
				var getter = prop.AccessorList?.Accessors.FirstOrDefault (acc => acc.Kind () == SyntaxKind.GetAccessorDeclaration);
				if (getter == null)
					return null;

				expr = getter.ExpressionBody?.Expression;
				if (expr is null) {
					expr = getter.Body?.Statements.OfType<ReturnStatementSyntax> ().FirstOrDefault ()?.Expression;
					if (expr is null)
						return null;
				}
			}
			return model.GetTypeInfo (expr, cancel).Type;
		}

		internal static bool Is (this ITypeSymbol? symbol, string ns, string typeName, bool exact = false)
		{
			if (symbol is null)
				return false;

			if (symbol.Name == typeName &&
				symbol.ContainingNamespace?.Name == ns &&
				symbol.ContainingAssembly?.Name == "SwiftUI")
				return true;

			return !exact && symbol.BaseType.Is (ns, typeName);
		}

		internal static bool ContainsId (this ImmutableArray<Diagnostic> diags, string id)
		{
			foreach (var diag in diags) {
				if (diag.Id == id)
					return true;
			}
			return false;
		}

		internal static InvocationExpressionSyntax WithArgs (this InvocationExpressionSyntax invoke, ExpressionSyntax arg)
			=> invoke.WithArgumentList (ArgumentList (SingletonSeparatedList (Argument (arg))));

		internal static InvocationExpressionSyntax WithArgs (this InvocationExpressionSyntax invoke, IEnumerable<ExpressionSyntax> args)
			=> invoke.WithArgumentList (ArgumentList (SeparatedList (args.Select (Argument))));

		internal static bool HasModifier (this ClassDeclarationSyntax cls, SyntaxKind modifier)
			=> cls.Modifiers.Any (m => m.IsKind (modifier));

		internal static bool HasModifier (this RecordDeclarationSyntax rec, SyntaxKind modifier)
			=> rec.Modifiers.Any (m => m.IsKind (modifier));
	}
}
