using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class CampaignOperationsScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RectTransform chapterRail;
        [SerializeField] private RectTransform strategicMap;
        [SerializeField] private RectTransform missionBriefing;
        [SerializeField] private RectTransform[] chapterCards;
        [SerializeField] private RectTransform[] missionNodes;
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
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;

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
        public Button StoryArchiveButton => storyArchiveButton;
        public Button ChapterIntelButton => chapterIntelButton;
        public Button LaunchMissionButton => launchMissionButton;

        public void BindGameTextResolver(IGameTextResolver gameTextResolver)
        {
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
        }

        public void Apply(UiCampaignOperationsModel model)
        {
            UiCampaignMissionModel mission = model.SelectedMission;
            screenTitle.enableAutoSizing = true;
            screenTitle.fontSizeMin = 48f;
            screenTitle.fontSizeMax = 118f;
            missionName.enableAutoSizing = true;
            missionName.fontSizeMin = 36f;
            missionName.fontSizeMax = 84f;
            bool m02 = mission.MissionId == UiCampaignMissionProjectionIds.M02;
            screenTitle.text = _gameTextResolver.Get(
                "campaign.operations.title",
                model.NextMissionRevealed ? "CAMPAIGN OPERATIONS  |  NEXT READY" : "CAMPAIGN OPERATIONS");
            Set(missionNumber, m02 ? "MISSION 02" : "MISSION 01");
            missionName.text = FormatMissionSummary(mission);
            Set(missionBriefingText, _gameTextResolver.Get(
                m02 ? "mission.m02.summary" : "mission.m01.summary",
                m02
                    ? "Reopen an abandoned forward post before a second hostile cell reaches it."
                    : "Secure the Old Market corridor and protect the civilian route."));
            Set(primaryObjectiveText, _gameTextResolver.Get(
                m02 ? "mission.m02.objective.build_forward_barracks" : "mission.m01.objective.secure_corridor",
                m02 ? "BUILD THE FORWARD BARRACKS" : "ELIMINATE THE HOSTILE PATROL"));
            Set(rewardSummaryText, m02
                ? _gameTextResolver.Get("mission.m02.reward.card", "320 XP  |  1,500 CREDITS  |  BARRACKS UNLOCK")
                : _gameTextResolver.Get("mission.m01.reward.card", "260 XP  |  1,200 CREDITS"));
            if (missionPreviewImage != null)
                missionPreviewImage.texture = m02 ? m02MissionPreview : m01MissionPreview;
            Set(launchMissionLabel, mission.PrimaryActionLabel);
            launchMissionButton.interactable = mission.Available;
            ApplyMissionNodes(mission.MissionId, model.NextMissionRevealed);
            for (int index = 0; index < progressNodes.Length; index++)
                progressNodes[index].gameObject.SetActive(index < mission.BestStars);
        }

        public void ApplyUnavailable()
        {
            missionName.text = "MISSION DATA UNAVAILABLE";
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

        private void ApplyMissionNodes(string selectedMissionId, bool m02Revealed)
        {
            for (int index = 0; index < (missionNodes?.Length ?? 0); index++)
            {
                bool available = index == 0 || index == 1 && m02Revealed ||
                                 index == 1 && selectedMissionId == UiCampaignMissionProjectionIds.M02;
                if (missionNodeButtons != null && index < missionNodeButtons.Length &&
                    missionNodeButtons[index] != null)
                    missionNodeButtons[index].interactable = available;
                Transform lockIcon = missionNodes[index] != null ? missionNodes[index].Find("Lock") : null;
                if (lockIcon != null)
                    lockIcon.gameObject.SetActive(!available);
            }
        }

        private static void Set(TMP_Text target, string value)
        {
            if (target != null)
                target.text = value ?? string.Empty;
        }
    }

    internal static class UiCampaignMissionProjectionIds
    {
        internal const string M01 = "saga.ch01.m01.first_contact";
        internal const string M02 = "saga.ch01.m02.establish_base";
    }
}
