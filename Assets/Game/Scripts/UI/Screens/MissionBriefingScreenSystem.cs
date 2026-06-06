using TMPro;
using UnityEngine;

public sealed class MissionBriefingScreenSystem : MonoBehaviour
{
    [SerializeField] private string fallbackMissionId = "saga.ch01.m05.breach_assault";
    [SerializeField] private TMP_Text missionTitleText;
    [SerializeField] private TMP_Text[] objectiveRows = System.Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] starGoalRows = System.Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] rewardLabels = System.Array.Empty<TMP_Text>();
    [SerializeField] private TMP_Text[] rewardValues = System.Array.Empty<TMP_Text>();

    private void OnEnable()
    {
        Refresh();
    }

    public void RefreshForTests()
    {
        Refresh();
    }

    private void Refresh()
    {
        MissionConfig mission = ResolveMission();
        if (mission == null)
            return;

        SetText(missionTitleText, $"{FormatMissionCode(mission.MissionId)} {mission.DisplayName}".ToUpperInvariant());
        BindObjectives(mission);
        BindStarGoals(mission);
        BindRewards(mission);
    }

    private MissionConfig ResolveMission()
    {
        if (new ActiveMissionSession().HasActiveMission)
            return new ActiveMissionSession().ActiveMission;

        try
        {
            return ChapterOneMissionCatalog.GetMission(fallbackMissionId);
        }
        catch
        {
            return ChapterOneMissionCatalog.All.Count > 0 ? ChapterOneMissionCatalog.All[0] : null;
        }
    }

    private void BindObjectives(MissionConfig mission)
    {
        ObjectiveConfig[] objectives = mission.Objectives;
        for (int i = 0; i < objectiveRows.Length; i++)
        {
            bool hasRow = i < objectives.Length && objectives[i] != null;
            SetText(objectiveRows[i], hasRow ? objectives[i].DisplayName : string.Empty);
            SetTextActive(objectiveRows[i], hasRow);
        }
    }

    private void BindStarGoals(MissionConfig mission)
    {
        StarGoalConfig[] goals = mission.StarGoals;
        for (int i = 0; i < starGoalRows.Length; i++)
        {
            bool hasRow = i < goals.Length && goals[i] != null;
            SetText(starGoalRows[i], hasRow ? goals[i].DisplayName : string.Empty);
            SetTextActive(starGoalRows[i], hasRow);
        }
    }

    private void BindRewards(MissionConfig mission)
    {
        bool operationFirst = new ActiveMissionSession().HasActiveMission
            && new ActiveMissionSession().ReturnRoute == WarlineCaptureRoute.OperationDashboard;
        RewardItemConfig[] previewItems = CollectPreviewItems(mission, operationFirst);
        for (int i = 0; i < rewardLabels.Length; i++)
        {
            bool hasReward = i < previewItems.Length && previewItems[i] != null;
            SetRewardActive(i, hasReward);
            if (!hasReward)
                continue;

            SetText(rewardLabels[i], FormatRewardLabel(previewItems[i]));
            SetText(rewardValues[i], FormatRewardValue(previewItems[i]));
        }
    }

    private static RewardItemConfig[] CollectPreviewItems(MissionConfig mission, bool operationFirst)
    {
        var items = new System.Collections.Generic.List<RewardItemConfig>();
        foreach (RewardConfig reward in mission.Rewards)
        {
            if (reward == null)
                continue;

            foreach (RewardItemConfig item in reward.Items)
            {
                if (item != null)
                    items.Add(item);
            }
        }

        if (operationFirst)
            PrioritizeOperationRewards(items);

        return items.ToArray();
    }

    private static void PrioritizeOperationRewards(System.Collections.Generic.List<RewardItemConfig> items)
    {
        var prioritized = new System.Collections.Generic.List<RewardItemConfig>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && IsOperationReward(items[i].Type))
                prioritized.Add(items[i]);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || !IsOperationReward(items[i].Type))
                prioritized.Add(items[i]);
        }

        items.Clear();
        items.AddRange(prioritized);
    }

    private void SetRewardActive(int index, bool active)
    {
        SetRowActive(GetAt(rewardLabels, index), active);
        SetRowActive(GetAt(rewardValues, index), active);
    }

    private static string FormatRewardLabel(RewardItemConfig item)
    {
        return item.Type switch
        {
            RewardType.CommanderXp => "COMMANDER XP",
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
            RewardType.OperationSupply => "OPERATION SUPPLY",
            RewardType.OperationTrust => "TRUST",
            RewardType.OperationSecurity => "SECURITY",
            RewardType.OperationIntel => "OPERATION INTEL",
            RewardType.OperationInfrastructure => "INFRASTRUCTURE",
            _ => item.Type.ToString().ToUpperInvariant()
        };
    }

    private static string FormatRewardValue(RewardItemConfig item)
    {
        return item.Type switch
        {
            RewardType.UnitUnlock or RewardType.BuildingUnlock or RewardType.SupportAbilityUnlock or RewardType.GearModule => FormatItemName(item.TargetItemId),
            RewardType.BlueprintParts => $"+{item.Amount} PARTS",
            RewardType.OperationTrust or RewardType.OperationSecurity or RewardType.OperationIntel or RewardType.OperationInfrastructure
                when !string.IsNullOrWhiteSpace(item.TargetItemId) => $"+{item.Amount:N0} {FormatItemName(item.TargetItemId)}",
            _ => $"+{item.Amount:N0}"
        };
    }

    private static bool IsOperationReward(RewardType type)
    {
        return type == RewardType.OperationSupply
            || type == RewardType.OperationTrust
            || type == RewardType.OperationSecurity
            || type == RewardType.OperationIntel
            || type == RewardType.OperationInfrastructure;
    }

    private static string FormatItemName(string targetItemId)
    {
        if (string.IsNullOrWhiteSpace(targetItemId))
            return "UNLOCK";

        int dot = targetItemId.LastIndexOf('.');
        string value = dot >= 0 && dot < targetItemId.Length - 1 ? targetItemId[(dot + 1)..] : targetItemId;
        value = StripCatalogPrefix(value);
        return value.Replace('_', ' ').ToUpperInvariant();
    }

    private static string StripCatalogPrefix(string value)
    {
        if (value.StartsWith("Building_", System.StringComparison.OrdinalIgnoreCase))
            return value["Building_".Length..];
        if (value.StartsWith("Unit_", System.StringComparison.OrdinalIgnoreCase))
            return value["Unit_".Length..];

        return value;
    }

    private static string FormatMissionCode(string missionId)
    {
        for (int i = 0; i < ChapterOneMissionCatalog.All.Count; i++)
        {
            if (ChapterOneMissionCatalog.All[i].MissionId == missionId)
                return $"1-{i + 1}";
        }

        return "1-?";
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static void SetTextActive(TMP_Text text, bool active)
    {
        if (text != null)
            text.gameObject.SetActive(active);
    }

    private static void SetRowActive(TMP_Text text, bool active)
    {
        if (text != null)
            text.transform.parent.gameObject.SetActive(active);
    }

    private static TMP_Text GetAt(TMP_Text[] values, int index)
    {
        return values != null && index >= 0 && index < values.Length ? values[index] : null;
    }
}
