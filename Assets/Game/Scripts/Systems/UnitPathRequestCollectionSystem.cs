using Unity.Collections;
using Unity.Entities;

internal struct UnitPathRequestCollectionSystem
{
    public int Collect(
        ref SystemState state,
        ref UnitPathfindingQuerySystem queries,
        ref UnitPathRequestBufferSystem requestBuffers,
        ref UnitPathIgnoredOccupancySystem ignoredOccupancy,
        int requestBudget)
    {
        requestBuffers.ClearForCollection();

        EntityTypeHandle entityType = state.GetEntityTypeHandle();
        ComponentTypeHandle<UnitGrid> unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
        ComponentTypeHandle<UnitPathRequest> requestType = state.GetComponentTypeHandle<UnitPathRequest>(true);
        ComponentTypeHandle<UnitFootprint> footprintType = state.GetComponentTypeHandle<UnitFootprint>(true);
        ComponentTypeHandle<UnitMovementBehavior> movementBehaviorType = state.GetComponentTypeHandle<UnitMovementBehavior>(true);
        ComponentTypeHandle<Faction> factionType = state.GetComponentTypeHandle<Faction>(true);
        ComponentLookup<UnitLongDistanceMove> longDistanceLookup = state.GetComponentLookup<UnitLongDistanceMove>(true);
        ComponentLookup<ManualMoveOrderTag> manualMoveLookup = state.GetComponentLookup<ManualMoveOrderTag>(true);

        CollectFromQuery(
            ref state,
            queries.ManualRequestQuery,
            ref requestBuffers,
            ref ignoredOccupancy,
            requestBudget,
            entityType,
            unitGridType,
            requestType,
            footprintType,
            movementBehaviorType,
            factionType,
            longDistanceLookup,
            manualMoveLookup,
            manualMove: true,
            skipManualMoveEntities: false);

        if (requestBuffers.Entities.Length < requestBudget)
        {
            CollectFromQuery(
                ref state,
                queries.RequestQuery,
                ref requestBuffers,
                ref ignoredOccupancy,
                requestBudget,
                entityType,
                unitGridType,
                requestType,
                footprintType,
                movementBehaviorType,
                factionType,
                longDistanceLookup,
                manualMoveLookup,
                manualMove: false,
                skipManualMoveEntities: true);
        }

        return requestBuffers.Entities.Length;
    }

    private static void CollectFromQuery(
        ref SystemState state,
        EntityQuery query,
        ref UnitPathRequestBufferSystem requestBuffers,
        ref UnitPathIgnoredOccupancySystem ignoredOccupancy,
        int requestBudget,
        EntityTypeHandle entityType,
        ComponentTypeHandle<UnitGrid> unitGridType,
        ComponentTypeHandle<UnitPathRequest> requestType,
        ComponentTypeHandle<UnitFootprint> footprintType,
        ComponentTypeHandle<UnitMovementBehavior> movementBehaviorType,
        ComponentTypeHandle<Faction> factionType,
        ComponentLookup<UnitLongDistanceMove> longDistanceLookup,
        ComponentLookup<ManualMoveOrderTag> manualMoveLookup,
        bool manualMove,
        bool skipManualMoveEntities)
    {
        using NativeArray<ArchetypeChunk> chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            ArchetypeChunk chunk = chunks[chunkIndex];
            NativeArray<Entity> entities = chunk.GetNativeArray(entityType);
            NativeArray<UnitGrid> unitGrids = chunk.GetNativeArray(ref unitGridType);
            NativeArray<UnitPathRequest> requests = chunk.GetNativeArray(ref requestType);
            NativeArray<UnitFootprint> footprints = chunk.GetNativeArray(ref footprintType);
            NativeArray<UnitMovementBehavior> movementBehaviors = chunk.GetNativeArray(ref movementBehaviorType);
            NativeArray<Faction> factions = chunk.GetNativeArray(ref factionType);

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (skipManualMoveEntities && manualMoveLookup.HasComponent(entity))
                    continue;

                requestBuffers.Entities.Add(entity);
                requestBuffers.UnitGrids.Add(unitGrids[i]);
                requestBuffers.Goals.Add(requests[i]);
                requestBuffers.Footprints.Add(footprints[i]);
                requestBuffers.MovementBehaviors.Add(movementBehaviors[i]);
                requestBuffers.Factions.Add(factions[i].Id);
                requestBuffers.ManualMoves.Add((byte)(manualMove ? 1 : 0));
                ignoredOccupancy.AddForRequest(ref state, ref requestBuffers, entity);
                requestBuffers.ContinuationMoves.Add((byte)(longDistanceLookup.HasComponent(entity) ? 1 : 0));

                if (requestBuffers.Entities.Length >= requestBudget)
                    return;
            }
        }
    }
}
