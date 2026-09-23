#if UNITY_EDITOR
using System;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishScenarioValidation
    {
        public static void RunFocusedValidation()
        {
            try
            {
                Debug.Log(Run());
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SkirmishScenarioValidation] result=Failed\n{exception}");
                EditorApplication.Exit(1);
            }
        }

        public static string Run()
        {
            int checks = 0;
            void Check(bool condition, string reason)
            {
                if (!condition) throw new InvalidOperationException(reason);
                checks++;
            }

            var config = QuickGameConfig.Defaults;
            config.ScenarioIndex = 1;
            config.MapSeed = 314159;
            config.Difficulty = AIDifficultySetting.Brutal;
            var normalized = config.NormalizeForBaseAssault();
            Check(normalized.ScenarioIndex == 1 && normalized.MapSeed == 314159 &&
                  normalized.Difficulty == AIDifficultySetting.Normal,
                "Scenario/seed must survive supported-rules normalization.");
            config.ScenarioIndex = 999;
            Check(config.NormalizeForBaseAssault().ScenarioIndex == 0,
                "Unknown scenario must safely select scenario 1.");
            var old = SkirmishSaveMigration.Normalize(new QuickGameSaveData
                { schemaVersion = 1, configuration = config });
            Check(old.configuration.ScenarioIndex == 0 && old.configuration.MapSeed == 314159,
                "Version-1 migration must preserve seed.");
            var saved = new QuickGameSaveData
                { schemaVersion = SkirmishSaveMigration.Version, configuration = normalized };
            var roundTrip =
                SkirmishSaveMigration.Normalize(JsonUtility.FromJson<QuickGameSaveData>(JsonUtility.ToJson(saved)));
            Check(roundTrip.configuration.ScenarioIndex == 1 && roundTrip.presetId == "city_crossroads",
                "Scenario-2 save round trip.");

            config.ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex;
            config.MapSeed = 104729;
            normalized = config.NormalizeForBaseAssault();
            Check(normalized.ScenarioIndex == SkirmishPresetConfig.IndustrialBasinScenarioIndex,
                "Industrial Basin scenario must survive normalization.");
            saved = new QuickGameSaveData
                { schemaVersion = SkirmishSaveMigration.Version, configuration = normalized };
            roundTrip =
                SkirmishSaveMigration.Normalize(JsonUtility.FromJson<QuickGameSaveData>(JsonUtility.ToJson(saved)));
            Check(roundTrip.configuration.ScenarioIndex == 3 && roundTrip.presetId == "industrial_basin",
                "Scenario-3 save round trip.");

            var first = SkirmishPresetConfig.Load(0);
            var second = SkirmishPresetConfig.Load(1);
            var third = SkirmishPresetConfig.Load(SkirmishPresetConfig.IndustrialBasinScenarioIndex);
            Check(first != null && second != null && third != null && first != second && second != third,
                "Separate scenario presets required.");
            Check(first.buildingPlacement != second.buildingPlacement &&
                  second.buildingPlacement != third.buildingPlacement &&
                  first.buildingPlacement.InitialUnitsConfig != third.buildingPlacement.InitialUnitsConfig,
                "Separate authored deployment required.");
            var map = third.operationMap;
            Check(map != null && map.OperationMapId == SkirmishIndustrialBasinAssetsBuilder.MapId &&
                  map.SourceBinding.IsConfigured, "Third map identity/binding.");
            Check(map.TryValidateMetadata(out _) && map.TryValidateLocalContentReferences(out _),
                "Third map metadata/content.");
            Check(map.Bounds.CameraMin.x <= map.Bounds.PlayableMin.x - 120 &&
                  map.Bounds.CameraMax.x >= map.Bounds.PlayableMax.x + 120 &&
                  map.Bounds.CameraMin.z <= map.Bounds.PlayableMin.z - 120 &&
                  map.Bounds.CameraMax.z >= map.Bounds.PlayableMax.z + 120,
                "Camera frustum needs room to center both bases without clamping them off-screen.");
            var a = third.buildingPlacement.InitialUnitsConfig.Factions[0].SpawnCell;
            var b = third.buildingPlacement.InitialUnitsConfig.Factions[1].SpawnCell;
            Check(a.x >= 770 && a.x <= 840 && a.y >= 580 && a.y <= 620 && a.y > b.y && Vector2Int.Distance(a, b) > 170,
                "Industrial Basin opening must stay on the eastern clearing, outside the northern mountains.");
            Check(a.x < SkirmishIndustrialBasinAssetsBuilder.PlayableMax.x &&
                  b.x > SkirmishIndustrialBasinAssetsBuilder.PlayableMin.x &&
                  a.y < SkirmishIndustrialBasinAssetsBuilder.PlayableMax.y &&
                  b.y > SkirmishIndustrialBasinAssetsBuilder.PlayableMin.y,
                "Industrial Basin spawns must stay inside the authored window.");
            Check(a != second.buildingPlacement.InitialUnitsConfig.Factions[0].SpawnCell &&
                  b != second.buildingPlacement.InitialUnitsConfig.Factions[1].SpawnCell,
                "Industrial Basin must retain its own deployment sites.");
            Check(first.rifleDamage == third.rifleDamage && first.armoredCarDamage == third.armoredCarDamage &&
                  first.reinforcementInfantryTarget == third.reinforcementInfantryTarget,
                "Shared combat difficulty must be preserved.");

            var cityA = second.buildingPlacement.InitialUnitsConfig.Factions[0].SpawnCell;
            var cityB = second.buildingPlacement.InitialUnitsConfig.Factions[1].SpawnCell;
            Check(Mathf.Abs(cityA.y - cityB.y) > 300 && Mathf.Abs(cityA.x - cityB.x) < 100,
                "Second battlefield must use its north/south approach.");

            using (var world = new Unity.Entities.World("Skirmish roster weapon validation"))
            {
                var em = world.EntityManager;
                var match = new SkirmishMatchState
                    { ScenarioIndex = SkirmishPresetConfig.IndustrialBasinScenarioIndex };
                em.SetComponentData(em.CreateEntity(typeof(SkirmishMatchState)), match);
                for (byte faction = 1; faction <= 2; faction++)
                {
                    var tower = em.CreateEntity(typeof(Faction), typeof(UnitSourcePrefabKey), typeof(UnitAttack),
                        typeof(BuildingDefenseWeapon));
                    em.SetComponentData(tower, new Faction { Id = faction });
                    em.SetComponentData(tower, new UnitSourcePrefabKey { Value = "Building_GuardTower" });
                    em.SetComponentData(tower,
                        new BuildingDefenseWeapon
                        {
                            Range = 100, Damage = 99, CooldownSeconds = .3f, MaxConcurrentAttacks = 4, TraceWidth = .25f
                        });
                    SkirmishCombatPolicy.ApplyRoster(em, match);
                    var weapon = em.GetComponentData<BuildingDefenseWeapon>(tower);
                    Check(weapon.Range == third.watchtowerRange && weapon.Damage == third.watchtowerDamage &&
                          weapon.CooldownSeconds == third.watchtowerCooldown,
                        "Both factions must fire the configured skirmish tower weapon.");
                    Check(weapon.MaxConcurrentAttacks == 4 && weapon.TraceWidth == .25f,
                        "Roster tuning must preserve weapon presentation and slots.");
                }
            }

            var prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary") != null,
                "Battle library scroll root required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryMapTabs") != null,
                "Battle library map tabs required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrarySearch") != null,
                "Battle library search field required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices") == null,
                "Legacy ScenarioChoices must be removed.");
            var catalog =
                AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogBuilder.AssetPath);
            Check(catalog != null && catalog.TryValidate(out _), "Battle catalog asset must validate.");
            Check(catalog.Entries.Count == 120 && catalog.CountPlayable() == 5,
                "Catalog must list 120 scenarios with exactly five playable entries.");
            Check(catalog.TryGet(SkirmishBattleCatalogConfig.IndustrialBasinScenarioId, out var basin) &&
                  basin.PlayableScenarioIndex == 3,
                "S073 must resolve to playable scenario index 3.");
            Check(catalog.TryGet(SkirmishBattleCatalogConfig.DesertBaseEstablishedScenarioId, out var established) &&
                  established.IsPlayable &&
                  established.PlayableScenarioIndex == SkirmishPresetConfig.DesertBaseEstablishedScenarioIndex &&
                  established.DefinitionId == SkirmishBattleCatalogConfig.DesertBaseEstablishedDefinitionId,
                "S002 must resolve to playable scenario index 4 and definition skirmish.s002.");
            Check(catalog.TryGet(SkirmishBattleCatalogConfig.DesertBaseAirMobileFieldScenarioId, out var airMobile) &&
                  airMobile.IsPlayable &&
                  airMobile.PlayableScenarioIndex == SkirmishPresetConfig.DesertBaseAirMobileFieldScenarioIndex &&
                  airMobile.DefinitionId == SkirmishBattleCatalogConfig.DesertBaseAirMobileFieldDefinitionId,
                "S003 must resolve to playable scenario index 5 and definition skirmish.s003.");
            return $"[SkirmishScenarioValidation] result=Passed cases={checks}";
        }
    }
}
#endif
