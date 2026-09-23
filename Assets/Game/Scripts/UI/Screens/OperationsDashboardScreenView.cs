using TMPro;
using Game.UI.Contracts;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class OperationsDashboardScreenView : MonoBehaviour
    {
        private static readonly string[] ReadinessKeys = { "security", "trust", "threat", "heat", "supply" };
        private static readonly string[] ReadinessFallbacks = { "CITY SECURITY", "CIVILIAN TRUST", "THREAT LEVEL", "HEAT LEVEL", "SUPPLY READINESS" };
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
        [SerializeField] private Button repairButton;
        [SerializeField] private Button endDayButton;
        [SerializeField] private GameObject confirmRaidPopupPrefab;
        [SerializeField] private GameObject endOfDayReportPopupPrefab;
        [SerializeField] private RawImage districtMapImage;
        [SerializeField] private TMP_Text screenTitle;
        [SerializeField] private TMP_Text dayLabel;

        private GameObject _activeModal;
        private bool _modalActionsBound;

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
        public Button PatrolButton => blackMarketButton;
        public Button ArmoryButton => armoryButton;
        public Button CommandLogButton => commandLogButton;
        public Button RaidButton => commandLogButton;
        public Button RepairButton => repairButton;
        public Button EndDayButton => endDayButton;
        public GameObject ConfirmRaidPopupPrefab => confirmRaidPopupPrefab;
        public GameObject EndOfDayReportPopupPrefab => endOfDayReportPopupPrefab;
        public RawImage DistrictMapImage => districtMapImage;
        public TMP_Text ScreenTitle => screenTitle;
        public TMP_Text DayLabel => dayLabel;

        public void Present(UiOperationsDashboardModel model)
        {
            SetValue(transform.Find("Credits/Value"), model.Credits.ToString("N0"));
            SetValue(transform.Find("Command/Value"), model.Command.ToString("N0"));
            string localeDay = UiShellRuntimeGateway.Localization.Get("operations.o001.dashboard_day", "DAY {0}");
            SetValue(dayLabel, model.HasRun
                ? string.Format(localeDay, model.Day)
                : UiShellRuntimeGateway.Localization.Get("operations.o001.dashboard_new_city", "NEW CITY"));
            SetValue(dailyBriefing != null ? dailyBriefing.Find("Time") : null, model.HasRun
                ? string.Format(UiShellRuntimeGateway.Localization.Get("operations.o001.dashboard_ap", "AP {0}"), model.ActionPoints)
                : "—");
            if (readinessCards != null)
                for (int i = 0; i < readinessCards.Length; i++)
                {
                    var card = readinessCards[i];
                    if (card == null) continue;
                    if (i < ReadinessKeys.Length)
                        SetValue(card.Find("Label"), UiShellRuntimeGateway.Localization.Get(
                            "operations.o001.dashboard_" + ReadinessKeys[i], ReadinessFallbacks[i]));
                    int value = model.Readiness != null && i < model.Readiness.Length ? Mathf.Clamp(model.Readiness[i], 0, 100) : -1;
                    var valueText = card.Find("Value")?.GetComponent<TMP_Text>();
                    SetValue(valueText, value < 0 ? "—" : value + "%");
                    for (int segment = 0; segment < 6; segment++)
                    {
                        var bar = card.Find("Segment" + segment)?.GetComponent<Image>();
                        if (bar != null) bar.color = value >= 0 && segment < Mathf.CeilToInt(value * .06f)
                            ? valueText.color : new Color32(39, 45, 45, 255);
                    }
                }
            if (warningButtons == null) return;
            int count = model.Warnings?.Length ?? 0;
            for (int i = 0; i < warningButtons.Length; i++)
            {
                var button = warningButtons[i];
                if (button == null) continue;
                bool show = i < count || i == 0 && count == 0;
                button.gameObject.SetActive(show);
                if (!show) continue;
                button.interactable = false;
                var label = button.transform.Find("Label")?.GetComponent<TMP_Text>();
                SetValue(label, i < count ? model.Warnings[i] : UiShellRuntimeGateway.Localization.Get(
                    model.HasRun ? "operations.o001.dashboard_no_warnings" : "operations.o001.dashboard_start",
                    model.HasRun ? "NO ACTIVE WARNINGS" : "DEPLOY TO BEGIN"));
            }
        }

        private static void SetValue(Transform target, string value) => SetValue(target?.GetComponent<TMP_Text>(), value);
        private static void SetValue(TMP_Text target, string value)
        { if (target != null) UiLocalizedText.Set(target, value); }

        private void Awake() => RefreshBindings();

        private void OnEnable() => RefreshBindings();

        private void OnDestroy()
        {
            RemoveModalBindings();
            if (_activeModal != null)
                DestroyModalObject(_activeModal);
        }

        public void RefreshBindings()
        {
            RemoveModalBindings();
            if (commandLogButton != null)
                commandLogButton.onClick.AddListener(OpenConfirmRaid);
            if (endDayButton != null)
                endDayButton.onClick.AddListener(OpenEndOfDayReport);
            _modalActionsBound = true;
        }

        private void RemoveModalBindings()
        {
            if (!_modalActionsBound)
                return;
            if (commandLogButton != null)
                commandLogButton.onClick.RemoveListener(OpenConfirmRaid);
            if (endDayButton != null)
                endDayButton.onClick.RemoveListener(OpenEndOfDayReport);
            _modalActionsBound = false;
        }

        private void OpenConfirmRaid()
        {
            GameObject modal = MountModal(confirmRaidPopupPrefab);
            ConfirmRaidV3PopupView raid = modal != null
                ? modal.GetComponent<ConfirmRaidV3PopupView>()
                : null;
            if (raid != null)
                raid.Confirmed += CloseActiveModal;
        }

        private void OpenEndOfDayReport()
        {
            GameObject modal = MountModal(endOfDayReportPopupPrefab);
            EndOfDayReportPopupView report = modal != null
                ? modal.GetComponent<EndOfDayReportPopupView>()
                : null;
            if (report != null)
                report.BindActions(CloseActiveModal, CloseActiveModal);
        }

        private GameObject MountModal(GameObject prefab)
        {
            if (prefab == null)
                return null;

            CloseActiveModal();
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.rootCanvas.transform : transform.root;
            _activeModal = Instantiate(prefab, parent, false);
            _activeModal.name = prefab.name;
            if (_activeModal.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            _activeModal.transform.SetAsLastSibling();
            return _activeModal;
        }

        private void CloseActiveModal()
        {
            if (_activeModal == null)
                return;
            DestroyModalObject(_activeModal);
            _activeModal = null;
        }

        private static void DestroyModalObject(GameObject modal)
        {
            if (modal == null)
                return;
            if (Application.isPlaying)
                Destroy(modal);
            else
                DestroyImmediate(modal);
        }
    }
}
