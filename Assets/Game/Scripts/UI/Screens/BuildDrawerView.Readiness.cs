using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public sealed partial class BuildDrawerView
    {
        private RectTransform readinessRoot;
        private Button readinessButton, readinessNavigationButton;
        private TMP_Text readinessNavigationLabel;
        private bool readinessRequested, readinessNavigationExpanded;
        private readonly System.Collections.Generic.List<System.Action<bool>> readinessLayouts = new();
        private TMP_Text readinessStatus, readinessAction;
        public Button ReadinessButton => readinessRoot != null && readinessRoot.gameObject.activeInHierarchy
            ? readinessButton : readinessNavigationButton;

        internal void RefreshReadiness()
        {
            bool available = UiShellRuntimeGateway.TryReadSkirmishReadiness(out var model) && model.Visible;
            if (available) EnsureReadinessNavigation();
            if (readinessNavigationButton != null)
            {
                readinessNavigationButton.gameObject.SetActive(available);
                if (readinessNavigationExpanded != available)
                {
                    foreach (var apply in readinessLayouts) apply(available);
                    readinessNavigationExpanded = available;
                }
            }
            bool fa = UiShellRuntimeGateway.Localization.IsRightToLeft;
            if (readinessNavigationLabel != null)
                UiLocalizedText.Set(readinessNavigationLabel, fa ? "آمادگی" : "READINESS");
            bool visible = available && (readinessRequested || productionPanelActive == null || !productionPanelActive.activeSelf);
            if (!visible)
            {
                if (readinessRoot != null) readinessRoot.gameObject.SetActive(false);
                if (noProductionText != null) noProductionText.enabled = true;
                return;
            }
            if (productionPanel == null) return;
            if (readinessRoot == null)
            {
                readinessRoot = new GameObject("Readiness", typeof(RectTransform), typeof(V3GradientGraphic)).GetComponent<RectTransform>();
                readinessRoot.SetParent(productionPanel.transform, false);
                var background = readinessRoot.GetComponent<V3GradientGraphic>();
                background.Configure(new Color(.12f,.17f,.18f), new Color(.04f,.07f,.08f), new Color(.35f,.42f,.43f), 1);
                background.raycastTarget = true;
                readinessRoot.anchorMin = Vector2.zero; readinessRoot.anchorMax = Vector2.one;
                readinessRoot.offsetMin = new Vector2(12, 12); readinessRoot.offsetMax = new Vector2(-12, -12);
                readinessStatus = ReadinessText(readinessRoot, "Status", 24);
                readinessStatus.rectTransform.anchorMin = new Vector2(0, .48f);
                readinessStatus.rectTransform.anchorMax = Vector2.one;
                readinessStatus.rectTransform.offsetMin = readinessStatus.rectTransform.offsetMax = Vector2.zero;
                var buttonRect = new GameObject("UpgradeReadiness", typeof(RectTransform), typeof(V3GradientGraphic), typeof(Button)).GetComponent<RectTransform>();
                buttonRect.SetParent(readinessRoot, false);
                buttonRect.anchorMin = Vector2.zero; buttonRect.anchorMax = new Vector2(1, .42f);
                buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
                var graphic = buttonRect.GetComponent<V3GradientGraphic>();
                graphic.Configure(new Color(.58f,.36f,.02f), new Color(.22f,.12f,.015f), new Color(1,.72f,.08f), 2);
                readinessButton = buttonRect.GetComponent<Button>();
                readinessButton.targetGraphic = graphic;
                readinessButton.onClick.AddListener(RequestReadiness);
                readinessAction = ReadinessText(buttonRect, "Action", 23);
                readinessAction.rectTransform.anchorMin = Vector2.zero; readinessAction.rectTransform.anchorMax = Vector2.one;
                readinessAction.rectTransform.offsetMin = new Vector2(8, 4); readinessAction.rectTransform.offsetMax = new Vector2(-8, -4);
            }
            if (noProductionText != null) noProductionText.enabled = false;
            readinessRoot.gameObject.SetActive(true);
            string stage = model.Stage >= 3 ? (fa ? "آمادگی کامل" : "Full Arsenal") : model.Stage >= 2
                ? (fa ? "آمادگی تثبیت‌شده" : "Established") : (fa ? "آمادگی میدانی" : "Field");
            string status = model.InProgress
                ? (fa ? "ارتقای آمادگی" : "Upgrading readiness") + " · " + Mathf.CeilToInt(model.RemainingSeconds) + (fa ? " ثانیه" : "s")
                : stage + (model.Stage >= 3 ? "" : (model.Stage < 2 ? (fa ? " ← تثبیت‌شده" : " → Established") : (fa ? " ← کامل" : " → Full Arsenal")) + "\n" + model.MaterialsCost + (fa ? " مصالح · " : " Materials · ") + Mathf.CeilToInt(model.RemainingSeconds) + (fa ? " ثانیه" : "s"));
            string action = model.InProgress ? (fa ? "لغو ارتقا" : "CANCEL UPGRADE") : model.Stage >= 3
                ? (fa ? "آمادگی کامل" : "FULL ARSENAL") : model.CanUpgrade
                    ? (fa ? "ارتقای آمادگی" : "UPGRADE READINESS") : model.Reason == "InsufficientMaterials"
                        ? (fa ? "مصالح ناکافی" : "NEED MATERIALS") : (fa ? "ارتقا در دسترس نیست" : "UPGRADE UNAVAILABLE");
            UiLocalizedText.Set(readinessStatus, status);
            UiLocalizedText.Set(readinessAction, action);
            readinessButton.interactable = model.CanUpgrade || model.CanCancel;
        }

        private void EnsureReadinessNavigation()
        {
            if (readinessNavigationButton != null || tabs == null || tabs.Length == 0 || tabs[0]?.Button == null) return;
            var parent = tabs[0].Button.transform.parent as RectTransform;
            if (parent == null) return;
            float step = parent.rect.height / (tabs.Length + 1);
            if (step <= 0) return;
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = tabs[i];
                if (tab?.Button == null) continue;
                var rect = (RectTransform)tab.Button.transform;
                Vector2 oldSize = rect.sizeDelta, oldPosition = rect.anchoredPosition;
                var icon = rect.Find("Icon") as RectTransform;
                var label = tab.LabelText != null ? tab.LabelText.rectTransform : null;
                var disabled = rect.Find("DisabledOverlay") as RectTransform;
                Vector2 iconPosition = icon != null ? icon.anchoredPosition : default;
                Vector2 labelPosition = label != null ? label.anchoredPosition : default;
                Vector2 disabledSize = disabled != null ? disabled.sizeDelta : default;
                float inset = (rect.rect.height - (step - 9)) * .5f;
                int index = i;
                System.Action<bool> apply = expanded =>
                {
                    rect.sizeDelta = expanded ? new Vector2(oldSize.x, step - 9) : oldSize;
                    rect.anchoredPosition = expanded ? new Vector2(oldPosition.x, -index * step) : oldPosition;
                    if (icon != null) icon.anchoredPosition = iconPosition + (expanded ? new Vector2(0, inset) : Vector2.zero);
                    if (label != null) label.anchoredPosition = labelPosition + (expanded ? new Vector2(0, inset) : Vector2.zero);
                    if (disabled != null) disabled.sizeDelta = expanded ? new Vector2(disabledSize.x, step - 15) : disabledSize;
                };
                readinessLayouts.Add(apply);
                apply(true);
                tab.Button.onClick.AddListener(() => readinessRequested = false);
            }
            readinessNavigationExpanded = true;
            var navigation = new GameObject("ReadinessTab", typeof(RectTransform), typeof(V3GradientGraphic), typeof(Button)).GetComponent<RectTransform>();
            navigation.SetParent(parent, false);
            navigation.anchorMin = navigation.anchorMax = navigation.pivot = new Vector2(0, 1);
            navigation.anchoredPosition = new Vector2(0, -tabs.Length * step);
            navigation.sizeDelta = new Vector2(parent.rect.width, step - 9);
            var graphic = navigation.GetComponent<V3GradientGraphic>();
            graphic.Configure(new Color(.28f,.21f,.04f), new Color(.10f,.08f,.02f), new Color(1,.72f,.08f), 2);
            graphic.raycastTarget = true;
            readinessNavigationButton = navigation.GetComponent<Button>();
            readinessNavigationButton.targetGraphic = graphic;
            readinessNavigationButton.onClick.AddListener(() => { readinessRequested = true; RefreshReadiness(); });
            readinessNavigationLabel = ReadinessText(navigation, "Label", 22);
            readinessNavigationLabel.rectTransform.anchorMin = Vector2.zero;
            readinessNavigationLabel.rectTransform.anchorMax = Vector2.one;
            readinessNavigationLabel.rectTransform.offsetMin = new Vector2(8, 8);
            readinessNavigationLabel.rectTransform.offsetMax = new Vector2(-8, -8);
            if (closeButton != null) closeButton.onClick.AddListener(() => readinessRequested = false);
        }

        private TMP_Text ReadinessText(Transform parent, string name, float size)
        {
            var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            text.transform.SetParent(parent, false);
            text.font = instructionText != null ? instructionText.font : TMP_Settings.defaultFontAsset;
            text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal; text.raycastTarget = false;
            return text;
        }

        private void RequestReadiness()
        {
            if (!UiShellRuntimeGateway.TryReadSkirmishReadiness(out var model)) return;
            UiShellRuntimeGateway.TryRequestSkirmishReadiness(model.InProgress ? model.ResearchId : 0);
        }
    }
}
