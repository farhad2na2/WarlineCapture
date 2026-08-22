using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AriaTutorialBriefingView : MonoBehaviour
    {
        [SerializeField] private RectTransform briefingLayout;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button showMeButton;
        [SerializeField] private Button doItButton;
        [SerializeField] private TMP_Text showMeButtonLabel;
        [SerializeField] private TMP_Text doItButtonLabel;

        private Action _closeRequested;
        private Action _showRecommendationRequested;
        private Action _executeRecommendationRequested;

        public RectTransform BriefingLayout => briefingLayout;
        public Image PortraitImage => portraitImage;
        public TMP_Text TitleText => titleText;
        public TMP_Text BodyText => bodyText;
        public TMP_Text ProgressText => progressText;
        public Button CloseButton => closeButton;
        public Button ShowMeButton => showMeButton;
        public Button DoItButton => doItButton;

        public bool TryBindHierarchy()
        {
            return briefingLayout != null && portraitImage != null && titleText != null &&
                   bodyText != null && progressText != null && closeButton != null &&
                   showMeButton != null && doItButton != null &&
                   showMeButtonLabel != null && doItButtonLabel != null;
        }

        public void BindActions(
            Action closeRequested,
            Action showRecommendationRequested,
            Action executeRecommendationRequested)
        {
            UnbindActions();
            _closeRequested = closeRequested;
            _showRecommendationRequested = showRecommendationRequested;
            _executeRecommendationRequested = executeRecommendationRequested;
            closeButton.onClick.AddListener(RequestClose);
            showMeButton.onClick.AddListener(RequestShowRecommendation);
            doItButton.onClick.AddListener(RequestExecuteRecommendation);
        }

        public void UnbindActions()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(RequestClose);
            if (showMeButton != null)
                showMeButton.onClick.RemoveListener(RequestShowRecommendation);
            if (doItButton != null)
                doItButton.onClick.RemoveListener(RequestExecuteRecommendation);

            _closeRequested = null;
            _showRecommendationRequested = null;
            _executeRecommendationRequested = null;
        }

        public void Apply(UiAssistantPanelModel model)
        {
            titleText.text = (model.RecommendationTitle ?? string.Empty).ToUpperInvariant();
            bodyText.text = model.RecommendationBody ?? string.Empty;
            int step = Mathf.Max(1, model.TutorialStep);
            int count = Mathf.Max(step, model.TutorialStepCount);
            progressText.SetText("TRAINING {0} / {1}", step, count);
            showMeButton.interactable = model.CanShow;
            doItButton.interactable = model.CanExecute;
            showMeButtonLabel.text = "SHOW ME";
            doItButtonLabel.text = "DO IT";
        }

        public void ApplyAccessibility(bool largeTextEnabled, bool highContrastEnabled)
        {
            float scale = largeTextEnabled ? 1.08f : 1f;
            titleText.fontSize = 58f * scale;
            bodyText.fontSize = 38f * scale;
            progressText.fontSize = 30f * scale;
            Color primary = highContrastEnabled ? Color.white : new Color(0.95f, 0.92f, 0.82f, 1f);
            titleText.color = primary;
            bodyText.color = primary;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!gameObject.activeInHierarchy || briefingLayout == null)
                return false;

            Canvas canvas = briefingLayout.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            return RectTransformUtility.RectangleContainsScreenPoint(briefingLayout, screenPosition, eventCamera);
        }

        private void OnDestroy()
        {
            UnbindActions();
        }

        private void RequestClose() => _closeRequested?.Invoke();
        private void RequestShowRecommendation() => _showRecommendationRequested?.Invoke();
        private void RequestExecuteRecommendation() => _executeRecommendationRequested?.Invoke();
    }
}
