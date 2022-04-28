module BP.Core

open System
open Fable.Core
open Fable.Core.JS

type UnknownRecord = Map<string, obj>

[<AllowNullLiteral>]
type BlockProtocolEntity =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set

    [<Emit "$0[$1]{{=$2}}">]
    abstract Item: key: string -> obj with get, set


[<AllowNullLiteral>]
type BlockProtocolEntityType =
    abstract accountId: string option with get, set
    abstract entityTypeId: string with get, set
    abstract ``$id``: string with get, set
    abstract ``$schema``: string with get, set
    abstract title: string with get, set
    abstract ``type``: string with get, set

    [<Emit "$0[$1]{{=$2}}">]
    abstract Item: key: string -> obj with get, set


[<AllowNullLiteral>]
type BlockProtocolCreateEntitiesAction =
    abstract entityTypeId: string with get, set
    abstract entityTypeVersionId: string option with get, set
    abstract data: UnknownRecord with get, set
    abstract accountId: string option with get, set
// abstract links: DistributedOmit listBlockProtocolCreateLinksAction, BlockProtocolCreateEntitiesActionLinksDistributedOmitArrayDistributedOmit>> option with get, set


[<AllowNullLiteral>]
type BlockProtocolGetEntitiesAction =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set


type BlockProtocolUpdateEntitiesAction =
    { entityId: string
      accountId: string option
      data: obj }

[<AllowNullLiteral>]
type BlockProtocolDeleteEntitiesAction =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set

[<AllowNullLiteral>]
type BlockProtocolAggregateEntitiesResult<'T> =
    abstract results: 'T [] with get, set
    abstract operation: obj with get, set


[<AllowNullLiteral>]
type BlockProtocolCreateEntitiesFunction =
    [<Emit "$0($1...)">]
    abstract Invoke: actions: BlockProtocolCreateEntitiesAction [] -> Promise<BlockProtocolEntity []>

[<AllowNullLiteral>]
type BlockProtocolGetEntitiesFunction =
    [<Emit "$0($1...)">]
    abstract Invoke: actions: BlockProtocolGetEntitiesAction [] -> Promise<BlockProtocolEntity []>

[<AllowNullLiteral>]
type BlockProtocolUpdateEntitiesFunction =
    [<Emit "$0($1...)">]
    abstract Invoke: actions: BlockProtocolUpdateEntitiesAction [] -> Promise<BlockProtocolEntity []>

[<AllowNullLiteral>]
type BlockProtocolDeleteEntitiesFunction =
    [<Emit "$0($1...)">]
    abstract Invoke: actions: BlockProtocolDeleteEntitiesAction [] -> Promise<bool []>

// type [<AllowNullLiteral>] BlockProtocolAggregateEntitiesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: payload: BlockProtocolAggregateEntitiesPayload -> Promise<BlockProtocolAggregateEntitiesResult<BlockProtocolEntity>>

// type [<AllowNullLiteral>] BlockProtocolGetLinkAction =
//     abstract linkId: string with get, set
// type [<AllowNullLiteral>] BlockProtocolGetLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolGetLinkAction list -> Promise<BlockProtocolLink list>

// type [<AllowNullLiteral>] BlockProtocolCreateLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolCreateLinksAction list -> Promise<BlockProtocolLink list>

// type [<AllowNullLiteral>] BlockProtocolUpdateLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolUpdateLinksAction list -> Promise<BlockProtocolLink list>

// type [<AllowNullLiteral>] BlockProtocolDeleteLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolDeleteLinksAction list -> Promise<bool list>

// type [<AllowNullLiteral>] BlockProtocolCreateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolCreateEntityTypesAction list -> Promise<BlockProtocolEntityType list>

// type [<AllowNullLiteral>] BlockProtocolAggregateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: payload: BlockProtocolAggregateEntityTypesPayload -> Promise<BlockProtocolAggregateEntitiesResult<BlockProtocolEntityType>>

// type [<AllowNullLiteral>] BlockProtocolGetEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolGetEntityTypesAction list -> Promise<BlockProtocolEntityType list>

// type [<AllowNullLiteral>] BlockProtocolUpdateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolUpdateEntityTypesAction list -> Promise<BlockProtocolEntityType list>

// type [<AllowNullLiteral>] BlockProtocolDeleteEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: BlockProtocolDeleteEntityTypesAction list -> Promise<bool list>

type BlockProtocolFunction = obj

[<AllowNullLiteral>]
type BlockProtocolFunctions =
    abstract createEntities: BlockProtocolCreateEntitiesFunction option with get, set
    abstract getEntities: BlockProtocolGetEntitiesFunction option with get, set
    abstract deleteEntities: BlockProtocolDeleteEntitiesFunction option with get, set
    abstract updateEntities: BlockProtocolUpdateEntitiesFunction option with get, set
// abstract aggregateEntities: BlockProtocolAggregateEntitiesFunction option with get, set
// abstract aggregateEntityTypes: BlockProtocolAggregateEntityTypesFunction option with get, set
// abstract createEntityTypes: BlockProtocolCreateEntityTypesFunction option with get, set
// abstract getEntityTypes: BlockProtocolGetEntityTypesFunction option with get, set
// abstract updateEntityTypes: BlockProtocolUpdateEntityTypesFunction option with get, set
// abstract deleteEntityTypes: BlockProtocolDeleteEntityTypesFunction option with get, set
// abstract getLinks: BlockProtocolGetLinksFunction option with get, set
// abstract createLinks: BlockProtocolCreateLinksFunction option with get, set
// abstract deleteLinks: BlockProtocolDeleteLinksFunction option with get, set
// abstract updateLinks: BlockProtocolUpdateLinksFunction option with get, set
// abstract uploadFile: BlockProtocolUploadFileFunction option with get, set


[<AllowNullLiteral>]
type BlockProtocolProps =
    interface
        inherit BlockProtocolFunctions

        abstract accountId: string option
        abstract entityId: string
        abstract entityTypeId: string option
        abstract entityTypeVersionId: string option
        abstract entityTypes: BlockProtocolEntityType [] option
    end
