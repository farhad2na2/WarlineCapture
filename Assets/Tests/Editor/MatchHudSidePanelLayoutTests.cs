using System;
using System.Collections.Generic;
using System.Linq;
using Game.Configs;
using Game.Editor;
using Game.UI.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// W39 (match UI polish ui-a): Board 1 left column spacing and Board 3 ARIA panel states.
// The world-space canvas is sized in screen pixels, so every value below is a reference pixel.
public sealed class MatchHudSidePanelLayoutTests
{
    private const string Prefab = "Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab";
    private const float Tolerance = 1.5f;

    [Test]
    public void ReferencePixelsConvertToHudUnits()
    {
        Assert.That(MatchHudSidePanelLayout.Pixels(MatchHudSidePanelLayout.Units(16)), Is.EqualTo(16).Within(1e-4));
        Assert.That(MatchHudSidePanelLayout.Units(1080), Is.EqualTo(941).Within(1e-3));
        Assert.That(MatchHudSidePanelLayout.Pixels(AriaTutorialBriefingView.ButtonHeight), Is.GreaterThanOrEqualTo(MatchHudSidePanelLayout.MinimumTargetPx));
        Assert.That(MatchHudSidePanelLayout.MinimapBottom(200, 230), Is.EqualTo(200 + MatchHudSidePanelLayout.Units(24)).Within(1e-4));
        Assert.That(MatchHudSidePanelLayout.MinimapBottom(float.NaN, 230), Is.EqualTo(230));
        Assert.That(MatchHudSquadTrayView.PageViewportWidth, Is.EqualTo(701).Within(1e-4), "Five 133-unit cards, 7-unit gaps, 4-unit insets.");
    }

    [TestCase(true, 2, true, true, AriaPanelState.ActiveTwoActions)]
    [TestCase(true, 1, true, true, AriaPanelState.ActiveOneAction)]
    [TestCase(true, 0, true, true, AriaPanelState.ActiveNoAction)]
    [TestCase(false, 0, true, false, AriaPanelState.Stopped)]
    [TestCase(true, 1, true, false, AriaPanelState.Stopped)]
    [TestCase(true, 1, false, false, AriaPanelState.GuidanceWithoutPlay)]
    [TestCase(false, 0, false, false, AriaPanelState.HeaderOnly)]
    public void PanelStateFollowsVisibleContent(bool guidance, int actions, bool playStop, bool active, AriaPanelState expected)
        => Assert.AreEqual(expected, AriaPanelStates.Classify(guidance, actions, playStop, active));

    [TestCase(1920f, false)] [TestCase(1920f, true)]
    [TestCase(2400f, false)] [TestCase(2400f, true)]
    public void AriaStatesUseInsetsGapsAndLabelledTargets(float width, bool persian)
    {
        using var hud = new Hud(width, persian);
        foreach (var (state, expected, names) in new[]
        {
            ("two", AriaPanelState.ActiveTwoActions, new[] { "ShowMeButton", "FieldGuide", "WatchAriaPlay" }),
            ("one", AriaPanelState.ActiveOneAction, new[] { "FieldGuide", "WatchAriaPlay" }),
            ("stopped", AriaPanelState.Stopped, new[] { "WatchAriaPlay" }),
        })
        {
            hud.ApplyAria(state);
            string context = state + " " + width + (persian ? " fa" : " en");
            Assert.AreEqual(expected, hud.Aria.PanelState, context);
            var rail = hud.Bounds((RectTransform)hud.Aria.transform);
            var buttons = hud.Aria.GetComponentsInChildren<Button>(false).Where(b => b.gameObject.activeInHierarchy && b.transform != hud.Aria.transform)
                .Select(b => (b, r: hud.Bounds((RectTransform)b.transform))).Where(x => x.r.height > 20)
                .OrderByDescending(x => x.r.yMax).ToList();
            CollectionAssert.AreEquivalent(names, buttons.Select(x => x.b.name).ToArray(), context);
            for (int i = 0; i < buttons.Count; i++)
            {
                var (button, r) = buttons[i];
                var label = button.GetComponentInChildren<TMP_Text>(false);
                Assert.IsFalse(string.IsNullOrWhiteSpace(label is RTLTMPro.RTLTextMeshPro rtl ? rtl.OriginalText : label?.text), context + " unlabelled " + button.name);
                Assert.That(r.height, Is.GreaterThanOrEqualTo(64 - .5f), context + " " + button.name);
                Assert.That(r.xMin - rail.xMin, Is.EqualTo(16).Within(Tolerance), context + " left inset " + button.name);
                Assert.That(rail.xMax - r.xMax, Is.EqualTo(16).Within(Tolerance), context + " right inset " + button.name);
                if (i > 0) Assert.That(buttons[i - 1].r.yMin - r.yMax, Is.EqualTo(12).Within(Tolerance), context + " gap above " + button.name);
            }
            Assert.That(buttons[^1].r.yMin - rail.yMin, Is.EqualTo(16).Within(Tolerance), context + " panel sized from content");
            var status = hud.Aria.StatusLine;
            bool statusShown = status != null && status.gameObject.activeInHierarchy;
            Assert.AreEqual(state == "stopped", statusShown, context + " status line");
            if (state == "two")
                Assert.AreEqual("HudAction_" + HudButtonIcon.Camera, hud.Aria.ShowMeButton.transform.Find("HudActionIcon").GetComponent<Image>().sprite.name,
                    "Show Me uses the native camera-focus glyph.");
        }
    }

    [TestCase(1920f, false)] [TestCase(1920f, true)]
    [TestCase(2400f, false)] [TestCase(2400f, true)]
    public void LeftColumnSharesInsetAndGutters(float width, bool persian)
    {
        using var hud = new Hud(width, persian);
        foreach (bool roster in new[] { false, true })
        {
            hud.ApplyLeftColumn(tall: !roster, roster: roster);
            string context = (roster ? "roster " : "categories ") + width + (persian ? " fa" : " en");
            var map = hud.Bounds(hud.Dock.Minimap);
            var card = hud.Bounds(hud.Selection.VisiblePanelRect);
            var tray = hud.Bounds((RectTransform)hud.Tray.transform);
            var cards = hud.Tray.GetComponentsInChildren<Button>(false).Where(b => b.transform.parent.name == "SquadScrollContent")
                .Select(b => hud.Bounds((RectTransform)b.transform)).Where(r => r.xMin >= tray.xMin - .5f && r.xMax <= tray.xMax + .5f)
                .OrderBy(r => r.xMin).ToList();
            Assert.That(cards.Count, Is.EqualTo(5), context + " five presented cards");
            float left = hud.CanvasLeft;
            Assert.That(card.xMin - left, Is.EqualTo(16).Within(Tolerance), context + " card inset");
            Assert.That(map.xMin - left, Is.EqualTo(16).Within(Tolerance), context + " minimap inset");
            Assert.That(cards[0].xMin - left, Is.EqualTo(16).Within(Tolerance), context + " first card inset");
            Assert.That(card.yMin - map.yMax, Is.GreaterThanOrEqualTo(24 - Tolerance), context + " card above minimap");
            Assert.That(map.yMin - cards.Max(r => r.yMax), Is.EqualTo(24).Within(Tolerance), context + " minimap above cards");
            Assert.That(cards.Min(r => r.yMin) - hud.CanvasBottom, Is.EqualTo(16).Within(Tolerance), context + " cards above bottom safe edge");
            for (int i = 1; i < cards.Count; i++)
                Assert.That(cards[i].xMin - cards[i - 1].xMax, Is.EqualTo(8).Within(Tolerance), context + " card gap " + i);
            var strip = hud.Bounds(hud.CommandRail);
            Assert.That(strip.xMin - cards[^1].xMax, Is.GreaterThanOrEqualTo(12 - Tolerance), context + " cards clear of command strip");
            Assert.That(tray.xMax, Is.LessThanOrEqualTo(strip.xMin + .5f), context + " command strip does not overlap the tray");
            var commands = hud.Bounds((RectTransform)hud.Selection.CommandWheelOpenButton.transform);
            Assert.That(commands.height, Is.GreaterThanOrEqualTo(64 - .5f), context + " COMMANDS target");
            Assert.That(commands.xMin - card.xMin, Is.EqualTo(16).Within(Tolerance), context + " card padding");
        }
    }

    private sealed class Hud : IDisposable
    {
        private readonly GameObject canvas;
        private readonly string previousLocale;
        public readonly AriaTutorialBriefingView Aria;
        public readonly MatchHudSelectionPanelView Selection;
        public readonly MatchHudSquadTrayView Tray;
        public readonly MissionHudTouchLayoutView Dock;
        public readonly RectTransform CommandRail;
        private readonly RectTransform utility;
        private readonly bool persian;

        public Hud(float width, bool persian)
        {
            this.persian = persian;
            previousLocale = GameLocalization.CurrentLocaleCode;
            GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), persian ? "fa-IR" : "en", false);
            canvas = new GameObject("W39 layout QA", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(width, 1080);
            var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), canvas.transform);
            var rect = (RectTransform)hud.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            foreach (var layout in hud.GetComponentsInChildren<MainMenuV3SectionLayoutView>(true)) layout.RefreshLayout();
            Aria = hud.GetComponentsInChildren<AriaTutorialBriefingView>(true).First(v => v.name == "AriaAssistantButton");
            Selection = hud.GetComponentInChildren<MatchHudSelectionPanelView>(true);
            Tray = hud.GetComponentInChildren<MatchHudSquadTrayView>(true);
            Dock = hud.GetComponentInChildren<MissionHudTouchLayoutView>(true);
            CommandRail = hud.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "CommandRail");
            utility = Aria.transform.Find("M03Actions") as RectTransform;
        }

        public float CanvasLeft => Bounds((RectTransform)canvas.transform).xMin;
        public float CanvasBottom => Bounds((RectTransform)canvas.transform).yMin;

        public void ApplyAria(string state)
        {
            bool two = state == "two", stopped = state == "stopped";
            Aria.Apply(new UiAssistantPanelModel(1, string.Empty, string.Empty, string.Empty, false, false, !stopped,
                stopped ? string.Empty : persian ? "سیگنال را پیدا کن" : "LOCATE THE SIGNAL",
                stopped ? string.Empty : persian ? "قبل از حرکت گروه، محدودهٔ اسکن را نشان بده." : "Show the scan area before moving your squad.",
                "HIGH", "DO IT", two, false, true, false, "PLAYER CONTROL", string.Empty));
            Aria.ApplyAccessibility(false, false);
            Aria.SetPresentationVisible(!stopped);
            var alert = Aria.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.name == "AlertCue");
            if (alert != null) { alert.text = string.Empty; alert.gameObject.SetActive(false); }
            utility.gameObject.SetActive(!stopped);
            foreach (Transform child in utility) child.gameObject.SetActive(!stopped && child.name == "FieldGuide");
            Aria.PresentWatch(stopped ? default : new AriaPlayModel(AriaPlayPhase.Observing, Vector2.zero, false, 1), true);
            Settle();
        }

        public void ApplyLeftColumn(bool tall, bool roster)
        {
            UiShellRuntimeGateway.Register(roster ? new W39ExpandedSkirmishGateway() : null);
            var portraits = new Sprite[5];
            for (int i = 0; i < 5; i++) Tray.TryGetPortraitSprite((MatchHudSquadTraySlot)(i + 1), out portraits[i]);
            Tray.SetSelectedSlot(MatchHudSquadTraySlot.Soldiers);
            Selection.Apply(new MatchHudSelectionPanelModel(true, "RIFLE SQUAD", "SQUAD 1", "MOVING TO MARKER", "120 / 120", 1f,
                Selection.ResolveFallbackPortraitSprite(SelectionSummaryPortraitKind.Soldiers), SelectionSummaryPortraitKind.Soldiers,
                true, null, true, true, true));
            Selection.ApplyTransportPassengers(new MatchHudTransportPassengersModel(true, false, new UiEntityHandle(1, 1), 0, 4, true,
                Array.Empty<MatchHudSelectionPanelPassengerItemModel>(), soldierPassengerCount: 0, soldierCapacity: tall ? 4 : 0));
            Selection.ShowSelection();
            var models = Enumerable.Range(0, 8).Select(i => new UiMatchHudSquadTrayCardModel(true, "RIFLE (4)", "4/4", 1f, (uint)(i + 1), 4, i + 1, i == 0)).ToList();
            Tray.ConfigureExpandedRoster(list => { list.Clear(); list.AddRange(models); }, id => portraits[(int)((id - 1) % 5)]);
            var method = typeof(MatchHudSquadTrayView).GetMethod(roster ? "RefreshScrollingRoster" : "RestoreCategoryCards",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method.Invoke(Tray, roster ? new object[] { true } : Array.Empty<object>());
            Settle();
        }

        private void Settle()
        {
            for (int i = 0; i < 3; i++)
            {
                Canvas.ForceUpdateCanvases();
                Dock.RefreshLayout();
                var sel = typeof(MatchHudSelectionPanelView).GetMethod("LayoutCompactSelection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                sel.Invoke(Selection, null);
                Aria.RefreshContentLayout();
            }
            Canvas.ForceUpdateCanvases();
        }

        public Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var parent = canvas.transform;
            Vector3 a = parent.InverseTransformPoint(corners[0]), b = parent.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        public void Dispose()
        {
            UiShellRuntimeGateway.Register(null);
            UnityEngine.Object.DestroyImmediate(canvas);
            GameLocalization.SetLocale(previousLocale, false);
        }
    }
}
