namespace Swift.Ast

type Literal =
    | StringLiteral of string
    | BooleanLiteral of bool
    | NilLiteral

type PlatformName =
    | IOS
    | IOSApplicationExtension
    | MacOS
    | MacOSApplicationExtension
    | WatchOS
    | TvOS

type PlatformVersion = System.Version

type Availability =
    | AvailableOn of PlatformName * PlatformVersion
    | UnavailableOn of PlatformName
    | Star

type Attr =
    | AvailabilityAttr of Availability list
    | OtherAttr of Name:string * Args:string list

type ThrowSpec = Throws | Rethrows

type MetatypeSpec = Type | Protocol

// https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_type-identifier
type TypeIdentifier = TypeIdentifier of Name:string * GenericArgs:Type list * Nested:TypeIdentifier option

and AttributedType = AttributedType of Attributes:Attr list * Inout:bool * Type:Type

and FuncTypeArgument = FuncTypeArgument of Label:string option * ArgType:AttributedType

and TupleTypeElement = TupleTypeElement of ElementName:string option * Type:AttributedType

// https://docs.swift.org/swift-book/ReferenceManual/Types.html
and Type =
    | FunctionType of Attributes:Attr list * Args:FuncTypeArgument list * ThrowSpec:ThrowSpec option * RetType:AttributedType
    | ArrayType of Type
    //type → dictionary-type
    | IdentifiedType of TypeIdentifier
    | TupleType of TupleTypeElement list
    | OptionalType of Type
    | ImplicitlyUnwrappedOptionalType of Type
    | ProtocolCompositionType of TypeIdentifier list
    | OpaqueType of Type
    | Metatype of Type * Type:MetatypeSpec
    | SelfType
    | AnyType

type GenericParameter = GenericParameter of Name:string * Constraints:TypeIdentifier list // (may be empty)

type GenericRequirement =
    | ConformanceRequirement of TypeIdentifier * TypeIdentifier list
    | SameTypeRequirement of TypeIdentifier * Type

type GenericParameterClause =
    | GenericParameterClause of Parameters:GenericParameter list * Requirements:GenericRequirement list

type AccessLevelModifier =
    | Private of SetterOnly:bool
    | FilePrivate of SetterOnly:bool
    | Internal of SetterOnly:bool
    | Public of SetterOnly:bool
    | Open of SetterOnly:bool

type MutationModifier =
    | Mutating
    | Nonmutating

type SafeOrUnsafe =
    | Safe
    | Unsafe

type Optionality =
    | NotOptional
    | Optional
    | ImplicityUnwrappedOptional

type LiteralExpr =
    | Literal of Literal
    // Special literals:
    | FileLiteral
    | LineLiteral
    | ColumnLiteral
    | FunctionLiteral
    //| #dsohandle // FIXME?

type SwiftExpr =
    | LiteralExpr of LiteralExpr

type DeclModifier =
    | Class
    | Convenience
    | Dynamic
    | Final
    | Infix
    | Lazy
    | Optional
    | Override
    | Postfix
    | Prefix
    | Required
    | Static
    | Unowned of SafeOrUnsafe option
    | Weak
    | AccessLevelModifier of AccessLevelModifier
    | MutationModifier of MutationModifier

type Parameter =
    | Parameter of ExternalName:string option * LocalName:string * Type:AttributedType * DefaultValue:SwiftExpr option

//https://docs.swift.org/swift-book/ReferenceManual/Declarations.html
type SwiftDecl =
    | ImportDecl of Attributes:Attr list * Path:string list // FIXME
    //declaration → constant-declaration
    //declaration → variable-declaration
    | TypealiasDecl of Attributes:Attr list * Access:AccessLevelModifier option * Name:string * Generic:GenericParameterClause option * Type
    | FuncDecl of Attributes:Attr list * Modifiers:DeclModifier list * Name:string * Generic:GenericParameterClause option * Parameters:Parameter list * ThrowSpec:ThrowSpec option * ReturnType:AttributedType option * WhereClause:GenericRequirement list
    //declaration → enum-declaration
    | StructDecl of Attributes:Attr list * Access:AccessLevelModifier option * Name:string * Generic:GenericParameterClause option * Protocols:TypeIdentifier list * WhereClause:GenericRequirement list * Body:SwiftDecl list
    //declaration → class-declaration
    //declaration → protocol-declaration
    | InitDecl of Attributes:Attr list * Modifiers:DeclModifier list * Failability:Optionality * Generic:GenericParameterClause option * Parameters:Parameter list * ThrowSpec:ThrowSpec option * WhereClause:GenericRequirement list
    //declaration → deinitializer-declaration
    //declaration → extension-declaration
    //declaration → subscript-declaration
    //declaration → operator-declaration
    //declaration → precedence-group-declaration
    //declarations → declaration declarations opt