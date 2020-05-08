using System;
using System.Linq;
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
		public static IPropertySymbol? GetBody (this INamedTypeSymbol viewType)
			=> viewType.GetMembers ()
			           .OfType<IPropertySymbol> ()
			           .SingleOrDefault (m => m.Name == "Body" && m.Type.Is ("SwiftUI", "View"));

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

		internal static ExpressionSyntax? GetReturnExpression (this PropertyDeclarationSyntax prop)
		{
			var expr = prop.ExpressionBody;
			if (expr != null)
				return expr.Expression;

			var getter = prop.AccessorList?.Accessors.FirstOrDefault (acc => acc.Kind () == SyntaxKind.GetAccessorDeclaration);
			if (getter == null)
				return null;

			expr = getter.ExpressionBody;
			if (expr != null)
				return expr.Expression;

			return getter.Body?.Statements.OfType<ReturnStatementSyntax> ().FirstOrDefault ()?.Expression;
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

		internal static bool HasModifier (this ClassDeclarationSyntax cls, SyntaxKind modifier)
			=> cls.Modifiers.Any (m => m.IsKind (modifier));
	}
}
