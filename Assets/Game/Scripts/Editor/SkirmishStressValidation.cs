#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Configs;
using Game.Runtime;
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
            Check(setup.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario1") != null
                && setup.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario2") != null
                && setup.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario3") == null,
                "Player setup must still expose only the two small battles.");
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
