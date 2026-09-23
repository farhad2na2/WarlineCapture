#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class SkirmishBattleCatalogBuilder
    {
        public const string CsvPath = "Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv";
        public const string AssetPath = "Assets/Game/Resources/SkirmishBattleCatalog.asset";
        public const string SetupPrefabPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab";

        [MenuItem("Tools/Warline/Skirmish/Rebuild Battle Catalog")]
        public static void RebuildMenu() => Debug.Log(Rebuild());

        public static string Rebuild()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Author the battle catalog in Edit mode.");

            Directory.CreateDirectory("Assets/Game/Resources");
            SkirmishBattleCatalogEntry[] entries = ParseCsv(
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", CsvPath)));
            SkirmishPublicationValidator.ApplyLegacyPrototypeCompatibility(
                entries,
                SkirmishPublicationValidator.LegacyPrototypeCompatibilityVersion);

            var catalog = AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(AssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<SkirmishBattleCatalogConfig>();
                AssetDatabase.CreateAsset(catalog, AssetPath);
            }

            catalog.Configure(entries);
            if (!catalog.TryValidate(out string error))
                throw new InvalidOperationException(error);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return $"[SkirmishBattleCatalog] result=Passed entries={entries.Length} playable={catalog.CountPlayable()}";
        }

        public static string RebuildLibraryAndPrefab()
        {
            string catalogResult = Rebuild();
            var root = PrefabUtility.LoadPrefabContents(SetupPrefabPath);
            try
            {
                SkirmishScenarioAssetsBuilder.ConfigureChoices(root);
                PrefabUtility.SaveAsPrefabAsset(root, SetupPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return catalogResult + " library=rebuilt";
        }

        public static void RunFocusedValidation()
        {
            try
            {
                string result = RebuildLibraryAndPrefab();
                var catalog = AssetDatabase.LoadAssetAtPath<SkirmishBattleCatalogConfig>(AssetPath);
                if (catalog == null)
                    throw new InvalidOperationException("Catalog missing.");
                if (!catalog.TryValidate(out string error))
                    throw new InvalidOperationException(error ?? "Catalog invalid.");
                if (catalog.Entries.Count != 120)
                    throw new InvalidOperationException($"Expected 120 catalog entries, got {catalog.Entries.Count}.");
                if (catalog.CountPlayable() != 4)
                    throw new InvalidOperationException($"Expected 4 playable entries, got {catalog.CountPlayable()}.");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SetupPrefabPath);
                if (prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrary") == null)
                    throw new InvalidOperationException("BattleLibrary root missing on SCN-13.");
                if (prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibraryMapTabs") == null)
                    throw new InvalidOperationException("BattleLibraryMapTabs missing on SCN-13.");
                if (prefab.transform.Find("SkirmishSetupComposition/OperationPreview/BattleLibrarySearch") == null)
                    throw new InvalidOperationException("BattleLibrarySearch missing on SCN-13.");
                if (prefab.transform.Find("SkirmishSetupComposition/OperationPreview/ScenarioChoices/Scenario1") != null)
                    throw new InvalidOperationException("Legacy ScenarioChoices must be removed.");

                var view = prefab.GetComponent<QuickCustomScreenView>();
                var so = new SerializedObject(view);
                if (so.FindProperty("battleLibraryContent").objectReferenceValue == null)
                    throw new InvalidOperationException("battleLibraryContent is not wired.");
                if (so.FindProperty("battleLibraryMapTabs").objectReferenceValue == null)
                    throw new InvalidOperationException("battleLibraryMapTabs is not wired.");
                if (so.FindProperty("battleLibrarySearch").objectReferenceValue == null)
                    throw new InvalidOperationException("battleLibrarySearch is not wired.");
                if (so.FindProperty("scenarioDescription").objectReferenceValue == null)
                    throw new InvalidOperationException("scenarioDescription is not wired.");

                Debug.Log($"[SkirmishBattleLibraryValidation] result=Passed {result}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SkirmishBattleLibraryValidation] result=Failed\n{exception}");
                EditorApplication.Exit(1);
            }
        }

        private static SkirmishBattleCatalogEntry[] ParseCsv(string absolutePath)
        {
            if (!File.Exists(absolutePath))
                throw new FileNotFoundException("Skirmish scenario catalog CSV missing.", absolutePath);

            string[] lines = File.ReadAllLines(absolutePath);
            if (lines.Length < 2)
                throw new InvalidOperationException("Skirmish scenario catalog CSV has no rows.");

            var entries = new List<SkirmishBattleCatalogEntry>(lines.Length - 1);
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] cols = SplitCsv(line);
                if (cols.Length < 6)
                    throw new InvalidOperationException($"Catalog CSV row {i + 1} is incomplete.");

                entries.Add(new SkirmishBattleCatalogEntry
                {
                    ScenarioId = cols[0].Trim(),
                    MapId = cols[1].Trim(),
                    ObjectiveId = cols[2].Trim(),
                    ArmyProfile = cols[3].Trim(),
                    StartProfile = cols[4].Trim(),
                    TitleEnglish = cols[5].Trim(),
                    TitleKey = string.Empty,
                    DescriptionEnglish = string.Empty,
                    DescriptionFarsi = string.Empty,
                    Status = SkirmishBattleCatalogStatus.Planned,
                    PlayableScenarioIndex = -1
                });
            }

            if (entries.Count != 120)
                throw new InvalidOperationException($"Expected 120 catalog rows, parsed {entries.Count}.");
            if (entries.Select(e => e.ScenarioId).Distinct(StringComparer.Ordinal).Count() != entries.Count)
                throw new InvalidOperationException("Catalog CSV contains duplicate scenario ids.");

            return entries.ToArray();
        }

        private static string[] SplitCsv(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
#endif
