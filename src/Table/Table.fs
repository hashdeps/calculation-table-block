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

type SerializedGrid = (Position * string) []

type TableProps =
    { accountId: string
      entityId: string
      aggregateEntityTypes: BlockProtocolAggregateEntityTypesFunction
      aggregateEntities: BlockProtocolAggregateEntitiesFunction
      updateEntities: BlockProtocolUpdateEntitiesFunction
      cells: SerializedGrid option }

// ----------------------------------------------------------------------------
// DOMAIN MODEL
// ----------------------------------------------------------------------------


type Event =
    | UpdateValue of Position * string
    | StartEdit of Position
    | SaveState
    | AddRow
    | RemoveRow of row: int
    | DispatchLoadEntityTypes
    | LoadEntityTypes of BlockProtocolEntityType []
    | DeserializeGrid of SerializedGrid * save: bool
    | SetRowEntityType of row: int * entityTypeId: string
    | LoadRowEntities of row: int * BlockProtocolEntity []

type RowSelection = RowSelection of row: int * entityTypeId: string option

type State =
    { Rows: RowSelection list
      Active: Position option
      Cols: char list
      Cells: Map<Position, string>
      entityTypes: BlockProtocolEntityType []
      loadedEntities: Map<int, BlockProtocolEntity []> }

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
            |> Option.map (fun (RowSelection (x, _)) -> x + 1)
            |> Option.defaultValue 1

        let newRows =
            state.Rows @ [ RowSelection(row, None) ]

        { state with Rows = newRows }, Cmd.none

    | RemoveRow n ->
        let rec removeRec rows n sub =
            match rows with
            | [] -> []
            | RowSelection (x, _) :: xs when x = n -> removeRec xs n true
            | (RowSelection (x, s) as row) :: xs ->
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
        Cmd.none

    | SaveState ->
        let serialized = Map.toArray state.Cells

        state,
        (Cmd.OfPromise.attempt
            (fun cells ->
                props.updateEntities.Invoke [|
                    { accountId = Some(props.accountId)
                      entityId = props.entityId
                      data = {| cells = cells |} }
                |]
                |> Promise.map (fun res -> console.log ("saved state", res)))
            serialized
            (fun _ -> SaveState))

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
            |> List.map (fun (RowSelection (x, et)) ->
                if row = x then
                    RowSelection(x, Some entityTypeId)
                else
                    RowSelection(x, et))

        let loadEntities =
            Cmd.OfPromise.perform
                (fun entityTypdId ->
                    props.aggregateEntities.Invoke(
                        { accountId = Some props.entityId
                          operation =
                            { entityTypeId = Some entityTypdId
                              entityTypeVersionId = None
                              pageNumber = 1
                              itemsPerPage = None
                              multiSort = None
                              multiFilter = None } }
                    ))
                entityTypeId
                (fun x -> LoadRowEntities(row, x.results))

        { state with Rows = newRows }, loadEntities

    | LoadRowEntities (row, entities) ->
        console.log ("Loaded Ent", entities)

        { state with
            loadedEntities =
                Map.change
                    row
                    (function
                    | _ -> Some entities)
                    state.loadedEntities },
        Cmd.none

    | DeserializeGrid (grid, save) ->
        let cells = Map.ofArray grid

        { state with Cells = cells },
        if save then
            Cmd.ofMsg SaveState
        else
            Cmd.none
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
           && List.exists (fun (RowSelection (x, _)) -> x = row) state.Rows then
            MoveTo(col, row)
        else
            Invalid

let getKeyPressEvent state trigger ke =
    match getDirection ke with
    | None ->
        if ke.key = "s" && ke.ctrlKey then
            ke.preventDefault ()
            trigger (SaveState)

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
        | Error x -> string x

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


let entityTypesDropdown trigger (et: BlockProtocolEntityType array) row =
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
        prop.onChange (fun value -> trigger (SetRowEntityType(row, value)))
        prop.children ((Html.option [ prop.text ("-") ]) :: options)
    ]

let view state trigger =

    let empty =
        Html.td [
            Html.button [
                prop.className stylesheet.["clear-button"]
                prop.innerHtml "&times;"
                prop.onClick (fun _ -> trigger (DeserializeGrid(Array.empty, true)))
            ]
        ]

    let header (text: string) =
        Html.th [
            prop.className stylesheet.["header"]
            prop.text text
        ]

    let colHeader n entityTypeId =
        Html.th [
            Html.button [
                prop.className stylesheet.["minus-button"]
                prop.innerHtml "&minus;"
                prop.onClick (fun _ -> trigger (RemoveRow n))
            ]
            Html.span [
                prop.className stylesheet.["header"]
                prop.text ($"{n}")
                prop.children [
                    entityTypesDropdown trigger state.entityTypes n
                ]
            ]
        ]

    let headers =
        state.Cols |> List.map (string >> header)

    let headers = empty :: headers

    let row cells = Html.tr cells

    let cells (RowSelection (n, entityTypeId)) =
        let cells =
            state.Cols
            |> List.map (fun h -> renderCell trigger (h, n) state)

        colHeader n entityTypeId :: cells

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

let initial cells =
    { Cols = [ 'A' .. 'E' ]
      Rows =
        [ 1..3 ]
        |> List.map (fun x -> RowSelection(x, None))
      Active = None
      Cells = Map.empty
      entityTypes = [||]
      loadedEntities = Map.empty },
    cells
    |> Option.map (fun c ->
        Cmd.batch [
            Cmd.ofMsg (DeserializeGrid(c, false))
            Cmd.ofMsg DispatchLoadEntityTypes
        ])
    |> Option.defaultValue (Cmd.ofMsg DispatchLoadEntityTypes)

[<ReactComponent>]
let Spreadsheet (props: TableProps) =

    let state, dispatch =
        React.useElmish (initial props.cells, (update props), [| props :> obj |])

    view state dispatch
