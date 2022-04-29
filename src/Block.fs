module Block

open BP.Core
open Feliz
open Table.Table
open Fable.Core.JsInterop

[<ReactComponent(exportDefault = true)>]
let Block (props: TableProps) = React.fragment [ Spreadsheet props ]
