using System;
using System.Collections.Generic;
using UnityEngine;

public static class RewardTrackService
{
    private static readonly RewardTrackNodeConfig[] CommanderTrack =
    {
        new RewardTrackNodeConfig(
            "commander.level.02",
            "Field Budget",
            "Early campaign credits for fast base recovery.",
            2,
            new[] { new RewardItemConfig(RewardType.Credits, 500) }),
        new RewardTrackNodeConfig(
            "commander.level.03",
            "Material Reserve",
            "Construction materials for upgraded build openings.",
            3,
            new[] { new RewardItemConfig(RewardType.Materials, 350) }),
        new RewardTrackNodeConfig(
            "commander.level.04",
            "Rapid Orders",
            "Rush tickets for account convenience.",
            4,
            new[] { new RewardItemConfig(RewardType.RushTicket, 2) }),
        new RewardTrackNodeConfig(
            "commander.level.05",
            "Command Authority",
            "Premium command resource earned from progression.",
            5,
            new[] { new RewardItemConfig(RewardType.CommandAuthority, 75) }),
        new RewardTrackNodeConfig(
            "commander.level.06",
            "Iron Guard Frame",
            "Profile cosmetic milestone for the first season shell.",
            6,
            new[] { new RewardItemConfig(RewardType.Cosmetic, 1, "cosmetic.commander_frame.iron_guard") })
    };

    public static RewardTrackNodeState[] GetCommanderTrack(PlayerProfileSaveData profile)
    {
        profile ??= new PlayerProfileSaveData();
        CommanderProgression progression = ProgressionService.GetCommanderProgression(profile);
        var nodes = new RewardTrackNodeState[CommanderTrack.Length];

        for (int i = 0; i < CommanderTrack.Length; i++)
        {
            RewardTrackNodeConfig config = CommanderTrack[i];
            nodes[i] = new RewardTrackNodeState(
                config,
                progression.Level >= config.RequiredCommanderLevel,
                IsClaimed(profile, config.NodeId));
        }

        return nodes;
    }

    public static RewardGrantResult[] ClaimCommanderTrackNode(PlayerProfileSaveData profile, string nodeId)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        RewardTrackNodeConfig? config = FindNode(nodeId);
        if (!config.HasValue)
            return new[] { new RewardGrantResult(nodeId ?? string.Empty, RewardType.CommanderXp, string.Empty, 0, false, "Reward track node was not found.") };

        RewardTrackNodeState state = GetNodeState(profile, config.Value);
        if (!state.IsUnlocked)
            return BuildSkippedResults(config.Value, "Commander level requirement is not met.");

        if (state.IsClaimed)
            return BuildSkippedResults(config.Value, "Reward track node was already claimed.");

        var grants = new List<RewardGrantResult>();
        foreach (RewardItemConfig reward in config.Value.Rewards)
            GrantTrackReward(profile, config.Value.NodeId, reward, grants);

        AddClaimedNode(profile, config.Value.NodeId);
        return grants.ToArray();
    }

    public static int CountClaimableCommanderTrackNodes(PlayerProfileSaveData profile)
    {
        int count = 0;
        RewardTrackNodeState[] nodes = GetCommanderTrack(profile);
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].CanClaim)
                count++;
        }

        return count;
    }

    private static RewardTrackNodeState GetNodeState(PlayerProfileSaveData profile, RewardTrackNodeConfig config)
    {
        CommanderProgression progression = ProgressionService.GetCommanderProgression(profile);
        return new RewardTrackNodeState(config, progression.Level >= config.RequiredCommanderLevel, IsClaimed(profile, config.NodeId));
    }

    private static RewardTrackNodeConfig? FindNode(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            return null;

        for (int i = 0; i < CommanderTrack.Length; i++)
        {
            if (CommanderTrack[i].NodeId == nodeId)
                return CommanderTrack[i];
        }

        return null;
    }

    private static RewardGrantResult[] BuildSkippedResults(RewardTrackNodeConfig config, string reason)
    {
        var results = new RewardGrantResult[config.Rewards.Length];
        for (int i = 0; i < config.Rewards.Length; i++)
        {
            RewardItemConfig item = config.Rewards[i];
            results[i] = new RewardGrantResult(config.NodeId, item.Type, item.TargetItemId, item.Amount, false, reason);
        }

        return results;
    }

    private static void GrantTrackReward(PlayerProfileSaveData profile, string nodeId, RewardItemConfig item, List<RewardGrantResult> grants)
    {
        if (item == null || item.Amount <= 0)
            return;

        switch (item.Type)
        {
            case RewardType.CommanderXp:
                ProgressionService.GrantCommanderXp(profile, item.Amount);
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.Credits:
                profile.credits += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.Materials:
                profile.materials += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.Fuel:
                profile.fuel += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.Intel:
                profile.intel += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.CommandAuthority:
                profile.commandAuthority += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.RushTicket:
                profile.rushTickets += item.Amount;
                grants.Add(Granted(nodeId, item));
                break;
            case RewardType.Cosmetic:
                GrantUnique(profile, item, ref profile.ownedCosmetics);
                grants.Add(Granted(nodeId, item));
                break;
            default:
                grants.Add(new RewardGrantResult(nodeId, item.Type, item.TargetItemId, item.Amount, false, "Reward track item type is not implemented yet."));
                break;
        }
    }

    private static void GrantUnique(PlayerProfileSaveData profile, RewardItemConfig item, ref string[] ownedIds)
    {
        if (string.IsNullOrWhiteSpace(item.TargetItemId))
            return;

        ownedIds ??= Array.Empty<string>();
        for (int i = 0; i < ownedIds.Length; i++)
        {
            if (ownedIds[i] == item.TargetItemId)
                return;
        }

        Array.Resize(ref ownedIds, ownedIds.Length + 1);
        ownedIds[^1] = item.TargetItemId;
    }

    private static RewardGrantResult Granted(string nodeId, RewardItemConfig item)
    {
        return new RewardGrantResult(nodeId, item.Type, item.TargetItemId, item.Amount, true, string.Empty);
    }

    private static bool IsClaimed(PlayerProfileSaveData profile, string nodeId)
    {
        profile.claimedRewardTrackNodes ??= Array.Empty<string>();
        for (int i = 0; i < profile.claimedRewardTrackNodes.Length; i++)
        {
            if (profile.claimedRewardTrackNodes[i] == nodeId)
                return true;
        }

        return false;
    }

    private static void AddClaimedNode(PlayerProfileSaveData profile, string nodeId)
    {
        if (IsClaimed(profile, nodeId))
            return;

        profile.claimedRewardTrackNodes ??= Array.Empty<string>();
        Array.Resize(ref profile.claimedRewardTrackNodes, profile.claimedRewardTrackNodes.Length + 1);
        profile.claimedRewardTrackNodes[^1] = nodeId;
    }
}
