module BP.Graph

open BP.Core
open Fable.Core
open Fable.Core.JS

[<AllowNullLiteral>]
type AnyBlockProperty =
    [<Emit "$0[$1]{{=$2}}">]
    abstract Item: key: string -> obj with get

type Entity<'props> =
    { entityId: string
      entityTypeId: string option
      properties: 'props option }

type EntityType<'schema> =
    { entityTypeId: string
      schema: 'schema }

let blockEntity = "blockEntity"

// UpdateEntity
type UpdateEntity<'props> =
    { entityId: string
      properties: 'props }

type UpdateEntityResponse<'props> =
    { entityId: string
      properties: 'props }

// filtering Operation
[<StringEnum>]
type MultiFilterOperatorType =
    | AND
    | OR

type MultiFilter =
    { field: string
      operator: MultiFilterOperatorType
      value: string }

type MultiFilters =
    { filters: MultiFilterOperatorType []
      operator: MultiFilterOperatorType }

type MultiSort = { field: string; desc: bool option }

type AggregateOperation =
    { entityTypeId: string option
      pageNumber: int
      itemsPerPage: int
      pageCount: int option
      totalCount: int option
      multiSort: MultiSort [] option
      multiFilter: MultiFilter option }

// aggregateEntityTypes
type AggregateEntityTypes = { operation: AggregateOperation }

type AggregateEntityTypesResponse<'props> =
    { results: EntityType<'props> []
      operation: AggregateOperation }

// aggregateEntities

type AggregateEntities = { operation: AggregateOperation }

type AggregateEntitiesResponse<'props> =
    { results: Entity<'props> []
      operation: AggregateOperation }


// Block Protocol functionality we're exposing

type BlockProtocolState =
    { blockEntityId: string
      updateEntity: BlockProtocolMessage<UpdateEntity<obj>> -> JS.Promise<UpdateEntityResponse<obj>>
      aggregateEntityTypes: BlockProtocolMessage<AggregateEntityTypes> -> JS.Promise<AggregateEntityTypesResponse<unit>>
      aggregateEntities: BlockProtocolMessage<AggregateEntities>
          -> JS.Promise<AggregateEntitiesResponse<AnyBlockProperty>> }


let updateEntity entityId properties : BlockProtocolMessage<UpdateEntity<'a>> =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "updateEntity"
        "graph"
        (Some "updateEntityResponse")
        (Some
            { entityId = entityId
              properties = properties })

let aggregateAllEntityTypes () : BlockProtocolMessage<AggregateEntityTypes> =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "aggregateEntityTypes"
        "graph"
        (Some "aggregateEntityTypesResponse")
        (Some
            { operation =
                { entityTypeId = None
                  pageNumber = 1
                  itemsPerPage = 100
                  pageCount = None
                  totalCount = None
                  multiSort = None
                  multiFilter = None } })

let aggregateAllEntitiesByType entityTypeId : BlockProtocolMessage<AggregateEntities> =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "aggregateEntities"
        "graph"
        (Some "aggregateEntitiesResponse")
        (Some
            { operation =
                { entityTypeId = (Some entityTypeId)
                  pageNumber = 1
                  itemsPerPage = 5000
                  pageCount = None
                  totalCount = None
                  multiSort = None
                  multiFilter = None } })
