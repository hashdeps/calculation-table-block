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
    | Number of int
    | Unary of Expr * Operator
    | Binary of Expr * Operator * Expr

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
    letter .>>. pint32 |>> Reference

let number = pint32 |>> Number


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

let termAux = number <|> reference <|> paren
let ops = tableParser termAux operators

termSetter.Value <- ops

let formula = wsIgnore (pchar '=') >>. term
let equation = wsIgnore (formula <|> number)

let parse = runString equation ()
