#if UNITY_EDITOR
using System;
using System.IO;
using Game.Configs;
using Game.Skirmish.Contracts;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class SkirmishDefinitionBuilder
    {
        public const string SharedFolder = "Assets/Game/Configs/SkirmishExpansion/Shared";
        public const string ScenarioFolder = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S002";
        public const string ScenarioFolderS003 = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S003";
        public const string ScenarioFolderS004 = "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004";
        public const string PublicationPath = SharedFolder + "/SkirmishPublicationManifest.asset";
        public const string CatalogCsvPath = "Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Expanded Definitions")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author expanded definitions in Edit mode.");

            Directory.CreateDirectory(SharedFolder);
            Directory.CreateDirectory(ScenarioFolder);
            Directory.CreateDirectory(ScenarioFolderS003);
            Directory.CreateDirectory(ScenarioFolderS004);
            SkirmishExpansionAuthoredSet set = SkirmishExpansionCatalogFactory.CreateInMemory();
            bool preserveS002Playable = TryReadPlayable("S002", out string preservedS002Notes);
            bool preserveS003Playable = TryReadPlayable("S003", out string preservedS003Notes);
            bool preserveS004Playable = TryReadPlayable("S004", out string preservedS004Notes);
            Persist(set.ObjectiveBa, SharedFolder + "/SkirmishObjective_BA.asset");
            Persist(set.ArmyGround, SharedFolder + "/SkirmishArmy_GroundManeuver.asset");
            Persist(set.ArmyAir, SharedFolder + "/SkirmishArmy_AirMobile.asset");
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
            Persist(set.Overlays, SharedFolder + "/SkirmishRoleOverlay_S002Ground.asset");
            Persist(set.GroundStaging, SharedFolder + "/SkirmishGroundStaging_V1.asset");
            Persist(set.LayoutDbBa, SharedFolder + "/SkirmishLayout_DB_BA.asset");
            Persist(set.Publication, SharedFolder + "/SkirmishPublicationManifest.asset");
            Persist(set.DefinitionS002, ScenarioFolder + "/SkirmishScenario_S002.asset");
            Persist(CopyLayout(set.LayoutDbBa), ScenarioFolder + "/SkirmishLayout_S002.asset");
            Persist(set.DefinitionS003, ScenarioFolderS003 + "/SkirmishScenario_S003.asset");
            Persist(CopyLayout(set.LayoutDbBa), ScenarioFolderS003 + "/SkirmishLayout_S003.asset");
            Persist(set.DefinitionS004, ScenarioFolderS004 + "/SkirmishScenario_S004.asset");
            Persist(CopyLayout(set.LayoutDbBa), ScenarioFolderS004 + "/SkirmishLayout_S004.asset");
            if (preserveS002Playable)
                RestorePlayable("S002", preservedS002Notes);
            if (preserveS003Playable)
                RestorePlayable("S003", preservedS003Notes);
            if (preserveS004Playable)
                RestorePlayable("S004", preservedS004Notes);
            AssetDatabase.SaveAssets();
            return "[SkirmishDefinitionBuilder] result=Passed definition=skirmish.s002,skirmish.s003,skirmish.s004";
        }

        private static bool TryReadPlayable(string catalogId, out string notes)
        {
            notes = null;
            SkirmishPublicationConfig existing = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(PublicationPath);
            if (existing == null || !existing.TryGet(catalogId, out SkirmishPublicationRowConfig row))
                return false;
            if (row.Status != SkirmishPublicationStatus.Playable)
                return false;
            notes = row.Notes;
            return true;
        }

        private static void RestorePlayable(string catalogId, string notes)
        {
            SkirmishPublicationConfig written = AssetDatabase.LoadAssetAtPath<SkirmishPublicationConfig>(PublicationPath);
            if (written == null || !written.TrySetStatus(catalogId, SkirmishPublicationStatus.Playable, notes))
                throw new InvalidOperationException("Could not preserve " + catalogId + " Playable publication.");
            EditorUtility.SetDirty(written);
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
