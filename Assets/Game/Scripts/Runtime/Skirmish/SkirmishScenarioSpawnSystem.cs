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
                if (!TrySubmitStartingForce(em, entity, setup, out SkirmishReasonCode reason))
                {
                    DestroyAttemptOwned(em, session.SessionId);
                    session.FailureCode = reason;
                    session.Phase = SkirmishSessionPhase.Failed;
                    em.SetComponentData(entity, session);
                    continue;
                }

                session.SpawnComplete = 1;
                session.Phase = SkirmishSessionPhase.Playing;
                if (em.HasComponent<SkirmishObjectiveStateComponent>(entity))
                {
                    var objective = em.GetComponentData<SkirmishObjectiveStateComponent>(entity);
                    objective.State = SkirmishObjectiveStateKind.Active;
                    em.SetComponentData(entity, objective);
                }

                em.SetComponentData(entity, session);
            }
        }

        private static bool TrySubmitStartingForce(
            EntityManager em,
            Entity session,
            SkirmishResolvedSetup setup,
            out SkirmishReasonCode reason)
        {
            reason = SkirmishReasonCode.None;
            if (setup?.Forces == null)
            {
                reason = SkirmishReasonCode.MissingResolvedSetup;
                return false;
            }

            using EntityQuery registryQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            bool registryReady = !registryQuery.IsEmptyIgnoreFilter;
            for (int i = 0; i < setup.Forces.Length; i++)
            {
                SkirmishResolvedForceEntry force = setup.Forces[i];
                var owned = em.CreateEntity();
                em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
                {
                    SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                    StableObjectId = new FixedString64Bytes(force.RoleId + "." + force.FactionId + "." + i),
                    FactionId = force.FactionId,
                    IsStructure = 0
                });
                em.AddComponentData(owned, new SkirmishUnitRoleComponent
                {
                    Role = force.RoleKind,
                    Category = SkirmishRoleIds.Category(force.RoleKind),
                    SupplyCost = force.SupplyCost
                });
            }

            for (int i = 0; i < setup.Structures.Length; i++)
            {
                SkirmishResolvedStructureEntry structure = setup.Structures[i];
                var owned = em.CreateEntity();
                em.AddComponentData(owned, new SkirmishAttemptOwnedComponent
                {
                    SessionId = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId,
                    StableObjectId = new FixedString64Bytes(structure.StructureId + "." + structure.FactionId),
                    FactionId = structure.FactionId,
                    IsStructure = 1
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
            }

            if (!registryReady)
            {
                reason = SkirmishReasonCode.SpawnBoundaryUnavailable;
                return false;
            }

            return true;
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
