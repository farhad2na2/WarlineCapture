using Game.UI.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class ModeResultStarRulesTests
{
    [Test]
    public void OperationsStarsFollowTheAttempt()
    {
        Assert.AreEqual(3, CampaignStyleResultCard.OperationsStars(true, false));
        Assert.AreEqual(2, CampaignStyleResultCard.OperationsStars(false, true));
        Assert.AreEqual(0, CampaignStyleResultCard.OperationsStars(false, false));
    }

    [Test]
    public void SkirmishStarsFollowThisMatch()
    {
        Assert.AreEqual(0, CampaignStyleResultCard.SkirmishStars(false, 0, 0));
        Assert.AreEqual(1, CampaignStyleResultCard.SkirmishStars(true, 1, 4));
        Assert.AreEqual(2, CampaignStyleResultCard.SkirmishStars(true, 0, 4));
        Assert.AreEqual(3, CampaignStyleResultCard.SkirmishStars(true, 0, 2));
        Assert.AreEqual(2, CampaignStyleResultCard.SkirmishStars(true, 3, 1));
    }

    public static void RunFocusedValidation()
    {
        var tests = new ModeResultStarRulesTests();
        tests.OperationsStarsFollowTheAttempt();
        tests.SkirmishStarsFollowThisMatch();
        var root = new GameObject("ResultCardHost", typeof(RectTransform));
        var card = CampaignStyleResultCard.Mount(root.transform, null);
        card.Bind(() => { }, () => { }, () => { }, () => { });
        card.Present(new ModeResultContent
        {
            Victory = true,
            Title = "VICTORY",
            Identity = "SKIRMISH\nDESERT BASE",
            Status = "MATCH COMPLETE",
            Elapsed = "4:12",
            Stars = 3,
            Objective1 = "Destroy the enemy main base",
            Objective1State = "COMPLETE",
            Objective2 = "Protect your main base",
            Objective2State = "HELD",
            Objective3 = "Hold through the match",
            Objective3State = "COMPLETE",
            Performance1 = "UNITS LOST",
            Performance1Value = "1",
            Performance2 = "UNITS DEFEATED",
            Performance2Value = "6",
            Performance3 = "BUILDINGS LOST",
            Performance3Value = "0 / 1",
            Summary = "The enemy base is gone.",
            LeaveLabel = "MAIN MENU",
            ShowAdjust = true,
            ActionsEnabled = true,
            Signature = "win"
        });
        Assert.IsNotNull(root.transform.Find("CampaignStyleResult/Retry"));
        Assert.IsNotNull(root.transform.Find("CampaignStyleResult/Replay"));
        Assert.IsNotNull(root.transform.Find("CampaignStyleResult/Leave"));
        Assert.IsNotNull(root.transform.Find("CampaignStyleResult/Adjust"));
        Assert.IsTrue(root.transform.Find("CampaignStyleResult/Adjust").gameObject.activeSelf);
        Object.DestroyImmediate(root);
        Debug.Log("[ModeResultStars] result=Passed");
    }
}
