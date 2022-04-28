#load "Parsec.fs"
#load "Parser.fs"

open Lang.Parser

parse """=sum(employee)+2*3/4^5"""


#r "nuget: Fable.Core"
#r "nuget: Fable.SimpleJson"
#load "../bp/Core.fs"
#load "Evaluator.fs"

open Lang.Evaluator
open BP.Core

let ast =
    match parse "=sum(employee)" with
    | Ok (ast, _, _) -> ast
    | Error x -> failwith $"{x}"

let ent =
    { new BlockProtocolEntity with
        member x.entityId = "test"
        member x.accountId = None
        member x.entityTypeId = None

        member x.Item
            with get (_) = 4.2 }

let ents = Map.ofList [ (1, [| ent |]) ]
evaluate Set.empty Map.empty ents ast ('a', 1)

parse "=sum(employee)"
