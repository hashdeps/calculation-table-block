module Lang.Evaluator

open BP.Core
open BP.Graph
open Lang.Parser

let (>>=) m f = Option.bind f m
let (<!>) m f = Option.map f m

let resultToOption (source: Result<'T, 'Error>) =
    match source with
    | Ok x -> Some x
    | Error _ -> None


// ----------------------------------------------------------------------------
// Aggregators
// ----------------------------------------------------------------------------

let env: Map<string, (float seq -> float)> =
    Map.ofList [
        ("count", Seq.length >> float)
        ("sum", Seq.sum)
        ("avg",
         (fun x ->
             let length = Seq.length x
             (Seq.sum x) / (float length)))
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


let rec evaluate
    visited
    (cells: Map<Position, string>)
    (ents: Map<int, Entity<AnyBlockProperty> []>)
    expr
    calculateAtPos
    =
    match expr with
    | Number num -> Some num

    | FunctionCall (func, propertyName) ->

        let ents =
            Map.tryFind (snd calculateAtPos) ents
            |> Option.defaultValue [||]

        let json =
            Array.foldBack
                (fun (entity: Entity<AnyBlockProperty>) acc ->
                    match entity with
                    | e when e.properties.IsSome ->
                        // This bit is very hacky. We've already established
                        // that the entity contains "any" data
                        // this nextp art checks if the specified property names
                        // exists, and whether or not it is a float.
                        let property =
                            e.properties.Value.Item(propertyName)

                        let x =
                            if property :? float then
                                // If it is a float, this means that we can use it
                                // for aggregation functions.
                                property :?> float
                            else
                                // Otherwise we allow 'counting'.
                                // Not the best way to do so, though!
                                0.0

                        x :: acc
                    | _ -> acc

                    )
                ents
                []


        Map.tryFind func env
        |> Option.map (fun f -> f json)

    | Unary (e, op) ->
        evaluate visited cells ents e calculateAtPos
        <!> (fun e -> unaryFuncs op e)

    | Binary (l, op, r) ->
        evaluate visited cells ents l calculateAtPos
        >>= (fun l ->
            evaluate visited cells ents r calculateAtPos
            <!> (fun r -> binaryFuncs op l r))

    | Reference pos when Set.contains pos visited -> None

    | Reference pos ->
        cells.TryFind pos
        >>= (fun value ->
            let parsed = parse value

            resultToOption parsed
            >>= (fun (parsed, _, _) -> evaluate (Set.add pos visited) cells ents parsed pos))
