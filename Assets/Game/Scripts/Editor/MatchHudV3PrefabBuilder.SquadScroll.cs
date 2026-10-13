#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        private static Action<GameObject> squadScrollCaptureSetup;

        public static void BuildSquadScroll()
        {
            LoadAssets();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                ApplySquadPagination(root, FindDeepChild(root.transform, "SquadTray") as RectTransform);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new InvalidOperationException("Squad scrolling prefab save failed.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[SquadScrollBuild] result=Passed retiredAboveTrayChrome=true");
        }

        // Native Canvas rendering for visual review. Fixture group counts are not mission wins.
        public static void CaptureSquadScrollReview()
        {
            LoadAssets();
            string locale = GameLocalization.CurrentLocaleCode;
            try
            {
                foreach (var language in new[] { "en", "fa-IR" })
                {
                    GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), language, false);
                    foreach (int count in new[] { 1, 2, 5, 8 })
                    {
                        int rosterCount = count;
                        squadScrollCaptureSetup = instance =>
                        {
                            var tray = instance.GetComponentInChildren<MatchHudSquadTrayView>(true);
                            var sprites = new List<Sprite>();
                            for (int i = 0; i < 5; i++)
                            { tray.TryGetPortraitSprite((MatchHudSquadTraySlot)(i + 1), out var sprite); sprites.Add(sprite); }
                            tray.ConfigureExpandedRoster(destination =>
                            {
                                destination.Clear();
                                for (int i = 0; i < rosterCount; i++)
                                    destination.Add(new UiMatchHudSquadTrayCardModel(true,
                                        (language == "en" ? "SQUAD " : "گروه ") + (i + 1), "4", 1,
                                        (uint)(i + 1), 4, i + 1, i == 0));
                            }, id => sprites[(int)(id - 1) % 5]);
                            typeof(MatchHudSquadTrayView).GetMethod("RefreshScrollingRoster", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tray, new object[] { true });
                        };
                        Capture("/private/tmp/squad-scroll-" + language + "-" + count + "-1920.png", 1920, 1080, applyLocalization: true);
                        if (count == 8) Capture("/private/tmp/squad-scroll-" + language + "-8-2400.png", 2400, 1080, applyLocalization: true);
                    }
                }
                Debug.Log("[SquadScrollCapture] result=Passed counts=1,2,5,8 locales=en,fa-IR captures=10");
            }
            finally { squadScrollCaptureSetup = null; GameLocalization.SetLocale(locale, false); }
        }
    }
}
#endif
