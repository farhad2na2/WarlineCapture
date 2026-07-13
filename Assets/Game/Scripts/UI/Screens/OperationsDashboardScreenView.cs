using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class OperationsDashboardScreenView : MonoBehaviour
    {
        [SerializeField] private UIShellRouteButtonView backRouteButton;
        [SerializeField] private RectTransform readinessRail;
        [SerializeField] private RectTransform districtMap;
        [SerializeField] private RectTransform dailyBriefing;
        [SerializeField] private RectTransform activeWarnings;
        [SerializeField] private RectTransform commandBar;
        [SerializeField] private RectTransform[] readinessCards;
        [SerializeField] private Button[] districtButtons;
        [SerializeField] private Button[] warningButtons;
        [SerializeField] private Button intelReportButton;
        [SerializeField] private Button blackMarketButton;
        [SerializeField] private Button armoryButton;
        [SerializeField] private Button commandLogButton;
        [SerializeField] private Button endDayButton;
        [SerializeField] private RawImage districtMapImage;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text dayLabel;

        public UIShellRouteButtonView BackRouteButton => backRouteButton;
        public RectTransform ReadinessRail => readinessRail;
        public RectTransform DistrictMap => districtMap;
        public RectTransform DailyBriefing => dailyBriefing;
        public RectTransform ActiveWarnings => activeWarnings;
        public RectTransform CommandBar => commandBar;
        public RectTransform[] ReadinessCards => readinessCards;
        public Button[] DistrictButtons => districtButtons;
        public Button[] WarningButtons => warningButtons;
        public Button IntelReportButton => intelReportButton;
        public Button BlackMarketButton => blackMarketButton;
        public Button ArmoryButton => armoryButton;
        public Button CommandLogButton => commandLogButton;
        public Button EndDayButton => endDayButton;
        public RawImage DistrictMapImage => districtMapImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text DayLabel => dayLabel;
    }
}
