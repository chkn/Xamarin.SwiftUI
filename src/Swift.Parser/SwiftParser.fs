(*** hide ***)
module Swift.Parser

open System

open Swift.Ast

(**
# Swift Parser

This is *not* a complete Swift parser. We are only concerned with parsing `swiftinterface`
files. It is a superset of Swift source, and only requires us to parse Swift declarations
without definitions.

A summary of the Swift grammar can be found here:
https://docs.swift.org/swift-book/ReferenceManual/zzSummaryOfTheGrammar.html

## FParsec

We are using the [FParsec](https://www.quanttec.com/fparsec/) parser combinator library.
*)

open FParsec

type UserState = unit
type Parser<'t> = Parser<'t,UserState>

(**
## Lexical Structure

Reference: https://docs.swift.org/swift-book/ReferenceManual/LexicalStructure.html

### Comments

As you'll note in the above reference, mult-line comments may be nested, as long as
the start and end markers are balanced. This recursive parser handles that:
*)
let multilineComment, multilineCommentRef = createParserForwardedToRef()
do multilineCommentRef :=
    let ign x = skipCharsTillString x false Int32.MaxValue
    between
        (pstring "/*")
        (pstring "*/")
        (attempt (ign "/*" >>. multilineComment >>. ign "*/") <|> ign "*/")

(**
### Whitespace

From the reference, the following characters are considered whitespace:
space (U+0020), line feed (U+000A), carriage return (U+000D), horizontal tab (U+0009),
vertical tab (U+000B), form feed (U+000C) and null (U+0000).
*)
let isWhitespaceChar = function
| '\u0020' | '\u000A' | '\u000D' | '\u0009' | '\u000B' | '\u000C' | '\u0000' -> true
| _ -> false

(**
This parser skips whitespace and comments:
*)
let unit_ws : Parser<_> =
    skipSatisfyL isWhitespaceChar "whitespace" <|>
    (pstring "//" >>. skipRestOfLine true) <|>
    multilineComment

/// Skips zero or more whitespace characters and/or comments
let ws = skipMany unit_ws

/// Skips one or more whitespace characters and/or comments
let ws1 = skipMany1 unit_ws

(**
Now that we have those definitions, define some helper functions for common parsing
scenarios, ignoring whitespace and comments:
*)
let wrappedIn opening closing p = between (skipChar opening) (skipChar closing) (between ws ws p)

let squares p = wrappedIn '[' ']' p
let squiggles p = wrappedIn '{' '}' p
let angles p = wrappedIn '<' '>' p
let parens p = wrappedIn '(' ')' p

/// Parses parenthesis containing a list of zero or more items
let parens_list (plist : Parser<_>) =
    pchar '(' >>. ws >>. (
        (charReturn ')' []) <|>
        (plist .>> ws .>> pchar ')')
    )

let sepBy1CharWS p chr = sepBy1 (p .>> ws) (skipChar chr .>> ws)

(**
### Identifiers
*)
//identifier-head → Upper- or lowercase letter A through Z
//identifier-head → _
// FIXME: Handle the rest of these:
//identifier-head → U+00A8, U+00AA, U+00AD, U+00AF, U+00B2–U+00B5, or U+00B7–U+00BA
//identifier-head → U+00BC–U+00BE, U+00C0–U+00D6, U+00D8–U+00F6, or U+00F8–U+00FF
//identifier-head → U+0100–U+02FF, U+0370–U+167F, U+1681–U+180D, or U+180F–U+1DBF
//identifier-head → U+1E00–U+1FFF
//identifier-head → U+200B–U+200D, U+202A–U+202E, U+203F–U+2040, U+2054, or U+2060–U+206F
//identifier-head → U+2070–U+20CF, U+2100–U+218F, U+2460–U+24FF, or U+2776–U+2793
//identifier-head → U+2C00–U+2DFF or U+2E80–U+2FFF
//identifier-head → U+3004–U+3007, U+3021–U+302F, U+3031–U+303F, or U+3040–U+D7FF
//identifier-head → U+F900–U+FD3D, U+FD40–U+FDCF, U+FDF0–U+FE1F, or U+FE30–U+FE44
//identifier-head → U+FE47–U+FFFD
//identifier-head → U+10000–U+1FFFD, U+20000–U+2FFFD, U+30000–U+3FFFD, or U+40000–U+4FFFD
//identifier-head → U+50000–U+5FFFD, U+60000–U+6FFFD, U+70000–U+7FFFD, or U+80000–U+8FFFD
//identifier-head → U+90000–U+9FFFD, U+A0000–U+AFFFD, U+B0000–U+BFFFD, or U+C0000–U+CFFFD
//identifier-head → U+D0000–U+DFFFD or U+E0000–U+EFFFD
let identifier_head : Parser<_> = regexL "[A-Za-z_]" "identifier"

//identifier-character → Digit 0 through 9
//FIXME: identifier-character → U+0300–U+036F, U+1DC0–U+1DFF, U+20D0–U+20FF, or U+FE20–U+FE2F
//identifier-character → identifier-head
//identifier-characters → identifier-character identifier-characters opt
let identifier_characters : Parser<_> = regexL "[A-Za-z_0-9]*" "identifier"

//identifier → identifier-head identifier-characters opt
// FIXME: The above should exclude keywords, and handle the following that includes them:
//identifier → ` identifier-head identifier-characters opt `
//identifier → implicit-parameter-name
let identifier : Parser<_> =
    pipe2 identifier_head identifier_characters (+)

(**
### Literals

Reference: https://docs.swift.org/swift-book/ReferenceManual/LexicalStructure.html#ID414

For now, we are ignoring the spec and implementing a very simplistic parser, which
so far handles the string literals we're seeing in SIL files:
*)
// FIXME: Actually implement according to spec one day?
let string_literal : Parser<_> =
    between (pchar '"') (pchar '"') (manySatisfy ((<>) '"'))

//nil-literal → nil
let nil_literal = stringReturn "nil" NilLiteral

//boolean-literal → true | false
let boolean_literal =
    (stringReturn "true" true) <|>
    (stringReturn "false" false)

//FIXME
let integer_literal = pint64

// FIXME
//numeric-literal → -opt integer-literal | -opt floating-point-literal
let numeric_literal = integer_literal

//literal → numeric-literal | string-literal | boolean-literal | nil-literal
let literal =
    //FIXME: numeric-literal
    (string_literal |>> StringLiteral) <|>
    (boolean_literal |>> BooleanLiteral) <|>
    nil_literal <?>
    "literal"

(**
## Attributes

Reference: https://docs.swift.org/swift-book/ReferenceManual/Attributes.html
*)

//platform-name → iOS | iOSApplicationExtension
//platform-name → macOS | macOSApplicationExtension // swiftinterface also include "OSX"
//platform-name → watchOS
//platform-name → tvOS
let platform_name =
    (stringReturn "iOSApplicationExtension" IOSApplicationExtension) <|>
    (stringReturn "iOS" IOS) <|>
    (stringReturn "macOSApplicationExtension" MacOSApplicationExtension) <|>
    (stringReturn "macOS" MacOS) <|>
    (stringReturn "OSX" MacOS) <|>
    (stringReturn "watchOS" WatchOS) <|>
    (stringReturn "tvOS" TvOS)

//platform-version → decimal-digits
//platform-version → decimal-digits . decimal-digits
//platform-version → decimal-digits . decimal-digits . decimal-digits
let platform_version =
    tuple3 pint32 ((pchar '.' >>. pint32) <|>% 0) ((pchar '.' >>. pint32) <|>% 0) |>> PlatformVersion

//availability-argument → platform-name platform-version
//availability-argument → *
let availability_argument =
    (charReturn '*' Star) <|>
    (attempt (platform_name .>> ws .>> pchar ',') .>> ws .>> pstring "unavailable" |>> UnavailableOn) <|>
    (platform_name .>> ws .>>. platform_version |>> AvailableOn)

//availability-arguments → availability-argument | availability-argument , availability-arguments
let availability_arguments = sepBy1CharWS availability_argument ','

(**
Note that for some parsers that produce lists and that are only referenced as
optional in other places, we are simply defining the optional version to return
an empty list. This is more efficient than defining a parser that requires at least
one element and then parsing that optionally. For example:

*)
//balanced-tokens → balanced-token balanced-tokens opt
let rec balanced_tokens_opt : Parser<_> = many (balanced_token .>> ws)

//balanced-token → ( balanced-tokens opt )
//balanced-token → [ balanced-tokens opt ]
//balanced-token → { balanced-tokens opt }
//balanced-token → Any identifier, keyword, literal, or operator
//balanced-token → Any punctuation except (, ), [, ], {, or }
and balanced_token : Parser<_> =
    //FIXME: Enable these? Haven't seen them yet in SIL files..
    //parens balanced_tokens_opt <|>
    //squares balanced_tokens_opt <|>
    //squiggles balanced_tokens_opt <|>
    identifier <|> //FIXME: keyword, literal, or operator
    regex @"[^A-Za-z0-9\(\)\[\]\{\}\s]+"

//attribute-name → identifier
let attribute_name = identifier

//attribute-argument-clause → ( balanced-tokens opt )
let attribute_argument_clause = parens balanced_tokens_opt

//attribute → @ attribute-name attribute-argument-clause opt
let attribute =
    skipChar '@' >>. ws >>. (
        (skipString "available" >>. parens availability_arguments |>> AvailabilityAttr) <|>
        (attribute_name .>> ws .>>. (attribute_argument_clause <|>% []) |>> OtherAttr)
    )

//attributes → attribute attributes opt
let attributes_opt = sepEndBy attribute ws

(**
## Types

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html
*)
//type-name → identifier
let type_name = identifier

let throw_spec =
    (stringReturn "throws" Throws) <|>
    (stringReturn "rethrows" Rethrows)


let ``type``, type_ref = createParserForwardedToRef()

//attributes opt inoutopt type
let attr_type =
    tuple3 attributes_opt ((pstring "inout" .>> ws >>% true) <|>% false) ``type`` |>> AttributedType

//type-annotation → : attributes opt inoutopt type
let type_annotation = skipChar ':' >>. ws >>. attr_type

(**
### Function Types

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_function-type
*)
//argument-label → identifier
let argument_label = identifier

//function-type-argument → attributes opt inoutopt type | argument-label type-annotation
let function_type_argument =
    (attr_type |>> (fun ty -> FuncTypeArgument(None, ty))) <|>
    ((argument_label |>> Some) .>> ws .>>. type_annotation |>> FuncTypeArgument)

//function-type-argument-list → function-type-argument | function-type-argument , function-type-argument-list
let function_type_argument_list = sepBy1CharWS function_type_argument ','

//function-type-argument-clause → ( )
//function-type-argument-clause → ( function-type-argument-list ...opt )
let function_type_argument_clause = parens_list function_type_argument_list

//function-type → attributes opt function-type-argument-clause throwsopt -> type
//function-type → attributes opt function-type-argument-clause rethrows -> type
let function_type =
    tuple4 attributes_opt (function_type_argument_clause .>> ws) (opt (throw_spec .>> ws)) (pstring "->" >>. ws >>. attr_type) |>> FunctionType

(**
### Generic Arguments

Reference: https://docs.swift.org/swift-book/ReferenceManual/GenericParametersAndArguments.html
*)
//generic-argument → type
let generic_argument = ``type``

//generic-argument-list → generic-argument | generic-argument , generic-argument-list
let generic_argument_list = sepBy1CharWS generic_argument ','

//generic-argument-clause → < generic-argument-list >
let generic_argument_clause = angles generic_argument_list

(**
### Array Types

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_array-type
*)
//array-type → [ type ]
let array_type = squares ``type`` |>> ArrayType

(**
### Type Identifier

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_type-identifier
*)
//type-identifier → type-name generic-argument-clause opt | type-name generic-argument-clause opt . type-identifier
let type_identifier, type_identifier_ref = createParserForwardedToRef()
do type_identifier_ref :=
    tuple3 type_name (generic_argument_clause <|>% []) (opt (pchar '.' >>. type_identifier)) |>> TypeIdentifier

(**
### Tuple Types
*)
//element-name → identifier
let element_name = identifier

//tuple-type-element → element-name type-annotation | type
let tuple_type_element =
    (attempt ((element_name |>> Some) .>> ws .>>. type_annotation) |>> TupleTypeElement) <|>
    (attr_type |>> fun aty -> TupleTypeElement(None, aty))

//tuple-type-element-list → tuple-type-element | tuple-type-element , tuple-type-element-list
let tuple_type_element_list = sepBy1CharWS tuple_type_element ','

//tuple-type → ( ) | ( tuple-type-element , tuple-type-element-list )
let tuple_type = parens_list tuple_type_element_list |>> TupleType

(**
### Protocol Composition Types

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_protocol-composition-type
*)
//protocol-composition-type → type-identifier & protocol-composition-continuation
//protocol-composition-continuation → type-identifier | protocol-composition-type
let protocol_composition_type = sepBy1CharWS type_identifier '&'

(**
### Opaque Types

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_opaque-type
*)
//opaque-type → some type
let opaque_type = skipString "some" >>. ws >>. ``type`` |>> OpaqueType

(**
### Metatypes

Reference: https://docs.swift.org/swift-book/ReferenceManual/Types.html#grammar_metatype-type
*)
//metatype-type → type . Type | type . Protocol
let metatype_type =
    ``type`` .>> ws .>> skipChar '.' .>> ws .>> (pstring "Type" <|> pstring "Protocol") |>> Metatype

(**
### Type Inheritance Clause
*)
//type-inheritance-list → type-identifier | type-identifier , type-inheritance-list
let type_inheritance_list = sepBy1CharWS type_identifier ',' 

//type-inheritance-clause → : type-inheritance-list
let type_inheritance_clause = skipChar ':' >>. ws >>. type_inheritance_list

(**
### Type Parser
*)
//type → function-type
//type → array-type
//type → dictionary-type
//type → type-identifier
//type → tuple-type
//type → optional-type
//type → implicitly-unwrapped-optional-type
//type → protocol-composition-type
//type → opaque-type
//type → metatype-type
//type → self-type
//type → Any
//type → ( type )
do type_ref :=
    attempt function_type <|> // backtrack to tuple_type
    array_type <|>
    //FIXME: dictionary-type
    (type_identifier |>> IdentifiedType) <|>
    tuple_type <|>
    // FIXME: optional-type
    // FIXME: implicitly-unwrapped-optional-type
    (protocol_composition_type |>> ProtocolCompositionType) <|>
    opaque_type <|>
    metatype_type <|>
    (stringReturn "Self" SelfType) <|>
    (stringReturn "Any"  AnyType) <|>
    (parens ``type``)

(**
### Generic Parameters

Reference: https://docs.swift.org/swift-book/ReferenceManual/GenericParametersAndArguments.html

Sometimes we can simplify the grammar a bit to avoid backtracking. In this case,
`protocol-composition-type` starts with `type-identifier`, so we don't need 2
separate cases here:
*)
//generic-parameter → type-name
//generic-parameter → type-name : type-identifier
//generic-parameter → type-name : protocol-composition-type
let generic_parameter =
    type_name .>> ws .>>. ((pchar ':' .>> ws >>. protocol_composition_type) <|>% []) |>> GenericParameter

//generic-parameter-list → generic-parameter | generic-parameter , generic-parameter-list
let generic_parameter_list = sepBy1CharWS generic_parameter ','

(**
Here's another case where we've refactored the grammar a little to avoid backtracking.
Once again, we take advantage of the fact that `protocol-composition-type` starts with
`type-identifier`.

We have also noted that both `conformance-requirement` and `same-type-requirement`
start with `type-identifier`, and so have left-factored that into `requirement`.
*)
//conformance-requirement → type-identifier : type-identifier
//conformance-requirement → type-identifier : protocol-composition-type
let conformance_requirement_tail =
    let p = skipChar ':' >>. ws >>. protocol_composition_type
    (fun ident -> p |>> fun pct -> ConformanceRequirement(ident, pct))

//same-type-requirement → type-identifier == type
let same_type_requirement_tail =
    let p = pstring "==" >>. ws >>. ``type``
    (fun ident -> p |>> fun ty -> SameTypeRequirement(ident, ty))

//requirement → conformance-requirement | same-type-requirement
let requirement =
    type_identifier .>> ws >>= (fun ident ->
        conformance_requirement_tail ident <|>
        same_type_requirement_tail ident
    )

//requirement-list → requirement | requirement , requirement-list
let requirement_list = sepBy1CharWS requirement ','

//generic-where-clause → where requirement-list
let generic_where_clause = skipString "where" >>. ws >>. requirement_list

(**
In some cases, the published grammar appears to be slightly incorrect, at least
with regard to the usage in SIL files. Here is one particular case, where the
grammar was augmented by reviewing the Swift
[source code](https://github.com/apple/swift/blob/0dc0b035218ca4412cbebd2a4d61a019b6b71ea0/lib/Parse/ParseGeneric.cpp#L27):
*)
//generic-params:
//     '<' generic-param (',' generic-param)* where-clause? '>'
//generic-parameter-clause → < generic-parameter-list >
let generic_parameter_clause =
    angles (generic_parameter_list .>>. (generic_where_clause <|>% [])) |>> GenericParameterClause

(**
## Expressions

Reference: https://docs.swift.org/swift-book/ReferenceManual/Expressions.html

For now, we are only implementing expressions relevant to `default-argument-clause`
*)

//literal-expression → literal
//FIXME: literal-expression → array-literal | dictionary-literal | playground-literal
//literal-expression → #file | #line | #column | #function | #dsohandle
let literal_expression =
    (stringReturn "#file" FileLiteral) <|>
    (stringReturn "#line" LineLiteral) <|>
    (stringReturn "#column" ColumnLiteral) <|>
    (stringReturn "#function" FunctionLiteral) <|>
    (literal |>> Literal) <?>
    "literal expression"

// FIXME: Implement all of these?
//primary-expression → identifier generic-argument-clause opt
//primary-expression → literal-expression
//primary-expression → self-expression
//primary-expression → superclass-expression
//primary-expression → closure-expression
//primary-expression → parenthesized-expression
//primary-expression → tuple-expression
//primary-expression → implicit-member-expression
//primary-expression → wildcard-expression
//primary-expression → key-path-expression
//primary-expression → selector-expression
//primary-expression → key-path-string-expression
let primary_expression =
    literal_expression |>> LiteralExpr

(**
We are skipping a lot of grammar here; this is nowhere near the actual definition:
*)
let expression = primary_expression

(**
## Declarations

Reference: https://docs.swift.org/swift-book/ReferenceManual/Declarations.html
*)
let declaration, declaration_ref = createParserForwardedToRef()

(**
### Modifiers

This is a helper function for parsing a single access level modifier:
*)
let access_level str case =
    skipString str >>. ws >>. ((parens (skipString "set") >>% true) <|>% false) |>> case

//access-level-modifier → private | private ( set )
//access-level-modifier → fileprivate | fileprivate ( set )
//access-level-modifier → internal | internal ( set )
//access-level-modifier → public | public ( set )
//access-level-modifier → open | open ( set )
let access_level_modifier =
    (access_level "private" Private) <|>
    (access_level "fileprivate" FilePrivate) <|>
    (access_level "internal" Internal) <|>
    (access_level "public" Public) <|>
    (access_level "open" Open)

//mutation-modifier → mutating | nonmutating
let mutation_modifier =
    (stringReturn "mutating" Mutating) <|>
    (stringReturn "nonmutating" Nonmutating)

let safe_or_unsafe =
    (stringReturn "safe" Safe) <|>
    (stringReturn "unsafe" Unsafe)

//declaration-modifier → class | convenience | dynamic | final | infix | lazy | optional | override | postfix | prefix | required | static | unowned | unowned ( safe ) | unowned ( unsafe ) | weak
//declaration-modifier → access-level-modifier
//declaration-modifier → mutation-modifier
let declaration_modifier =
    (stringReturn "class" Class) <|>
    (stringReturn "convenience" Convenience) <|>
    (stringReturn "dynamic" Dynamic) <|>
    (stringReturn "final" Final) <|>
    (stringReturn "infix" Infix) <|>
    (stringReturn "lazy" Lazy) <|>
    (stringReturn "optional" Optional) <|>
    (stringReturn "override" Override) <|>
    (stringReturn "postfix" Postfix) <|>
    (stringReturn "prefix" Prefix) <|>
    (stringReturn "required" Required) <|>
    (stringReturn "static" Static) <|>
    (pstring "unowned" >>. ws >>. (opt (parens safe_or_unsafe)) |>> Unowned) <|>
    (stringReturn "weak" Weak) <|>
    (access_level_modifier |>> AccessLevelModifier) <|>
    (mutation_modifier |>> MutationModifier)

//declaration-modifiers → declaration-modifier declaration-modifiers opt
let declaration_modifiers_opt = sepEndBy declaration_modifier ws

(**
### Import Declarations

Reference: https://docs.swift.org/swift-book/ReferenceManual/Declarations.html#grammar_import-path
*)
//import-kind → typealias | struct | class | enum | protocol | let | var | func

//import-path-identifier → identifier | operator
let import_path_identifier = identifier // FIXME

//import-path → import-path-identifier | import-path-identifier . import-path
let import_path = sepBy1CharWS import_path_identifier '.'

//FIXME
//import-declaration → attributes opt import import-kind opt import-path
let import_declaration =
    attempt (attributes_opt .>> skipString "import") .>> ws .>>. import_path |>> ImportDecl

(**
### Function Declarations

Reference: https://docs.swift.org/swift-book/ReferenceManual/Declarations.html#grammar_function-declaration
*)
//external-parameter-name → identifier
let external_parameter_name = attempt (identifier .>> ws1 .>> notFollowedBy (pchar ':'))

//local-parameter-name → identifier
let local_parameter_name = identifier

//default-argument-clause → = expression
let default_argument_clause = skipChar '=' >>. ws >>. expression

//parameter → external-parameter-name opt local-parameter-name type-annotation default-argument-clause opt
let parameter =
    tuple4 (opt external_parameter_name) (local_parameter_name .>> ws) (type_annotation .>> ws) (opt default_argument_clause) |>> Parameter

//parameter-list → parameter | parameter , parameter-list
let parameter_list = sepBy1CharWS parameter ',' <?> "parameter list"

//parameter-clause → ( ) | ( parameter-list )
let parameter_clause = parens_list parameter_list

//function-head → attributes opt declaration-modifiers opt func
let function_head = attempt (attributes_opt .>>. declaration_modifiers_opt .>> skipString "func")

//function-name → identifier | operator
let function_name = identifier //FIXME

//function-result → -> attributes opt type
let function_result = skipString "->" >>. ws >>. attr_type

//function-signature → parameter-clause throwsopt function-result opt
//function-signature → parameter-clause rethrows function-result opt
let function_signature = tuple3 (parameter_clause .>> ws) (opt (throw_spec .>> ws)) (opt function_result)

//function-declaration → function-head function-name generic-parameter-clause opt function-signature generic-where-clause opt function-body opt
let function_declaration =
    pipe5 (function_head .>> ws) (function_name .>> ws) (opt (generic_parameter_clause .>> ws)) (function_signature .>> ws) (generic_where_clause <|>% [])
    <| fun (a, b) c d (e, f, g) h -> FuncDecl(a, b, c, d, e, f, g, h)

(**
### Struct Declarations

Reference: https://docs.swift.org/swift-book/ReferenceManual/Declarations.html#grammar_struct-declaration
*)

//struct-name → identifier
let struct_name = identifier

//struct-members → struct-member struct-members opt
//struct-member → declaration | compiler-control-statement
let struct_member = //FIXME
    declaration

//struct-body → { struct-members opt }
let struct_body = squiggles (sepEndBy struct_member ws)

//struct-declaration → attributes opt access-level-modifier opt struct struct-name generic-parameter-clause opt type-inheritance-clause opt generic-where-clause opt struct-body
let struct_declaration =
    pipe2
    <| attempt (attributes_opt .>>. (opt (access_level_modifier .>> ws) .>> skipString "struct" .>> ws))
    <| tuple5 (struct_name .>> ws) (opt (generic_parameter_clause .>> ws)) ((type_inheritance_clause <|>% []) .>> ws) ((generic_where_clause <|>% []) .>> ws) struct_body
    <| fun (a, b) (c, d, e, f, g) -> StructDecl(a, b, c, d, e, f, g)

(**
### Initializer Declarations

Reference: https://docs.swift.org/swift-book/ReferenceManual/Declarations.html#grammar_initializer-declaration
*)
let optionality =
    (charReturn '?' Optionality.Optional) <|>
    (charReturn '!' ImplicityUnwrappedOptional) <|>
    preturn NotOptional

//initializer-head → attributes opt declaration-modifiers opt init
//initializer-head → attributes opt declaration-modifiers opt init ?
//initializer-head → attributes opt declaration-modifiers opt init !
let initializer_head =
    attempt (tuple3 attributes_opt (declaration_modifiers_opt .>> skipString "init") optionality)

//initializer-body → code-block

//initializer-declaration → initializer-head generic-parameter-clause opt parameter-clause throwsopt generic-where-clause opt initializer-body
//initializer-declaration → initializer-head generic-parameter-clause opt parameter-clause rethrows generic-where-clause opt initializer-body
let initializer_declaration =
    pipe5 (initializer_head .>> ws) (opt (generic_parameter_clause .>> ws)) (parameter_clause .>> ws) (opt throw_spec .>> ws) (generic_where_clause <|>% [])
    <| fun (a, b, c) d e f g -> InitDecl(a, b, c, d, e, f, g)

//declaration → import-declaration
//declaration → constant-declaration
//declaration → variable-declaration
//declaration → typealias-declaration
//declaration → function-declaration
//declaration → enum-declaration
//declaration → struct-declaration
//declaration → class-declaration
//declaration → protocol-declaration
//declaration → initializer-declaration
//declaration → deinitializer-declaration
//declaration → extension-declaration
//declaration → subscript-declaration
//declaration → operator-declaration
//declaration → precedence-group-declaration
do declaration_ref :=
    // FIXME: need backtracking for all of these because they all start with optional attributes
    import_declaration <|>
    function_declaration <|>
    struct_declaration <|>
    initializer_declaration <?>
    "declaration"

//declarations → declaration declarations opt
let declarations = sepEndBy1 declaration ws

(**
## Statements

Reference: https://docs.swift.org/swift-book/ReferenceManual/Statements.html

Note once again that we are only trying to parse the subset of Swift found in `swiftinterface` files.
Thus we only implement some of the grammar for statements.
*)

//statement → expression ;opt
//statement → declaration ;opt
//statement → loop-statement ;opt
//statement → branch-statement ;opt
//statement → labeled-statement ;opt
//statement → control-transfer-statement ;opt
//statement → defer-statement ;opt
//statement → do-statement ;opt
//statement → compiler-control-statement
//statements → statement statements opt
let statement =
    declaration .>> optional (skipChar ';')
    // FIXME: add the rest..?

//top-level-declaration → statements opt
let top_level_declaration = sepEndBy statement ws

let file = ws >>. top_level_declaration .>> eof
