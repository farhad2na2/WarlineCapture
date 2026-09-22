using System;
using Game.Components;
using Game.Configs;
using Game.Missions.Contracts;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Composition
{
    internal sealed class SkirmishLaunchSnapshot : IComponentData
    {
        public QuickGameConfig Configuration;
    }

    internal static class SkirmishLaunchProjection
    {
        public const string MissionId = "skirmish.base_assault";
        public const string ScenarioId = "scenario.skirmish.desert_base_standard";
        public const string OperationMapId = "opmap.skirmish.desert_base_01";

        public static bool TryQueue(EntityManager em, QuickGameConfig config)
        {
            using var campaign = em.CreateEntityQuery(typeof(CampaignMissionLaunchRequestElement));
            using var roots = campaign.ToEntityArray(Allocator.Temp);
            foreach (var root in roots)
                if (em.GetBuffer<CampaignMissionLaunchRequestElement>(root).Length != 0) return false;
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (!query.IsEmptyIgnoreFilter) return false;
            // The start boundary survives scene unloads. A retry/replay must not
            // inherit the previous attempt's Failed or Started status.
            using (var starts = em.CreateEntityQuery(typeof(MatchStartQueueComponent)))
            {
                using var boundaries = starts.ToEntityArray(Allocator.Temp);
                foreach (var boundary in boundaries)
                    if (em.GetComponentData<MatchStartQueueComponent>(boundary).IsStartPending != 0) return false;
                foreach (var boundary in boundaries)
                {
                    int sequence = em.GetComponentData<MatchStartQueueComponent>(boundary).LastRequestId;
                    em.SetComponentData(boundary, new MatchStartQueueComponent { LastRequestId = sequence });
                    if (em.HasBuffer<MatchStartRequestElement>(boundary)) em.GetBuffer<MatchStartRequestElement>(boundary).Clear();
                    if (em.HasBuffer<MatchStartResultElement>(boundary)) em.GetBuffer<MatchStartResultElement>(boundary).Clear();
                    if (em.HasComponent<MatchStartProgressComponent>(boundary)) em.SetComponentData(boundary, default(MatchStartProgressComponent));
                }
            }
            var entity = em.CreateEntity(typeof(SkirmishMatchState));
            config = config.NormalizeForBaseAssault();
            em.SetName(entity, "SkirmishSession");
            em.SetComponentData(entity, new SkirmishMatchState
            {
                SessionId = new FixedString64Bytes(Guid.NewGuid().ToString("N")),
                Seed = config.MapSeed,
                ScenarioIndex = config.ScenarioIndex,
                Phase = SkirmishPhase.Queued
            });
            em.AddBuffer<SkirmishActionRequest>(entity);
            em.AddBuffer<SkirmishTrackedUnit>(entity);
            em.AddComponentObject(entity, new SkirmishLaunchSnapshot { Configuration = config });
            return true;
        }

        public static bool TryGet(EntityManager em, out Entity entity, out SkirmishMatchState state)
        {
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
            entity = Entity.Null; state = default;
            if (query.CalculateEntityCount() != 1) return false;
            entity = query.GetSingletonEntity(); state = em.GetComponentData<SkirmishMatchState>(entity);
            return true;
        }

        public static void PrepareMap(EntityManager em)
        {
            if (!TryGet(em, out var session, out var match) || match.Phase != SkirmishPhase.Queued) return;
            // The shared map's authored ownership belongs to campaign scenarios. Only
            // this preset's newly spawned force and supply buildings belong to its sides.
            using var query = em.CreateEntityQuery(typeof(OperationMapBuildingComponent), typeof(Faction));
            using var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                em.SetComponentData(entity, new Faction { Id = 0 });
                if (em.HasComponent<RuntimeBuildingCombatInfo>(entity))
                {
                    var info = em.GetComponentData<RuntimeBuildingCombatInfo>(entity); info.OwnerFactionId = 0;
                    em.SetComponentData(entity, info);
                }
                if (em.HasComponent<BuildingResourceStorageComponent>(entity))
                {
                    var storage = em.GetComponentData<BuildingResourceStorageComponent>(entity); storage.OwnerFactionId = 0;
                    em.SetComponentData(entity, storage);
                }
                if (em.HasComponent<AIControlledTag>(entity)) em.RemoveComponent<AIControlledTag>(entity);
            }
            match.Phase = SkirmishPhase.Preparing; em.SetComponentData(session, match);
        }

        public static void ApplySeed(EntityManager em)
        {
            if (!TryGet(em, out _, out var state)) return;
            Game.Runtime.SkirmishWorldSetup.SuppressUnselectedStartupConfigs(em);
            using var query = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
            if (query.CalculateEntityCount() != 1) throw new InvalidOperationException("Skirmish requires one initial spawn configuration.");
            var config = query.GetSingleton<InitialUnitsSpawnConfig>(); config.RandomSeed = (uint)state.Seed;
            em.SetComponentData(query.GetSingletonEntity(), config);
        }

        public static bool IsShellReadyToEnterMatch(EntityManager em)
        {
            if (!TryGetShell(em, out var boundary, out var shell)) return false;
            if (shell.IsTransitionRunning != 0) return false;
            if (em.HasComponent<UiShellStartupDispositionComponent>(boundary))
            {
                var disposition = em.GetComponentData<UiShellStartupDispositionComponent>(boundary).Value;
                if (disposition == UiShellStartupDisposition.Pending ||
                    disposition == UiShellStartupDisposition.FirstLaunch)
                    return false;
            }
            return shell.CurrentMode == UiShellMode.MainMenu;
        }

        public static bool TryEnterMatch(EntityManager em)
        {
            if (!TryGetShell(em, out var boundary, out var shell)) return false;
            if (shell.ActiveRoute == UIRoute.Match) return true;
            var routes = em.GetBuffer<UiShellRouteRequestComponent>(boundary);
            for (int i = 0; i < routes.Length; i++)
                if (routes[i].Intent == UiShellRouteIntent.EnterMatch) return true;
            routes.Add(new UiShellRouteRequestComponent
            {
                Intent = UiShellRouteIntent.EnterMatch,
                Route = UIRoute.Match,
                PushHistory = 0
            });
            return true;
        }

        public static bool TryRequestPlay(EntityManager em)
        {
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1) return false;
            var entity = gameplay.GetSingletonEntity();
            var state = em.GetComponentData<RuntimeGameplayStateComponent>(entity);
            if (state.PlayRequested != 0) return true;
            state.PlayRequested = 1;
            em.SetComponentData(entity, state);
            Debug.Log(SkirmishStressRecipe.ReportMarker + " playRequested=1");
            return true;
        }

        /// <summary>
        /// Expanded S002 can reach Playing via spawn while SimulationActive is still 0.
        /// After gameplay bootstrap finishes (or fails), arm simulation so the objective
        /// clock and engagement systems advance. Does not bypass an intentional pause.
        /// </summary>
        public static bool TryArmExpandedSimulation(EntityManager em)
        {
            if (!TryGet(em, out Entity session, out SkirmishMatchState match))
                return false;
            if (match.Phase < SkirmishPhase.Playing)
                return false;
            if (!em.HasComponent<SkirmishExpandedSessionComponent>(session) ||
                em.GetComponentData<SkirmishExpandedSessionComponent>(session).IsLegacy != 0)
                return false;

            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1)
                return false;
            Entity entity = gameplay.GetSingletonEntity();
            var state = em.GetComponentData<RuntimeGameplayStateComponent>(entity);
            if (state.SimulationActive != 0)
                return true;
            if (state.PlayRequested == 0)
                return false;

            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (scene == null || !scene.GameplayStartRequested)
                return false;
            if (!scene.GameplayStartComplete && !scene.GameplayStartFailed)
                return false;

            state.SimulationActive = 1;
            em.SetComponentData(entity, state);
            Debug.Log("[SkirmishLaunch] expandedSimulationActive=1");
            return true;
        }

        public static bool IsSimulationActive(EntityManager em)
        {
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1)
                return false;
            return em.GetComponentData<RuntimeGameplayStateComponent>(gameplay.GetSingletonEntity()).SimulationActive != 0;
        }

        public static float ReadMatchElapsedSeconds(EntityManager em, Entity session, in SkirmishMatchState match)
        {
            if (session != Entity.Null &&
                em.HasComponent<SkirmishObjectiveClockComponent>(session))
                return em.GetComponentData<SkirmishObjectiveClockComponent>(session).ElapsedSeconds;
            return match.ElapsedSeconds;
        }

        public static bool TryActivateStressSimulation(EntityManager em)
        {
            if (!TryGet(em, out var session, out var match)) return false;
            if (match.ScenarioIndex != SkirmishPresetConfig.StressScaleProbeScenarioIndex) return false;
            if (match.StartupFailure == SkirmishStartupFailureCode.Content) return false;
            if (match.Phase != SkirmishPhase.Preparing) return false;
            if (match.StartupFailure == SkirmishStartupFailureCode.Timeout)
            {
                match.StartupFailure = SkirmishStartupFailureCode.None;
                em.SetComponentData(session, match);
                Debug.Log(SkirmishStressRecipe.ReportMarker + " cleared timeout so Playing can start");
            }
            using var gameplay = em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            if (gameplay.CalculateEntityCount() != 1) return false;
            var entity = gameplay.GetSingletonEntity();
            var state = em.GetComponentData<RuntimeGameplayStateComponent>(entity);
            bool changed = false;
            if (state.PlayRequested == 0)
            {
                state.PlayRequested = 1;
                changed = true;
            }
            if (state.SimulationActive == 0)
            {
                state.SimulationActive = 1;
                changed = true;
                Debug.Log(SkirmishStressRecipe.ReportMarker + " simulationActive=1 preparing barracks="
                    + (HasBothFactionBarracks(em) ? 1 : 0));
            }
            if (changed)
                em.SetComponentData(entity, state);
            return true;
        }

        public static bool TryBeginGameplayIfLoaded()
        {
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            if (scene == null) return false;
            if (!scene.GameplayStartRequested)
                scene.BeginGameplay();
            return scene.GameplayStartRequested;
        }

        public static bool DriveStressLaunch(EntityManager em)
        {
            if (!TryGet(em, out _, out var match)) return false;
            if (match.ScenarioIndex != SkirmishPresetConfig.StressScaleProbeScenarioIndex) return false;
            if (match.Phase >= SkirmishPhase.Playing)
                return match.Phase == SkirmishPhase.Playing;
            if (match.StartupFailure == SkirmishStartupFailureCode.Content)
                return false;
            if (match.Phase == SkirmishPhase.Queued &&
                (IsShellReadyToEnterMatch(em) || (TryGetShell(em, out _, out var shell) && shell.ActiveRoute == UIRoute.Match)))
                TryEnterMatch(em);
            TryBeginGameplayIfLoaded();
            TryRequestPlay(em);
            TryActivateStressSimulation(em);
            return true;
        }

        private static bool TryGetShell(EntityManager em, out Entity boundary, out UiShellStateComponent shell)
        {
            boundary = Entity.Null;
            shell = default;
            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<UiShellRootComponent>(),
                ComponentType.ReadWrite<UiShellStateComponent>(),
                ComponentType.ReadWrite<UiShellRouteRequestComponent>());
            if (query.IsEmptyIgnoreFilter || query.CalculateEntityCount() != 1) return false;
            boundary = query.GetSingletonEntity();
            shell = em.GetComponentData<UiShellStateComponent>(boundary);
            return true;
        }

        private static bool HasBothFactionBarracks(EntityManager em)
        {
            bool player = false;
            bool enemy = false;
            using var query = em.CreateEntityQuery(
                typeof(RuntimeBuildingCombatTag), typeof(UnitSourcePrefabKey), typeof(Faction), typeof(UnitHealth));
            using var buildings = query.ToEntityArray(Allocator.Temp);
            foreach (var building in buildings)
            {
                if (em.HasComponent<OperationMapBuildingComponent>(building)) continue;
                if (em.GetComponentData<UnitHealth>(building).Current <= 0) continue;
                if (!em.GetComponentData<UnitSourcePrefabKey>(building).Value.ToString().ToLowerInvariant()
                        .Contains("building_barrack"))
                    continue;
                byte faction = em.GetComponentData<Faction>(building).Id;
                if (faction == 1) player = true;
                if (faction == 2) enemy = true;
            }
            return player && enemy;
        }
    }
}
