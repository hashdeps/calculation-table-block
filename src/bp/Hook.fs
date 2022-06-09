module BP.Hook

open BP.Core
open BP.Graph
open Feliz
open System
open System.Collections.Generic
open Browser
open Browser.Types
open Fable.Core

type ResponseSettler =
    { expectedResponseName: string
      resolve: (obj -> unit)
      reject: (obj -> unit) }

type ResponseSettlersMap() =
    let mutable map =
        new Dictionary<string, ResponseSettler>()

    member _.Set reqId settler = map.Add(reqId, settler)

    member _.Get reqId =
        match map.TryGetValue reqId with
        | true, v -> Some v
        | _ -> None

    member _.Remove reqId = map.Remove(reqId)


let createBPEvent (detail: BlockProtocolMessage<'a>) =
    CustomEvent.Create(
        BlockProtocolEventType,
        JsInterop.jsOptions<CustomEventInit> (fun o ->
            o.bubbles <- true
            o.composed <- true
            o.detail <- detail)
    )

let dispatchBPMessageWithResponse
    (requestSettlerMap: ResponseSettlersMap)
    (blockMessageRoot: HTMLElement)
    (detail: BlockProtocolMessage<'b>)
    : JS.Promise<'a> =
    assert detail.respondedToBy.IsSome
    let bpEvent = createBPEvent detail

    let mutable resolve = None
    let mutable reject = None

    let promise: JS.Promise<'a> =
        JS.Constructors.Promise.Create (fun res rej ->
            resolve <- Some res
            reject <- Some rej)

    requestSettlerMap.Set
        (detail.requestId)
        { expectedResponseName = detail.respondedToBy.Value
          resolve = resolve.Value
          reject = reject.Value }

    blockMessageRoot.dispatchEvent bpEvent |> ignore

    promise

let dispatchBPMessageOneWay
    (requestSettlerMap: ResponseSettlersMap)
    (blockMessageRoot: HTMLElement)
    (detail: BlockProtocolMessage<'b>)
    =
    assert detail.respondedToBy.IsNone
    let bpEvent = createBPEvent detail

    blockMessageRoot.dispatchEvent bpEvent |> ignore

    detail


let listenForEAResponse (requestSettlerMap: ResponseSettlersMap) (blockMessageRoot: HTMLElement) setInitialBlockState =
    let handler (event: Event) =
        if (event :?> CustomEvent).detail <> JS.undefined then
            let bpMessage =
                (event :?> CustomEvent).detail :?> BlockProtocolMessage<obj>

            if bpMessage.source = Embedder then
                let settlerForMessage =
                    requestSettlerMap.Get bpMessage.requestId

                if settlerForMessage.IsSome then
                    let settler = settlerForMessage.Value

                    if settler.expectedResponseName = bpMessage.messageName then

                        if bpMessage.errors <> JS.undefined then
                            settler.reject bpMessage.errors
                        else
                            settler.resolve bpMessage.data
                    else
                        settler.reject ("error.")

                    requestSettlerMap.Remove bpMessage.requestId
                    |> ignore

            if bpMessage.messageName = blockEntity
               && bpMessage.data.IsSome then
                let entity =
                    bpMessage.data.Value :?> Entity<'blockState>

                setInitialBlockState entity.properties

            console.info ("Processed", bpMessage)

    blockMessageRoot.addEventListener (BlockProtocolEventType, handler)

type React with
    [<Hook>]
    static member useBlockProtocol<'a>(ref: IRefValue<option<Types.HTMLElement>>) =
        let blockProtocolState, setblockProtocolState =
            React.useState None

        let initialBlockState, setInitialBlockState =
            React.useState None

        let settlerMap, _ =
            React.useState (ResponseSettlersMap())

        React.useEffect (
            (fun () ->
                if ref.current.IsSome then
                    let container = ref.current.Value
                    listenForEAResponse settlerMap container setInitialBlockState

                    let request = BlockProtocolInitMessage()


                    let promise: JS.Promise<BlockProtocolCorePayload<'a>> =
                        dispatchBPMessageWithResponse settlerMap container (request)

                    promise
                    |> Promise.map (fun r ->
                        let entity =
                            r.graph.Item blockEntity :?> Entity<'a>

                        let sideEffects: BlockProtocolState =
                            { blockEntityId = entity.entityId
                              updateEntity = dispatchBPMessageWithResponse settlerMap container
                              aggregateEntityTypes = dispatchBPMessageWithResponse settlerMap container
                              aggregateEntities = dispatchBPMessageWithResponse settlerMap container }

                        setblockProtocolState (Some sideEffects)
                        setInitialBlockState (Some entity.properties))
                    |> ignore),
            [| setblockProtocolState :> obj
               setInitialBlockState
               ref |]
        )

        blockProtocolState, initialBlockState
