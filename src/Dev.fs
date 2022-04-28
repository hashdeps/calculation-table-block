module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React
open Block

let MockBlockDock =
    ofImport "MockBlockDock" "mock-block-dock" []

let node = document.getElementById "app"

let App () =
    MockBlockDock [
        (Block
            { accountId = "abc"
              entityId = "entityId-test"
              updateEntities = null
              aggregateEntityTypes = null
              cells = None })
    ]

ReactDOM.render (App(), node)
