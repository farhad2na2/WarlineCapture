using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Owner designer playtest 2026-11-17 (S002): a defeat with zero units lost showed
/// "Keep squad losses low — Failed" and a green star beside a red title.
/// </summary>
public sealed class SkirmishResultHonestyTests
{
    [Test]
    public void DefeatWithNoLossesKeepsTheLossCriterionMet()
    {
        var criteria = new SkirmishStarCriteria(false, 1, 0);
        Assert.AreEqual(0, criteria.Stars);
        Assert.AreEqual(SkirmishCriterionState.Failed, criteria.DestroyEnemyBase);
        Assert.AreEqual(SkirmishCriterionState.Failed, criteria.ProtectBase);
        Assert.AreEqual(SkirmishCriterionState.MetNeedsVictory, criteria.LowLosses);
        Assert.AreEqual(4, criteria.MetMask);
        Assert.AreEqual(0, criteria.EarnedMask);
    }

    [Test]
    public void DefeatOverTheLossLimitFailsTheLossCriterion()
    {
        var criteria = new SkirmishStarCriteria(false, 1, SkirmishStarCriteria.LowLossLimit + 1);
        Assert.AreEqual(SkirmishCriterionState.Failed, criteria.LowLosses);
    }

    [Test]
    public void EarnedCriteriaAlwaysMatchTheStarCount()
    {
        for (int victory = 0; victory < 2; victory++)
        for (int buildings = 0; buildings < 3; buildings++)
        for (int units = 0; units < 6; units++)
        {
            var criteria = new SkirmishStarCriteria(victory == 1, buildings, units);
            int earned = 0;
            for (int bit = 0; bit < 3; bit++)
                if ((criteria.EarnedMask & (1 << bit)) != 0) earned++;
            Assert.AreEqual(criteria.Stars, earned, $"victory={victory} buildings={buildings} units={units}");
            Assert.AreEqual(criteria.Stars, CampaignStyleResultCard.SkirmishStars(victory == 1, buildings, units));
            // Earned implies met; a met criterion never reads as failed.
            Assert.AreEqual(criteria.EarnedMask, criteria.EarnedMask & criteria.MetMask);
        }
    }

    [Test]
    public void StarCountIsUnchangedFromThePreviousRule()
    {
        Assert.AreEqual(0, CampaignStyleResultCard.SkirmishStars(false, 0, 0));
        Assert.AreEqual(1, CampaignStyleResultCard.SkirmishStars(true, 1, 4));
        Assert.AreEqual(2, CampaignStyleResultCard.SkirmishStars(true, 0, 4));
        Assert.AreEqual(3, CampaignStyleResultCard.SkirmishStars(true, 0, 2));
        Assert.AreEqual(2, CampaignStyleResultCard.SkirmishStars(true, 3, 1));
    }

    [Test]
    public void EnemyBaseMarkerNamesWhatIsKnown()
    {
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.InSight,
            SkirmishEnemyBaseMarkerState.Resolve(true, SkirmishEnemyBaseMarkerState.ReadoutVisible, false));
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.LastSeen,
            SkirmishEnemyBaseMarkerState.Resolve(true, SkirmishEnemyBaseMarkerState.ReadoutLastSeen, false));
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.Reported,
            SkirmishEnemyBaseMarkerState.Resolve(true, SkirmishEnemyBaseMarkerState.ReadoutHidden, false));
        // The fog readout wins over a stale sight flag: never claim sight the readout denies.
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.Reported,
            SkirmishEnemyBaseMarkerState.Resolve(true, SkirmishEnemyBaseMarkerState.ReadoutHidden, true));
        // Sessions without a readout fall back to the public sight flag.
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.InSight, SkirmishEnemyBaseMarkerState.Resolve(false, 0, true));
        Assert.AreEqual(SkirmishEnemyBaseMarkerKnowledge.Reported, SkirmishEnemyBaseMarkerState.Resolve(false, 0, false));
    }

    [Test]
    public void EnemyBaseMarkerAgeAndDistanceRead()
    {
        Assert.AreEqual("0:00", SkirmishEnemyBaseMarkerState.FormatAge(-3f));
        Assert.AreEqual("0:00", SkirmishEnemyBaseMarkerState.FormatAge(float.NaN));
        Assert.AreEqual("0:42", SkirmishEnemyBaseMarkerState.FormatAge(42.9f));
        Assert.AreEqual("12:05", SkirmishEnemyBaseMarkerState.FormatAge(725f));
        Assert.AreEqual(500, SkirmishEnemyBaseMarkerState.RoundedGroundDistance(new Vector3(0, 0, 0), new Vector3(300, 40, 400)));
        Assert.AreEqual(10, SkirmishEnemyBaseMarkerState.RoundedGroundDistance(Vector3.zero, new Vector3(0, 0, 14)));
    }

    /// <summary>Focused lane entry: pure rules plus the mounted defeat card colours.</summary>
    public static void RunFocusedValidation()
    {
        var tests = new SkirmishResultHonestyTests();
        tests.DefeatWithNoLossesKeepsTheLossCriterionMet();
        tests.DefeatOverTheLossLimitFailsTheLossCriterion();
        tests.EarnedCriteriaAlwaysMatchTheStarCount();
        tests.StarCountIsUnchangedFromThePreviousRule();
        tests.EnemyBaseMarkerNamesWhatIsKnown();
        tests.EnemyBaseMarkerAgeAndDistanceRead();
        var root = new GameObject("ResultHonestyHost", typeof(RectTransform));
        try
        {
            var card = CampaignStyleResultCard.Mount(root.transform, null);
            card.Present(new ModeResultContent
            {
                Victory = false, Title = "DEFEAT", Stars = 0,
                HasObjectiveFacts = true, CompletedObjectives = 0, MetNeedsVictoryObjectives = 4,
                Objective1State = "FAILED", Objective2State = "FAILED", Objective3State = "MET · NEEDS A WIN",
                Signature = "defeat-no-losses"
            });
            var emblemStar = root.transform.Find("CampaignStyleResult/Emblem/Filled").GetComponent<V3StarGraphic>();
            var title = root.transform.Find("CampaignStyleResult/Title").GetComponentInChildren<TMPro.TMP_Text>();
            Assert.AreEqual(title.color, emblemStar.color, "defeat emblem must use the defeat accent, not green");
            var failed = root.transform.Find("CampaignStyleResult/Objectives/Objective0").GetComponentsInChildren<TMPro.TMP_Text>()[1];
            var met = root.transform.Find("CampaignStyleResult/Objectives/Objective2").GetComponentsInChildren<TMPro.TMP_Text>()[1];
            Assert.AreNotEqual(failed.color, met.color, "a met-but-unawarded criterion must not share the failed colour");
            card.Present(new ModeResultContent { Victory = true, Title = "VICTORY", Stars = 3, Signature = "victory" });
            Assert.AreEqual(title.color, emblemStar.color, "victory emblem follows the victory accent");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        Debug.Log("[SkirmishResultHonesty] result=Passed");
    }
}
