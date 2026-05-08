using NUnit.Framework;

public sealed class RewardTrackServiceTests
{
    [Test]
    public void RewardTrackService_ReportsUnlockedAndClaimableNodes()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData
        {
            commanderXp = 450
        };
        ProgressionService.GrantCommanderXp(profile, 0);

        RewardTrackNodeState[] nodes = RewardTrackService.GetCommanderTrack(profile);

        Assert.AreEqual(5, nodes.Length);
        Assert.IsTrue(nodes[0].CanClaim);
        Assert.IsTrue(nodes[1].CanClaim);
        Assert.IsFalse(nodes[2].IsUnlocked);
        Assert.AreEqual(2, RewardTrackService.CountClaimableCommanderTrackNodes(profile));
    }

    [Test]
    public void RewardTrackService_ClaimGrantsRewardAndPersistsClaim()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData
        {
            commanderXp = 180
        };
        ProgressionService.GrantCommanderXp(profile, 0);

        RewardGrantResult[] grants = RewardTrackService.ClaimCommanderTrackNode(profile, "commander.level.02");
        RewardTrackNodeState[] nodes = RewardTrackService.GetCommanderTrack(profile);

        Assert.AreEqual(1, grants.Length);
        Assert.IsTrue(grants[0].Granted);
        Assert.AreEqual(500, profile.credits);
        Assert.AreEqual(1, profile.claimedRewardTrackNodes.Length);
        Assert.IsTrue(nodes[0].IsClaimed);
        Assert.IsFalse(nodes[0].CanClaim);
    }

    [Test]
    public void RewardTrackService_DoesNotClaimLockedOrClaimedNodeTwice()
    {
        PlayerProfileSaveData profile = new PlayerProfileSaveData();

        RewardGrantResult[] locked = RewardTrackService.ClaimCommanderTrackNode(profile, "commander.level.03");
        Assert.IsFalse(locked[0].Granted);
        Assert.AreEqual(0, profile.materials);

        ProgressionService.GrantCommanderXp(profile, 450);
        RewardTrackService.ClaimCommanderTrackNode(profile, "commander.level.03");
        RewardGrantResult[] duplicate = RewardTrackService.ClaimCommanderTrackNode(profile, "commander.level.03");

        Assert.AreEqual(350, profile.materials);
        Assert.IsFalse(duplicate[0].Granted);
        Assert.AreEqual(1, profile.claimedRewardTrackNodes.Length);
    }
}
