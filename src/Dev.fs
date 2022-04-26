module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React
open Block

let MockBlockDock = ofImport "MockBlockDock" "mock-block-dock" [] 

let node = document.getElementById "app"

let App() =
        MockBlockDock [
            (Block {| entityId = "entityId-test"; name= "Test" |})
        ]

ReactDOM.render(
    App(),
    node
)