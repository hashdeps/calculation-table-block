module Block

open BP.Core
open Feliz
open Fable.Core.JsInterop

let styleSheet =
    Stylesheet.load "./styles/style.module.scss"

[<ReactComponent(exportDefault = true)>]
let Block entityId =
    React.fragment [
        Html.div [
            prop.className "styleSheet"
            prop.children [
                Html.text $"Hello from F#! Block entityId is {entityId}"
            ]
        ]
    ]
