using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CommanderProfileScreenSystem : MonoBehaviour
{
    private enum ProfileTab
    {
        Overview = 0,
        Upgrades = 1,
        History = 2,
        Cosmetics = 3,
        Stats = 4,
        Settings = 5
    }

    private PlayerProfileSaveData _profileOverride;
    private Button _claimButton;
    private readonly List<Button> _trackNodeButtons = new();
    private readonly List<Button> _tabButtons = new();
    private ProfileTab _selectedTab = ProfileTab.Overview;

    private void OnEnable()
    {
        WireClaimButton();
        Refresh();
    }

    private void OnDisable()
    {
        if (_claimButton != null)
            _claimButton.onClick.RemoveListener(ClaimFirstRewardTrackNodeFromButton);

        for (int i = 0; i < _trackNodeButtons.Count; i++)
            _trackNodeButtons[i].onClick.RemoveAllListeners();

        for (int i = 0; i < _tabButtons.Count; i++)
            _tabButtons[i].onClick.RemoveAllListeners();
    }

    public void SetProfileForTests(PlayerProfileSaveData profile)
    {
        _profileOverride = profile;
    }

    public void RefreshForTests()
    {
        Refresh();
    }

    public void SelectTabForTests(int tabIndex)
    {
        SelectTab(tabIndex);
    }

    public bool TryClaimFirstRewardTrackNode()
    {
        PlayerProfileSaveData profile = LoadProfile();
        RewardTrackNodeState[] trackNodes = RewardTrackService.GetCommanderTrack(profile);
        for (int i = 0; i < trackNodes.Length; i++)
        {
            if (!trackNodes[i].CanClaim)
                continue;

            RewardGrantResult[] grants = RewardTrackService.ClaimCommanderTrackNode(profile, trackNodes[i].Config.NodeId);
            if (HasGrantedReward(grants))
            {
                SaveProfile(profile);
                Refresh();
                return true;
            }

            break;
        }

        Refresh();
        return false;
    }

    public bool TryClaimRewardTrackNodeAt(int index)
    {
        PlayerProfileSaveData profile = LoadProfile();
        RewardTrackNodeState[] trackNodes = RewardTrackService.GetCommanderTrack(profile);
        if (index < 0 || index >= trackNodes.Length || !trackNodes[index].CanClaim)
        {
            Refresh();
            return false;
        }

        RewardGrantResult[] grants = RewardTrackService.ClaimCommanderTrackNode(profile, trackNodes[index].Config.NodeId);
        bool granted = HasGrantedReward(grants);
        if (granted)
            SaveProfile(profile);

        Refresh();
        return granted;
    }

    public bool ShowRewardTrackNodeDetailForTests(int index)
    {
        return ShowRewardTrackNodeDetail(index);
    }

    private void Refresh()
    {
        PlayerProfileSaveData profile = LoadProfile();
        CommanderProgression progression = ProgressionService.GetCommanderProgression(profile);
        RewardTrackNodeState[] trackNodes = RewardTrackService.GetCommanderTrack(profile);
        int claimableTrackNodes = RewardTrackService.CountClaimableCommanderTrackNodes(profile);

        SetText("HeaderBar/CreditsCounter/ValueText", FormatCompact(profile.credits));
        SetText("HeaderBar/MaterialsCounter/ValueText", FormatCompact(profile.materials));
        SetText("HeaderBar/AuthorityCounter/ValueText", FormatCompact(profile.commandAuthority));

        SetText("HeroPanel/EyebrowText", "COMMANDER IDENTITY");
        SetText("HeroPanel/HeroTitleText", FormatCommanderName(profile.commanderName));
        SetText("HeroPanel/BodyText", $"{progression.FormatLevel()}  |  XP {progression.FormatXpProgress()}  |  Victories {FormatNumber(profile.victories)}  |  Stars {FormatNumber(profile.starsEarned)}");
        SetText("HeroPanel/UnavailableButton/LabelText", claimableTrackNodes > 0 ? "CLAIM REWARD" : "TRACK LIVE");

        SetText("StatusCard_1/TitleText", "COMMAND LEVEL");
        SetText("StatusCard_1/StatusText", progression.FormatLevel());
        SetText("StatusCard_1/BodyText", $"XP to next level: {FormatNumber(progression.XpToNextLevel)}. Progress {Mathf.RoundToInt(progression.Progress01 * 100f)}%. Reward nodes ready: {claimableTrackNodes}.");

        SetText("StatusCard_2/TitleText", "UNLOCK COLLECTION");
        SetText("StatusCard_2/StatusText", $"{FormatNumber(CountUnlocks(profile))} owned");
        SetText("StatusCard_2/BodyText", $"Units {Count(profile.ownedUnitUnlocks)}, buildings {Count(profile.ownedBuildingUnlocks)}, support {Count(profile.ownedSupportAbilityUnlocks)}, cosmetics {Count(profile.ownedCosmetics)}.");

        SetText("StatusCard_3/TitleText", "BATTLE HISTORY");
        SetText("StatusCard_3/StatusText", $"{FormatNumber(profile.victories)} W / {FormatNumber(profile.defeats)} L");
        SetText("StatusCard_3/BodyText", $"Missions {FormatNumber(profile.missionsCompleted)}. Kills {FormatNumber(profile.enemiesDefeated)}. Losses {FormatNumber(profile.unitsLost)}. Resources {FormatNumber(profile.resourcesEarned)}.");

        SetText("FeedRow_1/TagText", "TRACK");
        SetText("FeedRow_1/BodyText", FormatRewardTrackSummary(trackNodes));
        SetText("FeedRow_2/TagText", "STATS");
        SetText("FeedRow_2/BodyText", $"Buildings built {FormatNumber(profile.buildingsBuilt)}. Stars earned {FormatNumber(profile.starsEarned)}. Mission clears {FormatNumber(profile.missionsCompleted)}.");
        SetText("FeedRow_3/TagText", "WALLET");
        SetText("FeedRow_3/BodyText", $"Credits {FormatNumber(profile.credits)}. Materials {FormatNumber(profile.materials)}. Fuel {FormatNumber(profile.fuel)}. Intel {FormatNumber(profile.intel)}. Authority {FormatNumber(profile.commandAuthority)}.");

        SetText("RewardTrackPanel/TitleText", "REWARD TRACK");
        SetText("RewardTrackPanel/BodyText", claimableTrackNodes > 0 ? $"{claimableTrackNodes} milestone reward(s) ready." : "Claim deterministic commander milestones.");
        BindRewardTrackRows(trackNodes);
        BindSelectedTab(profile, progression, trackNodes);
    }

    private PlayerProfileSaveData LoadProfile()
    {
        if (_profileOverride != null)
            return _profileOverride;

        try
        {
            return SaveService.CreateDefault().LoadProfile() ?? new PlayerProfileSaveData();
        }
        catch (Exception)
        {
            return new PlayerProfileSaveData();
        }
    }

    private void SaveProfile(PlayerProfileSaveData profile)
    {
        if (_profileOverride != null)
            return;

        try
        {
            SaveService.CreateDefault().SaveProfile(profile);
        }
        catch (Exception)
        {
            // Keep the screen responsive if local persistence is unavailable.
        }
    }

    private void WireClaimButton()
    {
        Button button = Find<Button>("HeroPanel/UnavailableButton");
        if (_claimButton != null && _claimButton != button)
            _claimButton.onClick.RemoveListener(ClaimFirstRewardTrackNodeFromButton);

        _claimButton = button;
        if (_claimButton != null)
        {
            _claimButton.onClick.RemoveListener(ClaimFirstRewardTrackNodeFromButton);
            _claimButton.onClick.AddListener(ClaimFirstRewardTrackNodeFromButton);
        }

        _tabButtons.Clear();
        for (int i = 0; i < 6; i++)
        {
            int tabIndex = i;
            Button tabButton = Find<Button>($"CategoryRail/Tab_{i + 1}");
            if (tabButton == null)
                continue;

            tabButton.onClick.RemoveAllListeners();
            tabButton.onClick.AddListener(() => SelectTab(tabIndex));
            _tabButtons.Add(tabButton);
        }

        _trackNodeButtons.Clear();
        for (int i = 0; i < 3; i++)
        {
            int trackIndex = i;
            Button trackButton = Find<Button>($"RewardTrackPanel/TrackNode_{i + 1}");
            if (trackButton == null)
                continue;

            trackButton.onClick.RemoveAllListeners();
            trackButton.onClick.AddListener(() => ShowRewardTrackNodeDetail(trackIndex));
            _trackNodeButtons.Add(trackButton);
        }
    }

    private void ClaimFirstRewardTrackNodeFromButton()
    {
        TryClaimFirstRewardTrackNode();
    }

    private void SelectTab(int tabIndex)
    {
        int clamped = Mathf.Clamp(tabIndex, 0, 5);
        _selectedTab = (ProfileTab)clamped;
        Refresh();
    }

    private bool ShowRewardTrackNodeDetail(int index)
    {
        PlayerProfileSaveData profile = LoadProfile();
        RewardTrackNodeState[] trackNodes = RewardTrackService.GetCommanderTrack(profile);
        if (index < 0 || index >= trackNodes.Length)
        {
            ShowRewardTrackModal("Reward Track", "This commander reward milestone is not available yet.");
            return false;
        }

        RewardTrackNodeState node = trackNodes[index];
        if (node.CanClaim)
        {
            RewardGrantResult[] grants = RewardTrackService.ClaimCommanderTrackNode(profile, node.Config.NodeId);
            bool granted = HasGrantedReward(grants);
            if (granted)
                SaveProfile(profile);

            ShowRewardTrackModal(granted ? "Reward Claimed" : "Reward Track", FormatTrackNodeModalBody(node, grants));
            Refresh();
            return granted;
        }

        string title = node.IsClaimed ? "Reward Claimed" : "Reward Locked";
        ShowRewardTrackModal(title, FormatTrackNodeModalBody(node, Array.Empty<RewardGrantResult>()));
        Refresh();
        return false;
    }

    private void ShowRewardTrackModal(string title, string body)
    {
        WarlineCaptureModalSystem modal = GetComponentInParent<WarlineCaptureModalSystem>();
        modal?.ShowPlaceholder(title, body);
    }

    private void SetText(string path, string value)
    {
        TMP_Text text = Find<TMP_Text>(path);
        if (text != null)
            text.text = value;
    }

    private void BindRewardTrackRows(RewardTrackNodeState[] nodes)
    {
        for (int i = 0; i < 3; i++)
        {
            string path = $"RewardTrackPanel/TrackNode_{i + 1}";
            if (nodes == null || i >= nodes.Length)
            {
                SetText($"{path}/LevelText", "--");
                SetText($"{path}/TitleText", "COMING SOON");
                SetText($"{path}/StatusText", "LOCKED");
                SetInteractable(path, false);
                continue;
            }

            RewardTrackNodeState node = nodes[i];
            SetText($"{path}/LevelText", FormatLevel(node.Config.RequiredCommanderLevel));
            SetText($"{path}/TitleText", node.Config.Title);
            SetText($"{path}/StatusText", FormatTrackNodeStatus(node));
            SetInteractable(path, node.CanClaim);
        }
    }

    private void BindSelectedTab(PlayerProfileSaveData profile, CommanderProgression progression, RewardTrackNodeState[] trackNodes)
    {
        switch (_selectedTab)
        {
            case ProfileTab.Upgrades:
                SetText("StatusCard_1/TitleText", "UPGRADE LINKS");
                SetText("StatusCard_1/StatusText", "ARMORY READY");
                SetText("StatusCard_1/BodyText", $"Owned combat unlocks: {FormatNumber(Count(profile.ownedUnitUnlocks) + Count(profile.ownedBuildingUnlocks) + Count(profile.ownedSupportAbilityUnlocks))}. Armory detail routing is available.");
                SetText("StatusCard_2/TitleText", "NEXT TRACK REWARD");
                SetText("StatusCard_2/StatusText", FormatNextTrackRewardStatus(trackNodes));
                SetText("StatusCard_2/BodyText", FormatRewardTrackSummary(trackNodes));
                SetText("StatusCard_3/TitleText", "BLUEPRINT PARTS");
                SetText("StatusCard_3/StatusText", $"{FormatNumber(CountBlueprintPartStacks(profile))} stacks");
                SetText("StatusCard_3/BodyText", "Duplicate unlock fallback parts accumulate here until full upgrade crafting is enabled.");
                SetText("FeedRow_2/TagText", "UPGRADE");
                SetText("FeedRow_2/BodyText", "Use Armory links for owned roster inspection; upgrade spending remains gated behind profile economy rules.");
                break;
            case ProfileTab.History:
                MissionHistoryEntrySaveData latestMission = MissionHistoryService.GetLatest(profile);
                SetText("StatusCard_1/TitleText", "MISSION HISTORY");
                SetText("StatusCard_1/StatusText", $"{FormatNumber(profile.missionsCompleted)} clears");
                SetText("StatusCard_1/BodyText", $"Victories {FormatNumber(profile.victories)}. Defeats {FormatNumber(profile.defeats)}. Stars earned {FormatNumber(profile.starsEarned)}.");
                SetText("StatusCard_2/TitleText", "COMBAT TOTALS");
                SetText("StatusCard_2/StatusText", $"{FormatNumber(profile.enemiesDefeated)} kills");
                SetText("StatusCard_2/BodyText", $"Units lost {FormatNumber(profile.unitsLost)}. Buildings built {FormatNumber(profile.buildingsBuilt)}. Resources earned {FormatNumber(profile.resourcesEarned)}.");
                SetText("StatusCard_3/TitleText", "RECENT REPORTS");
                SetText("StatusCard_3/StatusText", latestMission != null ? FormatMissionOutcome(latestMission) : "NO REPORTS");
                SetText("StatusCard_3/BodyText", latestMission != null ? FormatMissionHistoryBody(latestMission) : "Complete a mission to archive the first local battle report.");
                SetText("FeedRow_2/TagText", "HISTORY");
                SetText("FeedRow_2/BodyText", latestMission != null ? $"Latest: {latestMission.missionName} | {latestMission.summary}" : $"Account record: {FormatNumber(profile.victories)} wins / {FormatNumber(profile.defeats)} losses.");
                break;
            case ProfileTab.Cosmetics:
                SetText("StatusCard_1/TitleText", "COSMETICS");
                SetText("StatusCard_1/StatusText", $"{FormatNumber(Count(profile.ownedCosmetics))} owned");
                SetText("StatusCard_1/BodyText", "Profile frames, banners, and commander portraits are tracked as deterministic unlock ids.");
                SetText("StatusCard_2/TitleText", "FEATURED FRAME");
                SetText("StatusCard_2/StatusText", HasCosmetic(profile, "cosmetic.commander_frame.iron_guard") ? "OWNED" : "LOCKED");
                SetText("StatusCard_2/BodyText", "Iron Guard Frame unlocks from the commander reward track.");
                SetText("StatusCard_3/TitleText", "COMMAND AUTHORITY");
                SetText("StatusCard_3/StatusText", FormatNumber(profile.commandAuthority));
                SetText("StatusCard_3/BodyText", "Authority is reserved for deterministic cosmetics and account convenience purchases.");
                SetText("FeedRow_2/TagText", "COSMETIC");
                SetText("FeedRow_2/BodyText", $"Owned cosmetics {FormatNumber(Count(profile.ownedCosmetics))}. Authority balance {FormatNumber(profile.commandAuthority)}.");
                break;
            case ProfileTab.Stats:
                SetText("StatusCard_1/TitleText", "ACCOUNT STATS");
                SetText("StatusCard_1/StatusText", progression.FormatLevel());
                SetText("StatusCard_1/BodyText", $"XP {progression.FormatXpProgress()}. Stars {FormatNumber(profile.starsEarned)}. Resources {FormatNumber(profile.resourcesEarned)}.");
                SetText("StatusCard_2/TitleText", "BUILD ECONOMY");
                SetText("StatusCard_2/StatusText", $"{FormatNumber(profile.buildingsBuilt)} built");
                SetText("StatusCard_2/BodyText", $"Credits {FormatNumber(profile.credits)}. Materials {FormatNumber(profile.materials)}. Fuel {FormatNumber(profile.fuel)}. Intel {FormatNumber(profile.intel)}.");
                SetText("StatusCard_3/TitleText", "COMBAT RATIO");
                SetText("StatusCard_3/StatusText", FormatKillLossRatio(profile));
                SetText("StatusCard_3/BodyText", $"Enemies defeated {FormatNumber(profile.enemiesDefeated)}. Units lost {FormatNumber(profile.unitsLost)}.");
                SetText("FeedRow_2/TagText", "STATS");
                SetText("FeedRow_2/BodyText", $"K/L {FormatKillLossRatio(profile)}. Mission clears {FormatNumber(profile.missionsCompleted)}.");
                break;
            case ProfileTab.Settings:
                SetText("StatusCard_1/TitleText", "PROFILE SETTINGS");
                SetText("StatusCard_1/StatusText", "LOCAL");
                SetText("StatusCard_1/BodyText", "Account display and accessibility profile hooks are local until platform account services are added.");
                SetText("StatusCard_2/TitleText", "COMMANDER NAME");
                SetText("StatusCard_2/StatusText", FormatCommanderName(profile.commanderName));
                SetText("StatusCard_2/BodyText", "Rename flow is intentionally deferred until profile validation rules are finalized.");
                SetText("StatusCard_3/TitleText", "DATA SOURCE");
                SetText("StatusCard_3/StatusText", "SAVE JSON");
                SetText("StatusCard_3/BodyText", "Profile values load from split local save data through SaveService.");
                SetText("FeedRow_2/TagText", "SETTINGS");
                SetText("FeedRow_2/BodyText", "Use the global Settings route for audio, language, accessibility, and notification controls.");
                break;
        }

        SetText("FeedRow_1/TagText", FormatTabName(_selectedTab));
    }

    private void SetInteractable(string path, bool interactable)
    {
        Button button = Find<Button>(path);
        if (button != null)
            button.interactable = interactable;
    }

    private T Find<T>(string path) where T : Component
    {
        Transform target = transform.Find(path);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static string FormatCommanderName(string commanderName)
    {
        return string.IsNullOrWhiteSpace(commanderName)
            ? "COMMANDER"
            : commanderName.Trim().ToUpperInvariant();
    }

    private static string FormatCompact(int value)
    {
        int amount = Mathf.Max(0, value);
        if (amount >= 1000000)
            return $"{amount / 1000000f:0.#}M";
        if (amount >= 1000)
            return $"{amount / 1000f:0.#}K";

        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(int value)
    {
        return Mathf.Max(0, value).ToString("N0", CultureInfo.InvariantCulture);
    }

    private static int CountUnlocks(PlayerProfileSaveData profile)
    {
        return Count(profile.ownedUnitUnlocks)
            + Count(profile.ownedBuildingUnlocks)
            + Count(profile.ownedSupportAbilityUnlocks)
            + Count(profile.ownedCosmetics);
    }

    private static string FormatRewardTrackSummary(RewardTrackNodeState[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            return "Reward track is awaiting milestone configuration.";

        for (int i = 0; i < nodes.Length; i++)
        {
            RewardTrackNodeState node = nodes[i];
            if (node.CanClaim)
                return $"{node.Config.Title} ready to claim at {FormatLevel(node.Config.RequiredCommanderLevel)}.";
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            RewardTrackNodeState node = nodes[i];
            if (!node.IsUnlocked)
                return $"Next reward: {node.Config.Title} unlocks at {FormatLevel(node.Config.RequiredCommanderLevel)}.";
        }

        return "All current reward-track milestones are claimed.";
    }

    private static string FormatLevel(int level)
    {
        return $"LV. {Mathf.Max(1, level)}";
    }

    private static string FormatTrackNodeStatus(RewardTrackNodeState node)
    {
        if (node.IsClaimed)
            return "CLAIMED";
        if (node.CanClaim)
            return "CLAIM";
        if (node.IsUnlocked)
            return "READY";

        return "LOCKED";
    }

    private static string FormatTrackNodeModalBody(RewardTrackNodeState node, RewardGrantResult[] grants)
    {
        string status = node.IsClaimed ? "Status: claimed." : node.CanClaim ? "Status: claimed now." : $"Status: unlocks at {FormatLevel(node.Config.RequiredCommanderLevel)}.";
        string rewards = FormatRewardItems(node.Config.Rewards);
        string grantLine = FormatGrantedRewards(grants);
        return $"{node.Config.Title}\n{node.Config.Description}\n{rewards}\n{status}{grantLine}";
    }

    private static string FormatRewardItems(RewardItemConfig[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
            return "Reward: none.";

        var parts = new string[rewards.Length];
        for (int i = 0; i < rewards.Length; i++)
            parts[i] = FormatRewardItem(rewards[i]);

        return $"Reward: {string.Join(", ", parts)}.";
    }

    private static string FormatRewardItem(RewardItemConfig reward)
    {
        if (reward == null)
            return "none";

        string label = FormatRewardType(reward.Type);
        if (reward.Type == RewardType.Cosmetic && !string.IsNullOrWhiteSpace(reward.TargetItemId))
            return FormatTargetName(reward.TargetItemId);

        return $"+{FormatNumber(reward.Amount)} {label}";
    }

    private static string FormatGrantedRewards(RewardGrantResult[] grants)
    {
        if (grants == null || grants.Length == 0 || !HasGrantedReward(grants))
            return string.Empty;

        var parts = new List<string>();
        for (int i = 0; i < grants.Length; i++)
        {
            if (!grants[i].Granted)
                continue;

            parts.Add(FormatRewardItem(new RewardItemConfig(grants[i].Type, grants[i].Amount, grants[i].TargetItemId)));
        }

        return parts.Count > 0 ? $"\nGranted: {string.Join(", ", parts)}." : string.Empty;
    }

    private static string FormatRewardType(RewardType type)
    {
        return type switch
        {
            RewardType.CommanderXp => "Command XP",
            RewardType.CommandAuthority => "Command Authority",
            RewardType.RushTicket => "Rush Tickets",
            _ => type.ToString()
        };
    }

    private static string FormatTargetName(string targetItemId)
    {
        string[] parts = targetItemId.Split('.');
        string value = parts.Length > 0 ? parts[^1] : targetItemId;
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

    private static bool HasGrantedReward(RewardGrantResult[] grants)
    {
        if (grants == null)
            return false;

        for (int i = 0; i < grants.Length; i++)
        {
            if (grants[i].Granted)
                return true;
        }

        return false;
    }

    private static int Count(Array values)
    {
        return values?.Length ?? 0;
    }

    private static int CountBlueprintPartStacks(PlayerProfileSaveData profile)
    {
        int count = 0;
        BlueprintPartSaveData[] parts = profile.blueprintParts ?? Array.Empty<BlueprintPartSaveData>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] != null && parts[i].amount > 0)
                count++;
        }

        return count;
    }

    private static bool HasCosmetic(PlayerProfileSaveData profile, string cosmeticId)
    {
        string[] cosmetics = profile.ownedCosmetics ?? Array.Empty<string>();
        for (int i = 0; i < cosmetics.Length; i++)
        {
            if (cosmetics[i] == cosmeticId)
                return true;
        }

        return false;
    }

    private static string FormatNextTrackRewardStatus(RewardTrackNodeState[] nodes)
    {
        if (nodes == null)
            return "NONE";

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].CanClaim)
                return "CLAIM";
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            if (!nodes[i].IsUnlocked)
                return FormatLevel(nodes[i].Config.RequiredCommanderLevel);
        }

        return "COMPLETE";
    }

    private static string FormatKillLossRatio(PlayerProfileSaveData profile)
    {
        int losses = Mathf.Max(1, profile.unitsLost);
        return (Mathf.Max(0, profile.enemiesDefeated) / (float)losses).ToString("0.0", CultureInfo.InvariantCulture);
    }

    private static string FormatMissionOutcome(MissionHistoryEntrySaveData entry)
    {
        return entry.victory ? "VICTORY" : "DEFEAT";
    }

    private static string FormatMissionHistoryBody(MissionHistoryEntrySaveData entry)
    {
        if (entry == null)
            return string.Empty;

        return $"{entry.missionName}: {entry.summary}. Built {FormatNumber(entry.buildingsBuilt)}. Resources {FormatNumber(entry.resourcesEarned)}.";
    }

    private static string FormatTabName(ProfileTab tab)
    {
        return tab switch
        {
            ProfileTab.Upgrades => "UPGRADES",
            ProfileTab.History => "HISTORY",
            ProfileTab.Cosmetics => "COSMETICS",
            ProfileTab.Stats => "STATS",
            ProfileTab.Settings => "SETTINGS",
            _ => "TRACK"
        };
    }
}
