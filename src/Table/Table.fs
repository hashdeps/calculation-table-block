module Table.Table
open Feliz
open Feliz.UseElmish
open Elmish

open Parser
open Evaluator

// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------

type Event =
    | UpdateValue of Position * string
    | StartEdit of Position

type State =
    { Rows: int list
      Active: Position option
      Cols: char list
      Cells: Map<Position, string> }

type Movement =
    | MoveTo of Position
    | Invalid

type Direction =
    | Up
    | Down
    | Left
    | Right

let KeyDirection: Map<float, Direction> =
    Map.ofList [ (37.0, Left)
                 (38.0, Up)
                 (39.0, Right)
                 (40.0, Down) ]

// ----------------------------------------------------------------------------
// EVENT HANDLING
// ----------------------------------------------------------------------------

let update msg state =
    match msg with
    | StartEdit (pos) -> { state with Active = Some pos }, []

    | UpdateValue (pos, value) ->
        let newCells =
            if value = "" then
                Map.remove pos state.Cells
            else
                Map.add pos value state.Cells

        { state with Cells = newCells }, []

// ----------------------------------------------------------------------------
// RENDERING
// ----------------------------------------------------------------------------

let getDirection (ke: Browser.Types.KeyboardEvent) : Option<Direction> = Map.tryFind ke.keyCode KeyDirection

let getPosition ((col, row): Position) (direction: Direction) : Position =
    match direction with
    | Up -> (col, row - 1)
    | Down -> (col, row + 1)
    | Left -> (char ((int col) - 1), row)
    | Right -> (char ((int col) + 1), row)

let getMovement (state: State) (direction: Direction) : Movement =
    match state.Active with
    | None -> Invalid
    | (Some position) ->
        let (col, row) = getPosition position direction

        if List.contains col state.Cols
           && List.contains row state.Rows then
            MoveTo(col, row)
        else
            Invalid

let getKeyPressEvent state trigger =
    fun (ke: Browser.Types.Event) ->
        match getDirection (ke :?> _) with
        | None -> ()
        | Some direction ->
            match getMovement state direction with
            | Invalid -> ()
            | MoveTo position -> trigger (StartEdit(position))

let renderEditor (trigger: Event -> unit) pos state (value: string) =
    Html.td [ prop.className "selected"
              prop.children (
                  Html.input [ prop.autoFocus (true)
                               prop.onKeyDown (getKeyPressEvent state trigger)
                               prop.onInput
                                   (fun e ->
                                       trigger (
                                           UpdateValue(
                                               pos,
                                               (e.target :?> Browser.Types.HTMLInputElement)
                                                   .value
                                           )
                                       ))
                               prop.value (value) ]
              ) ]

let renderView trigger pos (value: option<_>) =
    Html.td [ prop.onClick //prop.style (if value.IsNone then [("background", "#ffb0b0")] else [("background", "white")])
                  (fun _ -> trigger (StartEdit(pos)))
              prop.children (Html.text ((Option.defaultValue "#ERR" value))) ]

let renderCell trigger pos state =
    let value = Map.tryFind pos state.Cells

    if state.Active = Some pos then
        renderEditor trigger pos state (Option.defaultValue "" value)
    else
        let value =
            match value with
            | Some value ->
                parse value
                |> Option.bind (evaluate Set.empty state.Cells)
                |> Option.map string
            | _ -> Some ""

        renderView trigger pos value

let view state trigger =
    let empty = Html.td []
    let header (htext: string) = Html.th [ prop.text (htext) ]

    let headers =
        state.Cols |> List.map (string >> header)

    let headers = empty :: headers

    let row cells = Html.tr cells

    let cells n =
        let cells =
            state.Cols
            |> List.map (fun h -> renderCell trigger (h, n) state)

        header (string n) :: cells

    let rows =
        state.Rows
        |> List.map (fun r -> Html.tr (cells r))

    Html.table [ Html.thead [ Html.tr headers ]
                 Html.tbody rows ]

let initial () =
    { Cols = [ 'A' .. 'K' ]
      Rows = [ 1 .. 15 ]
      Active = None
      Cells = Map.empty },
    []

[<ReactComponent>]
let Spreadsheet () =
    let state, dispatch = React.useElmish (initial, update, [||])

    view state dispatch
