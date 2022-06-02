module BP.Graph

open BP.Core.Next
open Browser.Types
open Browser.Event
open Browser.Types
open Fable.Core
// We need to consider the init message,

let createBPEvent (detail: BlockProtocolMessageDetail<obj, obj>) =
    CustomEvent.Create(
        BlockProtocolEventType,
        {| detail = {|  |}
           bubbles = true
           cancelable = false
           composed = true |}
        :> CustomEventInit
    )

let dispatchBPEvent (dispatchFrom: HTMLElement) event = dispatchFrom.dispatchEvent
