#load "Parsec.fs"
#load "Parser.fs"
#load "Evaluator.fs"

open Lang.Parser
open Lang.Evaluator

let ast =
    match parse "=(2+4)*3^4" with
    | Ok (ast, _, _) -> ast
    | Error x -> failwith $"{x}"

evaluate Set.empty Map.empty ast

parse "=1+2*3/4^5"
