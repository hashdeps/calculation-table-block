module Block

open BP.Core
open Feliz
open Table.Table
open Fable.Core.JsInterop

[<ReactComponent(exportDefault = true)>]
let Block (props: TableProps) =
    React.useEffect (
        (fun () ->
            props.aggregateEntityTypes.Invoke
                { accountId = Some(props.accountId)
                  operation = None }
            |> Promise.map (fun res -> console.log ("Promise result", res))
            |> ignore),


        [||]
    )


    React.fragment [
        Html.div [
            Html.text $"Hello from F#! Block entityId is {props.entityId}"
        ]
        Spreadsheet props
    ]
