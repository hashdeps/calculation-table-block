module Lang.Parser

open Parsec

// ----------------------------------------------------------------------------
// AST
// ----------------------------------------------------------------------------

type Position = char * int

type Operator =
    | Plus
    | Minus
    | Multiply
    | Divide
    | Exponent


type Expr =
    | Reference of Position
    | Number of float
    | Unary of Expr * Operator
    | Binary of Expr * Operator * Expr
    | FunctionCall of func: string * jsonPath: string

let operatorMapping =
    function
    | Plus -> '+'
    | Minus -> '-'
    | Multiply -> '*'
    | Divide -> '/'
    | Exponent -> '^'

// ----------------------------------------------------------------------------
// Extra combinaotrs
// ----------------------------------------------------------------------------

let between l r p = l >>. p .>> r
let betweenSame x p = between x x p
let wsIgnore x = betweenSame spaces x


let operator: Parser<Operator, unit> =
    charReturn '+' Operator.Plus
    <|> charReturn '-' Operator.Minus
    <|> charReturn '*' Operator.Multiply
    <|> charReturn '/' Operator.Divide

let reference =
    upper .>>. pint32 |>> Reference

let functionIdentifier =
    many1Satisfy (fun c -> (c >= 'a' && c <= 'z') || c = '_')

let number = pfloat |>> Number


// ----------------------------------------------------------------------------
// Operator precedence parser
// ----------------------------------------------------------------------------
type Associativity =
    | Prefix
    | Postfix
    | BinaryLeft
    | BinaryRight

let parsePrefix operatorParser nextParser =
    let parser, parserSetter =
        createParserForwardedToRef ()

    parserSetter.Value <-
        (operatorParser .>>. parser
         |>> (fun (op, value) -> Unary(value, op)))
        <|> nextParser

    parser


let parsePostfix (operatorParser: Parser<Operator, 'a>) nextParser =
    nextParser .>>. (many operatorParser)
    |>> (fun (x, suffixes) -> List.fold (fun acc x -> Unary(acc, x)) x suffixes)

let parseBinaryLeft operatorParser nextParser =
    let parser, parserSetter =
        createParserForwardedToRef ()

    parserSetter.Value <-
        nextParser
        .>>. (many (operatorParser .>>. nextParser))
        |>> (fun (first, rest) -> List.fold (fun l (op, r) -> Binary(l, op, r)) first rest)

    parser

let parseBinaryRight operatorParser nextParser =
    let parser, parserSetter =
        createParserForwardedToRef ()

    parserSetter.Value <-
        nextParser
        >>= (fun p ->
            (operatorParser .>>. (preturn p) .>>. parser)
            |>> (fun ((op, l), r) -> Binary(l, op, r))
            <|> (preturn p))

    parser


let evaluateAssociation (assoc: Associativity) operatorParser nextParser =
    let operatorParser =
        List.map (fun op -> wsIgnore (charReturn (operatorMapping op) op)) operatorParser
        |> choice

    match assoc with
    | Prefix -> parsePrefix operatorParser nextParser
    | Postfix -> parsePostfix operatorParser nextParser
    | BinaryLeft -> parseBinaryLeft operatorParser nextParser
    | BinaryRight -> parseBinaryRight operatorParser nextParser


let tableParser termParser operators =
    List.fold (fun acc (assoc, ops) -> evaluateAssociation assoc ops acc) termParser operators


// ----------------------------------------------------------------------------
// Concrete parsers
// ----------------------------------------------------------------------------

let jsonPathArg =
    let escape =
        anyOf "\"\\/bfnrt"
        |>> function
            | 'b' -> "\b"
            | 'f' -> "\u000C"
            | 'n' -> "\n"
            | 'r' -> "\r"
            | 't' -> "\t"
            | c -> string c

    let unicodeEscape =
        /// converts a hex char ([0-9a-fA-F]) to its integer number (0-15)
        let hex2int c = (int c &&& 15) + (int c >>> 6) * 9

        pchar 'u'
        >>. pipe4 hex hex hex hex (fun h3 h2 h1 h0 ->
            (hex2int h3) * 4096
            + (hex2int h2) * 256
            + (hex2int h1) * 16
            + hex2int h0
            |> char
            |> string)

    let escaped =
        pchar '\\' >>. (escape <|> unicodeEscape)

    let anyChar =
        manySatisfy (fun c -> c <> '(' && c <> ')' && c <> '\\')

    between (pchar '(') (pchar ')') (stringsSepBy anyChar escaped)


// Precedence table of operators
let operators =
    [ (Prefix, [ Minus ])
      // (Postfix,     [Factorial])
      (BinaryRight, [ Exponent ])
      (BinaryLeft, [ Multiply; Divide ])
      (BinaryLeft, [ Plus; Minus ]) ]

let term, termSetter =
    createParserForwardedToRef ()

let paren =
    wsIgnore <| pchar '(' >>. wsIgnore term
    .>> pchar ')'

let functionCall =
    functionIdentifier .>>. jsonPathArg
    |>> FunctionCall

let termAux =
    functionCall <|> number <|> reference <|> paren

let ops = tableParser termAux operators

termSetter.Value <- functionCall

let formula = wsIgnore (pchar '=') >>. term
let equation = wsIgnore (formula <|> number)

let parse = runString equation ()
