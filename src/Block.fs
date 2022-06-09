module Block

open BP.Core
open BP.Graph
open Feliz
open Table.Types
open Table.Table
open Fable.Core.JsInterop

[<ReactComponent(exportDefault = true)>]
let Block () =
    let initialState, setInitialState =
        React.useState None

    let settlerMap, _ =
        React.useState (ResponseSettlersMap())


    let ref = React.useElementRef ()
    let token = React.useCancellationToken ()

    React.useEffect (
        (fun () ->
            if ref.current.IsSome then
                let container = ref.current.Value
                listenForEAResponse settlerMap container

                let request = BlockProtocolInitMessage()

                let promise: Fable.Core.JS.Promise<BlockProtocolCorePayload<SaveState>> =
                    dispatchBPMessageWithResponse settlerMap container (request)

                promise
                |> Promise.map (fun r ->
                    let entity =
                        r.graph.Item "blockEntity" :?> Entity<unit>

                    let sideEffects =
                        { blockEntityId = entity.entityId
                          blockAccountId = entity.accountId
                          updateEntity = dispatchBPMessageWithResponse settlerMap container
                          aggregateEntityTypes = dispatchBPMessageWithResponse settlerMap container
                          aggregateEntities = dispatchBPMessageWithResponse settlerMap container }

                    setInitialState (Some sideEffects))
                |> ignore),
        [| setInitialState :> obj; ref |]
    )




    Html.div [
        prop.ref ref
        if initialState.IsSome then
            prop.children [
                Spreadsheet initialState.Value
            ]
    ]
