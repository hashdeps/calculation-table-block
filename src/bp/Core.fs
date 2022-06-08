module BP.Core

open System
open System.Collections.Generic
open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop


[<StringEnum>]
type BlockProtocolSource =
    | Block
    | Embedder

type MessageError =
    { code: string
      message: string
      extensions: obj option }

type BlockProtocolMessage<'payload> =
    { requestId: string
      messageName: string
      respondedToBy: string option
      service: string
      source: BlockProtocolSource
      data: 'payload option
      errors: (MessageError []) option }

let createBPClientDetail requestId messageName service respondedToBy data =
    { requestId = defaultArg requestId (Guid.NewGuid().ToString())
      messageName = messageName
      respondedToBy = respondedToBy
      service = service
      source = BlockProtocolSource.Block
      data = data
      errors = None }

let BlockProtocolEventType =
    "blockprotocolmessage"

let BlockProtocolInitMessage () : BlockProtocolMessage<unit> =
    { requestId = Guid.NewGuid().ToString()
      messageName = "init"
      respondedToBy = Some "initResponse"
      service = "core"
      source = BlockProtocolSource.Block
      data = None
      errors = None

    }

type BlockProtocolCorePayload = { services: Map<string, obj> }

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
        jsOptions<CustomEventInit> (fun o ->
            o.bubbles <- true
            o.composed <- true
            o.detail <- detail)
    )

type DispatchResponse<'a, 'b> =
    | WithResponse of Promise<'a>
    | OneWay of BlockProtocolMessage<'b>

let dispatchBPEvent<'a, 'b>
    (requestSettlerMap: ResponseSettlersMap)
    (blockMessageRoot: HTMLElement)
    (detail: BlockProtocolMessage<'b>)
    : DispatchResponse<'a, 'b> =
    let bpEvent = createBPEvent detail

    let _dispatched =
        blockMessageRoot.dispatchEvent bpEvent

    if detail.respondedToBy.IsSome then
        let mutable resolve = None
        let mutable reject = None

        let promise: Promise<'a> =
            JS.Constructors.Promise.Create (fun res rej ->
                resolve <- Some res
                reject <- Some rej)

        requestSettlerMap.Set
            (detail.requestId)
            { expectedResponseName = detail.respondedToBy.Value
              resolve = resolve.Value
              reject = reject.Value }

        WithResponse promise
    else
        OneWay detail

let listenForEAResponse (requestSettlerMap: ResponseSettlersMap) (blockMessageRoot: HTMLElement) =
    let handler (event: Event) =
        if event :? CustomEvent then
            let bpMessage =
                (event :?> CustomEvent).detail :?> BlockProtocolMessage<obj>

            if bpMessage.source = Embedder then
                let settlerForMessage =
                    requestSettlerMap.Get bpMessage.requestId

                if settlerForMessage.IsSome then
                    let settler = settlerForMessage.Value

                    if settler.expectedResponseName = bpMessage.messageName then
                        settler.resolve
                            {| data = bpMessage.data
                               errors = bpMessage.errors |}
                    else
                        settler.reject ("error.")

                    requestSettlerMap.Remove bpMessage.requestId
                    |> ignore

                console.log (bpMessage.messageName)

    blockMessageRoot.addEventListener (BlockProtocolEventType, handler)
