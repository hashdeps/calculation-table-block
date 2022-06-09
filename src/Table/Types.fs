module Table.Types

open Fable.Core
open BP.Core
open BP.Graph

open Lang.Parser

type SerializedGrid = (Position * string) []


type RowSelection = int * string option


type SaveState =
    { active: Position option
      cells: SerializedGrid
      rows: RowSelection [] }


type BlockProtocolSideEffects =
    { blockAccountId: string
      blockEntityId: string
      updateEntity: BlockProtocolMessage<UpdateEntity<SaveState>> -> JS.Promise<UpdateEntityResponse<SaveState>>
      aggregateEntityTypes: BlockProtocolMessage<AggregateEntityTypes> -> JS.Promise<AggregateEntityTypesResponse<unit>>
      aggregateEntities: BlockProtocolMessage<AggregateEntities>
          -> JS.Promise<AggregateEntitiesResponse<AnyBlockProperty>> }
