module BP.Core

open System
open System.Collections.Generic
open Browser
open Browser.Types
open Fable.Core
open Fable.Core.JS
open Fable.Core.JsInterop


[<AllowNullLiteral>]
type AnyBlockProperty =
    [<Emit "$0[$1]{{=$2}}">]
    abstract Item: key: string -> obj with get

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

let BlockProtocolInitMessage () : BlockProtocolMessage<obj> =
    { requestId = Guid.NewGuid().ToString()
      messageName = "init"
      respondedToBy = Some "initResponse"
      service = "core"
      source = BlockProtocolSource.Block
      data = Some(createObj [])
      errors = None }

type BlockProtocolCorePayload<'a> = { graph: AnyBlockProperty }

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

let dispatchBPMessageWithResponse
    (requestSettlerMap: ResponseSettlersMap)
    (blockMessageRoot: HTMLElement)
    (detail: BlockProtocolMessage<'b>)
    : Promise<'a> =
    assert detail.respondedToBy.IsSome
    let bpEvent = createBPEvent detail

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


let listenForEAResponse (requestSettlerMap: ResponseSettlersMap) (blockMessageRoot: HTMLElement) =
    let handler (event: Event) =
        if (event :?> CustomEvent).detail <> undefined then
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

                console.info ("Processed", bpMessage.messageName)

    blockMessageRoot.addEventListener (BlockProtocolEventType, handler)
