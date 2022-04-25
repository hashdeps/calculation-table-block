module Main

open Feliz
open App
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React
open App

let MockBlockDock = ofImport "MockBlockDock" "mock-block-dock" [] 

let node = document.getElementById "app"
importSideEffects "./styles/global.scss"

let App() =
        MockBlockDock [
            (App "entityId-test")
        ]


ReactDOM.render(
    App(),
    document.getElementById "app"
)