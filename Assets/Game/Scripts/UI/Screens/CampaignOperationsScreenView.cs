using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed partial class CampaignOperationsScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RectTransform chapterRail;
        [SerializeField] private RectTransform strategicMap;
        [SerializeField] private RectTransform missionBriefing;
        [SerializeField] private RectTransform[] chapterCards;
        [SerializeField] private RectTransform[] missionNodes;
        [SerializeField] private GameObject[] missionLockIcons;
        [SerializeField] private Button[] missionNodeButtons;
        [SerializeField] private RectTransform[] progressNodes;
        [SerializeField] private RawImage districtMapImage;
        [SerializeField] private RawImage missionPreviewImage;
        [SerializeField] private Texture m01MissionPreview;
        [SerializeField] private Texture m02MissionPreview;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text missionNumber;
        [SerializeField] private TMP_Text missionName;
        [SerializeField] private TMP_Text missionBriefingText;
        [SerializeField] private TMP_Text primaryObjectiveText;
        [SerializeField] private TMP_Text rewardSummaryText;
        [SerializeField] private TMP_Text launchMissionLabel;
        [SerializeField] private Button storyArchiveButton;
        [SerializeField] private Button chapterIntelButton;
        [SerializeField] private Button launchMissionButton;
        [SerializeField] private RectTransform chapterSelectRoot;
        [SerializeField] private RectTransform missionSelectRoot;
        [SerializeField] private Button showMissionSelectButton;
        [SerializeField] private Button showChapterSelectButton;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private bool _stateButtonsBound;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RectTransform ChapterRail => chapterRail;
        public RectTransform StrategicMap => strategicMap;
        public RectTransform MissionBriefing => missionBriefing;
        public RectTransform[] ChapterCards => chapterCards;
        public RectTransform[] MissionNodes => missionNodes;
        public Button[] MissionNodeButtons => missionNodeButtons;
        public RectTransform[] ProgressNodes => progressNodes;
        public RawImage DistrictMapImage => districtMapImage;
        public RawImage MissionPreviewImage => missionPreviewImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text MissionName => missionName;
        public TMP_Text MissionNumber => missionNumber;
        public TMP_Text MissionBriefingText => missionBriefingText;
        public TMP_Text PrimaryObjectiveText => primaryObjectiveText;
        public TMP_Text RewardSummaryText => rewardSummaryText;
        public TMP_Text LaunchMissionLabel => launchMissionLabel;
        public Button StoryArchiveButton => storyArchiveButton;
        public Button ChapterIntelButton => chapterIntelButton;
        public Button LaunchMissionButton => launchMissionButton;
        public RectTransform ChapterSelectRoot => chapterSelectRoot;
        public RectTransform MissionSelectRoot => missionSelectRoot;
        public Button ShowMissionSelectButton => showMissionSelectButton;
        public Button ShowChapterSelectButton => showChapterSelectButton;

        private void OnEnable()
        {
            ReleaseMaskedRouteLines();
            storyArchiveButton?.onClick.AddListener(OpenRadarGuideArchive);
            footerStoryArchiveButton?.onClick.AddListener(OpenRadarGuideArchive);
            atlasStoryArchiveButton?.onClick.AddListener(OpenRadarGuideArchive);
            if (!_stateButtonsBound)
            {
                showMissionSelectButton?.onClick.AddListener(ShowMissionSelect);
                showChapterSelectButton?.onClick.AddListener(ShowChapterSelect);
                _stateButtonsBound = true;
            }

            if (chapterSelectRoot != null && missionSelectRoot != null &&
                !chapterSelectRoot.gameObject.activeSelf && !missionSelectRoot.gameObject.activeSelf)
                ShowMissionSelect();
        }

        private void ReleaseMaskedRouteLines()
        {
            // Rotated route marks live inside the map's RectMask2D. On the screen-space
            // menu canvas that clip leaks a one-pixel white edge across the campaign
            // screen after a mission returns here. The marks sit inside the map, so
            // they do not need the clip.
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null || image.name != "Route")
                    continue;

                RectMask2D mask = image.GetComponentInParent<RectMask2D>();
                if (mask == null || mask.transform.parent == null)
                    continue;

                image.transform.SetParent(mask.transform.parent, true);
                image.maskable = false;
            }
        }

        private void OnDisable()
        {
            storyArchiveButton?.onClick.RemoveListener(OpenRadarGuideArchive);
            footerStoryArchiveButton?.onClick.RemoveListener(OpenRadarGuideArchive);
            atlasStoryArchiveButton?.onClick.RemoveListener(OpenRadarGuideArchive);
            if (!_stateButtonsBound)
                return;
            showMissionSelectButton?.onClick.RemoveListener(ShowMissionSelect);
            showChapterSelectButton?.onClick.RemoveListener(ShowChapterSelect);
            _stateButtonsBound = false;
        }

        public void ShowChapterSelect()
        {
            if (chapterSelectRoot != null)
                chapterSelectRoot.gameObject.SetActive(true);
            if (missionSelectRoot != null)
                missionSelectRoot.gameObject.SetActive(false);
        }

        public void ShowMissionSelect()
        {
            if (chapterSelectRoot != null)
                chapterSelectRoot.gameObject.SetActive(false);
            if (missionSelectRoot != null)
                missionSelectRoot.gameObject.SetActive(true);
        }

        public void BindGameTextResolver(IGameTextResolver gameTextResolver)
        {
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        public void Apply(UiCampaignOperationsModel model)
        {
            UiCampaignMissionModel mission = model.SelectedMission;
            bool m02 = mission.MissionId == UiCampaignMissionProjectionIds.M02;
            bool v3StateLayout = missionSelectRoot != null;
            screenTitle.enableAutoSizing = !v3StateLayout;
            screenTitle.fontSize = v3StateLayout ? 43f : screenTitle.fontSize;
            screenTitle.fontSizeMin = v3StateLayout ? 43f : 48f;
            screenTitle.fontSizeMax = v3StateLayout ? 43f : 118f;
            missionName.enableAutoSizing = true;
            missionName.fontSizeMin = v3StateLayout ? 22f : 36f;
            missionName.fontSizeMax = v3StateLayout ? 34f : 84f;
            screenTitle.text = v3StateLayout
                ? UiShellRuntimeGateway.Localization.Get("ui.campaign.title", "CAMPAIGN")
                : _gameTextResolver.Get(
                    "campaign.operations.title",
                    model.NextMissionRevealed ? "CAMPAIGN OPERATIONS  |  NEXT READY" : "CAMPAIGN OPERATIONS");
            Set(missionNumber, v3StateLayout ? (m02 ? "M02" : "M01") : (m02 ? "MISSION 02" : "MISSION 01"));
            missionName.text = v3StateLayout
                ? m02
                    ? UiShellRuntimeGateway.Localization.Get("ui.campaign.m02_name", "ESTABLISH THE BASE")
                    : UiShellRuntimeGateway.Localization.Get("ui.campaign.m01_name", "FIRST CONTACT")
                : FormatMissionSummary(mission);
            Set(missionBriefingText, _gameTextResolver.Get(
                m02 ? "mission.m02.summary" : "mission.m01.summary",
                m02
                    ? "Reopen an abandoned forward post before a second hostile cell reaches it."
                    : "Secure the Old Market corridor and protect the civilian route."));
            Set(primaryObjectiveText, v3StateLayout
                ? m02
                    ? UiShellRuntimeGateway.Localization.Get("ui.campaign.build_barrack", "BUILD\nBARRACK")
                    : UiShellRuntimeGateway.Localization.Get("ui.campaign.secure_corridor", "SECURE\nCORRIDOR")
                : _gameTextResolver.Get(
                    m02 ? "mission.m02.objective.build_forward_barracks" : "mission.m01.objective.secure_corridor",
                    m02 ? "BUILD THE FORWARD BARRACKS" : "ELIMINATE THE HOSTILE PATROL"));
            Set(rewardSummaryText, m02
                ? _gameTextResolver.Get("mission.m02.reward.card", "320 XP  |  1,500 CREDITS  |  BARRACKS UNLOCK")
                : _gameTextResolver.Get("mission.m01.reward.card", "260 XP  |  1,200 CREDITS"));
            if (missionPreviewImage != null)
                missionPreviewImage.texture = m02 ? m02MissionPreview : m01MissionPreview;
            Set(
                launchMissionLabel,
                v3StateLayout
                    ? UiShellRuntimeGateway.Localization.Get("ui.campaign.start_briefing", "START BRIEFING")
                    : UiShellRuntimeGateway.Localization.GetBySource(mission.PrimaryActionLabel));
            launchMissionButton.interactable = mission.Available && mission.AccessState == UiContentAccessState.Allowed;
            ApplyGridlockChapter(in model);
            ApplyMissionNodes(mission.MissionId, model.NextMissionRevealed, model.AvailableMissionMask, model.CompletedMissionMask);
            ApplyDistrictAtlas(IsChapterFive ? 5 : IsChapterFour ? 4 : IsChapterThree ? 3 : IsChapterTwo ? 2 : 1);
            ApplyAtlasChapterProgress(model.CompletedMissionMask, model.AvailableMissionMask);
            ApplyRadarWarning(mission);
            ApplyAirlift(mission); ApplyBreach(mission);
            ApplyMissionGoals(mission.MissionId);
            if(IsChapterFive)
            {
                string[] goals={"clinic","utility","perimeter"};
                for(int i=0;i<3;i++)
                {SetGoal(objectiveCards,i,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.objective."+goals[i]));SetGoal(starGoalLabels,i,UiShellRuntimeGateway.Localization.Get("mission.citywide_alert.star."+(i+1)));}
            }
            if(mission.AccessState != UiContentAccessState.Allowed)
                Set(missionBriefingText,UiShellRuntimeGateway.Localization.GetBySource(mission.AccessState switch
                {
                    UiContentAccessState.NotOwned => "Campaign Edition required.",
                    UiContentAccessState.ProgressionLocked => "Complete the previous mission first.",
                    UiContentAccessState.InstalledContentMissing => "Content download required.",
                    _ => "This mission is currently unavailable."
                }));
            for (int index = 0; index < progressNodes.Length; index++)
                progressNodes[index].gameObject.SetActive(index < mission.BestStars);
        }

        public void ApplyUnavailable()
        {
            missionName.text = UiShellRuntimeGateway.Localization.Get(
                "ui.campaign.mission_unavailable",
                "MISSION DATA UNAVAILABLE");
            launchMissionButton.interactable = false;
            for (int index = 0; index < progressNodes.Length; index++)
                progressNodes[index].gameObject.SetActive(false);
        }

        private static string FormatMissionSummary(UiCampaignMissionModel mission)
        {
            string time = mission.BestCompletionMilliseconds > 0
                ? $"  |  BEST {mission.BestCompletionMilliseconds / 60000:00}:{mission.BestCompletionMilliseconds / 1000 % 60:00}"
                : string.Empty;
            return $"{mission.DisplayName}  |  {mission.PrimaryActionLabel}  |  {mission.BestStars}/3{time}";
        }

        private void ApplyMissionNodes(string selectedMissionId, bool m02Revealed, uint availableMask, uint completedMask)
        {
            latestCampaignAvailableMask=availableMask;latestCampaignCompletedMask=completedMask;
            selectedMissionNodeIndex = -1;
            for (int index = 0; index < (missionNodes?.Length ?? 0); index++)
            {
                int contentIndex=IsChapterFive?index+20:IsChapterFour?index+15:IsChapterThree?index+10:IsChapterTwo?index+5:index;
                bool available = !IsChapterTwo && !IsChapterThree && !IsChapterFour && !IsChapterFive && (index == 0 || index == 1 && m02Revealed ||
                                 index == 1 && selectedMissionId == UiCampaignMissionProjectionIds.M02);
                if (availableMask != 0) available = contentIndex < Game.Missions.Contracts.CampaignMissionSequence.RegisteredMissionCount && (availableMask & (1u << contentIndex)) != 0;
                if (missionNodeButtons != null && index < missionNodeButtons.Length &&
                    missionNodeButtons[index] != null)
                    missionNodeButtons[index].interactable = available;
                GameObject lockIcon = missionLockIcons != null && index < missionLockIcons.Length ? missionLockIcons[index] : null;
                if (lockIcon != null)
                    lockIcon.SetActive(!available);
                bool selected = selectedMissionId == Game.Missions.Contracts.CampaignMissionSequence.IdAt(contentIndex);
                if (selected) selectedMissionNodeIndex = index;
                if(nodeIdLabels!=null && index<nodeIdLabels.Length) Set(nodeIdLabels[index],"M"+(index+1).ToString("00"));
                ApplyNodeAppearance(index, available, (completedMask & (1u << contentIndex)) != 0, selected);
            }
        }

        private static void Set(TMP_Text target, string value)
        {
            if (target == null)
                return;
            UiLocalizedText.Set(target, value);
            if (target.name == "MissionNumber" && value != null && value.Length > 4)
            {
                RectTransform rect = target.rectTransform;
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 240f), rect.sizeDelta.y);
                target.overflowMode = TextOverflowModes.Overflow;
                target.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }
    }

    internal static class UiCampaignMissionProjectionIds
    {
        internal const string M01 = "saga.ch01.m01.first_contact";
        internal const string M02 = "saga.ch01.m02.establish_base";
        internal const string M03 = "saga.ch01.m03.radar_warning";
    }
}
