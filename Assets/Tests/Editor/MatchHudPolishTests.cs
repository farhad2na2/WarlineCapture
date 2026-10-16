#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.Reflection;
using Game.Configs;
using Game.Editor;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class MatchHudPolishTests
{
    private const string HudPath = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunFocusedValidation() => Run(true);
    public static void RunChecksOnly() => Run(false);

    private static void Run(bool capture)
    {
        try
        {
            if (capture) MatchHudV3PrefabBuilder.CapturePolishAfter();
            var test = new MatchHudPolishTests();
            foreach (bool persian in new[] { false, true })
            {
                foreach (float width in new[] { 1920f, 2400f })
                    test.LongInstructionsPreservePortraitAndAccessibleText(width, persian);
                test.CountdownHasAnIndependentNumericReadout(persian);
                test.PublicObjectiveScoreDoesNotReplaceTheCountdown(persian);
            }
            using (ValidationExit.SuppressProcessExit())
            {
                ValidationExit.ClearLastExitCode();
                M03PresentationTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new InvalidOperationException("Campaign layout regression failed.");
                AriaInstructionViewportTests.ValidateScrollTransitions();
                AriaTutorialPresentationV3PrefabTests.RunFocusedValidation();
                if (ValidationExit.LastExitCode != 0) throw new InvalidOperationException("Shared tutorial regression failed.");
            }
            Debug.Log("[MatchHudPolishTests] result=Passed layoutCases=4 timerCases=4 scrollTransitions=4 campaignPresentations=192 locales=en,fa-IR");
            ValidationExit.Passed();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("[MatchHudPolishTests] result=Failed");
            ValidationExit.Failed();
        }
    }

    [TestCase(1920f, false)] [TestCase(1920f, true)]
    [TestCase(2400f, false)] [TestCase(2400f, true)]
    public void LongInstructionsPreservePortraitAndAccessibleText(float width, bool persian)
    {
        string previous = GameLocalization.CurrentLocaleCode;
        var canvas = new GameObject("HUD polish layout QA", typeof(RectTransform), typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)canvas.transform).sizeDelta = new Vector2(width, 1080);
        try
        {
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), persian ? "fa-IR" : "en", false);
            var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HudPath), canvas.transform);
            foreach (var layout in hud.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) layout.RefreshLayout();
            hud.GetComponentInChildren<MissionHudTouchLayoutView>(true).RefreshLayout();
            var view = hud.GetComponentInChildren<AriaTutorialBriefingView>(true);
            view.Apply(AriaTutorialBriefingPrefabBuilder.CreateTargetLockPreviewModel(persian));
            view.SetPresentationVisible(true);
            view.ApplyAccessibility(false, false);
            Canvas.ForceUpdateCanvases(); view.RefreshContentLayout();
            var data = new SerializedObject(view);
            var header = (RectTransform)data.FindProperty("missionPortraitStage").objectReferenceValue;
            var clip = (RectTransform)data.FindProperty("missionPortraitClip").objectReferenceValue;
            float headerHeight = header.rect.height;
            float normalSize = view.BodyText.fontSize;
            string copy = persian ? "نیروها را انتخاب کن و هدف مشخص‌شده را لمس کن. " : "Select your squad, then tap the marked target. ";
            view.BodyText.text = string.Concat(System.Linq.Enumerable.Repeat(copy, 30));
            view.RefreshContentLayout(); Canvas.ForceUpdateCanvases();
            Assert.That(header.rect.height, Is.EqualTo(headerHeight).Within(.1), "Long prose must not shrink ARIA.");
            Assert.That(headerHeight, Is.GreaterThanOrEqualTo(88));
            Assert.That(headerHeight, Is.LessThanOrEqualTo(124));
            var corners = new Vector3[4]; clip.GetWorldCorners(corners);
            var bounds = new Bounds(header.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int i = 1; i < 4; i++) bounds.Encapsulate(header.InverseTransformPoint(corners[i]));
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(header.rect.xMin - .1));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(header.rect.xMax + .1));
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(header.rect.yMin - .1));
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(header.rect.yMax + .1));
            Assert.IsTrue(view.BodyText.GetComponentInParent<ScrollRect>().vertical);
            view.ApplyAccessibility(true, false);
            view.RefreshContentLayout(); Canvas.ForceUpdateCanvases();
            Assert.That(view.BodyText.fontSize, Is.GreaterThan(normalSize), "Measuring copy must preserve large text.");
            Assert.IsFalse(view.BodyText.enableAutoSizing);
            Assert.That(view.BodyText.alignment, Is.EqualTo(persian ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft));
            Assert.IsFalse(view.PortraitImage.raycastTarget);
        }
        finally { UnityEngine.Object.DestroyImmediate(canvas); GameLocalization.SetLocale(previous, false); }
    }

    [TestCase(false)] [TestCase(true)]
    public void PublicObjectiveScoreDoesNotReplaceTheCountdown(bool persian)
    {
        string previous = GameLocalization.CurrentLocaleCode;
        GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), persian ? "fa-IR" : "en", false);
        var view = SkirmishMatchView.Create();
        if (typeof(SkirmishMatchView).GetField("clock", Private).GetValue(view) == null)
            typeof(SkirmishMatchView).GetMethod("Awake", Private).Invoke(view, null);
        try
        {
            view.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)view.transform).sizeDelta = new Vector2(1920, 1080);
            typeof(SkirmishMatchView).GetField("previousPublicApplicable", Private).SetValue(view, true);
            typeof(SkirmishMatchView).GetMethod("RefreshClock", Private).Invoke(view, new object[] { "TIME LEFT  17:53" });
            typeof(SkirmishMatchView).GetMethod("PresentObjectiveClock", Private).Invoke(view, new object[] { persian ? "تکمیل 1 / 3\nزنده 2" : "COMPLETED 1 / 3\nALIVE 2" });
            typeof(SkirmishMatchView).GetMethod("PositionBelowWarning", Private).Invoke(view, null);
            Canvas.ForceUpdateCanvases();
            var value = (TMP_Text)typeof(SkirmishMatchView).GetField("clock", Private).GetValue(view);
            var heading = (TMP_Text)typeof(SkirmishMatchView).GetField("clockHeading", Private).GetValue(view);
            Assert.AreEqual("17:53", value.text);
            Assert.IsFalse(heading.text.Contains("17:53"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(heading.text));
            heading.ForceMeshUpdate(true, true);
            Assert.That(heading.preferredHeight, Is.LessThanOrEqualTo(heading.rectTransform.rect.height + 1), "Score and survivor rows must fit above the countdown. fa=" + persian);
            Assert.That(heading.fontSize, Is.EqualTo(persian ? 18 : 20).Within(.01f), "Locale refresh must preserve the score typography.");
            Assert.AreEqual(heading.fontSizeMin, heading.fontSizeMax, "Score rows cannot shrink below their readable size.");
            typeof(SkirmishMatchView).GetField("previousPublicApplicable", Private).SetValue(view, false);
            typeof(SkirmishMatchView).GetMethod("RestoreClockCaption", Private).Invoke(view, null);
            Assert.AreEqual(32, value.fontSize);
            Assert.IsFalse(heading.text.Contains("ALIVE"));
        }
        finally { UnityEngine.Object.DestroyImmediate(view.gameObject); GameLocalization.SetLocale(previous, false); }
    }

    [TestCase(false)] [TestCase(true)]
    public void CountdownHasAnIndependentNumericReadout(bool persian)
    {
        string previous = GameLocalization.CurrentLocaleCode;
        GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), persian ? "fa-IR" : "en", false);
        var view = SkirmishMatchView.Create();
        if (typeof(SkirmishMatchView).GetField("clock", Private).GetValue(view) == null)
            typeof(SkirmishMatchView).GetMethod("Awake", Private).Invoke(view, null);
        try
        {
            typeof(SkirmishMatchView).GetMethod("RefreshClock", Private).Invoke(view, new object[] { (persian ? "زمان باقی‌مانده" : "TIME LEFT") + "  17:53" });
            var value = (TMP_Text)typeof(SkirmishMatchView).GetField("clock", Private).GetValue(view);
            var heading = (TMP_Text)typeof(SkirmishMatchView).GetField("clockHeading", Private).GetValue(view);
            Assert.AreEqual("17:53", value.text);
            Assert.IsFalse(value.isRightToLeftText);
            Assert.AreNotSame(value, heading);
            Assert.IsFalse(heading.text.Contains("17:53"));
            typeof(SkirmishMatchView).GetMethod("RefreshClock", Private).Invoke(view, new object[] { "TIME LEFT  0:59" });
            Assert.AreEqual("0:59", value.text);
            Assert.That(value.color.g, Is.EqualTo(.65f).Within(.001));
        }
        finally { UnityEngine.Object.DestroyImmediate(view.gameObject); GameLocalization.SetLocale(previous, false); }
    }
}
#endif
