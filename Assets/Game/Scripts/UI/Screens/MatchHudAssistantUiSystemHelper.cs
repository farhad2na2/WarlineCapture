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
        private const string PanelRootName = "AriaAssistantPanel";

        private RectTransform _buttonRoot;
        private RectTransform _panelRoot;
        private Button _button;
        private Button _closeButton;
        private Button _nextActionButton;
        private Button _giveControlButton;
        private Button _stopButton;
        private readonly AssistantPanelUiSystemHelper _panelUiSystem = new();
        private readonly AssistantHighlightPresentationSystemHelper _highlightPresentationSystem = new();
        private Action _captureGameplayUiClick;

        public bool IsPanelOpen => _panelRoot != null && _panelRoot.gameObject.activeSelf;

        public void Bind(GameObject headerContent, RectTransform overlayRoot, Action captureGameplayUiClick)
        {
            Unbind();
            _captureGameplayUiClick = captureGameplayUiClick;

            RectTransform headerRect = headerContent != null ? headerContent.transform as RectTransform : null;
            if (headerRect == null)
                return;

            RectTransform panelParent = overlayRoot != null ? overlayRoot : headerRect;
            _buttonRoot = CreateButton(headerRect);
            _panelRoot = CreatePanel(panelParent, headerRect);
            SetPanelOpen(false);
        }

        public void Unbind()
        {
            UnbindButton(_button, TogglePanel);
            UnbindButton(_closeButton, ClosePanel);
            UnbindButton(_nextActionButton, ShowRecommendation);
            UnbindButton(_giveControlButton, ExecuteRecommendation);
            UnbindButton(_stopButton, StopAssistantControl);

            DestroyObject(_buttonRoot != null ? _buttonRoot.gameObject : null);
            DestroyObject(_panelRoot != null ? _panelRoot.gameObject : null);

            _buttonRoot = null;
            _panelRoot = null;
            _button = null;
            _closeButton = null;
            _nextActionButton = null;
            _giveControlButton = null;
            _stopButton = null;
            _captureGameplayUiClick = null;
            _panelUiSystem.Unbind();
            _highlightPresentationSystem.Unbind();
        }

        public void ApplyReadModel(UiAssistantPanelModel model)
        {
            if (_buttonRoot == null)
                return;

            _panelUiSystem.ApplyReadModel(model);
        }

        public void ApplyHighlightReadModel(UiAssistantHighlightModel model)
        {
            if (_buttonRoot == null)
                return;

            _highlightPresentationSystem.ApplyReadModel(model);
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            Camera eventCamera = ResolveEventCamera(_buttonRoot);
            return ContainsRect(_buttonRoot, screenPosition, eventCamera) ||
                   ContainsRect(_panelRoot, screenPosition, eventCamera);
        }

        private RectTransform CreateButton(RectTransform parent)
        {
            var root = CreateRect(ButtonRootName, parent);
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = new Vector2(-360f, -26f);
            root.sizeDelta = new Vector2(228f, 78f);
            root.SetAsLastSibling();
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(root.gameObject, needsRaycaster: true);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.025f, 0.055f, 0.075f, 0.96f);
            background.raycastTarget = true;

            _button = root.gameObject.AddComponent<Button>();
            _button.targetGraphic = background;
            _button.onClick.AddListener(TogglePanel);

            CreateText("Label", root, "ARIA", 28, TextAlignmentOptions.Left, new Vector2(18f, -8f), new Vector2(94f, 34f));
            TMP_Text stateText = CreateText("State", root, "PLAYER CONTROL", 18, TextAlignmentOptions.Left, new Vector2(18f, -42f), new Vector2(144f, 24f), new Color(0.45f, 0.95f, 1f, 1f));
            CreateText("Cue", root, ">", 42, TextAlignmentOptions.Center, new Vector2(-55f, -18f), new Vector2(44f, 48f), new Color(1f, 0.78f, 0.32f, 1f));
            _panelUiSystem.Bind(stateText, null, null, null, null, null, null, null, null, null, null);

            return root;
        }

        private RectTransform CreatePanel(RectTransform parent, RectTransform headerRect)
        {
            var root = CreateRect(PanelRootName, parent);
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = parent == headerRect ? new Vector2(-360f, -118f) : new Vector2(-60f, -330f);
            root.sizeDelta = new Vector2(640f, 590f);
            root.SetAsLastSibling();
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(root.gameObject, needsRaycaster: true);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.035f, 0.045f, 0.97f);
            background.raycastTarget = true;

            CreateText("Title", root, "ARIA COMMAND ASSISTANT", 28, TextAlignmentOptions.Left, new Vector2(28f, -24f), new Vector2(460f, 38f), new Color(0.92f, 0.96f, 0.95f, 1f));
            CreateText("OwnershipTitle", root, "CONTROL STATE", 14, TextAlignmentOptions.Right, new Vector2(404f, -18f), new Vector2(192f, 20f), new Color(1f, 0.80f, 0.34f, 1f));
            TMP_Text ownershipBodyText = CreateText("OwnershipBody", root, "You are issuing orders directly.", 14, TextAlignmentOptions.Right, new Vector2(384f, -42f), new Vector2(212f, 58f), new Color(0.45f, 0.95f, 1f, 1f));
            CreateText("GoalsTitle", root, "CURRENT GOALS", 18, TextAlignmentOptions.Left, new Vector2(28f, -88f), new Vector2(240f, 28f), new Color(1f, 0.80f, 0.34f, 1f));
            TMP_Text goalsBodyText = CreateText("GoalsBody", root, "No active objectives", 20, TextAlignmentOptions.Left, new Vector2(28f, -124f), new Vector2(286f, 104f), new Color(0.80f, 0.86f, 0.84f, 1f));
            CreateText("AlertsTitle", root, "ALERTS & REPORTS", 18, TextAlignmentOptions.Left, new Vector2(350f, -88f), new Vector2(240f, 28f), new Color(1f, 0.80f, 0.34f, 1f));
            TMP_Text alertsBodyText = CreateText("AlertsBody", root, "No priority alerts", 18, TextAlignmentOptions.Left, new Vector2(350f, -124f), new Vector2(250f, 104f), new Color(0.80f, 0.86f, 0.84f, 1f));
            CreateText("RecommendationTitle", root, "RECOMMENDED NEXT ACTION", 18, TextAlignmentOptions.Left, new Vector2(28f, -258f), new Vector2(330f, 28f), new Color(0.45f, 0.95f, 1f, 1f));
            Image previewPulse = CreatePulse("PreviewPulse", root, new Vector2(20f, -284f), new Vector2(560f, 92f));
            TMP_Text recommendationBodyText = CreateText("RecommendationBody", root, "ARIA is waiting for live battlefield context.", 20, TextAlignmentOptions.Left, new Vector2(28f, -294f), new Vector2(520f, 72f), new Color(0.78f, 0.84f, 0.82f, 1f));

            _nextActionButton = CreatePanelButton("NextActionButton", root, "SHOW ME", new Vector2(28f, -398f), out TMP_Text nextActionLabelText);
            _giveControlButton = CreatePanelButton("GiveControlButton", root, "CONTROL LOCKED", new Vector2(246f, -398f), out TMP_Text giveControlLabelText);
            _closeButton = CreatePanelButton("CloseButton", root, "CLOSE", new Vector2(464f, -398f), out _);
            _stopButton = CreatePanelButton("StopButton", root, "STOP", new Vector2(246f, -466f), out TMP_Text stopLabelText);
            _nextActionButton.interactable = false;
            _giveControlButton.interactable = false;
            _stopButton.interactable = false;

            _nextActionButton.onClick.AddListener(ShowRecommendation);
            _giveControlButton.onClick.AddListener(ExecuteRecommendation);
            _stopButton.onClick.AddListener(StopAssistantControl);
            _closeButton.onClick.AddListener(ClosePanel);
            TMP_Text stateText = _buttonRoot != null ? _buttonRoot.Find("State")?.GetComponent<TMP_Text>() : null;
            _panelUiSystem.Bind(
                stateText,
                ownershipBodyText,
                goalsBodyText,
                alertsBodyText,
                recommendationBodyText,
                _nextActionButton,
                _giveControlButton,
                _stopButton,
                nextActionLabelText,
                giveControlLabelText,
                stopLabelText);
            _highlightPresentationSystem.Bind(previewPulse);
            return root;
        }

        private static Image CreatePulse(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;

            Image image = root.gameObject.AddComponent<Image>();
            image.color = new Color(0.45f, 0.95f, 1f, 0.28f);
            image.raycastTarget = false;
            image.gameObject.SetActive(false);
            return image;
        }

        private Button CreatePanelButton(string name, RectTransform parent, string label, Vector2 anchoredPosition, out TMP_Text labelText)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = new Vector2(178f, 58f);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = new Color(0.09f, 0.12f, 0.13f, 1f);
            background.raycastTarget = true;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            labelText = CreateText("Label", root, label, 18, TextAlignmentOptions.Center, Vector2.zero, root.sizeDelta, new Color(0.94f, 0.86f, 0.62f, 1f));
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string text,
            float fontSize,
            TextAlignmentOptions alignment,
            Vector2 anchoredPosition,
            Vector2 size,
            Color? color = null)
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
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.color = color ?? Color.white;
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            return root.GetComponent<RectTransform>();
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
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation);
        }

        private void StopAssistantControl()
        {
            CaptureUiOnly();
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.StopAssistantControl);
        }

        private void CaptureUiOnly()
        {
            _captureGameplayUiClick?.Invoke();
            ClearSelectedButton();
        }

        private void SetPanelOpen(bool open)
        {
            if (_panelRoot != null && _panelRoot.gameObject.activeSelf != open)
                _panelRoot.gameObject.SetActive(open);
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

        private static void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
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
