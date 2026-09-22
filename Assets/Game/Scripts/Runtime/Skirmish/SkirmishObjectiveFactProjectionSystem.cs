using Game.Components;
using Game.Skirmish.Contracts;
using Unity.Collections;
using Unity.Entities;

namespace Game.Runtime
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SkirmishBaseAssaultObjectiveSystem))]
    public partial struct SkirmishObjectiveFactProjectionSystem : ISystem
    {
        private EntityQuery designatedBases;
        private EntityQuery structures;
        private EntityQuery combatUnits;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            designatedBases = state.GetEntityQuery(
                ComponentType.ReadOnly<SkirmishObjectiveRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
            structures = state.GetEntityQuery(ComponentType.ReadOnly<SkirmishStructureIdentityComponent>());
            combatUnits = state.GetEntityQuery(
                ComponentType.ReadOnly<SkirmishUnitRoleComponent>(),
                ComponentType.ReadOnly<UnitHealth>());
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityManager em = state.EntityManager;
            bool paused = SystemAPI.TryGetSingleton(out RuntimeGameplayStateComponent gameplay) &&
                          gameplay.SimulationActive == 0;
            float delta = paused ? 0f : SystemAPI.Time.DeltaTime;
            foreach ((RefRO<SkirmishExpandedSessionComponent> session,
                      RefRW<SkirmishObjectiveClockComponent> clock,
                      Entity entity) in
                     SystemAPI.Query<RefRO<SkirmishExpandedSessionComponent>,
                         RefRW<SkirmishObjectiveClockComponent>>().WithEntityAccess())
            {
                if (session.ValueRO.IsLegacy != 0)
                    continue;
                bool playing = session.ValueRO.Phase == SkirmishSessionPhase.Playing;
                clock.ValueRW.Playing = (byte)(playing ? 1 : 0);
                clock.ValueRW.Paused = (byte)(paused ? 1 : 0);
                if (clock.ValueRW.DeadlineSeconds <= 0 &&
                    em.HasComponent<SkirmishResolvedSetupComponent>(entity))
                {
                    int healed = em.GetComponentData<SkirmishResolvedSetupComponent>(entity).DeadlineSeconds;
                    if (healed > 0)
                        clock.ValueRW.DeadlineSeconds = healed;
                }

                if (playing && !paused)
                    clock.ValueRW.ElapsedSeconds += delta;

                SkirmishBaseAssaultFacts facts = Collect(
                    em,
                    designatedBases,
                    structures,
                    combatUnits);
                var component = new SkirmishBaseAssaultFactComponent
                {
                    PlayerDesignatedAlive = (byte)(facts.PlayerDesignatedAlive ? 1 : 0),
                    EnemyDesignatedAlive = (byte)(facts.EnemyDesignatedAlive ? 1 : 0),
                    ReplacementBarracksPresent = (byte)(facts.ReplacementBarracksPresent ? 1 : 0),
                    FieldArmyWiped = (byte)(facts.FieldArmyWiped ? 1 : 0)
                };
                if (em.HasComponent<SkirmishBaseAssaultFactComponent>(entity))
                    em.SetComponentData(entity, component);
                else
                    em.AddComponentData(entity, component);

                // Same system that advances the clock also publishes the terminal
                // fact. A live match was still Playing with elapsed past 1080 because
                // nothing copied that clock into the objective outcome.
                SkirmishBaseAssaultObjectiveSystem.TryPublishTerminal(em, entity);
            }
        }

        public static SkirmishBaseAssaultFacts Collect(
            EntityManager em,
            EntityQuery designated,
            EntityQuery structureQuery,
            EntityQuery combatQuery)
        {
            var facts = new SkirmishBaseAssaultFacts
            {
                PlayerDesignatedAlive = IsDesignatedAlive(em, designated, SkirmishObjectiveRoleKind.PlayerBase),
                EnemyDesignatedAlive = IsDesignatedAlive(em, designated, SkirmishObjectiveRoleKind.EnemyBase),
                ReplacementBarracksPresent = HasReplacementBarracks(em, structureQuery),
                FieldArmyWiped = IsArmyWiped(em, combatQuery)
            };
            return facts;
        }

        private static bool IsDesignatedAlive(EntityManager em, EntityQuery query, SkirmishObjectiveRoleKind role)
        {
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            bool found = false;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role != role)
                    continue;
                found = true;
                if (em.GetComponentData<UnitHealth>(entities[i]).Current > 0)
                    return true;
            }

            return !found;
        }

        private static bool HasReplacementBarracks(EntityManager em, EntityQuery query)
        {
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                SkirmishStructureIdentityComponent identity =
                    em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]);
                if (identity.DesignatedBase != 0)
                    continue;
                if (identity.StructureId.ToString().StartsWith(SkirmishStructureIds.Barracks))
                    return true;
            }

            return false;
        }

        private static bool IsArmyWiped(EntityManager em, EntityQuery query)
        {
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            bool any = false;
            for (int i = 0; i < entities.Length; i++)
            {
                any = true;
                if (em.GetComponentData<UnitHealth>(entities[i]).Current > 0)
                    return false;
            }

            return any;
        }
    }
}
