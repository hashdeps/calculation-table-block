module Main

open Feliz
open Browser.Dom
open Fable.Core.JsInterop
open Fable.React
open Block
open Fable.Core

type Elem = { ReactComponent: obj }
type MockBlockProps = BlockDefinition of Elem

let MockBlockDock props =
    ofImport "MockBlockDock" "mock-block-dock" props []

let node = document.getElementById "app"

let App () =
    MockBlockDock(keyValueList CaseRules.LowerFirst [ BlockDefinition { ReactComponent = Block } ])




ReactDOM.render (App(), node)
