module Lang.Evaluator

open BP.Core
open Lang.Parser
// open JsonPath
open Fable.SimpleJson

let (>>=) m f = Option.bind f m
let (<*>) m f = Option.map f m

let toOption (source: Result<'T, 'Error>) =
    match source with
    | Ok x -> Some x
    | Error _ -> None


// ----------------------------------------------------------------------------
// Aggregators
// ----------------------------------------------------------------------------

let env =
    Map.ofList [
        ("sum", Array.sum)
        ("avg",
         (fun x ->
             let length = Array.length x
             (Array.sum x) / (float length)))
    ]


// ----------------------------------------------------------------------------
// EVALUATOR
// ----------------------------------------------------------------------------

let unaryFuncs op (e: float) =
    match op with
    | Minus -> -e
    | _ -> failwith "Unimplemented"


let binaryFuncs op (l: float) (r: float) =
    match op with
    | Plus -> l + r
    | Minus -> l - r
    | Multiply -> l * r
    | Divide -> l / r
    | Exponent -> l ** r
// | _ -> failwith "Unimplemented"


let rec evaluateR visited (cells: Map<Position, string>) (ents: BlockProtocolEntity []) expr =
    match expr with
    | Number num -> Some num

    | FunctionCall (func, jsonPath) ->

        let json =
            ents
            |> Array.map (fun x -> x.Item(jsonPath))
            |> Array.map (fun x ->
                if x :? float then
                    Some(x :?> float)
                else
                    None)

            |> Array.choose id

        console.log ("Loaded Ent", json)

        Map.tryFind func env
        |> Option.map (fun f -> f json)

    | Unary (e, op) ->
        evaluateR visited cells ents e
        <*> (fun e -> unaryFuncs op e)

    | Binary (l, op, r) ->
        evaluateR visited cells ents l
        >>= (fun l ->
            evaluateR visited cells ents r
            <*> (fun r -> binaryFuncs op l r))

    | Reference pos when Set.contains pos visited -> None

    | Reference pos ->
        cells.TryFind pos
        |> Option.bind (fun value ->
            parse value
            |> toOption
            |> Option.bind (fun (parsed, _, _) -> evaluateR (Set.add pos visited) cells ents parsed))

let evaluate visited (cells: Map<Position, string>) (ents: Map<int, BlockProtocolEntity []>) expr ((_, r): Position) =

    let ents =
        Map.tryFind r ents |> Option.defaultValue [||]

    match expr with
    | FunctionCall (f, j) -> console.log ("a", (f, j))
    | _ -> ()

    evaluateR visited cells ents expr
