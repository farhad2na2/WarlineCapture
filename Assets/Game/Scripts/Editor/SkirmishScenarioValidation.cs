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
        public static string Run()
        {
            int checks = 0;
            void Check(bool condition, string reason) { if (!condition) throw new InvalidOperationException(reason); checks++; }
            var config = QuickGameConfig.Defaults; config.ScenarioIndex = 1; config.MapSeed = 314159;
            config.Difficulty = AIDifficultySetting.Brutal;
            var normalized = config.NormalizeForBaseAssault();
            Check(normalized.ScenarioIndex == 1 && normalized.MapSeed == 314159 && normalized.Difficulty == AIDifficultySetting.Normal,
                "Scenario/seed must survive supported-rules normalization.");
            config.ScenarioIndex = 999;
            Check(config.NormalizeForBaseAssault().ScenarioIndex == 0, "Unknown scenario must safely select scenario 1.");
            var old = SkirmishSaveMigration.Normalize(new QuickGameSaveData { schemaVersion = 1, configuration = config });
            Check(old.configuration.ScenarioIndex == 0 && old.configuration.MapSeed == 314159, "Version-1 migration must preserve seed.");
            var saved = new QuickGameSaveData { schemaVersion = SkirmishSaveMigration.Version, configuration = normalized };
            var roundTrip = SkirmishSaveMigration.Normalize(JsonUtility.FromJson<QuickGameSaveData>(JsonUtility.ToJson(saved)));
            Check(roundTrip.configuration.ScenarioIndex == 1 && roundTrip.presetId == "city_crossroads", "Scenario-2 save round trip.");
            var first = SkirmishPresetConfig.Load(0); var second = SkirmishPresetConfig.Load(1);
            Check(first != null && second != null && first != second, "Separate scenario presets required.");
            Check(first.buildingPlacement != second.buildingPlacement &&
                first.buildingPlacement.InitialUnitsConfig != second.buildingPlacement.InitialUnitsConfig, "Separate authored deployment required.");
            var map = second.operationMap;
            Check(map != null && map.OperationMapId == SkirmishScenarioAssetsBuilder.MapId && map.SourceBinding.IsConfigured, "Second map identity/binding.");
            Check(map.TryValidateMetadata(out _) && map.TryValidateLocalContentReferences(out _), "Second map metadata/content.");
            var a = second.buildingPlacement.InitialUnitsConfig.Factions[0].SpawnCell;
            var b = second.buildingPlacement.InitialUnitsConfig.Factions[1].SpawnCell;
            Check(Mathf.Abs(a.y-b.y) > 300 && Mathf.Abs(a.x-b.x) < 100, "Second battlefield must use its north/south approach.");
            Check(first.rifleDamage == second.rifleDamage && first.armoredCarDamage == second.armoredCarDamage &&
                first.reinforcementInfantryTarget == second.reinforcementInfantryTarget, "Shared combat difficulty must be preserved.");
            using (var world = new Unity.Entities.World("Skirmish roster weapon validation"))
            {
                var em = world.EntityManager;
                var match = new SkirmishMatchState { ScenarioIndex = 1 };
                em.SetComponentData(em.CreateEntity(typeof(SkirmishMatchState)), match);
                for (byte faction = 1; faction <= 2; faction++)
                {
                    var tower = em.CreateEntity(typeof(Faction), typeof(UnitSourcePrefabKey), typeof(UnitAttack), typeof(BuildingDefenseWeapon));
                    em.SetComponentData(tower, new Faction { Id = faction });
                    em.SetComponentData(tower, new UnitSourcePrefabKey { Value = "Building_GuardTower" });
                    em.SetComponentData(tower, new BuildingDefenseWeapon { Range = 100, Damage = 99, CooldownSeconds = .3f, MaxConcurrentAttacks = 4, TraceWidth = .25f });
                    SkirmishCombatPolicy.ApplyRoster(em, match);
                    var weapon = em.GetComponentData<BuildingDefenseWeapon>(tower);
                    Check(weapon.Range == second.watchtowerRange && weapon.Damage == second.watchtowerDamage &&
                        weapon.CooldownSeconds == second.watchtowerCooldown, "Both factions must fire the configured skirmish tower weapon.");
                    Check(weapon.MaxConcurrentAttacks == 4 && weapon.TraceWidth == .25f, "Roster tuning must preserve weapon presentation and slots.");
                }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary") != null,
                "Battle library scroll root required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryMapTabs") != null,
                "Battle library map tabs required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrarySearch") != null,
                "Battle library search field required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary/Viewport/Content") != null,
                "Battle library content root required.");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices") == null,
                "Legacy ScenarioChoices must be removed.");
            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(SkirmishBattleCatalogBuilder.AssetPath);
            Check(catalog != null && catalog.TryValidate(out _), "Battle catalog asset must validate.");
            Check(catalog.Entries.Count == 120 && catalog.CountPlayable() == 2,
                "Catalog must list 120 scenarios with exactly two playable entries.");
            return $"[SkirmishScenarioValidation] result=Passed cases={checks}";
        }
    }
}
#endif
