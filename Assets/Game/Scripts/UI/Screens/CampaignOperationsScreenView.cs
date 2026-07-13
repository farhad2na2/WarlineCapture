using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    }
}
