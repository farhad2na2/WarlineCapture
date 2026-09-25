using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Runtime
{
    public struct SkirmishStartingBuildingRequest : IBufferElementData
    {
        public int StructureIndex;
        public int RequestId;
        public Entity BoundEntity;
    }
    public struct SkirmishSharedBuildingsReady : IComponentData { }

    /// <summary>Submits authored starting grants to the shared placement/spawn owner.
    /// A mission cannot start until every result has been bound to its original objective identity.</summary>
    public static class SkirmishStartingBuildingsService
    {
        public static bool Step(EntityManager em, Entity session, SkirmishResolvedSetup setup, out SkirmishReasonCode reason)
        {
            reason = SkirmishReasonCode.None;
            if (em.HasComponent<SkirmishSharedBuildingsReady>(session)) return true;
            using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeStateTag), typeof(BuildingConfiguredSpawnableReadModel));
            using var grids = em.CreateEntityQuery(typeof(GridConfig));
            if (boundaries.CalculateEntityCount() != 1 || grids.CalculateEntityCount() != 1) return false;
            Entity boundary = boundaries.GetSingletonEntity();
            GridConfig grid = grids.GetSingleton<GridConfig>();
            if (!em.HasBuffer<BuildingRuntimeSpawnRequest>(boundary)) return false;
            if (!em.HasBuffer<SkirmishStartingBuildingRequest>(session))
            {
                // Preflight all keys before submitting any grants.
                var definitions = em.GetBuffer<BuildingConfiguredSpawnableReadModel>(boundary);
                if (definitions.Length == 0) return false;
                int count = setup.Structures?.Length ?? 0;
                var footprints = new int2[count];
                for (int i = 0; i < count; i++)
                {
                    string key = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(
                        SkirmishStructureIds.VisualKey(setup.Structures[i].StructureId));
                    for (int j = 0; j < definitions.Length; j++)
                        if (BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(definitions[j].BuildingId.ToString()) == key)
                            footprints[i] = definitions[j].FootprintCells;
                    if (footprints[i].x <= 0 || footprints[i].y <= 0)
                    { reason = SkirmishReasonCode.MissingReference; return false; }
                }
                em.AddComponent<BuildingStartingGrantOwner>(session);
                em.AddBuffer<SkirmishStartingBuildingRequest>(session);
                var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
                int next = 0;
                for (int i = 0; i < requests.Length; i++) next = math.max(next, requests[i].RequestId);
                for (int i = 0; i < count; i++)
                {
                    var structure = setup.Structures[i];
                    var center = GridUtils.WorldToCell(grid, new float3(structure.SpawnWorldX, 0f, structure.SpawnWorldZ));
                    requests.Add(new BuildingRuntimeSpawnRequest
                    {
                        RequestId = ++next, RequestKind = BuildingRuntimeSpawnRequest.KindBuilding,
                        BuildingId = new FixedString128Bytes(SkirmishStructureIds.VisualKey(structure.StructureId)),
                        FactionId = structure.FactionId, HasOwnerFaction = 1,
                        AuthoredStartingGrant = 1, PlanEntity = session, EntryIndex = i,
                        PreferredOrigin = center - footprints[i] / 2,
                        // The shared placement owner finds the nearest legal footprint around this anchor.
                        RequirePreferredOrigin = 0, Status = BuildingRuntimeSpawnRequest.Pending
                    });
                    em.GetBuffer<SkirmishStartingBuildingRequest>(session).Add(new SkirmishStartingBuildingRequest
                    { StructureIndex = i, RequestId = next });
                }
                return false;
            }
            var pending = em.GetBuffer<SkirmishStartingBuildingRequest>(session).ToNativeArray(Allocator.Temp);
            try
            {
                bool complete = true;
                var shared = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary).ToNativeArray(Allocator.Temp);
                try
                {
                    for (int i = 0; i < pending.Length; i++)
                    {
                        var item = pending[i];
                        if (item.BoundEntity != Entity.Null && em.Exists(item.BoundEntity)) continue;
                        BuildingRuntimeSpawnRequest result = default;
                        bool found = false;
                        for (int j = 0; j < shared.Length; j++)
                            if (shared[j].RequestId == item.RequestId) { result = shared[j]; found = true; break; }
                        if (!found) { reason = SkirmishReasonCode.SpawnBoundaryUnavailable; complete = false; continue; }
                        if (result.Status == BuildingRuntimeSpawnRequest.Failed)
                        { reason = SkirmishReasonCode.BlockedSpawn; complete = false; continue; }
                        if (result.Status == BuildingRuntimeSpawnRequest.Pending) { complete = false; continue; }
                        Entity building = FindBuilding(em, result.BuildingRuntimeId);
                        if (building == Entity.Null) { complete = false; continue; }
                        var id = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                        SkirmishScenarioSpawnSystem.BindStructure(em, building, id, setup.Structures[item.StructureIndex]);
                        em.AddComponentData(building, new SkirmishVisualSpawnedComponent { Spawned = 1, FromRegistry = 1 });
                        em.AddComponent<SkirmishSharedActorTag>(building);
                        item.BoundEntity = building;
                        var bindings = em.GetBuffer<SkirmishStartingBuildingRequest>(session);
                        bindings[i] = item;
                    }
                }
                finally { shared.Dispose(); }
                if (!complete) return false;
                em.AddComponent<SkirmishSharedBuildingsReady>(session);
                // The completed requests can no longer grant anything after a replay/return.
                em.RemoveComponent<BuildingStartingGrantOwner>(session);
                return true;
            }
            finally { pending.Dispose(); }
        }

        public static void Cancel(EntityManager em, Entity session)
        {
            em.RemoveComponent<BuildingStartingGrantOwner>(session);
            using var boundaries = em.CreateEntityQuery(typeof(BuildingRuntimeSpawnRequest));
            using var entities = boundaries.ToEntityArray(Allocator.Temp);
            for (int b = 0; b < entities.Length; b++)
            {
                var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(entities[b]);
                for (int i = 0; i < requests.Length; i++)
                {
                    var request = requests[i];
                    if (request.AuthoredStartingGrant == 0 || request.PlanEntity != session) continue;
                    if (request.Status == BuildingRuntimeSpawnRequest.Pending)
                    {
                        request.Status = BuildingRuntimeSpawnRequest.Failed;
                        request.ResultCode = BuildingRuntimeSpawnRequest.Blocked;
                        requests[i] = request;
                    }
                    else if (request.Status == BuildingRuntimeSpawnRequest.Succeeded)
                    {
                        Entity building = FindBuilding(em, request.BuildingRuntimeId);
                        if (building == Entity.Null || !em.HasComponent<UnitHealth>(building)) continue;
                        var health = em.GetComponentData<UnitHealth>(building);
                        health.Current = 0;
                        em.SetComponentData(building, health);
                    }
                }
            }
        }

        private static Entity FindBuilding(EntityManager em, int runtimeId)
        {
            // Streamed map buildings use placementIndex + 1; the runtime owner has
            // its own ID sequence. A runtime result must never claim a map entity
            // that happens to carry the same numeric ID.
            using var query = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>() },
                None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
            });
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
                if (em.GetComponentData<RuntimeBuildingCombatInfo>(entities[i]).RuntimeBuildingId == runtimeId)
                    return entities[i];
            return Entity.Null;
        }
    }
}
