#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.UI.Contracts;
using Game.UI.Shell.Contracts.Ecs;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishStressValidation
    {
        public static string Run()
        {
            int checks = 0;
            void Check(bool condition, string reason)
            {
                if (!condition) throw new InvalidOperationException(reason);
                checks++;
            }

            Check(SkirmishPresetConfig.StressScaleProbeScenarioIndex == 2, "Stress scenario index must stay hidden at 2.");
            var unknown = QuickGameConfig.Defaults;
            unknown.ScenarioIndex = 999;
            Check(unknown.NormalizeForBaseAssault().ScenarioIndex == 0, "Unknown scenario must still select Desert Base.");
            var player = QuickGameConfig.Defaults;
            player.ScenarioIndex = 1;
            player.MapSeed = 314159;
            var normalizedPlayer = player.NormalizeForBaseAssault();
            Check(normalizedPlayer.ScenarioIndex == 1 && normalizedPlayer.MapSeed == 314159, "City Crossroads identity must survive.");
            var stress = QuickGameConfig.Defaults;
            stress.ScenarioIndex = SkirmishPresetConfig.StressScaleProbeScenarioIndex;
            stress.MapSeed = SkirmishStressRecipe.FixedSeed;
            Check(stress.NormalizeForBaseAssault().ScenarioIndex == 2, "Editor stress index must survive normalization.");
            Check(SkirmishSaveMigration.Normalize(new QuickGameSaveData
            {
                schemaVersion = SkirmishSaveMigration.Version,
                configuration = stress.NormalizeForBaseAssault()
            }).presetId == "stress_scale_probe", "Stress save identity.");

            var desert = SkirmishPresetConfig.Load(0);
            var city = SkirmishPresetConfig.Load(1);
            var probe = SkirmishPresetConfig.Load(2);
            Check(desert != null && city != null && desert != city, "Player presets must remain distinct.");
            Check(probe != null && probe != desert && probe != city, "Stress preset must be additive.");
            Check(desert.buildingPlacement != probe.buildingPlacement, "Stress construction must not reuse the small preset asset.");
            Check(desert.buildingPlacement.InitialUnitsConfig != probe.buildingPlacement.InitialUnitsConfig,
                "Stress initial forces must be a separate asset.");
            Check(desert.buildingPlacement.UnitPrefabRegistryConfig != probe.buildingPlacement.UnitPrefabRegistryConfig,
                "Stress roster must be a separate registry.");
            int desertInfantry = CountRequested(desert.buildingPlacement.InitialUnitsConfig, "soldier");
            Check(desertInfantry == 16, "Desert Base starting infantry must stay 8+8.");
            int cityInfantry = CountRequested(city.buildingPlacement.InitialUnitsConfig, "soldier");
            Check(cityInfantry == 16, "City Crossroads starting infantry must stay 8+8.");

            var registry = probe.buildingPlacement.UnitPrefabRegistryConfig;
            foreach (string path in SkirmishStressRecipe.RosterPrefabPaths)
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Check(registry.UnitSpawnPrefabs.Exists(prefab => prefab != null && prefab.name == name),
                    "Stress registry must include real prefab " + name);
            }
            Check(!registry.UnitSpawnPrefabs.Exists(prefab => prefab != null && prefab.name.ToLowerInvariant().Contains("cube")),
                "Stress roster must not use stub cubes.");

            var requested = SkirmishStressRecipe.Requested(SkirmishStressScale.P100, SkirmishStressPhase.Warmup, true);
            Check(requested.Combat == 106 && requested.Air == 6 && requested.Support == 6 && requested.Buildings == 12,
                "P100 sequence requested counts must stay explicit.");
            Check(SkirmishStressRecipe.Requested(SkirmishStressScale.P50, SkirmishStressPhase.Idle, false).Combat == 50,
                "P50 ground requested combat must be 50.");
            Check(SkirmishStressRecipe.Requested(SkirmishStressScale.P200, SkirmishStressPhase.DenseCombat, false).Combat == 200,
                "P200 ground requested combat must be 200.");
            Check(SkirmishStressRecipe.Requested(SkirmishStressScale.P350, SkirmishStressPhase.MassMove, false).Combat == 350,
                "P350 ground requested combat must be 350.");
            Check(SkirmishStressRecipe.Requested(SkirmishStressScale.P500, SkirmishStressPhase.Destruction, false).Combat == 500,
                "P500 ground requested combat must be 500.");
            var airOnly = SkirmishStressRecipe.Requested(SkirmishStressScale.P100, SkirmishStressPhase.AirTransport, false);
            Check(airOnly.Air == 6 && airOnly.Combat == 24, "Air-only P100 includes escort plus aircraft.");
            Check(Mathf.Approximately(SkirmishStressRecipe.SpawnStallCompleteSeconds, 8f),
                "Warmup must complete after 8s of frozen spawn progress so census can record missing cells.");
            Check(SkirmishStressEditorProbe.ScreenshotFileName(new SkirmishStressSession
            {
                Scale = 100,
                Layout = SkirmishStressLayoutCode.Spread
            }, SkirmishStressPhaseCode.Idle) == "e03-idle-spread-p100.png",
                "Programmer 2 screenshot filenames must stay stable.");
            Check(SkirmishStressEditorProbe.PlayerSetupScreenshotFile == "e03-player-setup-two-battles.png",
                "Player-setup capture filename must stay stable.");
            Check(SkirmishStressEditorProbe.RequiredSpreadP100Files().Length == 8,
                "Evidence packet must list census plus setup and six sequence shots.");

            using (var launchWorld = new World("Skirmish stress launch validation"))
            {
                var em = launchWorld.EntityManager;
                var shell = em.CreateEntity(typeof(UiShellRootComponent), typeof(UiShellStateComponent));
                em.AddBuffer<UiShellRouteRequestComponent>(shell);
                em.SetComponentData(shell, new UiShellStateComponent
                {
                    CurrentMode = UiShellMode.None,
                    IsTransitionRunning = 1
                });
                em.AddComponentData(shell, new UiShellStartupDispositionComponent
                {
                    Value = UiShellStartupDisposition.FirstLaunch
                });
                Check(!SkirmishLaunchProjection.IsShellReadyToEnterMatch(em),
                    "Stress launch must wait for idle MainMenu.");
                var launch = QuickGameConfig.Defaults;
                launch.ScenarioIndex = 2;
                launch.MapSeed = SkirmishStressRecipe.FixedSeed;
                Check(SkirmishLaunchProjection.TryQueue(em, launch), "Stress TryQueue must accept scenario 2.");
                Check(SkirmishLaunchProjection.TryGet(em, out _, out var queued) &&
                    queued.ScenarioIndex == 2 && queued.Seed == SkirmishStressRecipe.FixedSeed,
                    "Queued stress session must keep scenario 2 and seed 104729.");
                SkirmishLaunchProjection.DriveStressLaunch(em);
                Check(em.GetBuffer<UiShellRouteRequestComponent>(shell).Length == 0,
                    "First-launch / splash must not consume EnterMatch.");
                em.SetComponentData(shell, new UiShellStateComponent
                {
                    CurrentMode = UiShellMode.MainMenu,
                    IsTransitionRunning = 0
                });
                em.SetComponentData(shell, new UiShellStartupDispositionComponent
                {
                    Value = UiShellStartupDisposition.EnterMenu
                });
                Check(SkirmishLaunchProjection.IsShellReadyToEnterMatch(em) &&
                    SkirmishLaunchProjection.TryEnterMatch(em),
                    "Idle MainMenu must enter the match route.");
                Check(em.GetBuffer<UiShellRouteRequestComponent>(shell).Length == 1 &&
                    em.GetBuffer<UiShellRouteRequestComponent>(shell)[0].Intent == UiShellRouteIntent.EnterMatch,
                    "Stress enter must request the Match route.");
                var gameplay = em.CreateEntity(typeof(RuntimeGameplayStateComponent));
                Check(SkirmishLaunchProjection.TryRequestPlay(em),
                    "Stress launch must request play so InitialUnits can spawn.");
                Check(SkirmishLaunchProjection.TryGet(em, out var launchSession, out var queuedMatch),
                    "Queued stress session must still exist before Preparing.");
                queuedMatch.Phase = SkirmishPhase.Preparing;
                em.SetComponentData(launchSession, queuedMatch);
                Check(SkirmishLaunchProjection.TryActivateStressSimulation(em) &&
                    em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive == 1,
                    "Preparing stress must turn simulation on without waiting for barracks.");
                em.SetComponentData(gameplay, new RuntimeGameplayStateComponent());
                queuedMatch.StartupFailure = SkirmishStartupFailureCode.Timeout;
                em.SetComponentData(launchSession, queuedMatch);
                Check(SkirmishLaunchProjection.DriveStressLaunch(em) &&
                    em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).PlayRequested == 1 &&
                    em.GetComponentData<RuntimeGameplayStateComponent>(gameplay).SimulationActive == 1 &&
                    em.GetComponentData<SkirmishMatchState>(launchSession).StartupFailure == SkirmishStartupFailureCode.None,
                    "Timeout must not clear playRequested or block simulation during stress launch.");
                Check(SkirmishStressEditorProbe.CanAdvancePastPreparing(SkirmishPhase.Preparing, 48) &&
                    !SkirmishStressEditorProbe.CanAdvancePastPreparing(SkirmishPhase.Preparing, 0),
                    "Probe stage 1 may advance when combat already spawned even if Phase lags.");
            }

            using (var world = new World("Skirmish stress census validation"))
            {
                var em = world.EntityManager;
                var sessionEntity = em.CreateEntity(typeof(SkirmishMatchState), typeof(SkirmishStressSession));
                em.SetComponentData(sessionEntity, new SkirmishMatchState
                {
                    Seed = SkirmishStressRecipe.FixedSeed,
                    ScenarioIndex = 2
                });
                em.SetComponentData(sessionEntity, new SkirmishStressSession
                {
                    Seed = SkirmishStressRecipe.FixedSeed,
                    Scale = 50,
                    RequestedCombat = 50,
                    RequestedAir = 0,
                    RequestedSupport = 6,
                    RequestedBuildings = 12
                });
                CreateUnit(em, 1, 10, "Unit_Chr_Soldier_Male_02_Alt_04", false, false);
                CreateUnit(em, 2, 10, "Unit_Chr_Soldier_Male_02_Alt_04", false, false);
                CreateUnit(em, 1, 0, "Unit_Chr_Soldier_Male_02_Alt_04", false, false);
                CreateUnit(em, 1, 8, "Unit_Veh_Truck_Tray", true, false);
                CreateUnit(em, 1, 20, "Building_Barrack", false, true);
                CreateUnit(em, 0, 20, "OperationMapProp", false, true);
                var sample = SkirmishStressCensus.Measure(em, em.GetComponentData<SkirmishStressSession>(sessionEntity), 1.5f);
                Check(sample.SpawnedCombat == 3 && sample.AliveCombat == 2 && sample.DestroyedCombat == 1,
                    "Census must count actual alive/destroyed combatants, not requested.");
                Check(sample.SpawnedSupport == 1 && sample.SpawnedBuildings == 1 && sample.MissingSpawnCombat == 47,
                    "Census must keep support/buildings and missing-spawn separate.");
                Check(SkirmishStressCensus.FormatLine(sample).Contains("spawnedCombat=3"),
                    "Log line must publish measured spawnedCombat.");
                Check(!SkirmishStressCensus.FormatLine(sample).Contains("spawnedCombat=50")
                    || sample.SpawnedCombat == 50,
                    "Log must not present the requested count as spawned.");
            }

            var setup = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
            Check(setup.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary") != null,
                "Player setup must expose the scalable battle library.");
            Check(setup.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryMapTabs") != null,
                "Player setup must expose map collection tabs.");
            Check(setup.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario1") == null,
                "Legacy fixed Scenario1 control must be removed.");
            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(
                SkirmishBattleCatalogBuilder.AssetPath);
            Check(catalog != null && catalog.Entries.Count == 120 && catalog.CountPlayable() == 2,
                "Player setup catalog must list 120 battles with only two playable.");
            return "[SkirmishStressValidation] result=Passed cases=" + checks;
        }

        private static int CountRequested(InitialUnitsSpawnerAuthoringConfig config, string token)
        {
            int count = 0;
            foreach (var faction in config.Factions)
                foreach (var unit in faction.Units)
                    if (unit.Prefab != null && unit.Prefab.name.ToLowerInvariant().Contains(token))
                        count += unit.Count;
            return count;
        }

        private static void CreateUnit(EntityManager em, byte faction, int health, string key, bool support, bool building)
        {
            var entity = em.CreateEntity(typeof(Faction), typeof(UnitHealth), typeof(UnitSourcePrefabKey));
            em.SetComponentData(entity, new Faction { Id = faction });
            em.SetComponentData(entity, new UnitHealth { Current = health, Max = 10 });
            em.SetComponentData(entity, new UnitSourcePrefabKey { Value = key });
            if (support) em.AddComponent<UnitResourceHauler>(entity);
            if (building) em.AddComponent<RuntimeBuildingCombatTag>(entity);
        }
    }
}
#endif
