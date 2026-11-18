using System.Collections.Generic;
using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-anchored mission chips stay clear of the HUD (S026 2400x1080 EN and fa-IR captures,
/// 2026-11-18: the RENDEZVOUS chip covered the settings and pause buttons).
/// Rects are the measured S026 layout in screen pixels, y up.
/// </summary>
public sealed class SkirmishHudChipPlacementTests
{
    private static readonly Rect Screen2400 = new(0, 0, 2400, 1080);
    private static readonly Vector2 ChipSize = new(250, 76);
    private static readonly Vector2 StemPivot = new(.5f, 0);
    private const float CueMargin = 44f;

    // Left-to-right S026 layout from the capture (top-left origin converted to y up).
    private static List<Rect> LeftToRightHud() => new()
    {
        Top(18, 12, 454, 468),      // Squad panel
        Top(18, 486, 384, 734),     // Minimap
        Top(718, 12, 1418, 92),     // Resource bar
        Top(1438, 18, 1602, 92),    // Settings and pause
        Top(722, 112, 1752, 222),   // Base objective strip
        Top(790, 234, 1610, 298),   // First-order prompt
        Top(1926, 12, 2382, 578),   // ARIA panel
        Top(18, 840, 816, 1062),    // Squad card tray
        Top(834, 888, 2382, 1062),  // Command bar
    };

    // The same chrome mirrored, as a right-to-left layout would dock it.
    private static List<Rect> RightToLeftHud()
    {
        var mirrored = new List<Rect>();
        foreach (var rect in LeftToRightHud())
            mirrored.Add(Rect.MinMaxRect(Screen2400.width - rect.xMax, rect.yMin, Screen2400.width - rect.xMin, rect.yMax));
        return mirrored;
    }

    private static Rect Top(float left, float top, float right, float bottom) =>
        Rect.MinMaxRect(left, Screen2400.height - bottom, right, Screen2400.height - top);

    private static Rect Area(List<Rect> hud)
    {
        Assert.IsTrue(SkirmishHudChipPlacement.TrySafePlayArea(Screen2400, hud, Screen2400.center, out var area));
        return area;
    }

    private static void AssertClearOfHud(Rect chip, List<Rect> hud, string because)
    {
        foreach (var rect in hud)
            Assert.IsFalse(chip.Overlaps(rect), because + ": chip " + chip + " overlaps HUD " + rect);
    }

    [Test]
    public void PlayAreaSitsBetweenTheDockedHudPanels()
    {
        var hud = LeftToRightHud();
        var area = Area(hud);
        AssertClearOfHud(area, hud, "the play area itself");
        Assert.AreEqual(Screen2400.height - 298, area.yMax, .01f, "below the first-order prompt");
        Assert.AreEqual(Screen2400.height - 840, area.yMin, .01f, "above the card tray");
        Assert.AreEqual(454, area.xMin, .01f, "right of the squad panel");
        Assert.AreEqual(1926, area.xMax, .01f, "left of the ARIA panel");
    }

    [Test]
    public void AnchorUnderTheSettingsButtonsIsClampedIntoThePlayAreaWithACueTowardIt()
    {
        foreach (var hud in new[] { LeftToRightHud(), RightToLeftHud() })
        {
            var area = Area(hud);
            // The S026 rendezvous projected just under the gear and pause buttons.
            Vector2 anchor = Centre(hud[3]);
            Assert.IsTrue(SkirmishHudChipPlacement.TryPlace(anchor, ChipSize, StemPivot, area, CueMargin, out var position, out bool clamped));
            Assert.IsTrue(clamped);
            var chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
            AssertClearOfHud(chip, hud, "clamped chip");
            Assert.AreEqual(anchor.x, position.x, .01f, "clamping keeps the anchor's column when it fits");
            var cue = SkirmishHudChipPlacement.CuePoint(chip, anchor, CueMargin, out float degrees);
            Assert.AreEqual(90f, degrees, 15f, "the cue points up toward the anchor");
            AssertClearOfHud(new Rect(cue.x - 18, cue.y - 15, 36, 30), hud, "direction cue");
            Assert.IsTrue(area.Contains(cue));
        }
    }

    [Test]
    public void AnchorsUnderEverySidePanelStayClear()
    {
        foreach (var hud in new[] { LeftToRightHud(), RightToLeftHud() })
        {
            var area = Area(hud);
            foreach (var panel in hud)
            {
                var anchor = Centre(panel);
                Assert.IsTrue(SkirmishHudChipPlacement.TryPlace(anchor, ChipSize, StemPivot, area, CueMargin, out var position, out bool clamped));
                Assert.IsTrue(clamped, "anchor under " + panel);
                var chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
                AssertClearOfHud(chip, hud, "anchor under " + panel);
                var cue = SkirmishHudChipPlacement.CuePoint(chip, anchor, CueMargin, out _);
                AssertClearOfHud(new Rect(cue.x - 18, cue.y - 15, 36, 30), hud, "cue for " + panel);
            }
        }
    }

    [Test]
    public void AnchorInTheOpenFieldStaysOnItsPoint()
    {
        var hud = LeftToRightHud();
        var area = Area(hud);
        var anchor = new Vector2(1200, 420);
        Assert.IsTrue(SkirmishHudChipPlacement.TryPlace(anchor, ChipSize, StemPivot, area, CueMargin, out var position, out bool clamped));
        Assert.IsFalse(clamped);
        Assert.AreEqual(anchor, position);
    }

    [Test]
    public void AnchorNearThePlayAreaEdgeIsNudgedNotHidden()
    {
        var hud = LeftToRightHud();
        var area = Area(hud);
        // In the play area, but the chip above it would reach under the prompt.
        var anchor = new Vector2(1200, area.yMax - 20);
        Assert.IsTrue(SkirmishHudChipPlacement.TryPlace(anchor, ChipSize, StemPivot, area, CueMargin, out var position, out bool clamped));
        Assert.IsTrue(clamped);
        AssertClearOfHud(SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot), hud, "nudged chip");
    }

    [Test]
    public void WorldTrackingOverlaysAndFullScreenCatchersAreNotHud()
    {
        var centreLabel = new Rect(1100, 480, 200, 80);
        Assert.IsFalse(SkirmishHudChipPlacement.IsHudOccluder(centreLabel, Screen2400), "a label in mid-field tracks the world");
        Assert.IsFalse(SkirmishHudChipPlacement.IsHudOccluder(Screen2400, Screen2400), "a full-screen outline is not a control");
        Assert.IsTrue(SkirmishHudChipPlacement.IsHudOccluder(Top(1438, 18, 1602, 92), Screen2400));
        var hud = LeftToRightHud();
        hud.Add(centreLabel);
        Assert.AreEqual(Area(LeftToRightHud()), Area(hud));
    }

    [Test]
    public void NoPlayAreaWhenEveryOpenPointIsCovered()
    {
        var hud = new List<Rect> // three docked columns over the whole screen
        { Rect.MinMaxRect(0, 0, 800, 1080), Rect.MinMaxRect(800, 0, 1600, 1080), Rect.MinMaxRect(1600, 0, 2400, 1080) };
        Assert.IsFalse(SkirmishHudChipPlacement.TrySafePlayArea(Screen2400, hud, Screen2400.center, out _));
        Assert.IsFalse(SkirmishHudChipPlacement.TryPlace(new Vector2(10, 10), ChipSize, StemPivot, default, CueMargin, out _, out _));
    }

    // S026 lane25 captures (2026-11-18, EN and fa-IR): the chip vanished. Shell screens and popups
    // stay active while hidden (CanvasGroup alpha 0, blocksRaycasts false); their graphics were
    // read as HUD, one covered the screen centre, no play area was found and the chip was hidden.

    [Test]
    public void HiddenShellPanelsAreNotHud()
    {
        var root = new GameObject("ChipPlacementCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var group = root.GetComponent<CanvasGroup>();
            var panel = new GameObject("HiddenPopupPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var image = panel.GetComponent<Image>();
            image.raycastTarget = true;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            Assert.IsFalse(SkirmishHudChipPlacement.BlocksTaps(image, Vector2.zero, null), "a hidden shell panel takes no taps");
            group.alpha = 1f;
            group.blocksRaycasts = true;
            Assert.IsTrue(SkirmishHudChipPlacement.BlocksTaps(image, Vector2.zero, null), "a visible panel is HUD");
            image.raycastTarget = false;
            Assert.IsFalse(SkirmishHudChipPlacement.BlocksTaps(image, Vector2.zero, null));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void OccluderOverTheCentreStillLeavesAPlayAreaAndTheChipShows()
    {
        var hud = LeftToRightHud();
        hud.Add(Rect.MinMaxRect(1100, 300, 1300, 780)); // docked panel over the screen centre
        Assert.IsTrue(SkirmishHudChipPlacement.TrySafePlayArea(Screen2400, hud, Screen2400.center, out var area),
            "another open point is used when the centre is covered");
        AssertClearOfHud(area, hud, "the play area");
        Vector2 anchor = Centre(hud[3]); // the S026 rendezvous under the settings and pause buttons
        var position = SkirmishHudChipPlacement.PlaceVisible(anchor, ChipSize, StemPivot, area, true, Screen2400, CueMargin, out bool clamped);
        Assert.IsTrue(clamped);
        var chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
        AssertClearOfHud(chip, hud, "chip");
        var cue = SkirmishHudChipPlacement.CuePoint(chip, anchor, CueMargin, out _);
        AssertClearOfHud(new Rect(cue.x - 18, cue.y - 15, 36, 30), hud, "cue");
    }

    [Test]
    public void OffScreenAnchorsInFrontOfTheCameraAreClampedWithACue()
    {
        foreach (var hud in new[] { LeftToRightHud(), RightToLeftHud() })
        {
            var area = Area(hud);
            foreach (var anchor in new[] { new Vector2(3000, 540), new Vector2(-500, 900), new Vector2(1200, -400) })
            {
                var position = SkirmishHudChipPlacement.PlaceVisible(anchor, ChipSize, StemPivot, area, true, Screen2400, CueMargin, out bool clamped);
                Assert.IsTrue(clamped);
                var chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
                AssertClearOfHud(chip, hud, "off-screen anchor " + anchor);
                var cue = SkirmishHudChipPlacement.CuePoint(chip, anchor, CueMargin, out float degrees);
                AssertClearOfHud(new Rect(cue.x - 18, cue.y - 15, 36, 30), hud, "cue for " + anchor);
                var toward = (anchor - chip.center).normalized;
                Assert.Greater(Vector2.Dot(toward, new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad))), .99f);
            }
        }
    }

    [Test]
    public void ChipIsNeverHiddenWhenNoPlayAreaIsKnown()
    {
        Vector2 anchor = Centre(LeftToRightHud()[3]);
        var position = SkirmishHudChipPlacement.PlaceVisible(anchor, ChipSize, StemPivot, default, false, Screen2400, CueMargin, out _);
        var chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
        Assert.IsTrue(chip.xMin >= 0 && chip.xMax <= 2400 && chip.yMin >= 0 && chip.yMax <= 1080, "on screen: " + chip);
        var tiny = new Rect(1000, 500, 100, 40); // known but smaller than the chip
        position = SkirmishHudChipPlacement.PlaceVisible(anchor, ChipSize, StemPivot, tiny, true, Screen2400, CueMargin, out _);
        chip = SkirmishHudChipPlacement.ChipRect(position, ChipSize, StemPivot);
        Assert.IsTrue(chip.xMin >= 0 && chip.xMax <= 2400 && chip.yMin >= 0 && chip.yMax <= 1080, "on screen: " + chip);
    }

    private static Vector2 Centre(Rect rect) => rect.center;

    public static void RunFocusedValidation()
    {
        var tests = new SkirmishHudChipPlacementTests();
        tests.PlayAreaSitsBetweenTheDockedHudPanels();
        tests.AnchorUnderTheSettingsButtonsIsClampedIntoThePlayAreaWithACueTowardIt();
        tests.AnchorsUnderEverySidePanelStayClear();
        tests.AnchorInTheOpenFieldStaysOnItsPoint();
        tests.AnchorNearThePlayAreaEdgeIsNudgedNotHidden();
        tests.WorldTrackingOverlaysAndFullScreenCatchersAreNotHud();
        tests.NoPlayAreaWhenEveryOpenPointIsCovered();
        tests.HiddenShellPanelsAreNotHud();
        tests.OccluderOverTheCentreStillLeavesAPlayAreaAndTheChipShows();
        tests.OffScreenAnchorsInFrontOfTheCameraAreClampedWithACue();
        tests.ChipIsNeverHiddenWhenNoPlayAreaIsKnown();
        Debug.Log("[SkirmishHudChipPlacement] result=Passed");
    }
}
