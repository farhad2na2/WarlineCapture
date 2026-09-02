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
    /// that all of them use the exact Main Menu V3 sprite from one shared atlas.
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
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN18_CommandFeedContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Content/SCN19_ArmoryContent.prefab", 1),
            new("Assets/Game/Prefabs/UI/Shell/Popups/POP12_ResourceExchangePopup.prefab", 1),
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
            GameObject canonicalLogo = AssetDatabase.LoadAssetAtPath<GameObject>(V3UiFoundationBuilder.BrandLogoPrefabPath);
            if (canonicalLogo == null)
                throw new FileNotFoundException($"Missing canonical V3 brand logo prefab: {V3UiFoundationBuilder.BrandLogoPrefabPath}");

            int referenceCount = 0;
            foreach (LogoPrefabExpectation expectation in Expectations)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(expectation.Path);
                if (prefab == null)
                    throw new FileNotFoundException($"Missing V3 logo screen prefab: {expectation.Path}");

                Transform[] transforms = prefab.GetComponentsInChildren<Transform>(true);
                Transform[] logoRoots = transforms
                    .Where(transform => string.Equals(transform.name, "SharedMainMenuLogo", StringComparison.Ordinal))
                    .ToArray();
                int count = logoRoots.Length;
                if (count != expectation.Count)
                    throw new InvalidOperationException(
                        $"V3 shared-logo reference mismatch on {expectation.Path}: expected={expectation.Count} actual={count}");
                referenceCount += count;

                foreach (Transform logoRoot in logoRoots)
                {
                    Image sharedImage = logoRoot.GetComponent<Image>();
                    string spritePath = sharedImage != null && sharedImage.sprite != null
                        ? AssetDatabase.GetAssetPath(sharedImage.sprite)
                        : string.Empty;
                    if (!string.Equals(spritePath, V3UiFoundationBuilder.MainMenuLogoPath, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"V3 logo does not reference the canonical shared-atlas sprite on {expectation.Path}: " +
                            $"{logoRoot.GetHierarchyPath()} sprite={spritePath}");
                    }
                    if (!sharedImage.preserveAspect || sharedImage.raycastTarget)
                        throw new InvalidOperationException(
                            $"V3 shared logo must preserve aspect and ignore raycasts on {expectation.Path}: {logoRoot.GetHierarchyPath()}");
                    if (logoRoot.GetComponentsInChildren<TMP_Text>(true).Length != 0)
                        throw new InvalidOperationException(
                            $"Procedural logo text remains under the shared logo root on {expectation.Path}: {logoRoot.GetHierarchyPath()}");
                }

                Image[] images = prefab.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    if (image.sprite == null)
                        continue;

                    string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                    string key = (image.name + " " + Path.GetFileNameWithoutExtension(spritePath)).ToLowerInvariant();
                    bool looksLikeBrand =
                        key.Contains("brand_logo") ||
                        key.Contains("warline_logo") ||
                        key.Contains("warlinelogo");
                    bool isCanonical =
                        string.Equals(spritePath, V3UiFoundationBuilder.MainMenuLogoPath, StringComparison.Ordinal) &&
                        IsWithinSharedLogo(image.transform);
                    if (looksLikeBrand && !isCanonical)
                        throw new InvalidOperationException(
                            $"Duplicate V3 brand sprite on {expectation.Path}: {spritePath}");
                }

                foreach (TMP_Text text in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    string value = text.text?.Trim();
                    if (string.Equals(value, "WARLINE", StringComparison.OrdinalIgnoreCase) &&
                        !IsWithinSharedLogo(text.transform))
                        throw new InvalidOperationException(
                            $"Procedural WARLINE logo copy remains on {expectation.Path}: {text.transform.GetHierarchyPath()}");
                }
            }

            Debug.Log(
                $"[V3SharedBrandLogoMigrationBuilder] result=Passed prefabs={Expectations.Length} references={referenceCount} " +
                $"prefab={V3UiFoundationBuilder.BrandLogoPrefabPath} sprite={V3UiFoundationBuilder.MainMenuLogoPath} " +
                $"atlas={V3UiFoundationBuilder.BrandAtlasPath} canonicalBitmap=1 duplicate=0");
        }

        private static bool IsWithinSharedLogo(Transform transform)
        {
            while (transform != null)
            {
                if (string.Equals(transform.name, "SharedMainMenuLogo", StringComparison.Ordinal))
                    return true;
                transform = transform.parent;
            }

            return false;
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
