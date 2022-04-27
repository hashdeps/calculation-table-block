module Table.Evaluator
open Table.Parser

let (>>=) m f = Option.bind f m
let (<*>) m f = Option.map f m

let toOption (source: Result<'T,'Error>) = match source with Ok x -> Some x | Error _ -> None
// ----------------------------------------------------------------------------
// EVALUATOR
// ----------------------------------------------------------------------------

let unaryFuncs op (e: int) = 
 match op with
 | Operator.Minus -> -e
 | _ -> failwith "Unimplemented"


let binaryFuncs op (l: int) (r: int) = 
 match op with
 | Operator.Plus -> l + r
 | Operator.Minus -> l - r
 | Operator.Multiply -> l * r
 | Operator.Divide -> l / r
 | Operator.Exponent -> pown l r
 | _ -> failwith "Unimplemented"


let rec evaluate visited (cells:Map<Position, string>) expr =
  match expr with
  | Number num ->
      Some num
  | Unary (e, op) -> 
      evaluate visited cells e 
      <*> (fun e ->  unaryFuncs op e)

  | Binary(l, op, r) ->
      evaluate visited cells l 
      >>= (fun l ->
        evaluate visited cells r 
        <*> (fun r ->
          binaryFuncs op l r ))

  | Reference pos when Set.contains pos visited ->
      None

  | Reference pos ->
      cells.TryFind pos |> Option.bind (fun value ->
        parse value |> toOption |> Option.bind (fun (parsed, _, _) ->
          evaluate (Set.add pos visited) cells parsed))
