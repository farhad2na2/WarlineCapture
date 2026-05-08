using System;
using System.Collections.Generic;
using UnityEngine;

public static class RewardService
{
    public static RewardGrantResult[] GrantMissionRewards(WarlineCaptureSaveData saveData, MissionConfig mission, MissionResultData result)
    {
        if (saveData == null)
            throw new ArgumentNullException(nameof(saveData));
        if (mission == null)
            throw new ArgumentNullException(nameof(mission));
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        SaveMigration.Migrate(saveData);
        bool alreadyCompleted = IsSagaMissionCompleted(saveData.saga, mission.MissionId);

        var grants = new List<RewardGrantResult>();
        foreach (RewardConfig reward in mission.Rewards)
            GrantReward(saveData.profile, saveData.operation, alreadyCompleted, result, reward, grants);

        int starDelta = ApplySagaProgress(saveData.saga, result);
        ApplyProfileResult(saveData.profile, result, starDelta);
        return grants.ToArray();
    }

    private static void GrantReward(PlayerProfileSaveData profile, OperationSaveData operation, bool alreadyCompleted, MissionResultData result, RewardConfig reward, List<RewardGrantResult> grants)
    {
        if (reward == null)
            return;

        if (!result.Victory)
        {
            AddSkipped(reward, grants, "Mission was not completed.");
            return;
        }

        if (reward.FirstClearOnly && alreadyCompleted)
        {
            AddSkipped(reward, grants, "First-clear reward already claimed.");
            return;
        }

        if (result.StarsEarned < reward.StarThreshold)
        {
            AddSkipped(reward, grants, $"Requires {reward.StarThreshold} stars.");
            return;
        }

        foreach (RewardItemConfig item in reward.Items)
            GrantItem(profile, operation, reward, item, grants);
    }

    private static void GrantItem(PlayerProfileSaveData profile, OperationSaveData operation, RewardConfig reward, RewardItemConfig item, List<RewardGrantResult> grants)
    {
        if (item == null || item.Amount <= 0)
            return;

        switch (item.Type)
        {
            case RewardType.CommanderXp:
                ProgressionService.GrantCommanderXp(profile, item.Amount);
                grants.Add(Granted(reward, item));
                break;
            case RewardType.Credits:
                profile.credits += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.Materials:
                profile.materials += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.Fuel:
                profile.fuel += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.Intel:
                profile.intel += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.CommandAuthority:
                profile.commandAuthority += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.RushTicket:
                profile.rushTickets += item.Amount;
                grants.Add(Granted(reward, item));
                break;
            case RewardType.UnitUnlock:
                GrantUniqueUnlock(profile, operation, reward, item, ref profile.ownedUnitUnlocks, grants);
                break;
            case RewardType.BuildingUnlock:
                GrantUniqueUnlock(profile, operation, reward, item, ref profile.ownedBuildingUnlocks, grants);
                break;
            case RewardType.SupportAbilityUnlock:
                GrantUniqueUnlock(profile, operation, reward, item, ref profile.ownedSupportAbilityUnlocks, grants);
                break;
            case RewardType.Cosmetic:
                GrantUniqueUnlock(profile, operation, reward, item, ref profile.ownedCosmetics, grants);
                break;
            case RewardType.BlueprintParts:
                AddBlueprintParts(profile, item.TargetItemId, item.Amount);
                grants.Add(Granted(reward, item));
                break;
            case RewardType.OperationSupply:
                EnsureOperationState(operation);
                operation.operationSupplies = Mathf.Max(0, operation.operationSupplies + item.Amount);
                grants.Add(Granted(reward, item));
                break;
            case RewardType.OperationTrust:
                EnsureOperationState(operation);
                GrantDistrictMetric(operation, reward, item, grants, district => district.trust += item.Amount);
                break;
            case RewardType.OperationSecurity:
                EnsureOperationState(operation);
                GrantDistrictMetric(operation, reward, item, grants, district => district.security += item.Amount);
                break;
            case RewardType.OperationIntel:
                EnsureOperationState(operation);
                GrantDistrictMetric(operation, reward, item, grants, district => district.intel += item.Amount);
                break;
            case RewardType.OperationInfrastructure:
                EnsureOperationState(operation);
                GrantDistrictMetric(operation, reward, item, grants, district => district.infrastructure += item.Amount);
                break;
            default:
                grants.Add(new RewardGrantResult(reward.RewardId, item.Type, item.TargetItemId, item.Amount, false, "Reward type is not implemented yet."));
                break;
        }
    }

    private static void EnsureOperationState(OperationSaveData operation)
    {
        if (operation.districts != null && operation.districts.Length > 0)
            return;

        OperationSaveData defaults = new OperationService().CreateDefaultState();
        operation.operationDay = defaults.operationDay;
        operation.operationSupplies = defaults.operationSupplies;
        operation.districts = defaults.districts;
        operation.pendingEvents ??= Array.Empty<OperationEventData>();
        operation.intelEvidence ??= Array.Empty<OperationIntelEvidenceData>();
    }

    private static void GrantDistrictMetric(OperationSaveData operation, RewardConfig reward, RewardItemConfig item, List<RewardGrantResult> grants, Action<DistrictStateData> apply)
    {
        DistrictStateData district = FindDistrict(operation, item.TargetItemId);
        if (district == null)
        {
            grants.Add(new RewardGrantResult(reward.RewardId, item.Type, item.TargetItemId, item.Amount, false, "Operation reward target district was not found."));
            return;
        }

        apply(district);
        ClampDistrictMeters(district);
        grants.Add(Granted(reward, item));
    }

    private static DistrictStateData FindDistrict(OperationSaveData operation, string districtId)
    {
        if (operation?.districts == null || string.IsNullOrWhiteSpace(districtId))
            return null;

        foreach (DistrictStateData district in operation.districts)
        {
            if (district != null && district.districtId == districtId)
                return district;
        }

        return null;
    }

    private static void ClampDistrictMeters(DistrictStateData district)
    {
        district.stability = Mathf.Clamp(district.stability, 0, 100);
        district.threat = Mathf.Clamp(district.threat, 0, 100);
        district.intel = Mathf.Clamp(district.intel, 0, 100);
        district.trust = Mathf.Clamp(district.trust, 0, 100);
        district.security = Mathf.Clamp(district.security, 0, 100);
        district.infrastructure = Mathf.Clamp(district.infrastructure, 0, 100);
        district.enemyInfluence = Mathf.Clamp(district.enemyInfluence, 0, 100);
        district.heat = Mathf.Clamp(district.heat, 0, 100);
        district.civilianRisk = Mathf.Clamp(district.civilianRisk, 0, 100);
    }

    private static void AddBlueprintParts(PlayerProfileSaveData profile, string targetItemId, int amount)
    {
        if (string.IsNullOrWhiteSpace(targetItemId) || amount <= 0)
            return;

        profile.blueprintParts ??= Array.Empty<BlueprintPartSaveData>();
        for (int i = 0; i < profile.blueprintParts.Length; i++)
        {
            BlueprintPartSaveData existing = profile.blueprintParts[i];
            if (existing == null || existing.targetItemId != targetItemId)
                continue;

            existing.amount += amount;
            return;
        }

        Array.Resize(ref profile.blueprintParts, profile.blueprintParts.Length + 1);
        profile.blueprintParts[^1] = new BlueprintPartSaveData
        {
            targetItemId = targetItemId,
            amount = amount
        };
    }

    private static void GrantUniqueUnlock(PlayerProfileSaveData profile, OperationSaveData operation, RewardConfig reward, RewardItemConfig item, ref string[] ownedIds, List<RewardGrantResult> grants)
    {
        if (string.IsNullOrWhiteSpace(item.TargetItemId))
        {
            grants.Add(new RewardGrantResult(reward.RewardId, item.Type, string.Empty, item.Amount, false, "Unlock reward is missing a target id."));
            return;
        }

        ownedIds ??= Array.Empty<string>();
        if (ContainsId(ownedIds, item.TargetItemId))
        {
            if (reward.DuplicateFallback != null)
            {
                foreach (RewardItemConfig fallbackItem in reward.DuplicateFallback.Items)
                    GrantItem(profile, operation, reward.DuplicateFallback, fallbackItem, grants);
            }
            else
            {
                grants.Add(new RewardGrantResult(reward.RewardId, item.Type, item.TargetItemId, item.Amount, false, "Duplicate unlock skipped without fallback."));
            }

            return;
        }

        Array.Resize(ref ownedIds, ownedIds.Length + 1);
        ownedIds[^1] = item.TargetItemId;
        grants.Add(Granted(reward, item));
    }

    private static void ApplyProfileResult(PlayerProfileSaveData profile, MissionResultData result, int starDelta)
    {
        ProgressionService.AccumulateAccountStats(profile, result, starDelta);
    }

    private static int ApplySagaProgress(SagaSaveData saga, MissionResultData result)
    {
        saga.missions ??= Array.Empty<SagaMissionProgressData>();
        int index = FindMissionIndex(saga.missions, result.MissionId);
        if (index < 0)
        {
            Array.Resize(ref saga.missions, saga.missions.Length + 1);
            index = saga.missions.Length - 1;
            saga.missions[index] = new SagaMissionProgressData { missionId = result.MissionId };
        }

        SagaMissionProgressData progress = saga.missions[index] ?? new SagaMissionProgressData { missionId = result.MissionId };
        int previousStars = progress.stars;
        progress.missionId = result.MissionId;
        progress.completed |= result.Victory;
        progress.stars = Mathf.Max(progress.stars, result.StarsEarned);
        saga.missions[index] = progress;
        return Mathf.Max(0, progress.stars - previousStars);
    }

    private static bool IsSagaMissionCompleted(SagaSaveData saga, string missionId)
    {
        saga.missions ??= Array.Empty<SagaMissionProgressData>();
        int index = FindMissionIndex(saga.missions, missionId);
        return index >= 0 && saga.missions[index] != null && saga.missions[index].completed;
    }

    private static int FindMissionIndex(SagaMissionProgressData[] missions, string missionId)
    {
        for (int i = 0; i < missions.Length; i++)
        {
            if (missions[i] != null && missions[i].missionId == missionId)
                return i;
        }

        return -1;
    }

    private static bool ContainsId(string[] ids, string id)
    {
        foreach (string owned in ids)
        {
            if (owned == id)
                return true;
        }

        return false;
    }

    private static RewardGrantResult Granted(RewardConfig reward, RewardItemConfig item)
    {
        return new RewardGrantResult(reward.RewardId, item.Type, item.TargetItemId, item.Amount, true, string.Empty);
    }

    private static void AddSkipped(RewardConfig reward, List<RewardGrantResult> grants, string reason)
    {
        foreach (RewardItemConfig item in reward.Items)
            grants.Add(new RewardGrantResult(reward.RewardId, item.Type, item.TargetItemId, item.Amount, false, reason));
    }
}
