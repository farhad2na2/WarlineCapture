#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishDefinitionBuilder
    {
        public const string SharedFolder = "Assets/Game/Configs/SkirmishExpansion/Shared";
        public const string ScenarioFolder = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002";
        public const string CatalogCsvPath = "Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Expanded Definitions")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author expanded definitions in Edit mode.");

            Directory.CreateDirectory(SharedFolder);
            Directory.CreateDirectory(ScenarioFolder);
            SkirmishExpansionAuthoredSet set = SkirmishExpansionCatalogFactory.CreateInMemory();
            Persist(set.ObjectiveBa, SharedFolder + "/SkirmishObjective_BA.asset");
            Persist(set.ArmyGround, SharedFolder + "/SkirmishArmy_GroundManeuver.asset");
            Persist(set.StartField, SharedFolder + "/SkirmishStart_Field.asset");
            Persist(set.StartEstablished, SharedFolder + "/SkirmishStart_Established.asset");
            Persist(set.SizeStandard, SharedFolder + "/SkirmishSize_Standard.asset");
            Persist(set.SizeWar, SharedFolder + "/SkirmishSize_War.asset");
            Persist(set.SizeLargeWar, SharedFolder + "/SkirmishSize_LargeWar.asset");
            Persist(set.DifficultyRecruit, SharedFolder + "/SkirmishDifficulty_Recruit.asset");
            Persist(set.DifficultyRegular, SharedFolder + "/SkirmishDifficulty_Regular.asset");
            Persist(set.DifficultyVeteran, SharedFolder + "/SkirmishDifficulty_Veteran.asset");
            Persist(set.DifficultyCommander, SharedFolder + "/SkirmishDifficulty_Commander.asset");
            Persist(set.Economy, SharedFolder + "/SkirmishEconomy_V1.asset");
            Persist(set.Upgrades, SharedFolder + "/SkirmishUpgrade_V1.asset");
            Persist(set.Intel, SharedFolder + "/SkirmishIntel_SharedFog.asset");
            Persist(set.Roles, SharedFolder + "/SkirmishRoleCatalog_V1.asset");
            Persist(set.LayoutDbBa, SharedFolder + "/SkirmishLayout_DB_BA.asset");
            Persist(set.Publication, SharedFolder + "/SkirmishPublicationManifest.asset");
            Persist(set.DefinitionS002, ScenarioFolder + "/SkirmishScenario_S002.asset");
            Persist(CopyLayout(set.LayoutDbBa), ScenarioFolder + "/SkirmishLayout_S002.asset");
            AssetDatabase.SaveAssets();
            return "[SkirmishDefinitionBuilder] result=Passed definition=skirmish.s002";
        }

        public static void AssertCatalogCsvUnchanged()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", CatalogCsvPath));
            string header = File.ReadAllLines(path)[0];
            if (!header.StartsWith("scenario_id,map_id,objective_id,army_profile,start_profile", StringComparison.Ordinal))
                throw new InvalidOperationException("SCENARIO_CATALOG.csv column order changed.");
        }

        private static T Persist<T>(T asset, string path) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static SkirmishMapLayoutConfig CopyLayout(SkirmishMapLayoutConfig source)
        {
            var copy = ScriptableObject.CreateInstance<SkirmishMapLayoutConfig>();
            EditorUtility.CopySerialized(source, copy);
            return copy;
        }

    }
}
#endif
