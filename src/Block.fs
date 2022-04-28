module Block

open BP.Core
open Feliz
open Table.Table
open Fable.Core.JsInterop

[<ReactComponent(exportDefault = true)>]
let Block
    (props: {| accountId: string
               entityId: string
               updateEntities: BlockProtocolUpdateEntitiesFunction
               cells: SerializedGrid option |})
    =
    // React.useEffect (
    //     (fun () ->
    //         props.updateEntities.Invoke [|
    //             { accountId = Some(props.accountId)
    //               entityId = props.entityId
    //               data = {| hello = "world" |} }
    //         |]
    //         |> Promise.map (fun res -> console.log ("Promise result", res))
    //         |> ignore),


    //     [||]
    // )


    React.fragment [
        Html.div [
            Html.text $"Hello from F#! Block entityId is {props.entityId}"
        ]
        Spreadsheet props
    ]
