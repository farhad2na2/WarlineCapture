#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game.Configs;
using Game.UI.Contracts;
using Game.UI.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class MatchHudV3PrefabBuilder
    {
        public static void CapturePolishBefore() => CapturePolish("Before");
        public static void CapturePolishAfter() => CapturePolish("After");

        private static void CapturePolish(string phase)
        {
            LoadAssets();
            string previous = GameLocalization.CurrentLocaleCode;
            string directory = "Design/AgentReports/MatchHudPolish20261016/" + phase + "Preview";
            Directory.CreateDirectory(directory);
            try
            {
                foreach (string language in new[] { "en", "fa-IR" })
                {
                    GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), language, false);
                    squadScrollCaptureSetup = root => ConfigurePolishPreview(root, language);
                    foreach (int width in new[] { 1920, 1280, 2400 })
                        Capture(directory + "/hud-" + language + "-" + width + ".png", width,
                            width == 1280 ? 720 : 1080, applyLocalization: true);
                }
                Debug.Log("[MatchHudPolishPreview] result=Passed phase=" + phase +
                    " captures=6 locales=en,fa-IR widths=1280,1920,2400 source=current-prefab presentationFixture=1 normalInput=NotClaimed");
            }
            finally { squadScrollCaptureSetup = null; GameLocalization.SetLocale(previous, false); }
        }

        private static void ConfigurePolishPreview(GameObject root, string language)
        {
            bool fa = language == "fa-IR";
            var tray = root.GetComponentInChildren<MatchHudSquadTrayView>(true);
            var portraits = new List<Sprite>();
            for (int i = 0; i < 5; i++)
            { tray.TryGetPortraitSprite((MatchHudSquadTraySlot)(i + 1), out var sprite); portraits.Add(sprite); }
            tray.ConfigureExpandedRoster(destination =>
            {
                destination.Clear();
                for (int i = 0; i < 8; i++)
                    destination.Add(new UiMatchHudSquadTrayCardModel(true,
                        fa ? "تفنگدار" : "RIFLE", "4", 1, (uint)(i + 1), 4, i + 1, i == 0));
            }, id => portraits[(int)(id - 1) % 5]);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(MatchHudSquadTrayView).GetMethod("RefreshScrollingRoster", flags).Invoke(tray, new object[] { true });
            var feedback = root.GetComponentInChildren<BattleHudRuntimeFeedbackView>(true);
            feedback?.FeedbackPanel.SetActive(false);
            FindDeepChild(root.transform, "ThreatJumpPanel")?.gameObject.SetActive(false);
            FindDeepChild(root.transform, "ObjectivesPanel")?.gameObject.SetActive(false);
            var aria = root.GetComponentInChildren<AriaTutorialBriefingView>(true);
            SkirmishSetupBriefing.TryLoad("S005", 155926, out var setup);
            var model = new UiAssistantPanelModel(1,"","","",false,false,true,
                fa ? "حمله به پایگاه" : "BASE ASSAULT", SkirmishSetupBriefing.MatchHelp(setup, fa),
                "","",false,false,false,false,"","");
            aria.Apply(model);
            aria.SetPresentationVisible(true);
            typeof(AriaTutorialBriefingView).GetMethod("PresentWatch", flags).Invoke(aria,
                new object[] { new AriaPlayModel(AriaPlayPhase.Manual, Vector2.zero, false, 0), true });
            aria.ApplyAccessibility(false, false);
            aria.RefreshContentLayout();
            var match = SkirmishMatchView.Create();
            var matchCanvas = match.GetComponent<Canvas>();
            matchCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            matchCanvas.worldCamera = root.GetComponentInParent<Canvas>().worldCamera;
            matchCanvas.planeDistance = 8;
            if (typeof(SkirmishMatchView).GetField("hud", flags).GetValue(match) == null)
                typeof(SkirmishMatchView).GetMethod("Awake", flags).Invoke(match, null);
            foreach (var name in new[] { "playerHealth", "enemyHealth" })
                UiLocalizedText.Set((TMP_Text)typeof(SkirmishMatchView).GetField(name, flags).GetValue(match), "800 / 800");
            var refreshClock = typeof(SkirmishMatchView).GetMethod("RefreshClock", flags);
            string clock = (fa ? "زمان باقی‌مانده" : "TIME LEFT") + "  17:53";
            if (refreshClock != null && typeof(SkirmishMatchView).GetField("clockHeading", flags)?.GetValue(match) != null)
                refreshClock.Invoke(match, new object[] { clock });
            else UiLocalizedText.Set((TMP_Text)typeof(SkirmishMatchView).GetField("clock", flags).GetValue(match), clock);
            foreach (var icon in match.GetComponentsInChildren<HudIconButtonView>(true))
                typeof(HudIconButtonView).GetMethod("Refresh", flags).Invoke(icon, null);
            Canvas.ForceUpdateCanvases();
        }
    }
}
#endif
