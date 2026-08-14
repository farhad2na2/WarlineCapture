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
        [SerializeField] private RectTransform[] progressNodes;
        [SerializeField] private RawImage districtMapImage;
        [SerializeField] private RawImage missionPreviewImage;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text missionName;
        [SerializeField] private Button storyArchiveButton;
        [SerializeField] private Button chapterIntelButton;
        [SerializeField] private Button launchMissionButton;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RectTransform ChapterRail => chapterRail;
        public RectTransform StrategicMap => strategicMap;
        public RectTransform MissionBriefing => missionBriefing;
        public RectTransform[] ChapterCards => chapterCards;
        public RectTransform[] MissionNodes => missionNodes;
        public RectTransform[] ProgressNodes => progressNodes;
        public RawImage DistrictMapImage => districtMapImage;
        public RawImage MissionPreviewImage => missionPreviewImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text MissionName => missionName;
        public Button StoryArchiveButton => storyArchiveButton;
        public Button ChapterIntelButton => chapterIntelButton;
        public Button LaunchMissionButton => launchMissionButton;

        public void Apply(UiCampaignOperationsModel model)
        {
            UiCampaignMissionModel mission = model.SelectedMission;
            screenTitle.enableAutoSizing = true;
            screenTitle.fontSizeMin = 48f;
            screenTitle.fontSizeMax = 118f;
            missionName.enableAutoSizing = true;
            missionName.fontSizeMin = 36f;
            missionName.fontSizeMax = 84f;
            screenTitle.text = model.NextMissionRevealed
                ? "CAMPAIGN OPERATIONS  |  NEXT READY"
                : "CAMPAIGN OPERATIONS";
            missionName.text = FormatMissionSummary(mission);
            launchMissionButton.interactable = mission.Available;
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
    }
}
