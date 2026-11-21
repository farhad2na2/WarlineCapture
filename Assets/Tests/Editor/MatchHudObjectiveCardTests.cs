#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using Game.Configs;
using Game.Editor;
using Game.Skirmish.Contracts;
using Game.UI.Runtime;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Card = Game.UI.Runtime.SkirmishObjectiveCardLayout;

// W40 rules for the compact objective card (Board 2) and HUD ownership colours (Board 4).
public sealed class MatchHudObjectiveCardTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var colour); return colour; }
    private static void Same(Color expected, Color actual, string what)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(.005f), what);
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(.005f), what);
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(.005f), what);
    }

    [Test]
    public void OwnershipMapsToTheApprovedColourGlyphAndRole()
    {
        Assert.AreEqual(HudOwnership.Neutral, Card.Resolve(0, 1));
        Assert.AreEqual(HudOwnership.Friendly, Card.Resolve(1, 1));
        Assert.AreEqual(HudOwnership.Enemy, Card.Resolve(2, 1));
        Assert.AreEqual(HudOwnership.Friendly, Card.Resolve(2, 2), "ownership follows the observer");
        Same(Hex("#49BD62"), Card.Colour(HudOwnership.Friendly), "friendly green");
        Same(Hex("#F45C3B"), Card.Colour(HudOwnership.Enemy), "enemy red-orange");
        Same(Hex("#BBC7CD"), Card.Colour(HudOwnership.Neutral), "neutral silver");
        Assert.AreEqual(HudOwnershipGlyph.Shield, Card.Glyph(HudOwnership.Friendly));
        Assert.AreEqual(HudOwnershipGlyph.Diamond, Card.Glyph(HudOwnership.Enemy));
        Assert.AreEqual(HudOwnershipGlyph.Ring, Card.Glyph(HudOwnership.Neutral));
        Assert.AreEqual(HudButtonRole.Friendly, Card.Role(HudOwnership.Friendly));
        Assert.AreEqual(HudButtonRole.Enemy, Card.Role(HudOwnership.Enemy));
        Assert.AreEqual(HudButtonRole.Unowned, Card.Role(HudOwnership.Neutral));
    }

    // Live S031 showed the Base Assault sentence: each family states its own objective in EN and FA.
    [Test]
    public void NextActionUsesTheFamilyObjectiveCopy()
    {
        var cases = new[]
        {
            (SkirmishObjectiveKind.FrontlineControl, SkirmishPublicGoalPerspective.Capture, "zones"),
            (SkirmishObjectiveKind.Breakthrough, SkirmishPublicGoalPerspective.Evacuate, "riflemen"),
            (SkirmishObjectiveKind.ConvoyEscort, SkirmishPublicGoalPerspective.Escort, "convoy trucks"),
        };
        foreach (var (kind, perspective, word) in cases)
        {
            string key = Card.ObjectiveCopyKey(kind, perspective);
            Assert.IsNotNull(key, kind.ToString());
            var row = SkirmishObjectiveCopyTable.Rows.Single(r => r.Key == key);
            StringAssert.Contains(word, row.English, kind + " sentence");
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.Farsi), kind + " Farsi");
        }
        Assert.IsNull(Card.ObjectiveCopyKey(SkirmishObjectiveKind.BaseAssault, SkirmishPublicGoalPerspective.BaseAssault), "Base Assault keeps the session objective");
        Assert.IsNull(Card.ObjectiveCopyKey(SkirmishObjectiveKind.Breakthrough, SkirmishPublicGoalPerspective.DefendExits), "only the player's own side");
    }

    [Test]
    public void CardSizePaddingAndPlacementFollowTheBoard()
    {
        Assert.AreEqual(184f, Card.Height, "16 padding + 72 purpose row + 8 gap + 72 status row + 16 padding");
        Assert.AreEqual(12f, Card.ChipGap, "12 px gaps between status cards");
        Assert.GreaterOrEqual(Card.ControlHeight, 64f, "status controls are thumb-sized targets");
        Assert.AreEqual(16f, Card.Padding);
        Same(Hex("#E7562C"), Card.Keyline, "warm keyline");
        Assert.That(Card.KeylineWidth, Is.InRange(2f, 3f));
        Assert.AreEqual(560f, Card.CardWidth(380f, 420f, 1920f), "English Base Assault card is the 560 px target");
        Assert.AreEqual(700f + 32f, Card.CardWidth(700f, 300f, 1920f), "grows to fit its status row");
        Assert.AreEqual(Card.MaxWidth(1920f), Card.CardWidth(2000f, 300f, 1920f), "never wider than the space between the side panels");
        Assert.LessOrEqual(Card.MaxWidth(1920f), 1920f - 2f * SkirmishBaseAlertRowLayout.SidePanelReserve);
        Assert.LessOrEqual(Card.MaxWidth(2400f), 2400f - 2f * SkirmishBaseAlertRowLayout.SidePanelReserve);
        Assert.AreEqual(93f + 12f, Card.Top(93f, 0f), "12 px under the resource bar");
        Assert.AreEqual(220f + 12f, Card.Top(93f, 220f), "under a visible alert banner");
        Assert.AreEqual(Card.DefaultTop, Card.Top(0f, 0f));
    }

    [Test]
    public void ChipsAreSizedForBothTextLines()
    {
        float lead = Card.ChipInset + Card.ChipIcon + Card.ChipIconGap;
        Assert.AreEqual(Card.ChipMinWidth, Card.ChipWidth(20f, 20f, false), "short copy keeps a thumb-sized chip");
        Assert.GreaterOrEqual(Card.ChipWidth(150f, 40f, false), lead + 150f + Card.ChipInset, "title line fits");
        Assert.GreaterOrEqual(Card.ChipWidth(60f, 200f, true), Card.ChipInset + Card.OwnershipGlyphSize + Card.ChipGlyphGap + 200f + Card.ChipInset, "ownership line fits");
        Assert.GreaterOrEqual(Card.ChipWidth(80f, 40f, true, 90f), lead + 80f + Card.TagGap + 90f + Card.ChipInset, "status tag fits beside the title");
        Assert.AreEqual(Card.ChipMaxWidth, Card.ChipWidth(900f, 10f, false), "auto-size takes over past the maximum");
    }

    private static float Measure(TMP_FontAsset font, string text, float size, bool farsi)
    {
        var go = new GameObject("W40Measure", typeof(RectTransform));
        try
        {
            var label = go.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label.GetPreferredValues(farsi ? V3LocalizedTextBindingView.ShapeForRendering(text) : text, float.PositiveInfinity, float.PositiveInfinity).x;
        }
        finally { Object.DestroyImmediate(go); }
    }

    // S011: NEUTRAL wrapped into NEUTRA / L. Ownership and zone names fit one line at full size in EN and FA.
    [Test]
    public void ZoneOwnershipAndCorridorNamesFitOneLineInEnglishAndFarsi()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath);
        Assert.IsNotNull(catalog);
        var latin = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Oxanium-Bold SDF t:TMP_FontAsset").First()));
        var persian = catalog.FindLocale(GameLocalization.PersianLocaleCode).FontAsset as TMP_FontAsset;
        Assert.IsNotNull(latin); Assert.IsNotNull(persian);
        var cases = new[]
        {
            (false, "ZONE A", "NEUTRAL", "CONTESTED"), (false, "ZONE C", "OPPONENT", "NEUTRALIZE 99%"), (false, "CORRIDOR A", "20 / 20 s", ""),
            (false, "SERVICE PAD", "WITHIN 12 m · MATERIALS 900", ""), (false, "DESTINATION", "DELIVER 0 / 2", ""),
            (true, "منطقه A", "بی‌طرف", "درگیری"), (true, "مسیر A", "20 / 20 ثانیه", ""), (true, "محل تعمیر", "شعاع ۱۲ متر · مصالح 900", ""),
        };
        foreach (var (fa, title, detail, tag) in cases)
        {
            var font = fa ? persian : latin;
            float t = Measure(font, title, Card.TitleMaxSize, fa), d = Measure(font, detail, Card.DetailMaxSize, fa);
            float g = string.IsNullOrEmpty(tag) ? 0f : Measure(font, tag, Card.TagSize, fa);
            float width = Card.ChipWidth(t, d, true, g);
            Assert.LessOrEqual(width, Card.ChipMaxWidth, title + " chip within its maximum");
            // Past the maximum chip width the line auto-sizes; it must still fit at the minimum size.
            float detailRoom = width - Card.ChipInset * 2f - Card.OwnershipGlyphSize - Card.ChipGlyphGap;
            float detailNeed = width < Card.ChipMaxWidth ? d : d * Card.DetailMinSize / Card.DetailMaxSize;
            Assert.GreaterOrEqual(detailRoom, detailNeed - .5f, detail + " fits on one line");
            float titleRoom = width - Card.ChipInset * 2f - Card.ChipIcon - Card.ChipIconGap - (g > 0f ? g + Card.TagGap : 0f);
            float titleNeed = width < Card.ChipMaxWidth ? t : t * Card.TitleMinSize / Card.TitleMaxSize;
            Assert.GreaterOrEqual(titleRoom, titleNeed - .5f, title + " fits beside its icon");
        }
    }

    [TestCase(false)] [TestCase(true)]
    public void CardBuildsOneCardWithPurposeBeforeStatusAndOwnershipChips(bool persian)
    {
        string previous = GameLocalization.CurrentLocaleCode;
        GameLocalization.Initialize(AssetDatabase.LoadAssetAtPath<GameLocalizationCatalog>(V3UiLocalizationCatalogBuilder.CatalogPath), persian ? "fa-IR" : "en", false);
        var view = SkirmishMatchView.Create();
        if (typeof(SkirmishMatchView).GetField("clock", Private).GetValue(view) == null)
            typeof(SkirmishMatchView).GetMethod("Awake", Private).Invoke(view, null);
        try
        {
            var card = view.transform.Find("BaseObjectives");
            Assert.IsNotNull(card);
            Assert.IsNull(view.transform.Find("FirstOrderPrompt"), "no second stacked centre bar");
            var surface = card.GetComponent<V3GradientGraphic>();
            Same(Card.Keyline, (Color)typeof(V3GradientGraphic).GetField("borderColor", Private).GetValue(surface), "card keyline");
            var header = (RectTransform)card.Find("ObjectiveHeader");
            var status = (RectTransform)card.Find("ObjectiveStatus");
            Assert.IsNotNull(header); Assert.IsNotNull(status);
            Assert.Greater(header.offsetMax.y, status.offsetMax.y, "purpose and next action sit above the status row");
            Assert.AreEqual(-Card.Padding, header.offsetMax.y); Assert.AreEqual(Card.Padding, header.offsetMin.x);
            Assert.AreEqual(Card.ControlHeight, status.offsetMax.y - status.offsetMin.y);
            Assert.IsNotNull(header.Find("ObjectivePurpose")); Assert.IsNotNull(header.Find("ObjectiveNextAction"));
            Assert.IsNotNull(header.Find("MatchClock"), "the timer is in the card's purpose row");
            Assert.IsFalse(header.GetComponentsInChildren<Graphic>(true).Any(g => g.raycastTarget), "the purpose row never takes taps");
            var order = status.Cast<Transform>().Select(t => t.name).ToArray();
            CollectionAssert.AreEqual(new[] { "YOUR MAIN BASE", "ObjectiveScoreCell", "ENEMY MAIN BASE" }, order, "own base, score, enemy base");
            Assert.AreSame(view.PlayerFocusButton.transform, status.GetChild(0));
            Assert.AreSame(view.EnemyFocusButton.transform, status.GetChild(2));
            foreach (var (button, colour) in new[] { (view.PlayerFocusButton, Card.Friendly), (view.EnemyFocusButton, Card.Enemy) })
            {
                var role = button.transform.Find("HudRoleSurface").GetComponent<V3GradientGraphic>();
                Same(colour, (Color)typeof(V3GradientGraphic).GetField("borderColor", Private).GetValue(role), button.name + " ownership keyline");
                Assert.IsTrue(role.raycastTarget, button.name + " keeps its hit target");
            }
        }
        finally { Object.DestroyImmediate(view.gameObject); GameLocalization.SetLocale(previous, false); }
    }

    [Test]
    public void GlyphNeverTakesTapsAndDrawsEachShape()
    {
        var go = new GameObject("W40Glyph", typeof(RectTransform));
        try
        {
            var glyph = go.AddComponent<HudOwnershipGlyphGraphic>();
            ((RectTransform)go.transform).sizeDelta = new Vector2(24, 24);
            foreach (HudOwnershipGlyph shape in System.Enum.GetValues(typeof(HudOwnershipGlyph)))
            {
                glyph.Present(shape, Color.white);
                Assert.AreEqual(shape, glyph.Shape);
                Assert.IsFalse(glyph.raycastTarget);
                var helper = new UnityEngine.UI.VertexHelper();
                typeof(HudOwnershipGlyphGraphic).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null).Invoke(glyph, new object[] { helper });
                Assert.Greater(helper.currentIndexCount, 0, shape + " has geometry");
                helper.Dispose();
            }
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
