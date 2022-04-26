module Table.Parser
open Parsec

// ----------------------------------------------------------------------------
// AST
// ----------------------------------------------------------------------------

type Position = char * int


type Operator = Add = '+' | Subtract = '-' | Multiply = '*' | Divide = '/' | Exponent = '^' | Factorial = '!' | Negate = '-'


type Expr =
  | Reference of Position
  | Number of int
  | Unary of Expr * Operator
  | Binary of Expr * Operator * Expr

// ----------------------------------------------------------------------------
// PARSER
// ----------------------------------------------------------------------------

let between l r p = (ignore l) <*>> p <<*> (ignore r)
let betweenSame x p = between x x p
let wsIgnore x = betweenSame (ignore spaces) x

type Associativity = Prefix
                   | Postfix
                   | BinaryLeft
                   | BinaryRight

let parsePrefix operatorParser nextParser = 
  // Lazy parser that allows recursion
  let parserSetter, parser = slot ()
  let next = (operatorParser <*> parser |>> (fun (op, value) -> Unary (value, op))) <|> nextParser 

  parserSetter.Set next 

  parser


let parsePostfix operatorParser nextParser = 
  
let parseBinaryLeft operatorParser nextParser = 0
let parseBinaryRight operatorParser nextParser = 0

let evaluateAssociation (assoc: Associativity)  operatorParser = 
  let operatorParser = List.map (fun p -> wsIgnore p) operatorParser |> anyOf

  match assoc with
  | Prefix -> parsePrefix operatorParser
  | Postfix -> parsePostfix operatorParser
  | BinaryLeft -> parseBinaryLeft operatorParser
  | BinaryRight -> parseBinaryRight operatorParser

// Precedence table of operators
let operators = [
  (Prefix,      [Operator.Negate])
  (Postfix,     [Operator.Factorial])
  (BinaryRight, [Operator.Exponent])
  (BinaryLeft,  [Operator.Multiply ; Operator.Divide])
  (BinaryLeft,  [Operator.Add      ; Operator.Subtract])
]


// Basics: operators (+, -, *, /), cell reference (e.g. A10), number (e.g. 123)

let operator = char '+' <|> char '-' <|> char '*' <|> char '/'
let reference = letter <*> integer |> map Reference
let number = integer |> map Number

// Nested operator uses need to be parethesized, for example (1 + (3 * 4)).
// <expr> is a binary operator without parentheses, number, reference or
// nested brackets, while <term> is always bracketed or primitive. We need
// to use `expr` recursively, which is handled via mutable slots.
let exprSetter, expr = slot ()
let brack = char '(' <*>> anySpace <*>> expr <<*> anySpace <<*> char ')'
let term = number <|> reference <|> brack
let binary = term <<*> anySpace <*> operator <<*> anySpace <*> term |> map (fun ((l,op), r) -> Binary(l, op, r))
let exprAux = binary <|> term
exprSetter.Set exprAux

// Formula starts with `=` followed by expression
// Equation you can write in a cell is either number or a formula
let formula = char '=' <*>> anySpace <*>> expr
let equation = anySpace <*>> (formula <|> number) <<*> anySpace

// Run the parser on a given input
let parse input = run equation input

