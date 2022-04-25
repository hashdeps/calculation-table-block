module App
open BP.Core
open Feliz

[<ReactComponent>]
let App entityId  = 
  React.fragment [
    Html.h1 [ Html.text $"Hello from F#!"]
    Html.p [ Html.text $"Through mock-block-dock! Your entityId is {entityId}"]
  ]

