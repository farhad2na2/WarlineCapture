using Unity.Collections;
using Unity.Entities;
using Game.Components;

namespace Game.Runtime
{
    internal struct UnitPathRequestCollection
    {
        private EntityTypeHandle _entityType;
        private ComponentTypeHandle<UnitGrid> _unitGridType;
        private ComponentTypeHandle<UnitPathRequest> _requestType;
        private ComponentTypeHandle<UnitFootprint> _footprintType;
        private ComponentTypeHandle<UnitMovementBehavior> _movementBehaviorType;
        private ComponentTypeHandle<Faction> _factionType;
        private ComponentLookup<UnitLongDistanceMove> _longDistanceLookup;
        private ComponentLookup<ManualMoveOrderTag> _manualMoveLookup;

        public void Initialize(ref SystemState state)
        {
            _entityType = state.GetEntityTypeHandle();
            _unitGridType = state.GetComponentTypeHandle<UnitGrid>(true);
            _requestType = state.GetComponentTypeHandle<UnitPathRequest>(true);
            _footprintType = state.GetComponentTypeHandle<UnitFootprint>(true);
            _movementBehaviorType = state.GetComponentTypeHandle<UnitMovementBehavior>(true);
            _factionType = state.GetComponentTypeHandle<Faction>(true);
            _longDistanceLookup = state.GetComponentLookup<UnitLongDistanceMove>(true);
            _manualMoveLookup = state.GetComponentLookup<ManualMoveOrderTag>(true);
        }

        public int Collect(
            ref SystemState state,
            ref UnitPathfindingEntitySets queries,
            ref UnitPathRequestBuffer requestBuffers,
            ref UnitPathIgnoredOccupancy ignoredOccupancy,
            int requestBudget)
        {
            requestBuffers.ClearForCollection();
            Update(ref state);

            CollectFromQuery(
                ref state,
                queries.ManualRequestQuery,
                ref requestBuffers,
                ref ignoredOccupancy,
                requestBudget,
                _entityType,
                _unitGridType,
                _requestType,
                _footprintType,
                _movementBehaviorType,
                _factionType,
                _longDistanceLookup,
                _manualMoveLookup,
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
                    _entityType,
                    _unitGridType,
                    _requestType,
                    _footprintType,
                    _movementBehaviorType,
                    _factionType,
                    _longDistanceLookup,
                    _manualMoveLookup,
                    manualMove: false,
                    skipManualMoveEntities: true);
            }

            return requestBuffers.Entities.Length;
        }

        private void Update(ref SystemState state)
        {
            _entityType.Update(ref state);
            _unitGridType.Update(ref state);
            _requestType.Update(ref state);
            _footprintType.Update(ref state);
            _movementBehaviorType.Update(ref state);
            _factionType.Update(ref state);
            _longDistanceLookup.Update(ref state);
            _manualMoveLookup.Update(ref state);
        }

        private static void CollectFromQuery(
            ref SystemState state,
            EntityQuery query,
            ref UnitPathRequestBuffer requestBuffers,
            ref UnitPathIgnoredOccupancy ignoredOccupancy,
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
}
