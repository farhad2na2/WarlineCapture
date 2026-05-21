using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionResultPopupController : MonoBehaviour
{
    private static readonly string[] RewardCardPaths =
    {
        "Frame/BodyRoot/RewardsPanel/CommanderXpReward",
        "Frame/BodyRoot/RewardsPanel/CreditsReward",
        "Frame/BodyRoot/RewardsPanel/MaterialsReward",
        "Frame/BodyRoot/RewardsPanel/IntelReward"
    };

    private WarlineCapturePopupFrameView _frameView;

    public void Bind(MissionResultData result)
    {
        if (result == null)
            return;

        _frameView ??= GetComponent<WarlineCapturePopupFrameView>();
        SetText("Frame/Header/TitleText", result.Victory ? "VICTORY" : "DEFEAT");
        SetText("Frame/Header/MissionNameText", result.MissionName);
        SetText("Frame/BodyRoot/StatsPanel/EnemiesDefeatedCard/ValueText", result.EnemiesDefeated.ToString());
        SetText("Frame/BodyRoot/StatsPanel/UnitsLostCard/ValueText", result.UnitsLost.ToString());
        SetText("Frame/BodyRoot/StatsPanel/BuildingsCapturedCard/ValueText", result.BuildingsBuilt.ToString());
        SetText("Frame/BodyRoot/StatsPanel/CiviliansSafeCard/ValueText", "0");

        for (int i = 0; i < 3; i++)
        {
            Image star = Find<Image>($"Frame/Header/Star_{i + 1}");
            if (star != null)
            {
                Color color = star.color;
                color.a = i < result.StarsEarned ? 1f : 0.22f;
                star.color = color;
            }
        }

        BindObjectiveRow("Frame/BodyRoot/ObjectivesPanel/Objective_DestroyHostilePatrol", result.Objectives, 0);
        BindObjectiveRow("Frame/BodyRoot/ObjectivesPanel/Objective_KeepCommandSquadAlive", result.Objectives, 1);
        BindObjectiveRow("Frame/BodyRoot/ObjectivesPanel/Objective_CityConsequenceNeutral", result.Objectives, 2);
        BindRewardRows(result.Rewards);
    }

    public void Show(MissionResultData result)
    {
        Bind(result);
        _frameView ??= GetComponent<WarlineCapturePopupFrameView>();
        if (_frameView != null)
            _frameView.Show(result != null && result.Victory ? "VICTORY" : "DEFEAT");
        else
            gameObject.SetActive(true);
    }

    private void BindObjectiveRow(string rowPath, ObjectiveRuntimeState[] objectives, int index)
    {
        Transform row = transform.Find(rowPath);
        if (row == null)
            return;

        bool hasObjective = objectives != null && index >= 0 && index < objectives.Length;
        row.gameObject.SetActive(hasObjective);
        if (!hasObjective)
            return;

        ObjectiveRuntimeState objective = objectives[index];
        SetText($"{rowPath}/LabelText", objective.DisplayName);
        SetText($"{rowPath}/StatusText", objective.Complete ? "COMPLETED" : $"{objective.CurrentAmount} / {objective.TargetAmount}");

        TMP_Text status = Find<TMP_Text>($"{rowPath}/StatusText");
        if (status != null)
            status.color = objective.Complete ? new Color(0.65f, 0.90f, 0.34f, 1f) : new Color(1f, 0.58f, 0.22f, 1f);
    }

    private void BindRewardRows(RewardGrantResult[] rewards)
    {
        List<RewardGrantResult> grantedRewards = GetGrantedRewards(rewards);
        for (int i = 0; i < RewardCardPaths.Length; i++)
        {
            Transform row = transform.Find(RewardCardPaths[i]);
            if (row == null)
                continue;

            bool hasReward = i < grantedRewards.Count;
            row.gameObject.SetActive(hasReward);
            if (!hasReward)
                continue;

            RewardGrantResult reward = grantedRewards[i];
            SetText($"{RewardCardPaths[i]}/LabelText", FormatRewardLabel(reward));
            SetText($"{RewardCardPaths[i]}/ValueText", FormatRewardValue(reward));
        }
    }

    private static List<RewardGrantResult> GetGrantedRewards(RewardGrantResult[] rewards)
    {
        var grantedRewards = new List<RewardGrantResult>();
        if (rewards == null)
            return grantedRewards;

        for (int i = 0; i < rewards.Length; i++)
        {
            if (rewards[i].Granted)
                grantedRewards.Add(rewards[i]);
        }

        if (WarlineCaptureMissionSession.HasActiveMission && WarlineCaptureMissionSession.ReturnRoute == WarlineCaptureRoute.OperationDashboard)
            PrioritizeOperationRewards(grantedRewards);

        return grantedRewards;
    }

    private static void PrioritizeOperationRewards(List<RewardGrantResult> rewards)
    {
        var prioritized = new List<RewardGrantResult>(rewards.Count);
        for (int i = 0; i < rewards.Count; i++)
        {
            if (IsOperationReward(rewards[i].Type))
                prioritized.Add(rewards[i]);
        }

        for (int i = 0; i < rewards.Count; i++)
        {
            if (!IsOperationReward(rewards[i].Type))
                prioritized.Add(rewards[i]);
        }

        rewards.Clear();
        rewards.AddRange(prioritized);
    }

    private static string FormatRewardLabel(RewardGrantResult reward)
    {
        return reward.Type switch
        {
            RewardType.CommanderXp => "COMMAND XP",
            RewardType.Credits => "CREDITS",
            RewardType.Materials => "MATERIALS",
            RewardType.Fuel => "FUEL",
            RewardType.Intel => "INTEL",
            RewardType.CommandAuthority => "COMMAND AUTHORITY",
            RewardType.RushTicket => "RUSH TICKETS",
            RewardType.UnitUnlock => "UNIT UNLOCK",
            RewardType.BuildingUnlock => "BUILDING UNLOCK",
            RewardType.SupportAbilityUnlock => "SUPPORT UNLOCK",
            RewardType.BlueprintParts => "BLUEPRINT PARTS",
            RewardType.GearModule => "GEAR MODULE",
            RewardType.Cosmetic => "COSMETIC",
            RewardType.OperationSupply => "OPERATION SUPPLY",
            RewardType.SagaStars => "SAGA STARS",
            RewardType.OperationTrust => "TRUST",
            RewardType.OperationSecurity => "SECURITY",
            RewardType.OperationIntel => "OPERATION INTEL",
            RewardType.OperationInfrastructure => "INFRASTRUCTURE",
            _ => reward.Type.ToString().ToUpperInvariant()
        };
    }

    private static string FormatRewardValue(RewardGrantResult reward)
    {
        if (IsUnlockReward(reward.Type) && !string.IsNullOrWhiteSpace(reward.TargetItemId))
            return FormatTargetName(reward.TargetItemId);

        string amount = reward.Amount.ToString("#,0", CultureInfo.InvariantCulture);
        if (reward.Type == RewardType.BlueprintParts && !string.IsNullOrWhiteSpace(reward.TargetItemId))
            return $"+{amount} {FormatTargetName(reward.TargetItemId)}";
        if (IsOperationDistrictReward(reward.Type) && !string.IsNullOrWhiteSpace(reward.TargetItemId))
            return $"+{amount} {FormatTargetName(reward.TargetItemId)}";

        return $"+{amount}";
    }

    private static bool IsOperationDistrictReward(RewardType type)
    {
        return type == RewardType.OperationTrust
            || type == RewardType.OperationSecurity
            || type == RewardType.OperationIntel
            || type == RewardType.OperationInfrastructure;
    }

    private static bool IsOperationReward(RewardType type)
    {
        return type == RewardType.OperationSupply || IsOperationDistrictReward(type);
    }

    private static bool IsUnlockReward(RewardType type)
    {
        return type == RewardType.UnitUnlock
            || type == RewardType.BuildingUnlock
            || type == RewardType.SupportAbilityUnlock
            || type == RewardType.Cosmetic;
    }

    private static string FormatTargetName(string targetItemId)
    {
        if (string.IsNullOrWhiteSpace(targetItemId))
            return string.Empty;

        int separatorIndex = targetItemId.LastIndexOf('.');
        string name = separatorIndex >= 0 && separatorIndex < targetItemId.Length - 1
            ? targetItemId[(separatorIndex + 1)..]
            : targetItemId;

        name = StripCatalogPrefix(name);
        return name.Replace('_', ' ').ToUpperInvariant();
    }

    private static string StripCatalogPrefix(string value)
    {
        if (value.StartsWith("Building_", StringComparison.OrdinalIgnoreCase))
            return value["Building_".Length..];
        if (value.StartsWith("Unit_", StringComparison.OrdinalIgnoreCase))
            return value["Unit_".Length..];

        return value;
    }

    private void SetText(string path, string value)
    {
        TMP_Text text = Find<TMP_Text>(path);
        if (text != null)
            text.text = value;
    }

    private T Find<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }
}
