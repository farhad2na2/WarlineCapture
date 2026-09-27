using Game.UI.Contracts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Runtime
{
    public enum SettingsPopupContext
    {
        Menu = 0,
        Match = 1
    }

    [DisallowMultipleComponent]
    public sealed class SettingsPopupView : MonoBehaviour
    {
        private readonly SettingsScreenFlowUiSystemHelper flowSystem = new();

        [SerializeField] private SettingsPopupContext context;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button applyButton;

        private UISettingsModel _model;
        private System.Action _closeRequested;
        private Button skipButton;
        private bool footerCaptured;
        private Vector2 resetAnchorMin, resetAnchorMax, resetSize, resetPosition;
        private Vector2 applyAnchorMin, applyAnchorMax, applySize, applyPosition;

        public SettingsPopupContext Context => context;
        public Button CloseButton => closeButton;
        public Button ResetButton => resetButton;
        public Button ApplyButton => applyButton;
        public SettingsPanelView SettingsPanel => settingsPanel;

        private void Awake()
        {
            ApplyContextTitle();

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSettings);
            if (applyButton != null)
                applyButton.onClick.AddListener(SaveSettings);

            LoadSettings();
        }

        private void OnEnable()
        {
            UiShellRuntimeGateway.Localization.LocaleChanged += ApplyContextTitle;
            ApplyContextTitle();
        }

        private void OnDisable()
        {
            UiShellRuntimeGateway.Localization.LocaleChanged -= ApplyContextTitle;
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetSettings);
            if (applyButton != null)
                applyButton.onClick.RemoveListener(SaveSettings);
            if (skipButton != null)
                skipButton.onClick.RemoveListener(SkipToVictory);

            _closeRequested = null;
        }

        public void BindClose(System.Action closeRequested)
        {
            _closeRequested = closeRequested;
        }

        public void ConfigureContext(SettingsPopupContext popupContext)
        {
            context = popupContext;
            ApplyContextTitle();
            EnsureSkipControl();
        }

        public void LoadSettings()
        {
            _model = flowSystem.LoadSettings(settingsPanel);
        }

        public void SaveSettings()
        {
            _model = flowSystem.SaveSettings(settingsPanel, _model);
        }

        public void ResetSettings()
        {
            _model = flowSystem.ResetSettings(settingsPanel);
        }

        private void Close()
        {
            _closeRequested?.Invoke();
        }

        private void ApplyContextTitle()
        {
            if (titleText != null)
                UiLocalizedText.Set(titleText, "COMMAND SETTINGS");
            EnsureSkipControl();
        }

        private void EnsureSkipControl()
        {
            bool show = context == SettingsPopupContext.Match;
            if (show)
                EnsureSkipButton();
            if (skipButton != null)
                skipButton.gameObject.SetActive(show);
            LayoutFooter(show);
        }

        private void EnsureSkipButton()
        {
            if (skipButton != null || applyButton == null)
                return;
            GameObject copy = Instantiate(applyButton.gameObject, applyButton.transform.parent);
            copy.name = "SkipToVictoryButton";
            skipButton = copy.GetComponent<Button>();
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipToVictory);
            foreach (Image image in skipButton.GetComponentsInChildren<Image>(true))
            {
                if (image.transform != skipButton.transform)
                    image.gameObject.SetActive(false);
            }
            TMP_Text label = skipButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableAutoSizing = true;
                label.fontSizeMin = 18f;
                label.fontSizeMax = 32f;
                label.rectTransform.anchoredPosition = Vector2.zero;
                label.rectTransform.sizeDelta = new Vector2(-28f, 0f);
                UiLocalizedText.Set(label, "SKIP TO VICTORY");
            }
        }

        private void LayoutFooter(bool includeSkip)
        {
            if (resetButton == null || applyButton == null)
                return;
            RectTransform reset = resetButton.GetComponent<RectTransform>();
            RectTransform apply = applyButton.GetComponent<RectTransform>();
            if (!footerCaptured)
            {
                resetAnchorMin = reset.anchorMin;
                resetAnchorMax = reset.anchorMax;
                resetSize = reset.sizeDelta;
                resetPosition = reset.anchoredPosition;
                applyAnchorMin = apply.anchorMin;
                applyAnchorMax = apply.anchorMax;
                applySize = apply.sizeDelta;
                applyPosition = apply.anchoredPosition;
                footerCaptured = true;
            }
            if (!includeSkip || skipButton == null)
            {
                reset.anchorMin = resetAnchorMin;
                reset.anchorMax = resetAnchorMax;
                reset.sizeDelta = resetSize;
                reset.anchoredPosition = resetPosition;
                apply.anchorMin = applyAnchorMin;
                apply.anchorMax = applyAnchorMax;
                apply.sizeDelta = applySize;
                apply.anchoredPosition = applyPosition;
                return;
            }

            PlaceFooterButton(reset, 0f, 0.333f);
            PlaceFooterButton(skipButton.GetComponent<RectTransform>(), 0.333f, 0.667f);
            PlaceFooterButton(apply, 0.667f, 1f);
        }

        private static void PlaceFooterButton(RectTransform rect, float minX, float maxX)
        {
            rect.anchorMin = new Vector2(minX, 0.5f);
            rect.anchorMax = new Vector2(maxX, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(-28f, 112f);
            rect.anchoredPosition = Vector2.zero;
        }

        private void SkipToVictory()
        {
            if (!UiShellRuntimeGateway.TrySkipMatchToPerfectWin())
                return;
            UiShellRuntimeGateway.TryEnqueueUiAction(UiActionKind.ClosePause);
            Close();
        }
    }
}
