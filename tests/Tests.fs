module Tests

open Fable.Mocha
open Lang.Parser

let unwrapParse x =
    match parse x with
    | Ok (ast, _, _) -> ast
    | Error x -> failwith $"{x}"

let parseTestCase title input expected expectMessage =
    testCase title
    <| fun _ ->
        let result = unwrapParse input

        Expect.equal expected result expectMessage

let precedenceEquivalence title input precedence expectMessage =
    testCase title
    <| fun _ ->
        let result = unwrapParse input
        let precedence = unwrapParse precedence

        Expect.equal precedence result expectMessage



let parserTests =
    testList
        "Parser tests"
        [ parseTestCase "unary operator" "=-2" (Unary(Number 2, Minus)) "Result must be a unary negation"
          parseTestCase
              "unary operator in binary op"
              "=2*-2"
              (Binary(Number 2, Multiply, (Unary(Number 2, Minus))))
              "Result must be a binary operation with negaiton"
          parseTestCase
              "complex operation with precedence and exponent"
              "=(2+4)*3^4"
              (Binary(Binary(Number 2, Plus, Number 4), Multiply, Binary(Number 3, Exponent, Number 4)))
              "Result must be nested ibnary operations with correct, mathematical precedence"

          precedenceEquivalence
              "basic precedence test"
              "= 1 + 2 * 3 / 4^5"
              "=1+((2*3)/(4^5))"
              "Result must have proper precedence" ]

let add x y = x + y

let appTests =
    testList
        "App tests"
        [ testCase "add works"
          <| fun _ ->
              let result = add 2 3
              Expect.equal result 5 "Result must be 5" ]

let allTests =
    testList "All" [ appTests; parserTests ]

[<EntryPoint>]
let main (args: string []) = Mocha.runTests allTests
