#if WARLINE_VALIDATION_PLAYER
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Unity.Entities;
using UnityEngine;
namespace Game.Composition
{
    // Explicit build-only adapter to the SAME launch calls used by the native harness.
    // It never constructs actors, updates systems, forces readiness or sets results.
    public static class SkirmishValidationPlayerLaunchBridge
    {
        public static bool ShellReady(EntityManager em) => SkirmishLaunchProjection.IsShellReadyToEnterMatch(em);
        public static bool QueueS002(EntityManager em, int seed)
        {
            if (SkirmishLaunchProjection.TryGet(em, out _, out _)) return false;
            var config = QuickGameConfig.Defaults;
            config.ScenarioIndex = SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex;
            config.MapSeed = seed;
            return SkirmishLaunchProjection.TryQueue(em, config);
        }
        public static void AdvanceExistingLaunch(EntityManager em)
        {
            SkirmishLaunchProjection.TryEnterMatch(em);
            SkirmishLaunchProjection.TryBeginGameplayIfLoaded();
            SkirmishLaunchProjection.TryRequestPlay(em);
            SkirmishLaunchProjection.TryArmExpandedSimulation(em);
        }
        public static bool TryRead(EntityManager em, out Entity session, out SkirmishMatchState match)
            => SkirmishLaunchProjection.TryGet(em, out session, out match);
        public static bool SimulationActive(EntityManager em) => SkirmishLaunchProjection.IsSimulationActive(em);
        public static float Elapsed(EntityManager em, Entity session, in SkirmishMatchState match)
            => SkirmishLaunchProjection.ReadMatchElapsedSeconds(em, session, in match);
        public static bool StartupRosterReady(EntityManager em, Entity session, int seed, out string facts)
        {
            facts = "resolvedSetupMissing";
            if (!em.HasComponent<SkirmishResolvedSetupRecord>(session)) return false;
            var setup = em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup;
            if (setup.CatalogId != "S002" || setup.Seed != seed)
                throw new System.InvalidOperationException("Actual packaged setup mismatch");
            int expected = 0;
            foreach (var force in setup.Forces) expected += System.Math.Max(1, force.Quantity);
            using var units = em.CreateEntityQuery(typeof(SkirmishUnitRoleComponent), typeof(SkirmishAttemptOwnedComponent), typeof(SkirmishVisualSpawnedComponent));
            using var buildings = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent));
            using var ready = em.CreateEntityQuery(typeof(SkirmishSharedBuildingsReady));
            int actual = units.CalculateEntityCount(), structures = buildings.CalculateEntityCount();
            facts = "units=" + actual + "/" + expected + " structures=" + structures + "/" + setup.Structures.Length + " setup=" + setup.SetupHash;
            return actual == expected && structures == setup.Structures.Length && ready.CalculateEntityCount() == 1 &&
                em.HasComponent<SkirmishSharedSupplyInitialized>(session);
        }
        public static void BeginClock(World world)
        {
            if (SkirmishValidationClock.Active || Time.timeScale != 1f || Time.captureDeltaTime != 0f)
                throw new System.InvalidOperationException("Unmodified native clock required before validation scope");
            var group = world.GetExistingSystemManaged<InitializationSystemGroup>();
            if (group == null) throw new System.InvalidOperationException("Native InitializationSystemGroup absent");
            SkirmishValidationClock.Begin(Time.frameCount, Time.timeAsDouble, Time.unscaledTime,
                Time.realtimeSinceStartupAsDouble, 1f / 60f);
            group.AddSystemToUpdateList(world.GetOrCreateSystemManaged<SkirmishValidationClockSampler>());
            group.SortSystems();
            Time.captureDeltaTime = 1f / 60f;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }
}
#endif
