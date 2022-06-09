module Block

open BP.Core
open BP.Graph
open BP.Hook
open Feliz
open Table.Types
open Table.Table
open Fable.Core.JsInterop

[<ReactComponent(exportDefault = true)>]
let Block () =
    let ref = React.useElementRef ()

    let blockProtocolState, initialBlockState =
        React.useBlockProtocol<SaveState> (ref)

    Html.div [
        prop.ref ref
        if blockProtocolState.IsSome then
            prop.children [
                Spreadsheet blockProtocolState.Value initialBlockState
            ]
    ]
