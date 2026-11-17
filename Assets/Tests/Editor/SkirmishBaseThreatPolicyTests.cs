using Game.UI.Runtime;
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

    public static void RunFocusedValidation()
    {
        var tests = new SkirmishBaseThreatPolicyTests();
        tests.FirstDropStartsAnAttackAndLaterDropsDoNotRestartIt();
        tests.WarningClearsAfterAQuietPeriodAndANewAttackIsAFirstHitAgain();
        tests.RepairOrUnchangedHealthIsNotAHit();
        tests.NearestGroupUsesOnlyTheSuppliedCenters();
        tests.ArrowPointsTowardTheTargetAndMirrorsBehindTheCamera();
        Debug.Log("[SkirmishBaseThreatPolicy] result=Passed");
    }
}
