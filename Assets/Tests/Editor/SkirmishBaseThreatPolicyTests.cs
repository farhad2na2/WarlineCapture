using Game.Runtime;
using Game.UI.Runtime;
using Unity.Mathematics;
using NUnit.Framework;
using UnityEngine;

/// <summary>Base-under-fire cue rules (owner designer playtest 2026-11-17, S002).</summary>
public sealed class SkirmishBaseThreatPolicyTests
{
    [Test]
    public void FirstDropStartsAnAttackAndLaterDropsDoNotRestartIt()
    {
        var tracker = new SkirmishBaseFireTracker();
        Assert.IsFalse(tracker.Observe(800, 0f), "first sample only establishes the baseline");
        Assert.IsFalse(tracker.UnderFire);
        Assert.IsFalse(tracker.Observe(800, 60f));
        Assert.IsTrue(tracker.Observe(772, 67.2f), "first hit");
        Assert.IsTrue(tracker.UnderFire);
        Assert.IsFalse(tracker.Observe(744, 68.2f), "same attack");
        Assert.IsTrue(tracker.UnderFire);
    }

    [Test]
    public void WarningClearsAfterAQuietPeriodAndANewAttackIsAFirstHitAgain()
    {
        var tracker = new SkirmishBaseFireTracker();
        tracker.Observe(800, 0f);
        tracker.Observe(772, 10f);
        tracker.Observe(772, 10f + SkirmishBaseFireTracker.QuietSeconds - .1f);
        Assert.IsTrue(tracker.UnderFire);
        tracker.Observe(772, 10f + SkirmishBaseFireTracker.QuietSeconds + .1f);
        Assert.IsFalse(tracker.UnderFire);
        Assert.IsTrue(tracker.Observe(744, 40f));
    }

    [Test]
    public void RepairOrUnchangedHealthIsNotAHit()
    {
        var tracker = new SkirmishBaseFireTracker();
        tracker.Observe(500, 0f);
        Assert.IsFalse(tracker.Observe(520, 1f));
        Assert.IsFalse(tracker.Observe(520, 2f));
        Assert.IsFalse(tracker.UnderFire);
    }

    [Test]
    public void NearestGroupUsesOnlyTheSuppliedCenters()
    {
        Assert.AreEqual(-1, SkirmishBaseThreatPolicy.NearestGroup(Vector2.zero, null));
        Assert.AreEqual(-1, SkirmishBaseThreatPolicy.NearestGroup(Vector2.zero, new Vector2[0]));
        var centers = new[] { new Vector2(300, 0), new Vector2(-20, 15), new Vector2(0, 90) };
        Assert.AreEqual(1, SkirmishBaseThreatPolicy.NearestGroup(Vector2.zero, centers));
    }

    [Test]
    public void ArrowPointsTowardTheTargetAndMirrorsBehindTheCamera()
    {
        var centre = new Vector2(960, 540);
        Assert.AreEqual(0f, SkirmishBaseThreatPolicy.ArrowDegrees(new Vector3(1500, 540, 10), centre), .01f);
        Assert.AreEqual(90f, SkirmishBaseThreatPolicy.ArrowDegrees(new Vector3(960, 900, 10), centre), .01f);
        Assert.AreEqual(180f, Mathf.Abs(SkirmishBaseThreatPolicy.ArrowDegrees(new Vector3(1500, 540, -10), centre)), .01f);
        var area = new Rect(0, 0, 1920, 1080);
        Assert.IsTrue(SkirmishBaseThreatPolicy.OnScreen(new Vector3(100, 100, 5), area));
        Assert.IsFalse(SkirmishBaseThreatPolicy.OnScreen(new Vector3(100, 100, -5), area));
        Assert.IsFalse(SkirmishBaseThreatPolicy.OnScreen(new Vector3(2100, 100, 5), area));
    }

    [Test]
    public void BaseRevealCoversTheTankFiringEnvelopeAndNoMore()
    {
        // UnitAttackSystem fires when centre distance <= range + attacker radius + target radius + 0.25 cell.
        // Battle Tank: attackRange 50, 3x3 footprint (radius 1 cell). 1 m cells as in S002.
        float tankRadius = SkirmishBaseVisionPolicy.CombatRadius(new int2(3, 3), 1f);
        Assert.AreEqual(1f, tankRadius, 1e-4f);
        Assert.AreEqual(51.25f, SkirmishBaseVisionPolicy.RevealRadius(new int2(1, 1), 1f), 1e-4f);
        float baseRadius = SkirmishBaseVisionPolicy.CombatRadius(new int2(9, 9), 1f);
        Assert.AreEqual(50f + tankRadius + baseRadius + .25f, SkirmishBaseVisionPolicy.RevealRadius(new int2(9, 9), 1f), 1e-4f);
        // The S002 tank fired from (1001, 277) at a base centred on (1001, 303.5): 26.5 m, well inside.
        Assert.Less(26.5f, SkirmishBaseVisionPolicy.RevealRadius(new int2(1, 1), 1f));
    }

    [Test]
    public void AlertRaisesOnPerimeterEntryThenOnFirstDamage()
    {
        var state = new SkirmishBaseThreatAlertState();
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 1f, 800, false, false));
        Assert.AreEqual(SkirmishBaseThreatReason.Perimeter, SkirmishBaseThreatAlertRules.Step(ref state, 60f, 800, true, true));
        Assert.AreEqual(1, state.Revision);
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 61f, 800, true, true));
        Assert.AreEqual(SkirmishBaseThreatReason.FirstDamage, SkirmishBaseThreatAlertRules.Step(ref state, 67f, 772, true, true));
        Assert.AreEqual(2, state.Revision);
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 68f, 744, true, true), "same attack");
        Assert.AreEqual(2, state.Revision);
    }

    [Test]
    public void AlertHysteresisAndRearm()
    {
        var state = new SkirmishBaseThreatAlertState();
        SkirmishBaseThreatAlertRules.Step(ref state, 1f, 800, false, false);
        SkirmishBaseThreatAlertRules.Step(ref state, 10f, 800, true, true);
        // Stepping just outside the radius but within the exit margin keeps it inside: no repeat.
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 11f, 800, false, true));
        Assert.AreEqual(1, state.IntruderInside);
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 12f, 800, false, false));
        Assert.AreEqual(0, state.IntruderInside);
        // Re-entry inside the re-arm window stays quiet; after it, it alerts again.
        Assert.AreEqual(SkirmishBaseThreatReason.None, SkirmishBaseThreatAlertRules.Step(ref state, 15f, 800, true, true));
        SkirmishBaseThreatAlertRules.Step(ref state, 16f, 800, false, false);
        Assert.AreEqual(SkirmishBaseThreatReason.Perimeter,
            SkirmishBaseThreatAlertRules.Step(ref state, 16f + SkirmishBaseThreatAlertRules.PerimeterRearmSeconds + 1f, 800, true, true));
        // A hit after a quiet period is a first hit again.
        SkirmishBaseThreatAlertRules.Step(ref state, 40f, 700, true, true);
        Assert.AreEqual(SkirmishBaseThreatReason.FirstDamage,
            SkirmishBaseThreatAlertRules.Step(ref state, 40f + SkirmishBaseThreatAlertRules.DamageQuietSeconds + 1f, 650, true, true));
    }

    [Test]
    public void DefenderPrefersAntiArmourHomeGuardThenNearestHomeThenAnyone()
    {
        var home = Vector2.zero;
        var centers = new[] { new Vector2(30, 0), new Vector2(90, 0), new Vector2(400, 0) };
        int pick = SkirmishBaseThreatPolicy.PickDefender(home, centers, new[] { false, true, false }, 120f, out bool guard);
        Assert.AreEqual(1, pick); Assert.IsTrue(guard);
        pick = SkirmishBaseThreatPolicy.PickDefender(home, centers, new[] { false, false, true }, 120f, out guard);
        Assert.AreEqual(0, pick); Assert.IsTrue(guard, "anti-armour outside the home zone is not preferred");
        pick = SkirmishBaseThreatPolicy.PickDefender(home, new[] { new Vector2(300, 0), new Vector2(200, 0) }, null, 120f, out guard);
        Assert.AreEqual(1, pick); Assert.IsFalse(guard);
        Assert.AreEqual(120f, SkirmishBaseThreatPolicy.HomeZoneRadius(51.25f));
        Assert.AreEqual(150f, SkirmishBaseThreatPolicy.HomeZoneRadius(75f));
    }

    public static void RunFocusedValidation()
    {
        var tests = new SkirmishBaseThreatPolicyTests();
        tests.FirstDropStartsAnAttackAndLaterDropsDoNotRestartIt();
        tests.WarningClearsAfterAQuietPeriodAndANewAttackIsAFirstHitAgain();
        tests.RepairOrUnchangedHealthIsNotAHit();
        tests.NearestGroupUsesOnlyTheSuppliedCenters();
        tests.ArrowPointsTowardTheTargetAndMirrorsBehindTheCamera();
        tests.BaseRevealCoversTheTankFiringEnvelopeAndNoMore();
        tests.AlertRaisesOnPerimeterEntryThenOnFirstDamage();
        tests.AlertHysteresisAndRearm();
        tests.DefenderPrefersAntiArmourHomeGuardThenNearestHomeThenAnyone();
        Debug.Log("[SkirmishBaseThreatPolicy] result=Passed");
    }
}
