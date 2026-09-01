#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// Rebuilds every V3 screen that owns the WARLINE/CAPTURE header and proves
    /// that all of them reference one shared sprite from one dedicated atlas.
    /// </summary>
    public static class V3SharedBrandLogoMigrationBuilder
    {
        private readonly struct LogoPrefabExpectation
        {
            public LogoPrefabExpectation(string path, int count)
            {
                Path = path;
                Count = count;
            }

            public string Path { get; }
            public int Count { get; }
        }

        private static readonly LogoPrefabExpectation[] Expectations =
        {
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN03_CommanderProfileContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN05_CampaignOperationsContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN06_MissionBriefingContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN07_LoadoutSquadPrepContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN11_OperationsDashboardContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN12_DistrictDetailActionsContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN15_InboxContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN16_EventsContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN17_RankingContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Popups/RewardUnlockPopup.prefab", 1),
            new("Assets/Game/Prefabs/UI/Popups/EndOfDayReportPopup.prefab", 1),
            new("Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchLanguageChoice.prefab", 1),
            new("Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab", 2)
        };

        [MenuItem("Game/UI/V3/Rebuild All Shared Brand Logo Screens")]
        public static void Build()
        {
            V3UiFoundationBuilder.BeginBatchBuild();
            try
            {
                V3UiFoundationBuilder.Build();
                SplashLoadingV3PrefabBuilder.Build();
                MainMenuV3PrefabBuilder.Build();
                CommanderProfileV3PrefabBuilder.Build();
                CampaignOperationsV3PrefabBuilder.Build();
                MissionBriefingV3PrefabBuilder.Build();
                LoadoutSquadPrepV3PrefabBuilder.Build();
                OperationsDashboardV3PrefabBuilder.Build();
                DistrictDetailActionsV3PrefabBuilder.Build();
                SkirmishSetupPrefabBuilder.Build();
                InboxV3PrefabBuilder.Build();
                EventsV3PrefabBuilder.Build();
                RankingV3PrefabBuilder.Build();
                ArmoryV3PrefabBuilder.Build();
                RewardUnlockV3PrefabBuilder.Build();
                EndOfDayReportV3PrefabBuilder.Build();
                FirstLaunchNarrativeV3PrefabBuilder.Build();
                Validate();
            }
            finally
            {
                V3UiFoundationBuilder.EndBatchBuild();
            }
        }

        [MenuItem("Game/UI/V3/Validate Shared Brand Logo")]
        public static void Validate()
        {
            Sprite canonicalLogo = AssetDatabase.LoadAssetAtPath<Sprite>(V3UiFoundationBuilder.MainMenuLogoPath);
            if (canonicalLogo == null)
                throw new FileNotFoundException($"Missing canonical V3 brand logo: {V3UiFoundationBuilder.MainMenuLogoPath}");

            int referenceCount = 0;
            foreach (LogoPrefabExpectation expectation in Expectations)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectation.Path);
                if (prefab == null)
                    throw new FileNotFoundException($"Missing V3 logo screen prefab: {expectation.Path}");

                Image[] images = prefab.GetComponentsInChildren<Image>(true);
                int count = images.Count(image => image.sprite == canonicalLogo);
                if (count != expectation.Count)
                    throw new InvalidOperationException(
                        $"V3 shared-logo reference mismatch on {expectation.Path}: expected={expectation.Count} actual={count}");
                referenceCount += count;

                foreach (Image image in images)
                {
                    if (image.sprite == null || image.sprite == canonicalLogo)
                        continue;

                    string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                    string key = (image.name + " " + Path.GetFileNameWithoutExtension(spritePath)).ToLowerInvariant();
                    if (key.Contains("brand_logo") || key.Contains("warline_logo") || key.Contains("warlinelogo"))
                        throw new InvalidOperationException(
                            $"Duplicate V3 brand sprite on {expectation.Path}: {spritePath}");
                }

                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    string value = text.text?.Trim();
                    if (string.Equals(value, "WARLINE", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException(
                            $"Procedural WARLINE logo copy remains on {expectation.Path}: {text.transform.GetHierarchyPath()}");
                }
            }

            Debug.Log(
                $"[V3SharedBrandLogoMigrationBuilder] result=Passed prefabs={Expectations.Length} references={referenceCount} " +
                $"sprite={V3UiFoundationBuilder.MainMenuLogoPath} atlas={V3UiFoundationBuilder.BrandAtlasPath} duplicate=0");
        }

        private static string GetHierarchyPath(this Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
#endif
