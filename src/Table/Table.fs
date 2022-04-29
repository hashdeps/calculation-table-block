module Table.Table

open Fable.Core.JsInterop
open BP.Core
open Elmish
open Feliz
open Feliz.UseElmish

open Lang.Parser
open Lang.Evaluator

let private stylesheet =
    Stylesheet.load "../styles/table.module.scss"

let isError =
    function
    | Error _ -> true
    | _ -> false

let flip f a b = f b a
let pair a b = (a, b)


// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------

type SerializedGrid = (Position * string) []


type RowSelection = int * string option


type SaveState =
    { active: Position option
      cells: SerializedGrid
      rows: RowSelection [] }

type TableProps =
    { accountId: string
      entityId: string
      aggregateEntityTypes: BlockProtocolAggregateEntityTypesFunction
      aggregateEntities: BlockProtocolAggregateEntitiesFunction
      updateEntities: BlockProtocolUpdateEntitiesFunction
      saveState: SaveState option }

type State =
    { Rows: RowSelection list
      Active: Position option
      Cols: char list
      Cells: Map<Position, string>
      entityTypes: BlockProtocolEntityType []
      loadedEntities: Map<int, BlockProtocolEntity []> }

type Event =
    | UpdateValue of Position * string
    | StartEdit of Position
    | StopEdit
    | SaveState
    | AddRow
    | RemoveRow of row: int
    | DispatchLoadEntityTypes
    | LoadEntityTypes of BlockProtocolEntityType []
    | DeserializeSaveState of SaveState
    | ClearBoard
    | SetRowEntityType of row: int * entityTypeId: string
    | LoadRowEntities of row: int * entityTypeId: string
    | SetRowEntities of row: int * BlockProtocolEntity []

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
        ("Shift-Tab", Left)
        ("Shift-Enter", Up)
        ("Tab", Right)
        ("Enter", Down)
    ]

// ----------------------------------------------------------------------------
// EVENT HANDLING
// ----------------------------------------------------------------------------

let update (props: TableProps) msg state =
    match msg with
    | StartEdit (pos) -> { state with Active = Some pos }, Cmd.none

    | StopEdit -> { state with Active = None }, Cmd.ofMsg SaveState

    | UpdateValue (pos, value) ->
        let newCells =
            if value = "" then
                Map.remove pos state.Cells
            else
                Map.add pos value state.Cells

        { state with Cells = newCells }, Cmd.none

    | AddRow ->
        let row =
            List.tryLast state.Rows
            |> Option.map (fun (x, _) -> x + 1)
            |> Option.defaultValue 1

        let newRows =
            state.Rows @ [ RowSelection(row, None) ]

        { state with Rows = newRows }, Cmd.ofMsg SaveState

    | RemoveRow n ->
        let rec removeRec rows n sub =
            match rows with
            | [] -> []
            | (x, _) :: xs when x = n -> removeRec xs n true
            | (x, s) as row :: xs ->
                (if sub then
                     RowSelection(x - 1, s)
                 else
                     row)
                :: (removeRec xs n sub)


        let newRows = (removeRec state.Rows n false)

        let cellSelector x = if x > n then true else false

        let newCells =
            Map.toList state.Cells
            |> List.fold
                (fun acc ((c, x), v) ->
                    match x with
                    | x when x = n -> acc
                    | x when x > n -> ((c, x - 1), v) :: acc
                    | _ -> ((c, x), v) :: acc)
                []
            |> Map.ofList

        { state with
            Rows = newRows
            Cells = newCells },
        Cmd.ofMsg SaveState

    | SaveState ->
        let serialized: SaveState =
            { active = state.Active
              cells = Map.toArray state.Cells
              rows = state.Rows |> Array.ofList }

        state,
        (Cmd.OfPromise.attempt
            (fun serialized ->
                props.updateEntities.Invoke [|
                    { accountId = Some props.accountId
                      entityId = props.entityId
                      data = {| saveState = Some serialized |} }
                |]
                |> Promise.map (fun _ -> console.info ("Saved state")))
            serialized
            (fun _ -> SaveState))

    | DeserializeSaveState saveState ->
        let cells = Map.ofArray saveState.cells

        let entityAggToLoad =
            saveState.rows
            |> Array.filter (fun (n, etid) ->
                etid.IsSome
                && not (Map.containsKey n state.loadedEntities))
            |> Array.map (fun (n, etid) -> Cmd.ofMsg (LoadRowEntities(n, etid.Value)))


        { state with
            Cells = cells
            Active = saveState.active
            Rows = List.ofArray saveState.rows },
        Cmd.batch entityAggToLoad

    | DispatchLoadEntityTypes ->
        let cmd =
            Cmd.OfPromise.perform
                (fun _ ->
                    props.aggregateEntityTypes.Invoke(
                        { accountId = Some(props.accountId)
                          operation = None }
                    ))
                null
                (fun et -> LoadEntityTypes et.results)

        state, cmd

    | LoadEntityTypes et -> { state with entityTypes = et }, Cmd.none

    | SetRowEntityType (row, entityTypeId) ->
        let newRows =
            state.Rows
            |> List.map (fun (x, et) ->
                if row = x then
                    RowSelection(x, Some entityTypeId)
                else
                    RowSelection(x, et))

        { state with Rows = newRows }, Cmd.ofMsg (LoadRowEntities(row, entityTypeId))

    | LoadRowEntities (row, entityTypeId) ->
        let loadEntities =
            Cmd.OfPromise.perform
                (fun entityTypdId ->
                    props.aggregateEntities.Invoke(
                        { accountId = Some props.accountId
                          operation =
                            { entityTypeId = Some entityTypdId
                              entityTypeVersionId = None
                              pageNumber = None
                              itemsPerPage = Some 100
                              multiSort = None
                              multiFilter = None } }
                    ))
                entityTypeId
                (fun x -> SetRowEntities(row, x.results))

        state, loadEntities

    | SetRowEntities (row, entities) ->
        console.info ("Loaded Entities", entities.Length)

        { state with
            loadedEntities =
                Map.change
                    row
                    (function
                    | _ -> Some entities)
                    state.loadedEntities },
        Cmd.none

    | ClearBoard -> { state with Cells = Map.empty }, Cmd.none

// ----------------------------------------------------------------------------
// RENDERING
// ----------------------------------------------------------------------------

let getDirection (ke: Browser.Types.KeyboardEvent) : Option<Direction> =
    let key =
        if ke.shiftKey then
            $"Shift-{ke.key}"
        else
            ke.key

    let maybeDirection =
        Map.tryFind key KeyDirection

    if maybeDirection.IsSome then
        ke.preventDefault ()

    maybeDirection

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
           && List.exists (fun (x, _) -> x = row) state.Rows then
            MoveTo(col, row)
        else
            Invalid

let getKeyPressEvent state trigger ke =
    match getDirection ke with
    | None ->
        if ke.key = "s" && ke.ctrlKey then
            ke.preventDefault ()
            trigger (SaveState)

        if ke.key = "Escape" then
            ke.preventDefault ()
            trigger (StopEdit)

    | Some direction ->
        match getMovement state direction with
        | Invalid -> ()
        | MoveTo position -> trigger (StartEdit(position))

let renderEditor (trigger: Event -> unit) pos state (value: string) =
    Html.td [

        prop.className [
            stylesheet.["cell"]
            stylesheet.["selected"]
        ]
        prop.children (
            Html.div [
                prop.className stylesheet.["cell-wrap"]
                prop.children [
                    Html.input [
                        prop.className stylesheet.["cell-input"]
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
                ]
            ]
        )
    ]

let renderView trigger pos value =
    let display =
        match value with
        | Ok x -> x
        // | Error x -> string x
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
                |> Result.map (fun (parsed, _, _) -> evaluate Set.empty state.Cells state.loadedEntities parsed pos)
                |> Result.map string
            | _ -> Ok ""

        renderView trigger pos value


let entityTypesDropdown trigger (et: BlockProtocolEntityType array) row (entityTypeId: string) =
    let options =
        et
        |> List.ofArray
        |> List.map (fun x ->
            let x = x

            Html.option [
                prop.value (x.entityTypeId)
                prop.text (x.title)
            ])


    Html.select [
        prop.value entityTypeId
        prop.onChange (fun value -> trigger (SetRowEntityType(row, value)))
        prop.children (
            (Html.option [
                prop.value ""
                prop.text ("-")
             ])
            :: options
        )
    ]

let view state trigger =

    let empty =
        Html.td [
            Html.button [
                prop.className stylesheet.["clear-button"]
                prop.innerHtml "&times;"
                prop.onClick (fun _ -> trigger (ClearBoard))
            ]
        ]

    let header (text: string) =
        Html.th [
            prop.className stylesheet.["header"]
            prop.text text
        ]

    let colHeader n entityTypeId =
        let entityTypeId =
            Option.defaultValue "" entityTypeId

        Html.th [
            Html.button [
                prop.className stylesheet.["minus-button"]
                prop.innerHtml "&minus;"
                prop.onClick (fun _ -> trigger (RemoveRow n))
            ]
            Html.span [
                prop.className stylesheet.["header"]
                prop.children [
                    entityTypesDropdown trigger state.entityTypes n entityTypeId
                    Html.text ($" {n}")
                ]
            ]
        ]

    let headers =
        state.Cols |> List.map (string >> header)

    let headers = empty :: headers

    let row cells = Html.tr cells

    let cells (n, entityTypeId) =
        let cells =
            state.Cols
            |> List.map (fun h -> renderCell trigger (h, n) state)

        (colHeader n entityTypeId) :: cells

    let rows =
        state.Rows
        |> List.map (fun r -> Html.tr (cells r))

    let rows =
        rows
        @ [ Html.tr [
                Html.td [
                    Html.button [
                        prop.onClick (fun _ -> trigger AddRow)
                        prop.text "add row"
                    ]
                ]
            ] ]

    Html.table [
        prop.className stylesheet.["table"]
        prop.children (
            React.fragment [
                Html.thead [ Html.tr headers ]
                Html.tbody rows

                ]
        )
    ]

let initial (saveState: SaveState option) =
    { Cols = [ 'A' .. 'E' ]
      Rows =
        [ 1..3 ]
        |> List.map (fun x -> RowSelection(x, None))
      Active = None
      Cells = Map.empty
      entityTypes = [||]
      loadedEntities = Map.empty },

    saveState
    |> Option.map (fun s ->
        Cmd.batch [
            Cmd.ofMsg DispatchLoadEntityTypes
            Cmd.ofMsg (DeserializeSaveState s)
        ])
    |> Option.defaultValue (Cmd.ofMsg DispatchLoadEntityTypes)

[<ReactComponent>]
let Spreadsheet (props: TableProps) =

    let state, dispatch =
        React.useElmish (initial props.saveState, (update props), [| props.saveState :> obj |])

    view state dispatch
