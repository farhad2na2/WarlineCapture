using NUnit.Framework;
using System.Collections.Generic;

public sealed class ChapterOneMissionCatalogTests
{
    [Test]
    public void ChapterOneCatalog_ProvidesFiveRouteReadyMissions()
    {
        Assert.AreEqual(5, ChapterOneMissionCatalog.All.Count);
        Assert.AreEqual("First Contact", ChapterOneMissionCatalog.All[0].DisplayName);
        Assert.AreEqual("Establish The Base", ChapterOneMissionCatalog.All[1].DisplayName);
        Assert.AreEqual("Radar Warning", ChapterOneMissionCatalog.All[2].DisplayName);
        Assert.AreEqual("Airlift", ChapterOneMissionCatalog.All[3].DisplayName);
        Assert.AreEqual("Breach Assault", ChapterOneMissionCatalog.All[4].DisplayName);

        foreach (MissionConfig mission in ChapterOneMissionCatalog.All)
        {
            Assert.IsNotEmpty(mission.MissionId);
            Assert.GreaterOrEqual(mission.Objectives.Length, 2);
            Assert.GreaterOrEqual(mission.StarGoals.Length, 2);
            Assert.AreEqual(3, mission.Rewards.Length);
            StringAssert.EndsWith(".operation_outcome", mission.Rewards[2].RewardId);
            Assert.AreEqual(RewardType.OperationSupply, mission.Rewards[2].Items[0].Type);
        }
    }

    [Test]
    public void ChapterOneCatalog_LookupReturnsMissionConfig()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m02.establish_base");

        Assert.AreEqual("Establish The Base", mission.DisplayName);
        Assert.AreEqual(ObjectiveType.BuildStructure, mission.Objectives[0].Type);
        Assert.AreEqual(RewardType.CommanderXp, mission.Rewards[0].Items[0].Type);
        Assert.AreEqual(RewardType.Credits, mission.Rewards[0].Items[1].Type);
    }

    [Test]
    public void BreachAssault_IncludesOperationOutcomeRewards()
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission("saga.ch01.m05.breach_assault");

        Assert.AreEqual(3, mission.Rewards.Length);
        RewardConfig operationReward = mission.Rewards[2];
        Assert.AreEqual("ch01.m05.operation_outcome", operationReward.RewardId);
        Assert.AreEqual(RewardType.OperationSupply, operationReward.Items[0].Type);
        Assert.AreEqual(RewardType.OperationSecurity, operationReward.Items[1].Type);
        Assert.AreEqual("port_breach", operationReward.Items[1].TargetItemId);
        Assert.AreEqual(RewardType.OperationInfrastructure, operationReward.Items[2].Type);
        Assert.AreEqual("port_breach", operationReward.Items[2].TargetItemId);
    }

    [Test]
    public void ChapterOneCatalog_OperationOutcomesTargetAuthoredDistricts()
    {
        AssertOperationOutcome("saga.ch01.m01.first_contact", "north_bridge", RewardType.OperationIntel, RewardType.OperationTrust);
        AssertOperationOutcome("saga.ch01.m02.establish_base", "old_market", RewardType.OperationInfrastructure, RewardType.OperationSecurity);
        AssertOperationOutcome("saga.ch01.m03.radar_warning", "north_bridge", RewardType.OperationIntel, RewardType.OperationSecurity);
        AssertOperationOutcome("saga.ch01.m04.airlift", "old_market", RewardType.OperationTrust, RewardType.OperationInfrastructure);
        AssertOperationOutcome("saga.ch01.m05.breach_assault", "port_breach", RewardType.OperationSecurity, RewardType.OperationInfrastructure);
    }

    [Test]
    public void ChapterOneCatalog_FirstClearUnlocksUseCatalogRewardIds()
    {
        AssertFirstClearUnlock("saga.ch01.m01.first_contact", RewardType.UnitUnlock, "Unit_Chr_Soldier_Male_02_Alt_04");
        AssertFirstClearUnlock("saga.ch01.m02.establish_base", RewardType.BuildingUnlock, "Building_Barrack");
        AssertFirstClearUnlock("saga.ch01.m03.radar_warning", RewardType.SupportAbilityUnlock, "ability.radar_ping");
        AssertFirstClearUnlock("saga.ch01.m04.airlift", RewardType.SupportAbilityUnlock, "ability.evacuation_corridor");
        AssertFirstClearUnlock("saga.ch01.m05.breach_assault", RewardType.UnitUnlock, "Unit_Chr_Ghillie_Male_01");
    }

    [Test]
    public void ChapterOneCatalog_RewardConfigsHaveValidIdsAmountsTargetsAndFallbacks()
    {
        var rewardIds = new HashSet<string>();

        foreach (MissionConfig mission in ChapterOneMissionCatalog.All)
        {
            Assert.IsNotEmpty(mission.MissionId);
            foreach (RewardConfig reward in mission.Rewards)
                AssertValidReward(reward, rewardIds, $"{mission.MissionId}/{reward?.RewardId}");
        }
    }

    private static void AssertOperationOutcome(string missionId, string districtId, RewardType firstDistrictReward, RewardType secondDistrictReward)
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission(missionId);
        RewardConfig operationReward = mission.Rewards[2];

        Assert.AreEqual(RewardType.OperationSupply, operationReward.Items[0].Type);
        Assert.AreEqual(firstDistrictReward, operationReward.Items[1].Type);
        Assert.AreEqual(districtId, operationReward.Items[1].TargetItemId);
        Assert.AreEqual(secondDistrictReward, operationReward.Items[2].Type);
        Assert.AreEqual(districtId, operationReward.Items[2].TargetItemId);
    }

    private static void AssertFirstClearUnlock(string missionId, RewardType expectedType, string expectedTargetItemId)
    {
        MissionConfig mission = ChapterOneMissionCatalog.GetMission(missionId);
        RewardConfig unlockReward = mission.Rewards[1];

        Assert.IsTrue(unlockReward.FirstClearOnly);
        Assert.AreEqual(expectedType, unlockReward.Items[0].Type);
        Assert.AreEqual(expectedTargetItemId, unlockReward.Items[0].TargetItemId);
        Assert.NotNull(unlockReward.DuplicateFallback);
        Assert.AreEqual(RewardType.BlueprintParts, unlockReward.DuplicateFallback.Items[0].Type);
        Assert.AreEqual(expectedTargetItemId, unlockReward.DuplicateFallback.Items[0].TargetItemId);
    }

    private static void AssertValidReward(RewardConfig reward, HashSet<string> rewardIds, string context)
    {
        Assert.NotNull(reward, context);
        Assert.IsNotEmpty(reward.RewardId, context);
        Assert.IsTrue(rewardIds.Add(reward.RewardId), $"Duplicate reward id: {reward.RewardId}");
        Assert.Greater(reward.Items.Length, 0, context);

        foreach (RewardItemConfig item in reward.Items)
            AssertValidRewardItem(item, context);

        if (reward.FirstClearOnly)
        {
            Assert.NotNull(reward.DuplicateFallback, $"{context} first-clear rewards must define duplicate fallback.");
            Assert.Greater(reward.DuplicateFallback.Items.Length, 0, $"{context} duplicate fallback must grant at least one item.");
            foreach (RewardItemConfig fallbackItem in reward.DuplicateFallback.Items)
                AssertValidRewardItem(fallbackItem, $"{context}/fallback");
        }
    }

    private static void AssertValidRewardItem(RewardItemConfig item, string context)
    {
        Assert.NotNull(item, context);
        Assert.Greater(item.Amount, 0, context);

        if (RequiresTarget(item.Type))
            Assert.IsFalse(string.IsNullOrWhiteSpace(item.TargetItemId), $"{context} {item.Type} must define a target id.");
    }

    private static bool RequiresTarget(RewardType type)
    {
        return type == RewardType.UnitUnlock
            || type == RewardType.BuildingUnlock
            || type == RewardType.SupportAbilityUnlock
            || type == RewardType.BlueprintParts
            || type == RewardType.GearModule
            || type == RewardType.Cosmetic
            || type == RewardType.OperationTrust
            || type == RewardType.OperationSecurity
            || type == RewardType.OperationIntel
            || type == RewardType.OperationInfrastructure;
    }
}
