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
        private EntityQuery clockedSessions;
        private EntityQuery designatedBases;
        private EntityQuery structures;
        private EntityQuery combatUnits;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SkirmishExpandedSessionComponent>();
            clockedSessions = state.GetEntityQuery(
                ComponentType.ReadWrite<SkirmishExpandedSessionComponent>(),
                ComponentType.ReadWrite<SkirmishObjectiveClockComponent>());
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
            // ToEntityArray copies ids and ends the query. Idiomatic foreach keeps
            // structural changes illegal for the rest of OnUpdate, which is why
            // AddComponent after a foreach still threw in the harness.
            using NativeArray<Entity> sessions = clockedSessions.ToEntityArray(Allocator.Temp);
            var missingFacts = new NativeList<Entity>(Allocator.Temp);
            for (int i = 0; i < sessions.Length; i++)
            {
                Entity entity = sessions[i];
                SkirmishExpandedSessionComponent session = em.GetComponentData<SkirmishExpandedSessionComponent>(entity);
                if (session.IsLegacy != 0)
                    continue;
                SkirmishObjectiveClockComponent clock = em.GetComponentData<SkirmishObjectiveClockComponent>(entity);
                bool playing = session.Phase == SkirmishSessionPhase.Playing;
                clock.Playing = (byte)(playing ? 1 : 0);
                clock.Paused = (byte)(paused ? 1 : 0);
                if (clock.DeadlineSeconds <= 0 &&
                    em.HasComponent<SkirmishResolvedSetupComponent>(entity))
                {
                    int healed = em.GetComponentData<SkirmishResolvedSetupComponent>(entity).DeadlineSeconds;
                    if (healed > 0)
                        clock.DeadlineSeconds = healed;
                }

                if (playing && !paused)
                    clock.ElapsedSeconds += delta;
                em.SetComponentData(entity, clock);

                if (!em.HasComponent<SkirmishBaseAssaultFactComponent>(entity))
                    missingFacts.Add(entity);
            }

            for (int i = 0; i < missingFacts.Length; i++)
            {
                if (!em.HasComponent<SkirmishBaseAssaultFactComponent>(missingFacts[i]))
                    em.AddComponentData(missingFacts[i], new SkirmishBaseAssaultFactComponent());
            }

            missingFacts.Dispose();
            for (int i = 0; i < sessions.Length; i++)
            {
                Entity entity = sessions[i];
                if (em.GetComponentData<SkirmishExpandedSessionComponent>(entity).IsLegacy != 0)
                    continue;
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
                em.SetComponentData(entity, component);

                // Same system that advances the clock also publishes the terminal
                // fact. RulesSystem returns immediately for expanded sessions, so
                // this is what turns a passed deadline into an objective outcome.
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
                PlayerDesignatedAlive = IsDesignatedAlive(em, designated, structureQuery, SkirmishObjectiveRoleKind.PlayerBase),
                EnemyDesignatedAlive = IsDesignatedAlive(em, designated, structureQuery, SkirmishObjectiveRoleKind.EnemyBase),
                ReplacementBarracksPresent = HasReplacementBarracks(em, structureQuery),
                FieldArmyWiped = IsArmyWiped(em, combatQuery)
            };
            return facts;
        }

        private static bool IsDesignatedAlive(
            EntityManager em,
            EntityQuery roleQuery,
            EntityQuery structureQuery,
            SkirmishObjectiveRoleKind role)
        {
            using NativeArray<Entity> entities = roleQuery.ToEntityArray(Allocator.Temp);
            bool found = false;
            for (int i = 0; i < entities.Length; i++)
            {
                if (em.GetComponentData<SkirmishObjectiveRoleComponent>(entities[i]).Role != role)
                    continue;
                found = true;
                if (em.GetComponentData<UnitHealth>(entities[i]).Current > 0)
                    return true;
            }

            if (found)
                return false;

            // No role+health pair. A designated barracks at 0 HP must still count
            // as dead. An unspawned base (no structure yet) stays alive so the
            // match does not Draw before roster projection.
            return !DesignatedStructureIsDead(em, structureQuery, role);
        }

        private static bool DesignatedStructureIsDead(
            EntityManager em,
            EntityQuery structureQuery,
            SkirmishObjectiveRoleKind role)
        {
            byte faction = role == SkirmishObjectiveRoleKind.PlayerBase ? (byte)1 : (byte)2;
            using NativeArray<Entity> entities = structureQuery.ToEntityArray(Allocator.Temp);
            bool sawHealth = false;
            for (int i = 0; i < entities.Length; i++)
            {
                if (!em.HasComponent<SkirmishStructureIdentityComponent>(entities[i]) ||
                    !em.HasComponent<SkirmishAttemptOwnedComponent>(entities[i]))
                    continue;
                if (em.GetComponentData<SkirmishStructureIdentityComponent>(entities[i]).DesignatedBase == 0)
                    continue;
                if (em.GetComponentData<SkirmishAttemptOwnedComponent>(entities[i]).FactionId != faction)
                    continue;
                if (!em.HasComponent<UnitHealth>(entities[i]))
                    return false;
                sawHealth = true;
                if (em.GetComponentData<UnitHealth>(entities[i]).Current > 0)
                    return false;
            }

            return sawHealth;
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
