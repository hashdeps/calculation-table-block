module Main

open Feliz
open App
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React
open Block

let MockBlockDock = ofImport "MockBlockDock" "mock-block-dock" [] 

let node = document.getElementById "app"
importSideEffects "./styles/global.scss"

let App() =
        MockBlockDock [
            (Block {| entityId = "entityId-test"; name= "Test" |})
        ]


ReactDOM.render(
    App(),
    document.getElementById "app"
)