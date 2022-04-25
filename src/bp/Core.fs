module BP.Core
open System
open Fable.Core
open Fable.Core.JS

type UnknownRecord =
    Map<string, obj>

type [<AllowNullLiteral>] BlockProtocolEntity =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set
    [<Emit "$0[$1]{{=$2}}">] abstract Item: key: string -> obj with get, set


type [<AllowNullLiteral>] BlockProtocolEntityType =
    abstract accountId: string option with get, set
    abstract entityTypeId: string with get, set
    abstract ``$id``: string with get, set
    abstract ``$schema``: string with get, set
    abstract title: string with get, set
    abstract ``type``: string with get, set
    [<Emit "$0[$1]{{=$2}}">] abstract Item: key: string -> obj with get, set


type [<AllowNullLiteral>] BlockProtocolCreateEntitiesAction =
    abstract entityTypeId: string with get, set
    abstract entityTypeVersionId: string option with get, set
    abstract data: UnknownRecord with get, set
    abstract accountId: string option with get, set
    // abstract links: ResizeArray<DistributedOmit<BlockProtocolCreateLinksAction, BlockProtocolCreateEntitiesActionLinksDistributedOmitArrayDistributedOmit>> option with get, set


type [<AllowNullLiteral>] BlockProtocolGetEntitiesAction =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set


type [<AllowNullLiteral>] BlockProtocolUpdateEntitiesAction =
    abstract entityTypeId: string option with get, set
    abstract entityTypeVersionId: string option with get, set
    abstract entityId: string with get, set
    abstract accountId: string option with get, set
    abstract data: UnknownRecord with get, set

type [<AllowNullLiteral>] BlockProtocolDeleteEntitiesAction =
    abstract accountId: string option with get, set
    abstract entityId: string with get, set
    abstract entityTypeId: string option with get, set

type [<AllowNullLiteral>] BlockProtocolAggregateEntitiesResult<'T> =
    abstract results: ResizeArray<'T> with get, set
    abstract operation: obj with get, set

  
type [<AllowNullLiteral>] BlockProtocolCreateEntitiesFunction =
    [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolCreateEntitiesAction> -> Promise<ResizeArray<BlockProtocolEntity>>

type [<AllowNullLiteral>] BlockProtocolGetEntitiesFunction =
    [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolGetEntitiesAction> -> Promise<ResizeArray<BlockProtocolEntity>>

type [<AllowNullLiteral>] BlockProtocolUpdateEntitiesFunction =
    [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolUpdateEntitiesAction> -> Promise<ResizeArray<BlockProtocolEntity>>

type [<AllowNullLiteral>] BlockProtocolDeleteEntitiesFunction =
    [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolDeleteEntitiesAction> -> Promise<ResizeArray<bool>>

// type [<AllowNullLiteral>] BlockProtocolAggregateEntitiesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: payload: BlockProtocolAggregateEntitiesPayload -> Promise<BlockProtocolAggregateEntitiesResult<BlockProtocolEntity>>

// type [<AllowNullLiteral>] BlockProtocolGetLinkAction =
//     abstract linkId: string with get, set
// type [<AllowNullLiteral>] BlockProtocolGetLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolGetLinkAction> -> Promise<ResizeArray<BlockProtocolLink>>

// type [<AllowNullLiteral>] BlockProtocolCreateLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolCreateLinksAction> -> Promise<ResizeArray<BlockProtocolLink>>

// type [<AllowNullLiteral>] BlockProtocolUpdateLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolUpdateLinksAction> -> Promise<ResizeArray<BlockProtocolLink>>

// type [<AllowNullLiteral>] BlockProtocolDeleteLinksFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolDeleteLinksAction> -> Promise<ResizeArray<bool>>

// type [<AllowNullLiteral>] BlockProtocolCreateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolCreateEntityTypesAction> -> Promise<ResizeArray<BlockProtocolEntityType>>

// type [<AllowNullLiteral>] BlockProtocolAggregateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: payload: BlockProtocolAggregateEntityTypesPayload -> Promise<BlockProtocolAggregateEntitiesResult<BlockProtocolEntityType>>

// type [<AllowNullLiteral>] BlockProtocolGetEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolGetEntityTypesAction> -> Promise<ResizeArray<BlockProtocolEntityType>>

// type [<AllowNullLiteral>] BlockProtocolUpdateEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolUpdateEntityTypesAction> -> Promise<ResizeArray<BlockProtocolEntityType>>

// type [<AllowNullLiteral>] BlockProtocolDeleteEntityTypesFunction =
//     [<Emit "$0($1...)">] abstract Invoke: actions: ResizeArray<BlockProtocolDeleteEntityTypesAction> -> Promise<ResizeArray<bool>>

type BlockProtocolFunction =
    obj

type [<AllowNullLiteral>] BlockProtocolFunctions =
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


type [<AllowNullLiteral>] BlockProtocolProps =
    interface
      inherit BlockProtocolFunctions

      abstract accountId: string option with get
      abstract entityId: string with get
      abstract entityTypeId: string option with get
      abstract entityTypeVersionId: string option with get
      abstract entityTypes: ResizeArray<BlockProtocolEntityType> option with get
      // missing links and linkedaggregations
    end

