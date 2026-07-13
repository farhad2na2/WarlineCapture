using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class MissionBriefingScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RectTransform missionOverview;
        [SerializeField] private RectTransform primaryObjectives;
        [SerializeField] private RectTransform tacticalConditions;
        [SerializeField] private RectTransform enemyIntel;
        [SerializeField] private RectTransform chapterProgress;
        [SerializeField] private RectTransform rewards;
        [SerializeField] private RectTransform[] progressNodes;
        [SerializeField] private RawImage missionArtImage;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text missionTitle;
        [SerializeField] private Button deployOperationButton;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RectTransform MissionOverview => missionOverview;
        public RectTransform PrimaryObjectives => primaryObjectives;
        public RectTransform TacticalConditions => tacticalConditions;
        public RectTransform EnemyIntel => enemyIntel;
        public RectTransform ChapterProgress => chapterProgress;
        public RectTransform Rewards => rewards;
        public RectTransform[] ProgressNodes => progressNodes;
        public RawImage MissionArtImage => missionArtImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text MissionTitle => missionTitle;
        public Button DeployOperationButton => deployOperationButton;
    }
}
