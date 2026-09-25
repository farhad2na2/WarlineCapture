using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishScenarioSpawnSystem))]
    public partial struct SkirmishNativeProductionSystem : ISystem
    {
        public void OnCreate(ref SystemState state) => state.RequireForUpdate<SkirmishProductionCatalogRecord>();
        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            if (!SkirmishNativeProduction.TrySession(em, out var session)) return;
            var expanded = em.GetComponentData<SkirmishExpandedSessionComponent>(session);
            if (expanded.SpawnComplete == 0 || expanded.Phase != SkirmishSessionPhase.Playing) return;
            using var query = em.CreateEntityQuery(new EntityQueryDesc {
                All = new[] { ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(), ComponentType.ReadOnly<UnitHealth>(),
                    ComponentType.ReadOnly<UnitSourcePrefabKey>() },
                None = new[] { ComponentType.ReadOnly<OperationMapBuildingComponent>() }
            });
            using var buildings = query.ToEntityArray(Allocator.Temp);
            foreach (var building in buildings)
            {
                var info = em.GetComponentData<RuntimeBuildingCombatInfo>(building);
                if ((info.OwnerFactionId != 1 && info.OwnerFactionId != 2) || em.GetComponentData<UnitHealth>(building).Current <= 0 ||
                    em.HasComponent<SkirmishAttemptOwnedComponent>(building)) continue;
                string id = SkirmishStructureIds.FromVisualKey(em.GetComponentData<UnitSourcePrefabKey>(building).Value.ToString());
                if (string.IsNullOrEmpty(id)) continue;
                SkirmishScenarioSpawnSystem.BindStructure(em, building, expanded.SessionId, new SkirmishResolvedStructureEntry {
                    StructureId = id, FactionId = info.OwnerFactionId
                });
                var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(building);
                owner.StableObjectId = new FixedString64Bytes("constructed." + info.RuntimeBuildingId);
                em.SetComponentData(building, owner);
                if (!em.HasComponent<SkirmishSharedActorTag>(building)) em.AddComponent<SkirmishSharedActorTag>(building);
            }
            if (!em.HasBuffer<SkirmishProductionReservation>(session)) return;
            using var receipts = em.GetBuffer<SkirmishProductionReservation>(session).ToNativeArray(Allocator.Temp);
            foreach (var receipt in receipts)
            {
                if (receipt.ProducerRuntimeId == 0 ||
                    (receipt.Phase != SkirmishReservationPhase.Reserved && receipt.Phase != SkirmishReservationPhase.Producing)) continue;
                bool alive = false;
                foreach (var building in buildings)
                {
                    var info = em.GetComponentData<RuntimeBuildingCombatInfo>(building);
                    if (info.RuntimeBuildingId == receipt.ProducerRuntimeId && info.OwnerFactionId == receipt.FactionId &&
                        em.HasComponent<SkirmishAttemptOwnedComponent>(building) &&
                        em.GetComponentData<SkirmishAttemptOwnedComponent>(building).SessionId.Equals(expanded.SessionId) &&
                        em.GetComponentData<UnitHealth>(building).Current > 0) { alive = true; break; }
                }
                if (!alive) SkirmishProductionService.NotifyProducerDestroyed(em, session,
                    receipt.Producer, receipt.FactionId, receipt.ProducerRuntimeId);
            }
        }
    }
}
