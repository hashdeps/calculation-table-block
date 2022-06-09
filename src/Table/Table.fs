module Table.Table

// open Fable.Core.JsInterop
open Fable.Core
open BP.Core
open BP.Graph
open Elmish
open Feliz
open Feliz.UseElmish


open Lang.Parser
open Lang.Evaluator

open Table.Types

let private stylesheet =
    Stylesheet.load "../styles/table.module.scss"

let flip f a b = f b a
let pair a b = (a, b)

let merge (a: Map<'a, 'b>) (b: Map<'a, 'b>) (f: 'a -> 'b * 'b -> 'b) =
    Map.fold
        (fun s k v ->
            match Map.tryFind k s with
            | Some v' -> Map.add k (f k (v, v')) s
            | None -> Map.add k v s)
        a
        b

// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------

// type TableProps =
//     { accountId: string
//       entityId: string
//       aggregateEntityTypes: BlockProtocolAggregateEntityTypesFunction
//       aggregateEntities: BlockProtocolAggregateEntitiesFunction
//       updateEntities: BlockProtocolUpdateEntitiesFunction
//       saveState: SaveState option }
type State =
    { Cols: char list
      Rows: RowSelection list
      Cells: Map<Position, string>
      Active: Position option
      EntityTypes: EntityType<unit> []
      LoadedEntities: Map<int, Entity<AnyBlockProperty> []> }

type Event =
    | UpdateValue of Position * content: string
    | StartEdit of Position
    | StopEdit
    | SaveState
    | DeserializeSaveState of SaveState
    | AddRow
    | RemoveRow of row: int
    | ClearBoard
    | DispatchLoadEntityTypes
    | LoadEntityTypes of EntityType<unit> []
    | SetRowEntityType of row: int * entityTypeId: string
    | SetRowEntities of row: int * entities: Entity<AnyBlockProperty> []
    | LoadRowEntities of row: int * entityTypeId: string

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

let update (sideEffect: BlockProtocolState) msg state =
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

        { state with Rows = newRows },
        Cmd.batch [
            Cmd.ofMsg SaveState
            Cmd.ofMsg StopEdit
        ]

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
        Cmd.batch [
            Cmd.ofMsg SaveState
            Cmd.ofMsg StopEdit
        ]

    | SaveState ->
        let serialized: SaveState =
            { active = state.Active
              cells = Map.toArray state.Cells
              rows = state.Rows |> Array.ofList }

        state,
        (Cmd.OfPromise.attempt
            (fun serialized ->
                console.log (serialized)


                updateEntity sideEffect.blockEntityId serialized
                |> sideEffect.updateEntity
                |> Promise.map (fun _ -> console.info ("Saved state")))
            serialized
            (fun _ -> SaveState))

    | DeserializeSaveState saveState ->
        let loadedCells =
            Map.ofArray saveState.cells

        let cells =
            merge loadedCells state.Cells (fun k (v, v') -> v')


        let entityAggToLoad =
            saveState.rows
            |> Array.filter (fun (n, etid) ->
                etid.IsSome
                && not (Map.containsKey n state.LoadedEntities))
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
                    aggregateAllEntityTypes ()
                    |> sideEffect.aggregateEntityTypes)
                null
                (fun et -> LoadEntityTypes et.results)

        state, cmd

    | LoadEntityTypes et -> { state with EntityTypes = et }, Cmd.none

    | SetRowEntityType (row, entityTypeId) ->
        let newRows =
            state.Rows
            |> List.map (fun (x, et) ->
                if row = x then
                    RowSelection(
                        x,
                        if entityTypeId <> "" then
                            Some entityTypeId
                        else
                            None
                    )
                else
                    RowSelection(x, et))

        let cmd =
            if entityTypeId <> "" then
                Cmd.ofMsg (LoadRowEntities(row, entityTypeId))
            else
                Cmd.none

        { state with Rows = newRows }, Cmd.batch [ cmd; Cmd.ofMsg SaveState ]

    | LoadRowEntities (row, entityTypeId) ->
        let loadEntities =
            Cmd.OfPromise.perform
                (fun entityTypdId ->
                    aggregateAllEntitiesByType entityTypdId
                    |> sideEffect.aggregateEntities)
                entityTypeId
                (fun x -> SetRowEntities(row, x.results))

        state, loadEntities

    | SetRowEntities (row, entities) ->
        console.info ("Loaded Entities", entities.Length)

        { state with
            LoadedEntities =
                Map.change
                    row
                    (function
                    | _ -> Some entities)
                    state.LoadedEntities },
        Cmd.none

    | ClearBoard -> { state with Cells = Map.empty }, Cmd.ofMsg SaveState

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
        | Error x -> string x

    Html.td [
        prop.className stylesheet.["cell"]
        prop.onClick (fun _ -> trigger (StartEdit(pos)))
        prop.children (Html.text (display))
    ]

let renderCell trigger pos state =
    let value = Map.tryFind pos state.Cells

    if state.Active = Some pos then
        renderEditor trigger pos state (Option.defaultValue "" value)
    else
        let parsed =
            match value with
            | Some value ->
                parse value
                |> Result.map (fun (parsed, _, _) -> evaluate Set.empty state.Cells state.LoadedEntities parsed pos)
                |> Result.map string
            | _ -> Ok ""

        parsed
        // Parser error is discarded here
        |> Result.mapError (fun _ -> Option.defaultValue "" value)
        |> renderView trigger pos


let entityTypesDropdown trigger (et: EntityType<'a> array) row (entityTypeId: string) =
    let options =
        et
        |> List.ofArray
        |> List.map (fun x ->
            let x = x

            Html.option [
                prop.value (x.entityTypeId)
                prop.text (x.entityTypeId)
            ])


    Html.select [
        prop.className stylesheet.["dropdown"]
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
                    entityTypesDropdown trigger state.EntityTypes n entityTypeId
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
                        prop.className stylesheet.["add-row"]
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
      EntityTypes = [||]
      LoadedEntities = Map.empty },

    saveState
    |> Option.map (fun s ->
        Cmd.batch [
            Cmd.ofMsg DispatchLoadEntityTypes
            Cmd.ofMsg (DeserializeSaveState s)
        ])
    |> Option.defaultValue (Cmd.ofMsg DispatchLoadEntityTypes)

[<ReactComponent>]
let Spreadsheet (bpState: BlockProtocolState) (initialBlockState: SaveState option) =
    let state, dispatch =
        React.useElmish (initial initialBlockState, (update bpState), [| bpState :> obj; initialBlockState |])

    view state dispatch
