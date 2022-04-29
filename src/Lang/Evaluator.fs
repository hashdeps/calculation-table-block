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

let env: Map<string, (float [] -> float)> =
    Map.ofList [
        ("count", Array.length >> float)
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


let rec evaluate visited (cells: Map<Position, string>) (ents: Map<int, BlockProtocolEntity []>) expr calculateAtPos =
    match expr with
    | Number num -> Some num

    | FunctionCall (func, jsonPath) ->

        let ents =
            Map.tryFind (snd calculateAtPos) ents
            |> Option.defaultValue [||]

        let json =
            ents
            |> Array.map (fun x -> x.Item(jsonPath))
            |> Array.map (fun x ->
                if x :? float then
                    Some(x :?> float)
                else
                    // This allows counting. Not the best way to do so, though!
                    Some(0.0))

            |> Array.choose id

        Map.tryFind func env
        |> Option.map (fun f -> f json)

    | Unary (e, op) ->
        evaluate visited cells ents e calculateAtPos
        <*> (fun e -> unaryFuncs op e)

    | Binary (l, op, r) ->
        evaluate visited cells ents l calculateAtPos
        >>= (fun l ->
            evaluate visited cells ents r calculateAtPos
            <*> (fun r -> binaryFuncs op l r))

    | Reference pos when Set.contains pos visited -> None

    | Reference pos ->
        cells.TryFind pos
        |> Option.bind (fun value ->
            let parsed = parse value

            parsed
            |> toOption
            |> Option.bind (fun (parsed, _, _) -> evaluate (Set.add pos visited) cells ents parsed pos))
