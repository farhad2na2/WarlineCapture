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

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            bool registryReady = !registryQuery.IsEmptyIgnoreFilter;
            visualPending = (byte)(registryReady ? 0 : 1);
            FixedString64Bytes sessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;

            for (int i = 0; i < setup.Forces.Length; i++)
            {
                SkirmishResolvedForceEntry force = setup.Forces[i];
                int quantity = force.Quantity < 1 ? 1 : force.Quantity;
                int perMemberSupply = quantity == 0 ? 0 : force.SupplyCost / quantity;
                for (int member = 0; member < quantity; member++)
                {
                    var owned = em.CreateEntity();
                    em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
                    {
                        SessionId = sessionId,
                        StableObjectId = new FixedString64Bytes(force.RoleId + "." + force.FactionId + "." + i + "." + member),
                        FactionId = force.FactionId,
                        IsStructure = 0
                    });
                    em.AddComponentData(owned, new SkirmishUnitRoleComponent
                    {
                        Role = force.RoleKind,
                        Category = SkirmishRoleIds.Category(force.RoleKind),
                        SupplyCost = perMemberSupply
                    });
                }
            }

            if (setup.Structures != null)
            {
                for (int i = 0; i < setup.Structures.Length; i++)
                    CreateStructure(em, sessionId, setup.Structures[i]);
            }

            return true;
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
