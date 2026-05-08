using System.Linq;
using NUnit.Framework;

public sealed class WarlineCaptureRewardServiceTests
{
    [Test]
    public void RewardService_GrantsXpCreditsUnlockAndUpdatesSagaProgress()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = CreateRewardMission();
        MissionResultData result = CreateResult(victory: true, stars: 2);

        RewardGrantResult[] grants = RewardService.GrantMissionRewards(saveData, mission, result);

        Assert.AreEqual(100, saveData.profile.commanderXp);
        Assert.AreEqual(1, saveData.profile.commanderLevel);
        Assert.AreEqual(250, saveData.profile.credits);
        Assert.Contains("Unit_Chr_Soldier_Male_02_Alt_04", saveData.profile.ownedUnitUnlocks);
        Assert.AreEqual(1, saveData.profile.victories);
        Assert.AreEqual(1, saveData.profile.missionsCompleted);
        Assert.AreEqual(2, saveData.profile.starsEarned);
        Assert.AreEqual(4, saveData.profile.enemiesDefeated);
        Assert.AreEqual(1, saveData.profile.buildingsBuilt);
        Assert.AreEqual("test.reward.m01", saveData.saga.missions[0].missionId);
        Assert.IsTrue(saveData.saga.missions[0].completed);
        Assert.AreEqual(2, saveData.saga.missions[0].stars);
        Assert.AreEqual(3, grants.Count(grant => grant.Granted));
    }

    [Test]
    public void RewardService_DoesNotDuplicateFirstClearUnlock()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = CreateRewardMission();
        RewardService.GrantMissionRewards(saveData, mission, CreateResult(victory: true, stars: 2));

        RewardGrantResult[] secondGrants = RewardService.GrantMissionRewards(saveData, mission, CreateResult(victory: true, stars: 2));

        Assert.AreEqual(1, saveData.profile.ownedUnitUnlocks.Length);
        Assert.AreEqual("Unit_Chr_Soldier_Male_02_Alt_04", saveData.profile.ownedUnitUnlocks[0]);
        Assert.IsTrue(secondGrants.Any(grant => !grant.Granted && grant.Reason.Contains("First-clear")));
    }

    [Test]
    public void RewardService_UsesDuplicateFallbackForRepeatableUnlocks()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = new MissionConfig(
            "test.reward.m02",
            "Repeatable Unlock",
            System.Array.Empty<ObjectiveConfig>(),
            System.Array.Empty<StarGoalConfig>(),
            new[]
            {
                new RewardConfig(
                    "repeatable.unit",
                    "Repeatable Unit",
                    new[] { new RewardItemConfig(RewardType.UnitUnlock, 1, "Unit_Veh_APC_Heavy") },
                    duplicateFallback: new RewardConfig(
                        "repeatable.unit.duplicate",
                        "Duplicate Parts",
                        new[] { new RewardItemConfig(RewardType.BlueprintParts, 40, "Unit_Veh_APC_Heavy") }))
            });

        RewardService.GrantMissionRewards(saveData, mission, CreateResult("test.reward.m02", true, 1));
        RewardGrantResult[] duplicateGrants = RewardService.GrantMissionRewards(saveData, mission, CreateResult("test.reward.m02", true, 1));

        Assert.AreEqual(1, saveData.profile.ownedUnitUnlocks.Length);
        Assert.AreEqual("Unit_Veh_APC_Heavy", saveData.profile.blueprintParts[0].targetItemId);
        Assert.AreEqual(40, saveData.profile.blueprintParts[0].amount);
        Assert.IsTrue(duplicateGrants.Any(grant => grant.Granted && grant.Type == RewardType.BlueprintParts));
    }

    [Test]
    public void RewardService_RequiresStarThreshold()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = new MissionConfig(
            "test.reward.m03",
            "Star Reward",
            System.Array.Empty<ObjectiveConfig>(),
            System.Array.Empty<StarGoalConfig>(),
            new[]
            {
                new RewardConfig(
                    "three_star_authority",
                    "Three Star Authority",
                    new[] { new RewardItemConfig(RewardType.CommandAuthority, 25) },
                    starThreshold: 3)
            });

        RewardGrantResult[] grants = RewardService.GrantMissionRewards(saveData, mission, CreateResult("test.reward.m03", true, 2));

        Assert.AreEqual(0, saveData.profile.commandAuthority);
        Assert.IsTrue(grants.Any(grant => !grant.Granted && grant.Reason.Contains("Requires 3 stars")));
    }

    [Test]
    public void RewardService_GrantsOperationRewardsIntoOperationState()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = new MissionConfig(
            "test.reward.operation",
            "Operation Reward",
            System.Array.Empty<ObjectiveConfig>(),
            System.Array.Empty<StarGoalConfig>(),
            new[]
            {
                new RewardConfig(
                    "operation_support",
                    "Operation Support",
                    new[]
                    {
                        new RewardItemConfig(RewardType.OperationSupply, 2),
                        new RewardItemConfig(RewardType.OperationTrust, 3, "old_market"),
                        new RewardItemConfig(RewardType.OperationSecurity, 4, "old_market"),
                        new RewardItemConfig(RewardType.OperationIntel, 5, "old_market"),
                        new RewardItemConfig(RewardType.OperationInfrastructure, 6, "old_market")
                    })
            });

        RewardGrantResult[] grants = RewardService.GrantMissionRewards(saveData, mission, CreateResult("test.reward.operation", true, 2));

        DistrictStateData oldMarket = saveData.operation.districts.First(district => district.districtId == "old_market");
        Assert.AreEqual(6, saveData.operation.operationSupplies);
        Assert.AreEqual(69, oldMarket.trust);
        Assert.AreEqual(55, oldMarket.security);
        Assert.AreEqual(45, oldMarket.intel);
        Assert.AreEqual(64, oldMarket.infrastructure);
        Assert.AreEqual(5, grants.Count(grant => grant.Granted));
    }

    [Test]
    public void RewardService_GrantsBreachAssaultAuthoredOperationOutcome()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m05.breach_assault");

        RewardGrantResult[] grants = RewardService.GrantMissionRewards(saveData, mission, CreateResult("saga.ch01.m05.breach_assault", true, 2));

        DistrictStateData portBreach = saveData.operation.districts.First(district => district.districtId == "port_breach");
        Assert.AreEqual(5, saveData.operation.operationSupplies);
        Assert.AreEqual(28, portBreach.security);
        Assert.AreEqual(46, portBreach.infrastructure);
        Assert.IsTrue(grants.Any(grant => grant.Granted && grant.Type == RewardType.OperationSupply));
        Assert.IsTrue(grants.Any(grant => grant.Granted && grant.Type == RewardType.OperationSecurity && grant.TargetItemId == "port_breach"));
        Assert.IsTrue(grants.Any(grant => grant.Granted && grant.Type == RewardType.OperationInfrastructure && grant.TargetItemId == "port_breach"));
    }

    [Test]
    public void RewardService_GrantsAllChapterOneOperationOutcomeRewards()
    {
        WarlineCaptureSaveData saveData = new WarlineCaptureSaveData();

        foreach (MissionConfig mission in ChapterOneMissionCatalog.All)
            RewardService.GrantMissionRewards(saveData, mission, CreateResult(mission.MissionId, true, 2));

        DistrictStateData northBridge = saveData.operation.districts.First(district => district.districtId == "north_bridge");
        DistrictStateData oldMarket = saveData.operation.districts.First(district => district.districtId == "old_market");
        DistrictStateData portBreach = saveData.operation.districts.First(district => district.districtId == "port_breach");

        Assert.AreEqual(9, saveData.operation.operationSupplies);
        Assert.AreEqual(41, northBridge.intel);
        Assert.AreEqual(56, northBridge.trust);
        Assert.AreEqual(34, northBridge.security);
        Assert.AreEqual(64, oldMarket.infrastructure);
        Assert.AreEqual(53, oldMarket.security);
        Assert.AreEqual(70, oldMarket.trust);
        Assert.AreEqual(28, portBreach.security);
        Assert.AreEqual(46, portBreach.infrastructure);
    }

    private static MissionConfig CreateRewardMission()
    {
        return new MissionConfig(
            "test.reward.m01",
            "Reward Mission",
            System.Array.Empty<ObjectiveConfig>(),
            System.Array.Empty<StarGoalConfig>(),
            new[]
            {
                new RewardConfig(
                    "clear_resources",
                    "Clear Resources",
                    new[]
                    {
                        new RewardItemConfig(RewardType.CommanderXp, 100),
                        new RewardItemConfig(RewardType.Credits, 250)
                    }),
                new RewardConfig(
                    "first_clear_unit",
                    "First Clear Unit",
                    new[] { new RewardItemConfig(RewardType.UnitUnlock, 1, "Unit_Chr_Soldier_Male_02_Alt_04") },
                    firstClearOnly: true)
            });
    }

    private static MissionResultData CreateResult(bool victory, int stars)
    {
        return CreateResult("test.reward.m01", victory, stars);
    }

    private static MissionResultData CreateResult(string missionId, bool victory, int stars)
    {
        return new MissionResultData(
            missionId,
            "Reward Mission",
            victory,
            stars,
            4,
            0,
            1,
            0,
            System.Array.Empty<ObjectiveRuntimeState>());
    }
}
