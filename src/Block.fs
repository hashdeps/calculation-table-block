module Block
open BP.Core
open Feliz
open Table

[<ReactComponent>]
let Block entityId  = 
  React.fragment [
    Html.h1 [ Html.text $"Hello from F#!"]
    Html.p [ Html.text $"Through mock-block-dock! Your entityId is {entityId}"]
    Counter()
  ]

