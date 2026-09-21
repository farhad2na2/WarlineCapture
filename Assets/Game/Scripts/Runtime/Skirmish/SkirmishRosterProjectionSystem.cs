using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SkirmishScenarioSpawnSystem))]
    public partial struct SkirmishRosterProjectionSystem : ISystem
    {
        private EntityQuery ownedQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            ownedQuery = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishAttemptOwnedComponent>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session, Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0 || session.ValueRO.SpawnComplete == 0)
                    continue;
                if (!em.HasComponent<SkirmishResolvedSetupRecord>(entity))
                    continue;
                SkirmishResolvedSetup setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(entity).Setup;
                Apply(em, ownedQuery, session.ValueRO.SessionId, setup);
            }
        }

        public static int Apply(
            EntityManager em,
            EntityQuery owned,
            FixedString64Bytes sessionId,
            SkirmishResolvedSetup setup)
        {
            SkirmishRoleOverlay[] overlays = setup != null && setup.RoleOverlays != null && setup.RoleOverlays.Length > 0
                ? setup.RoleOverlays
                : SkirmishRoleOverlayCatalog.CreateS002GroundSlice();
            using NativeArray<Entity> entities = owned.ToEntityArray(Allocator.Temp);
            int applied = 0;
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                if (!em.HasComponent<SkirmishAttemptOwnedComponent>(entity))
                    continue;
                if (!em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(sessionId))
                    continue;
                if (em.HasComponent<SkirmishRoleOverlayComponent>(entity) &&
                    em.GetComponentData<SkirmishRoleOverlayComponent>(entity).Applied != 0)
                    continue;

                if (em.HasComponent<SkirmishUnitRoleComponent>(entity))
                {
                    SkirmishUnitRoleComponent role = em.GetComponentData<SkirmishUnitRoleComponent>(entity);
                    if (!SkirmishRoleOverlayCatalog.TryGet(overlays, role.Role, out SkirmishRoleOverlay overlay))
                        continue;
                    WriteOverlay(em, entity, overlay);
                    applied++;
                    continue;
                }

                if (em.HasComponent<SkirmishStructureIdentityComponent>(entity))
                {
                    string structureId = em.GetComponentData<SkirmishStructureIdentityComponent>(entity)
                        .StructureId.ToString();
                    if (structureId.StartsWith(SkirmishStructureIds.Barracks))
                    {
                        WriteOverlay(em, entity, SkirmishRoleOverlayCatalog.BarracksStructure());
                        applied++;
                    }
                    else if (structureId == SkirmishStructureIds.GroundStaging)
                    {
                        WriteOverlay(em, entity, SkirmishRoleOverlayCatalog.GroundStagingStructure());
                        applied++;
                    }
                }
            }

            return applied;
        }

        private static void WriteOverlay(EntityManager em, Entity entity, SkirmishRoleOverlay overlay)
        {
            var component = new SkirmishRoleOverlayComponent
            {
                Role = overlay.RoleKind,
                MaxHealth = overlay.MaxHealth,
                Damage = overlay.Damage,
                RangeWorld = overlay.RangeWorld,
                Producer = overlay.Producer,
                TargetDomains = overlay.TargetDomains,
                Applied = 1
            };
            if (em.HasComponent<SkirmishRoleOverlayComponent>(entity))
                em.SetComponentData(entity, component);
            else
                em.AddComponentData(entity, component);

            var health = new UnitHealth { Current = overlay.MaxHealth, Max = overlay.MaxHealth };
            if (em.HasComponent<UnitHealth>(entity))
                em.SetComponentData(entity, health);
            else
                em.AddComponentData(entity, health);
        }
    }
}
