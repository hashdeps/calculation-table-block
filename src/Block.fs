module Block
open BP.Core
open Feliz
open Table.Table
open Fable.Core.JsInterop
open Feliz.Bulma

[<ReactComponent(exportDefault = true)>]
let Block entityId  = 
  React.fragment [
    Html.div [Html.text $"Hello from F#! B lock entityId is {entityId}"]
    Bulma.tag [
        Bulma.color.isPrimary
        Bulma.color.isLight
        prop.text "Tag light"
    ]
    Spreadsheet()
  ]

