using System;
using System.Collections.Generic;

public sealed class ChapterOneMissionCatalog
{
    private ChapterOneMissionCatalog()
    {
    }

    public const string ChapterId = "saga.ch01";
    public const string FirstContactMissionId = "saga.ch01.m01.first_contact";
    private const string FirstContactRiflelineUnlockId = "Unit_Chr_Soldier_Male_02_Alt_04";
    private const string EstablishBaseBuildingUnlockId = "Building_Barrack";
    private const string RadarWarningSupportUnlockId = "ability.radar_ping";
    private const string AirliftSupportUnlockId = "ability.evacuation_corridor";
    private const string BreachAssaultUnitUnlockId = "Unit_Chr_Ghillie_Male_01";

    private static readonly MissionConfig[] Missions =
    {
        new(
            FirstContactMissionId,
            "First Contact",
            "scenario.ch01.m01.first_contact",
            "level.ch01.district_edge_01",
            "iso.ch01.district_edge_01",
            "preview.ch01.first_contact",
            "minimap.ch01.first_contact",
            new[]
            {
                new ObjectiveConfig("destroy_patrol", "Destroy the forward patrol", ObjectiveType.DestroyAllEnemies, 1),
                new ObjectiveConfig("command_squad_survives", "Keep command squad alive", ObjectiveType.KeepUnitLossesBelow, 0),
                new ObjectiveConfig("keep_losses_low", "Keep unit losses below 2", ObjectiveType.KeepUnitLossesBelow, 2, false, true)
            },
            new[]
            {
                new StarGoalConfig("no_losses", "Lose no more than one squad", ObjectiveType.KeepUnitLossesBelow, 1),
                new StarGoalConfig("recover_resources", "Recover 100 resources", ObjectiveType.ReachResourceAmount, 100)
            },
            CreateMissionRewards(
                "ch01.m01",
                180,
                900,
                FirstContactRiflelineUnlockId,
                CreateOperationOutcomeReward(
                    "ch01.m01",
                    "North Bridge Recon Stabilization",
                    "north_bridge",
                    new RewardItemConfig(RewardType.OperationSupply, 1),
                    new RewardItemConfig(RewardType.OperationIntel, 4, "north_bridge"),
                    new RewardItemConfig(RewardType.OperationTrust, 2, "north_bridge")))),
        new(
            "saga.ch01.m02.establish_base",
            "Establish The Base",
            "scenario.ch01.m02.establish_base",
            "level.ch01.forward_post_01",
            "iso.ch01.forward_post_01",
            "preview.ch01.establish_base",
            "minimap.ch01.establish_base",
            new[]
            {
                new ObjectiveConfig("build_outpost", "Build the first operations outpost", ObjectiveType.BuildStructure, 1),
                new ObjectiveConfig("defeat_attackers", "Defeat the first attack group", ObjectiveType.DestroyAllEnemies, 8)
            },
            new[]
            {
                new StarGoalConfig("build_two", "Build two support structures", ObjectiveType.BuildStructure, 2),
                new StarGoalConfig("save_resources", "Reach 150 resources", ObjectiveType.ReachResourceAmount, 150)
            },
            CreateMissionRewards(
                "ch01.m02",
                220,
                1200,
                EstablishBaseBuildingUnlockId,
                CreateOperationOutcomeReward(
                    "ch01.m02",
                    "Old Market Forward Base",
                    "old_market",
                    new RewardItemConfig(RewardType.OperationSupply, 1),
                    new RewardItemConfig(RewardType.OperationInfrastructure, 4, "old_market"),
                    new RewardItemConfig(RewardType.OperationSecurity, 2, "old_market")))),
        new(
            "saga.ch01.m03.radar_warning",
            "Radar Warning",
            "scenario.ch01.m03.radar_warning",
            "level.ch01.convoy_approach_01",
            "iso.ch01.convoy_approach_01",
            "preview.ch01.radar_warning",
            "minimap.ch01.radar_warning",
            new[]
            {
                new ObjectiveConfig("stop_convoy", "Stop the incoming convoy", ObjectiveType.DestroyAllEnemies, 12),
                new ObjectiveConfig("hold_losses", "Keep losses below 3", ObjectiveType.KeepUnitLossesBelow, 3, false, true)
            },
            new[]
            {
                new StarGoalConfig("minimal_losses", "Lose no more than two units", ObjectiveType.KeepUnitLossesBelow, 2),
                new StarGoalConfig("secure_supplies", "Reach 180 resources", ObjectiveType.ReachResourceAmount, 180)
            },
            CreateMissionRewards(
                "ch01.m03",
                240,
                1350,
                RadarWarningSupportUnlockId,
                CreateOperationOutcomeReward(
                    "ch01.m03",
                    "North Bridge Radar Coverage",
                    "north_bridge",
                    new RewardItemConfig(RewardType.OperationSupply, 1),
                    new RewardItemConfig(RewardType.OperationIntel, 5, "north_bridge"),
                    new RewardItemConfig(RewardType.OperationSecurity, 2, "north_bridge")))),
        new(
            "saga.ch01.m04.airlift",
            "Airlift",
            "scenario.ch01.m04.airlift",
            "level.ch01.landing_zone_01",
            "iso.ch01.landing_zone_01",
            "preview.ch01.airlift",
            "minimap.ch01.airlift",
            new[]
            {
                new ObjectiveConfig("secure_lz", "Secure the landing zone", ObjectiveType.DestroyAllEnemies, 10),
                new ObjectiveConfig("extract_squad", "Complete extraction window", ObjectiveType.SurviveDuration, 1)
            },
            new[]
            {
                new StarGoalConfig("low_losses", "Keep losses below 2", ObjectiveType.KeepUnitLossesBelow, 2),
                new StarGoalConfig("recover_fuel", "Recover 120 resources", ObjectiveType.ReachResourceAmount, 120)
            },
            CreateMissionRewards(
                "ch01.m04",
                250,
                1500,
                AirliftSupportUnlockId,
                CreateOperationOutcomeReward(
                    "ch01.m04",
                    "Old Market Evacuation Corridor",
                    "old_market",
                    new RewardItemConfig(RewardType.OperationSupply, 1),
                    new RewardItemConfig(RewardType.OperationTrust, 4, "old_market"),
                    new RewardItemConfig(RewardType.OperationInfrastructure, 2, "old_market")))),
        new(
            "saga.ch01.m05.breach_assault",
            "Breach Assault",
            "scenario.ch01.m05.breach_assault",
            "level.ch01.fortified_node_01",
            "iso.ch01.fortified_node_01",
            "preview.ch01.breach_assault",
            "minimap.ch01.breach_assault",
            new[]
            {
                new ObjectiveConfig("destroy_core", "Destroy the fortified command core", ObjectiveType.DestroyAllEnemies, 18),
                new ObjectiveConfig("build_forward", "Establish a forward breach point", ObjectiveType.BuildStructure, 1)
            },
            new[]
            {
                new StarGoalConfig("elite_losses", "Keep losses below 4", ObjectiveType.KeepUnitLossesBelow, 4),
                new StarGoalConfig("secure_cache", "Reach 240 resources", ObjectiveType.ReachResourceAmount, 240)
            },
            CreateMissionRewards(
                "ch01.m05",
                260,
                1200,
                BreachAssaultUnitUnlockId,
                CreateOperationOutcomeReward(
                    "ch01.m05",
                    "Port Breach Stabilization",
                    "port_breach",
                    new RewardItemConfig(RewardType.OperationSupply, 1),
                    new RewardItemConfig(RewardType.OperationSecurity, 4, "port_breach"),
                    new RewardItemConfig(RewardType.OperationInfrastructure, 5, "port_breach"))))
    };

    public static IReadOnlyList<MissionConfig> All => Missions;

    public static MissionConfig GetMission(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
            throw new ArgumentException("Mission id cannot be empty.", nameof(missionId));

        foreach (MissionConfig mission in Missions)
        {
            if (mission.MissionId == missionId)
                return mission;
        }

        throw new KeyNotFoundException($"Unknown Chapter 1 mission id '{missionId}'.");
    }

    private static RewardConfig[] CreateMissionRewards(string idPrefix, int commanderXp, int credits, string firstClearUnlockId, params RewardConfig[] extraRewards)
    {
        RewardConfig duplicateFallback = new(
            $"{idPrefix}.duplicate_parts",
            "Duplicate Unlock Parts",
            new[]
            {
                new RewardItemConfig(RewardType.BlueprintParts, 40, firstClearUnlockId)
            });

        var rewards = new List<RewardConfig>
        {
            new RewardConfig(
                $"{idPrefix}.clear_resources",
                "Mission Clear Resources",
                new[]
                {
                    new RewardItemConfig(RewardType.CommanderXp, commanderXp),
                    new RewardItemConfig(RewardType.Credits, credits)
                }),
            new RewardConfig(
                $"{idPrefix}.first_clear_unlock",
                "First Clear Unlock",
                new[]
                {
                    new RewardItemConfig(ResolveUnlockType(firstClearUnlockId), 1, firstClearUnlockId)
                },
                firstClearOnly: true,
                duplicateFallback: duplicateFallback)
        };

        if (extraRewards != null)
        {
            foreach (RewardConfig reward in extraRewards)
            {
                if (reward != null)
                    rewards.Add(reward);
            }
        }

        return rewards.ToArray();
    }

    private static RewardType ResolveUnlockType(string targetItemId)
    {
        if (targetItemId.StartsWith("building.", StringComparison.OrdinalIgnoreCase)
            || targetItemId.StartsWith("Building_", StringComparison.OrdinalIgnoreCase)
            || targetItemId.StartsWith("Tent_", StringComparison.OrdinalIgnoreCase)
            || targetItemId.StartsWith("Wall_", StringComparison.OrdinalIgnoreCase))
            return RewardType.BuildingUnlock;
        if (targetItemId.StartsWith("support.", StringComparison.OrdinalIgnoreCase)
            || targetItemId.StartsWith("ability.", StringComparison.OrdinalIgnoreCase))
            return RewardType.SupportAbilityUnlock;

        return RewardType.UnitUnlock;
    }

    private static RewardConfig CreateOperationOutcomeReward(string idPrefix, string title, string targetDistrictId, params RewardItemConfig[] items)
    {
        var rewardItems = new List<RewardItemConfig>();

        if (items != null)
        {
            foreach (RewardItemConfig item in items)
            {
                if (item == null)
                    continue;

                rewardItems.Add(NormalizeOperationTarget(item, targetDistrictId));
            }
        }

        return new RewardConfig(
            $"{idPrefix}.operation_outcome",
            title,
            rewardItems.ToArray());
    }

    private static RewardItemConfig NormalizeOperationTarget(RewardItemConfig item, string targetDistrictId)
    {
        if (item.Type == RewardType.OperationSupply || !string.IsNullOrWhiteSpace(item.TargetItemId))
            return item;

        return new RewardItemConfig(item.Type, item.Amount, targetDistrictId);
    }
}
