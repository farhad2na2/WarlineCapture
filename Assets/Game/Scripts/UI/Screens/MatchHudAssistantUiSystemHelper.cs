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
        private const string ButtonRootName = "AriaAssistantButton";
        private static readonly Vector2 ObjectiveSlotButtonSize = new(454f, 155f);
        private static readonly Vector2 HeaderFallbackButtonSize = new(228f, 78f);
        private static bool s_loggedMissingObjectiveSlot;

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

            RectTransform objectiveRect = ResolveObjectivesPanel(headerContent.transform);
            if (objectiveRect != null)
            {
                _objectivePanel = objectiveRect.gameObject;
                _objectivePanelOriginalActive = _objectivePanel.activeSelf;
            }
            else
            {
                LogMissingObjectiveSlot();
            }

            if (!EnsurePopupView())
                return;

            _buttonRoot = CreateButton(headerRect, objectiveRect);
            if (_buttonRoot == null)
            {
                DestroyPopupInstance();
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
            DestroyObject(_buttonRoot != null ? _buttonRoot.gameObject : null);
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

        private RectTransform CreateButton(RectTransform headerRect, RectTransform objectiveRect)
        {
            RectTransform parent = objectiveRect != null
                ? objectiveRect.parent as RectTransform
                : headerRect;
            if (parent == null)
                return null;

            RectTransform root = CreateRect(ButtonRootName, parent);
            bool usesObjectiveSlot = objectiveRect != null;
            if (usesObjectiveSlot)
            {
                root.anchorMin = objectiveRect.anchorMin;
                root.anchorMax = objectiveRect.anchorMax;
                root.pivot = objectiveRect.pivot;
                root.anchoredPosition = objectiveRect.anchoredPosition;
                root.sizeDelta = ObjectiveSlotButtonSize;
                root.SetSiblingIndex(objectiveRect.GetSiblingIndex() + 1);
            }
            else
            {
                root.anchorMin = new Vector2(1f, 1f);
                root.anchorMax = new Vector2(1f, 1f);
                root.pivot = new Vector2(1f, 1f);
                root.anchoredPosition = new Vector2(-360f, -26f);
                root.sizeDelta = HeaderFallbackButtonSize;
                root.SetAsLastSibling();
            }

            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(root.gameObject, needsRaycaster: true);
            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.025f, 0.055f, 0.075f, 0.96f);
            background.raycastTarget = true;

            _button = root.gameObject.AddComponent<Button>();
            _button.targetGraphic = background;
            _button.onClick.AddListener(TogglePanel);

            float titleSize = usesObjectiveSlot ? 48f : 28f;
            float stateSize = usesObjectiveSlot ? 25f : 18f;
            Vector2 titlePosition = usesObjectiveSlot ? new Vector2(34f, -22f) : new Vector2(18f, -8f);
            Vector2 titleBounds = usesObjectiveSlot ? new Vector2(260f, 58f) : new Vector2(94f, 34f);
            Vector2 statePosition = usesObjectiveSlot ? new Vector2(34f, -88f) : new Vector2(18f, -42f);
            Vector2 stateBounds = usesObjectiveSlot ? new Vector2(300f, 38f) : new Vector2(144f, 24f);
            CreateText("Label", root, "ARIA", titleSize, titlePosition, titleBounds, Color.white);
            _accessStateText = CreateText(
                "State",
                root,
                "PLAYER CONTROL",
                stateSize,
                statePosition,
                stateBounds,
                new Color(0.45f, 0.95f, 1f, 1f));
            _accessCueText = CreateText(
                "AlertCue",
                root,
                string.Empty,
                usesObjectiveSlot ? 22f : 16f,
                usesObjectiveSlot ? new Vector2(322f, -92f) : new Vector2(154f, -44f),
                usesObjectiveSlot ? new Vector2(104f, 34f) : new Vector2(62f, 24f),
                new Color(1f, 0.72f, 0.24f, 1f));
            _accessCueText.alignment = TextAlignmentOptions.Right;
            _accessCueText.gameObject.SetActive(false);
            return root;
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

        private static RectTransform ResolveObjectivesPanel(Transform headerContent)
        {
            if (headerContent == null)
                return null;

            Transform objective = headerContent.Find("ObjectivesPanel");
            objective ??= headerContent.Find("HeaderContent/ObjectivesPanel");
            return objective as RectTransform;
        }

        private static void LogMissingObjectiveSlot()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (s_loggedMissingObjectiveSlot)
                return;
            s_loggedMissingObjectiveSlot = true;
            Debug.LogWarning("[ARIA] HeaderContent/ObjectivesPanel was not found; keeping objectives visible and using the header fallback anchor.");
#endif
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string text,
            float fontSize,
            Vector2 anchoredPosition,
            Vector2 size,
            Color color)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;

            TMP_Text label = root.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Left;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(14f, fontSize * 0.7f);
            label.fontSizeMax = fontSize;
            label.raycastTarget = false;
            label.color = color;
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.GetComponent<RectTransform>();
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
