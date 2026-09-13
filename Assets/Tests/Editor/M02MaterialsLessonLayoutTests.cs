using Game.Configs;
using Game.Editor;
using Game.Narrative.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

public sealed class M02MaterialsLessonLayoutTests
{
    [Test]
    public void TutorialStaysAboveInheritedPopupCanvasAndRestoresItsOrder()
    {
        var popup = new GameObject("Popup sorting owner", typeof(Canvas));
        var local = new GameObject("Popup batching canvas", typeof(Canvas));
        local.transform.SetParent(popup.transform, false);
        popup.GetComponent<Canvas>().sortingOrder = 200;
        local.GetComponent<Canvas>().overrideSorting = false;
        var occlusion = local.AddComponent<BuildDrawerHudOcclusionView>();
        var hud = new GameObject("HUD sorting owner", typeof(RectTransform), typeof(Canvas));
        var aria = new GameObject("ARIA", typeof(RectTransform), typeof(Canvas));
        aria.transform.SetParent(hud.transform, false);
        aria.GetComponent<Canvas>().overrideSorting = true;
        aria.GetComponent<Canvas>().sortingOrder = 10;
        try
        {
            var view = aria.AddComponent<AriaTutorialBriefingView>();
            typeof(BuildDrawerHudOcclusionView).GetMethod("PreserveTutorial", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(occlusion, new object[] { view });
            Assert.AreEqual(201, aria.GetComponent<Canvas>().sortingOrder);
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(aria, true);
            Assert.IsTrue(aria.GetComponent<Canvas>().overrideSorting, "Rebinding ARIA must preserve the popup's input ordering.");
            occlusion.RestoreOcclusion();
            Assert.AreEqual(10, aria.GetComponent<Canvas>().sortingOrder);
        }
        finally { Object.DestroyImmediate(local); Object.DestroyImmediate(popup); Object.DestroyImmediate(aria); Object.DestroyImmediate(hud); }
    }

    [Test]
    public void EveryM2InstructionFitsEnglishAndFarsiAtBothTextSizes()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab");
        var canvasRoot = new GameObject("M2 ARIA layout QA", typeof(RectTransform), typeof(Canvas));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvasRoot.transform).sizeDelta = new Vector2(1920, 1080);
        var instance = Object.Instantiate(prefab, canvasRoot.transform);
        string previousLocale = GameLocalization.CurrentLocaleCode;
        try
        {
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), previousLocale, false);
            instance.GetComponentInChildren<MissionHudTouchLayoutView>(true).Apply(true);
            var view = instance.GetComponentInChildren<AriaTutorialBriefingView>(true);
            Assert.IsTrue(view.TryBindHierarchy());
            foreach (bool persian in new[] { false, true })
            foreach (bool large in new[] { false, true })
            for (byte step = 2; step <= 8; step++)
            {
                GameLocalization.SetLocale(persian ? "fa-IR" : "en", false);
                M02EstablishBaseTextCatalog.TryGetTutorial(step, persian ? FirstLaunchNarrativeLanguage.Persian : FirstLaunchNarrativeLanguage.English, out string title, out string body);
                if (step == 5)
                {
                    Assert.AreEqual(title, GameLocalization.Get("tutorial.m02.materials.title"));
                    Assert.AreEqual(body, GameLocalization.Get("tutorial.m02.materials.body"));
                }
                var model = new UiAssistantPanelModel(1, true, 0, UiAssistantGoalRowModel.Empty, UiAssistantGoalRowModel.Empty, UiAssistantGoalRowModel.Empty,
                    UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty,
                    UiAssistantMessageRowModel.Empty, UiAssistantMessageRowModel.Empty, UiAssistantTargetLockModel.Empty, UiAssistantNarrationModel.Empty, true, title,
                    body, "", "DO IT", true, true, false, false, "", "",
                    largeTextEnabled: large, tutorialStep: step, tutorialStepCount: 9, tutorialRightToLeft: persian);
                view.Apply(model);
                view.ApplyAccessibility(large, false);
                view.SetPresentationVisible(true);
                Canvas.ForceUpdateCanvases();
                foreach (var text in new[] { view.TitleText, view.BodyText, view.ProgressText,
                    view.ShowMeButton.GetComponentInChildren<TMP_Text>(), view.DoItButton.GetComponentInChildren<TMP_Text>() })
                {
                    text.ForceMeshUpdate(true, true);
                    Assert.IsFalse(text.isTextOverflowing || text.isTextTruncated,
                        $"M2 step={step}, Farsi={persian}, large={large}, {text.name}");
                }
            }
        }
        finally { Object.DestroyImmediate(canvasRoot); GameLocalization.SetLocale(previousLocale, false); }
    }
}
