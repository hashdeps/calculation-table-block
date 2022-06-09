module BP.Graph

open BP.Core
open Fable.Core
open Fable.Core.JS

type Entity<'props> =
    { accountId: string
      entityId: string
      entityTypeId: string
      properties: 'props }

type EntityType<'schema> =
    { accountId: string
      entityTypeId: string
      schema: 'schema }

// UpdateEntity
type UpdateEntity<'props> =
    { entityId: string
      properties: 'props }

let updateEntity entityId properties =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "updateEntity"
        "graph"
        (Some "updateEntityResponse")
        (Some
            { entityId = entityId
              properties = properties })

type UpdateEntityResponse<'props> =
    { entityId: string
      properties: 'props }

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

// aggregateEntityTypes
type AggregateEntityTypesOperationInput =
    { pageNumber: int
      itemsPerPage: int option
      multiSort: MultiSort [] option
      multiFilter: MultiFilter option }

type AggregateEntityTypes =
    { operation: AggregateEntityTypesOperationInput }

let aggregateAllEntityTypes () =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "aggregateEntityTypes"
        "graph"
        (Some "aggregateEntityTypesResponse")
        (Some
            { operation =
                { pageNumber = 0
                  itemsPerPage = None
                  multiSort = None
                  multiFilter = None } })

type AggregateEntityTypesResponse<'props> =
    { results: EntityType<'props> []
      operation: AggregateEntityTypesOperationInput }

// aggregateEntities
type AggregateOperationInput =
    { entityTypeId: string option
      pageNumber: int option
      itemsPerPage: int option
      multiSort: MultiSort [] option
      multiFilter: MultiFilter option }

type AggregateEntities = { operation: AggregateOperationInput }

let aggregateAllEntitiesByType entityTypeId accountId =
    createBPClientDetail
        (Some(System.Guid.NewGuid().ToString()))
        "aggregateEntities"
        "graph"
        (Some "aggregateEntitiesResponse")
        (Some
            { operation =
                { entityTypeId = (Some entityTypeId)
                  pageNumber = Some 1
                  itemsPerPage = None
                  multiSort = None
                  multiFilter = None } })

type AggregateEntitiesResponse<'props> =
    { results: Entity<'props> []
      operation: AggregateOperationInput }
