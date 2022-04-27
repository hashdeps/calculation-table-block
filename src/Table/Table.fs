module Table.Table

open Feliz
open Feliz.UseElmish

open Parser
open Evaluator

let private stylesheet =
    Stylesheet.load "../styles/table.module.scss"

let isError =
    function
    | Error _ -> true
    | _ -> false

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

let KeyDirection: Map<string, Direction> =
    Map.ofList [
        ("ArrowLeft", Left)
        ("ArrowUp", Up)
        ("ArrowRight", Right)
        ("ArrowDown", Down)
    ]

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

let getDirection (ke: Browser.Types.KeyboardEvent) : Option<Direction> = Map.tryFind ke.key KeyDirection

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
        let (col, row) =
            getPosition position direction

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
    Html.td [
        prop.className stylesheet.["selected"]
        prop.children (
            Html.input [
                prop.className stylesheet.["input"]
                prop.autoFocus (true)
                prop.onKeyDown (getKeyPressEvent state trigger)
                prop.onInput (fun e ->
                    trigger (
                        UpdateValue(
                            pos,
                            (e.target :?> Browser.Types.HTMLInputElement)
                                .value
                        )
                    ))
                prop.value (value)
            ]
        )
    ]

let renderView trigger pos value =
    let display =
        match value with
        | Ok x -> x
        | _ -> "#ERR"

    Html.td [
        prop.className [
            stylesheet.["cell"]
            if isError value then
                stylesheet.["error"]
        ]
        prop.onClick (fun _ -> trigger (StartEdit(pos)))
        prop.children (Html.text (display))
    ]

let renderCell trigger pos state =
    let value = Map.tryFind pos state.Cells

    if state.Active = Some pos then
        renderEditor trigger pos state (Option.defaultValue "" value)
    else
        let value =
            match value with
            | Some value ->
                parse value
                |> Result.map (fun (parsed, _, _) -> evaluate Set.empty state.Cells parsed)
                |> Result.map string
            | _ -> Ok ""

        renderView trigger pos value

let view state trigger =
    let empty = Html.td []

    let header (htext: string) =
        Html.th [
            prop.className stylesheet.["header"]
            prop.text (htext)
        ]

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

    Html.table [
        prop.className stylesheet.["table"]
        prop.children (
            React.fragment [
                Html.thead [ Html.tr headers ]
                Html.tbody rows

                ]
        )
    ]

let initial () =
    { Cols = [ 'A' .. 'K' ]
      Rows = [ 1..15 ]
      Active = None
      Cells = Map.empty },
    []

[<ReactComponent>]
let Spreadsheet () =
    let state, dispatch =
        React.useElmish (initial, update, [||])

    view state dispatch
