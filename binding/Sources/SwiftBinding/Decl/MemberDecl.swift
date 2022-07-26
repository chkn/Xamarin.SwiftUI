import SwiftSyntax

open class MemberDecl: Decl, HasTypesToResolve {
    public var genericParameters: [GenericParameter]

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		genericParameters = genericParameterClause?.genericParameterList.map { GenericParameter($0, genericWhereClause) } ?? []
		super.init(in: context, attributes, modifiers, name)
	}

	open func resolveTypes(_ resolve: (TypeRef) -> TypeRef) {
		for i in 0..<genericParameters.count {
			var gp = genericParameters[i]
			gp.resolveTypes(resolve)
			genericParameters[i] = gp
		}
	}
}

open class FunctionDecl: MemberDecl {
	public var parameters: [Parameter]

	public init(in context: Decl?, _ attributes: AttributeListSyntax?, _ modifiers: ModifierListSyntax?, _ name: String, _ genericParameterClause: GenericParameterClauseSyntax?, _ parameterClause: ParameterClauseSyntax, _ genericWhereClause: GenericWhereClauseSyntax?)
	{
		// init generic parameters first
		parameters = []
		super.init(in: context, attributes, modifiers, name, genericParameterClause, genericWhereClause)
		parameters = parameterClause.parameterList.map { Parameter(self, $0) }
	}

	open override func resolveTypes(_ resolve: (TypeRef) -> TypeRef) {
		super.resolveTypes(resolve)
		for i in 0..<parameters.count {
			var p = parameters[i]
			p.resolveTypes(resolve)
			parameters[i] = p
		}
	}
}
