using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishSessionInitializationSystem))]
    public partial struct SkirmishScenarioSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRW<SkirmishExpandedSessionComponent> sessionRef, Entity entity) in
                     SystemAPI.Query<RefRW<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                ref SkirmishExpandedSessionComponent session = ref sessionRef.ValueRW;
                if (session.IsLegacy != 0 || session.InitializationComplete == 0 || session.SpawnComplete != 0)
                    continue;
                if (session.Phase == SkirmishSessionPhase.Failed || session.Phase == SkirmishSessionPhase.Finished)
                    continue;
                if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                {
                    session.FailureCode = SkirmishReasonCode.MissingResolvedSetup;
                    session.Phase = SkirmishSessionPhase.Failed;
                    em.SetComponentData(entity, session);
                    continue;
                }

                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
                if (!TrySpawnLedgers(em, entity, setup, out SkirmishReasonCode reason, out byte visualPending))
                {
                    DestroyAttemptOwned(em, session.SessionId);
                    session.FailureCode = reason;
                    session.Phase = SkirmishSessionPhase.Failed;
                    em.SetComponentData(entity, session);
                    continue;
                }

                session.SpawnComplete = 1;
                session.SpawnVisualPending = visualPending;
                session.Phase = SkirmishSessionPhase.Playing;
                if (em.HasComponent<SkirmishObjectiveStateComponent>(entity))
                {
                    var objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                    objective.State = SkirmishObjectiveStateKind.Active;
                    em.SetComponentData(entity, objective);
                }

                if (!em.HasComponent<SkirmishObjectiveClockComponent>(entity))
                {
                    em.AddComponentData(entity, new SkirmishObjectiveClockComponent
                    {
                        ElapsedSeconds = 0f,
                        DeadlineSeconds = setup.DeadlineSeconds,
                        Paused = 0,
                        Playing = 1
                    });
                }

                em.SetComponentData(entity, session);
            }
        }

        public static bool TrySpawnLedgers(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            out SkirmishReasonCode reason,
            out byte visualPending)
        {
            reason = SkirmishReasonCode.None;
            visualPending = 0;
            if (setup?.Forces == null)
            {
                reason = SkirmishReasonCode.MissingResolvedSetup;
                return false;
            }

            if (!em.HasComponent<SkirmishCapacityComponent>(session))
            {
                em.AddComponentData(session, new SkirmishCapacityComponent
                {
                    FactionId = 1,
                    InfantryCap = setup.InfantryCapEach,
                    GroundCap = setup.GroundCapEach,
                    AirCap = setup.TacticalAirCapEach,
                    SupplyCap = setup.SupplyCapEach
                });
            }

            if (!em.HasComponent<SkirmishEconomyStockComponent>(session))
            {
                em.AddComponentData(session, new SkirmishEconomyStockComponent
                {
                    FactionId = 1,
                    Materials = setup.MaterialsEach,
                    Oil = setup.OilEach,
                    Fuel = setup.UsableFuelEach,
                    MaterialsCapacity = setup.MaterialsCapacityEach,
                    OilCapacity = setup.OilCapacityEach,
                    FuelCapacity = setup.FuelCapacityEach
                });
            }

            if (!em.HasBuffer<SkirmishProductionReservation>(session))
                em.AddBuffer<SkirmishProductionReservation>(session);

            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            bool startingSetBound = true;
            int infantryLive = 0;
            int groundLive = 0;
            int airLive = 0;
            int supplyLive = 0;

            for (int i = 0; i < setup.Forces.Length; i++)
            {
                SkirmishResolvedForceEntry force = setup.Forces[i];
                int quantity = force.Quantity < 1 ? 1 : force.Quantity;
                int perMemberSupply = quantity == 0 ? 0 : force.SupplyCost / quantity;
                string prefabKey = force.RuntimePrefabKey ?? string.Empty;
                if (string.IsNullOrEmpty(prefabKey))
                    startingSetBound = false;
                for (int member = 0; member < quantity; member++)
                {
                    CreateForceMember(
                        em,
                        sessionId,
                        force,
                        i,
                        member,
                        perMemberSupply,
                        prefabKey);
                    if (force.FactionId == 1)
                    {
                        SkirmishPopulationCategory category = SkirmishRoleIds.Category(force.RoleKind);
                        if (category == SkirmishPopulationCategory.Infantry)
                            infantryLive++;
                        else if (category == SkirmishPopulationCategory.Ground)
                            groundLive++;
                        else if (category == SkirmishPopulationCategory.Air)
                            airLive++;
                        if (category == SkirmishPopulationCategory.Infantry ||
                            category == SkirmishPopulationCategory.Ground ||
                            category == SkirmishPopulationCategory.Air)
                            supplyLive += perMemberSupply;
                    }
                }
            }

            if (setup.Structures != null)
            {
                for (int i = 0; i < setup.Structures.Length; i++)
                {
                    if (string.IsNullOrEmpty(SkirmishStructureIds.VisualKey(setup.Structures[i].StructureId)))
                        startingSetBound = false;
                    CreateStructure(em, sessionId, setup.Structures[i]);
                }
            }

            visualPending = (byte)(startingSetBound ? 0 : 1);
            if (em.HasComponent<SkirmishCapacityComponent>(session))
            {
                SkirmishCapacityComponent capacity = em.GetComponentData<SkirmishCapacityComponent>(session);
                var snapshot = SkirmishCapacityLedger.ToSnapshot(capacity);
                SkirmishCapacityLedger.SeedLive(ref snapshot, infantryLive, groundLive, airLive, supplyLive);
                em.SetComponentData(session, SkirmishCapacityLedger.FromSnapshot(capacity, snapshot));
            }

            return true;
        }

        public static Entity CreateForceMember(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishResolvedForceEntry force,
            int forceIndex,
            int member,
            int perMemberSupply,
            string prefabKey)
        {
            var owned = em.CreateEntity();
            em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new FixedString64Bytes(force.RoleId + "." + force.FactionId + "." + forceIndex + "." + member),
                FactionId = force.FactionId,
                IsStructure = 0
            });
            em.AddComponentData(owned, new SkirmishUnitRoleComponent
            {
                Role = force.RoleKind,
                Category = SkirmishRoleIds.Category(force.RoleKind),
                SupplyCost = perMemberSupply
            });
            em.AddComponentData(owned, new Faction { Id = force.FactionId });
            if (!string.IsNullOrEmpty(prefabKey))
                em.AddComponentData(owned, new UnitSourcePrefabKey { Value = new FixedString64Bytes(prefabKey) });
            return owned;
        }

        public static Entity CreateStructure(
            EntityManager em,
            FixedString64Bytes sessionId,
            SkirmishResolvedStructureEntry structure)
        {
            var owned = em.CreateEntity();
            em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
            {
                SessionId = sessionId,
                StableObjectId = new FixedString64Bytes(structure.StructureId + "." + structure.FactionId),
                FactionId = structure.FactionId,
                IsStructure = 1
            });
            em.AddComponentData(owned, new SkirmishStructureIdentityComponent
            {
                StructureId = new FixedString64Bytes(structure.StructureId ?? string.Empty),
                Producer = structure.StructureId == SkirmishStructureIds.GroundStaging
                    ? SkirmishProducerKind.GroundStaging
                    : structure.StructureId != null && structure.StructureId.StartsWith(SkirmishStructureIds.Barracks)
                        ? SkirmishProducerKind.Barracks
                        : SkirmishProducerKind.None,
                DesignatedBase = (byte)(structure.DesignatedBase ? 1 : 0)
            });
            if (structure.DesignatedBase)
            {
                em.AddComponentData(owned, new SkirmishObjectiveRoleComponent
                {
                    Role = structure.FactionId == 1
                        ? SkirmishObjectiveRoleKind.PlayerBase
                        : SkirmishObjectiveRoleKind.EnemyBase,
                    StableObjectId = new FixedString64Bytes(structure.ObjectiveRoleId),
                    FactionId = structure.FactionId
                });
            }

            em.AddComponentData(owned, new Faction { Id = structure.FactionId });
            string visual = SkirmishStructureIds.VisualKey(structure.StructureId);
            if (!string.IsNullOrEmpty(visual))
                em.AddComponentData(owned, new UnitSourcePrefabKey { Value = new FixedString64Bytes(visual) });

            return owned;
        }

        internal static void DestroyAttemptOwned(EntityManager em, FixedString64Bytes sessionId)
        {
            using EntityQuery query = em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).SessionId.Equals(sessionId))
                    em.DestroyEntity(entities[i]);
            }
        }
    }
}
