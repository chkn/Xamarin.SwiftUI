import SwiftSyntax

open class MemberDecl: Decl, HasTypesToResolve {
    public var genericParameters: [GenericParameter]

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter(in: context, $0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}

	open func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?) {
		for var gp in genericParameters {
			gp.resolveTypes(resolve)
		}
	}
}

open class FunctionDecl: MemberDecl {
	public var parameters: [Parameter]

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ parameterClause: ParameterClauseSyntax, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		parameters = parameterClause.parameterList.map { Parameter($0) }
		super.init(in: context, attributes, modifiers, name, genericParameterClause, genericWhereClause)
	}

	open override func resolveTypes(_ resolve: (TypeDecl) -> TypeDecl?) {
		super.resolveTypes(resolve)
		for var p in parameters {
			p.resolveTypes(resolve)
		}
	}
}
