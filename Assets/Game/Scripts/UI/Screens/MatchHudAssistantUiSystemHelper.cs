using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    internal sealed class MatchHudAssistantUiSystemHelper
    {
        private bool _loggedMissingButton;
        private bool _loggedInvalidButton;

        private RectTransform _buttonRoot;
        private Button _button;
        private TMP_Text _accessStateText;
        private TMP_Text _accessCueText;
        private GameObject _objectivePanel;
        private bool _objectivePanelOriginalActive;
        private RectTransform _popupLayer;
        private GameObject _popupPrefab;
        private GameObject _popupInstance;
        private AriaCommandAssistantPopupView _popupView;
        private readonly AssistantPanelUiSystemHelper _panelUiSystem = new();
        private readonly AssistantHighlightPresentationSystemHelper _highlightPresentationSystem = new();
        private Action _captureGameplayUiClick;
        private Action _beforePanelOpen;
        private Action<bool> _panelOpenChanged;
        private bool _mirroredPanelOpen;
        private UiAssistantPanelModel _lastPanelModel = UiAssistantPanelModel.Empty;
        private UiAssistantHighlightModel _lastHighlightModel = UiAssistantHighlightModel.Empty;

        public bool IsPanelOpen => _popupView != null && _popupView.IsOpen;
        public bool IsBound => _buttonRoot != null && _popupView != null;

        public void Bind(
            GameObject headerContent,
            RectTransform popupLayer,
            GameObject popupPrefab,
            Action captureGameplayUiClick,
            Action beforePanelOpen,
            Action<bool> panelOpenChanged)
        {
            Unbind();
            _captureGameplayUiClick = captureGameplayUiClick;
            _beforePanelOpen = beforePanelOpen;
            _panelOpenChanged = panelOpenChanged;
            _popupLayer = popupLayer;
            _popupPrefab = popupPrefab;

            RectTransform headerRect = headerContent != null ? headerContent.transform as RectTransform : null;
            if (headerRect == null || _popupLayer == null || _popupPrefab == null)
                return;

            _buttonRoot = MatchHudAssistantReferenceUiSystemHelper.ResolveButton(headerContent, out _button);
            if (_buttonRoot == null)
            {
                LogMissingButton();
                return;
            }

            RectTransform objectiveRect = MatchHudAssistantReferenceUiSystemHelper.ResolveObjectivesPanel(
                headerContent,
                _buttonRoot);
            if (objectiveRect != null)
            {
                _objectivePanel = objectiveRect.gameObject;
                _objectivePanelOriginalActive = _objectivePanel.activeSelf;
            }

            if (!EnsurePopupView())
                return;

            if (!TryBindButtonHierarchy())
            {
                LogInvalidButton();
                DestroyPopupInstance();
                _buttonRoot = null;
                return;
            }

            _panelUiSystem.Bind(_popupView, _accessStateText, _accessCueText);
            _panelUiSystem.ApplyReadModel(_lastPanelModel);
            _popupView.Hide();
            MirrorPanelOpen(false, force: true);

            if (_objectivePanel != null)
                _objectivePanel.SetActive(false);
        }

        public void Unbind()
        {
            MirrorPanelOpen(false, force: true);
            if (_button != null)
                _button.onClick.RemoveListener(TogglePanel);

            _popupView?.UnbindActions();
            DestroyPopupInstance();
            if (_objectivePanel != null)
                _objectivePanel.SetActive(_objectivePanelOriginalActive);

            _buttonRoot = null;
            _button = null;
            _accessStateText = null;
            _accessCueText = null;
            _objectivePanel = null;
            _objectivePanelOriginalActive = false;
            _popupLayer = null;
            _popupPrefab = null;
            _captureGameplayUiClick = null;
            _beforePanelOpen = null;
            _panelOpenChanged = null;
            _panelUiSystem.Unbind();
            _highlightPresentationSystem.Unbind();
            _lastPanelModel = UiAssistantPanelModel.Empty;
            _lastHighlightModel = UiAssistantHighlightModel.Empty;
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            _lastPanelModel = model;
            if (_buttonRoot == null)
                return;

            if (_popupView == null && !EnsurePopupView())
                return;
            _panelUiSystem.ApplyReadModel(model);
        }

        public void ApplyHighlightReadModel(UiAssistantHighlightModel model)
        {
            _lastHighlightModel = model;
            if (_buttonRoot == null)
                return;

            if (_popupView == null && !EnsurePopupView())
                return;
            _highlightPresentationSystem.ApplyReadModel(model);
        }

        public bool TryClosePanel()
        {
            if (!IsPanelOpen)
                return false;

            ClosePanel();
            return true;
        }

        public void ClosePanelWithoutInputCapture()
        {
            SetPanelOpen(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera(_buttonRoot);
            return ContainsRect(_buttonRoot, screenPosition, eventCamera) ||
                   (_popupView != null && _popupView.ContainsScreenPoint(screenPosition));
        }

        private bool EnsurePopupView()
        {
            if (_popupView != null)
                return true;
            if (_popupLayer == null || _popupPrefab == null)
                return false;

            _popupInstance = UnityEngine.Object.Instantiate(_popupPrefab, _popupLayer, false);
            _popupInstance.name = _popupPrefab.name;
            _popupView = _popupInstance.GetComponent<AriaCommandAssistantPopupView>();
            if (_popupView == null || !_popupView.TryBindHierarchy())
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("[ARIA] POP13 popup is missing AriaCommandAssistantPopupView or its locked LandscapeLayout hierarchy.");
#endif
                DestroyPopupInstance();
                return false;
            }

            RectTransform layout = _popupView.LandscapeLayout;
            layout.anchorMin = new Vector2(0.5f, 0.5f);
            layout.anchorMax = new Vector2(0.5f, 0.5f);
            layout.pivot = new Vector2(0.5f, 0.5f);
            layout.anchoredPosition = new Vector2(0f, 156f);
            layout.sizeDelta = new Vector2(2460f, 1510f);
            _popupView.BindActions(
                ClosePanel,
                ShowRecommendation,
                ExecuteRecommendation,
                StopAssistantControl);
            _panelUiSystem.Bind(_popupView, _accessStateText, _accessCueText);
            _highlightPresentationSystem.Bind(_popupView.PreviewHighlight);
            _panelUiSystem.ApplyReadModel(_lastPanelModel);
            _highlightPresentationSystem.ApplyReadModel(_lastHighlightModel);
            _popupView.Hide();
            return true;
        }

        private bool TryBindButtonHierarchy()
        {
            if (_buttonRoot == null)
                return false;

            if (_button == null ||
                !MatchHudAssistantReferenceUiSystemHelper.TryResolveButtonText(
                    _buttonRoot,
                    out _accessStateText,
                    out _accessCueText))
                return false;

            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(_buttonRoot.gameObject, needsRaycaster: true);
            _button.onClick.RemoveListener(TogglePanel);
            _button.onClick.AddListener(TogglePanel);
            return true;
        }

        private void TogglePanel()
        {
            CaptureUiOnly();
            SetPanelOpen(!IsPanelOpen);
        }

        private void ClosePanel()
        {
            CaptureUiOnly();
            SetPanelOpen(false);
        }

        private void ShowRecommendation()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ShowRecommendation);
        }

        private void ExecuteRecommendation()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(
                UiAssistantCommandIntentKind.ExecuteRecommendation,
                fromTakeover: true);
        }

        private void StopAssistantControl()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.StopAssistantControl);
        }

        private void SetPanelOpen(bool open)
        {
            if (open)
            {
                _beforePanelOpen?.Invoke();
                if (!EnsurePopupView())
                    return;
                _popupView.Show();
            }
            else
            {
                _popupView?.Hide();
            }

            MirrorPanelOpen(open);
        }

        private void MirrorPanelOpen(bool open, bool force = false)
        {
            if (!force && _mirroredPanelOpen == open)
                return;

            _mirroredPanelOpen = open;
            _panelOpenChanged?.Invoke(open);
        }

        private void DestroyPopupInstance()
        {
            _popupView?.UnbindActions();
            DestroyObject(_popupInstance);
            _popupInstance = null;
            _popupView = null;
            _panelUiSystem.Unbind();
        }

        private void CaptureUiOnly()
        {
            _captureGameplayUiClick?.Invoke();
            ClearSelectedButton();
        }

        private void LogMissingButton()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_loggedMissingButton)
                return;
            _loggedMissingButton = true;
            Debug.LogError("[ARIA] Match HUD prefab is missing HeaderContent/AriaAssistantButton; runtime button creation is disabled.");
#endif
        }

        private void LogInvalidButton()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_loggedInvalidButton)
                return;
            _loggedInvalidButton = true;
            Debug.LogError("[ARIA] HeaderContent/AriaAssistantButton must contain a Button plus TMP State and AlertCue children.");
#endif
        }

        private static bool ContainsRect(RectTransform rect, Vector2 screenPosition, Camera eventCamera)
        {
            return rect != null &&
                   rect.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, eventCamera);
        }

        private static Camera ResolveEventCamera(Component component)
        {
            Canvas canvas = component != null ? component.GetComponentInParent<Canvas>() : null;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;
            return canvas.worldCamera;
        }

        private static void ClearSelectedButton()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(null);
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
