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

let BlockProtocolEventName =
    "blockprotocolmessage"

let BlockProtocolInitMessage () =
    { requestId = Guid.NewGuid().ToString()
      messageName = "init"
      respondedToBy = Some "initResponse"
      service = "core"
      source = BlockProtocolSource.Block
      data = Some(createObj [])
      errors = None }

type BlockEntity<'a> = { blockEntity: 'a }
type BlockProtocolCorePayload<'a> = { graph: BlockEntity<'a> }
