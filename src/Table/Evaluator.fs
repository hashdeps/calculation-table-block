module Table.Evaluator
open Table.Parser

// ----------------------------------------------------------------------------
// EVALUATOR
// ----------------------------------------------------------------------------

let rec evaluate visited (cells:Map<Position, string>) expr =
  match expr with
  | Number num ->
      Some num

  | Binary(l, op, r) ->
      let ops = dict [ '+', (+); '-', (-); '*', (*); '/', (/) ]
      evaluate visited cells l |> Option.bind (fun l ->
        evaluate visited cells r |> Option.map (fun r ->
          ops.[op] l r ))

  | Reference pos when Set.contains pos visited ->
      None

  | Reference pos ->
      cells.TryFind pos |> Option.bind (fun value ->
        parse value |> Option.bind (fun parsed ->
          evaluate (Set.add pos visited) cells parsed))
