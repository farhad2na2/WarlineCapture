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
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab");
            Check(prefab.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario1") != null &&
                prefab.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario2") != null, "Two visible scenario controls required.");
            return $"[SkirmishScenarioValidation] result=Passed cases={checks}";
        }
    }
}
#endif
