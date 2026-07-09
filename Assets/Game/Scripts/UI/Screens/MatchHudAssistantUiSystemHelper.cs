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
        private static readonly Vector2 PanelSize = new(1040f, 760f);
        private static readonly Vector2 PanelOverlayPosition = new(-300f, -136f);
        private static readonly Vector2 PanelHeaderFallbackPosition = new(-360f, -118f);
        private static readonly Vector2 PanelButtonSize = new(220f, 72f);
        private static readonly Color PanelFillColor = new(0.018f, 0.027f, 0.032f, 0.985f);
        private static readonly Color SectionFillColor = new(0.035f, 0.047f, 0.052f, 0.92f);
        private static readonly Color RowFillColor = new(0.050f, 0.061f, 0.063f, 0.86f);
        private static readonly Color GoldColor = new(1f, 0.72f, 0.24f, 1f);
        private static readonly Color DimGoldColor = new(0.55f, 0.42f, 0.20f, 1f);
        private static readonly Color CyanColor = new(0.24f, 0.92f, 1f, 1f);
        private static readonly Color TextPrimaryColor = new(0.92f, 0.96f, 0.92f, 1f);
        private static readonly Color TextMutedColor = new(0.72f, 0.78f, 0.74f, 1f);
        private static readonly Color WarningColor = new(1f, 0.32f, 0.24f, 1f);

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
            _panelUiSystem.Bind(stateText, null, null, null, null, null, null, null, null, null, null, null);

            return root;
        }

        private RectTransform CreatePanel(RectTransform parent, RectTransform headerRect)
        {
            var root = CreateRect(PanelRootName, parent);
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = parent == headerRect ? PanelHeaderFallbackPosition : PanelOverlayPosition;
            root.sizeDelta = PanelSize;
            root.SetAsLastSibling();
            MatchHudCanvasBatchingUtility.EnsureLocalCanvas(root.gameObject, needsRaycaster: true);

            Image background = root.gameObject.AddComponent<Image>();
            background.color = PanelFillColor;
            background.raycastTarget = true;
            CreateBorder(root, "OuterBorder", GoldColor, 4f);

            CreateReticleBadge("HeaderReticle", root, new Vector2(40f, -28f), new Vector2(64f, 64f));
            CreateText("Title", root, "ARIA COMMAND ASSISTANT", 42, TextAlignmentOptions.Left, new Vector2(116f, -34f), new Vector2(590f, 58f), TextPrimaryColor);
            RectTransform controlChip = CreateFramedBlock("ControlChip", root, new Vector2(720f, -32f), new Vector2(220f, 48f), new Color(0.025f, 0.040f, 0.046f, 0.96f), CyanColor, 2f);
            CreateText("ControlChipLabel", controlChip, "PLAYER CONTROL", 24, TextAlignmentOptions.Center, Vector2.zero, controlChip.sizeDelta, CyanColor);
            TMP_Text ownershipBodyText = CreateText("OwnershipBody", root, "You are issuing orders directly.", 18, TextAlignmentOptions.Right, new Vector2(632f, -88f), new Vector2(308f, 28f), CyanColor);

            RectTransform goalsBlock = CreateFramedBlock("GoalsBlock", root, new Vector2(32f, -122f), new Vector2(464f, 238f), SectionFillColor, DimGoldColor, 3f);
            CreateText("GoalsTitle", root, "CURRENT GOALS", 28, TextAlignmentOptions.Left, new Vector2(56f, -144f), new Vector2(400f, 36f), GoldColor);
            CreateRowGuides(root, new Vector2(52f, -194f), 420f, 3);
            TMP_Text goalsBodyText = CreateText("GoalsBody", root, "No active objectives", 30, TextAlignmentOptions.Left, new Vector2(74f, -198f), new Vector2(386f, 132f), TextPrimaryColor);

            RectTransform alertsBlock = CreateFramedBlock("AlertsBlock", root, new Vector2(528f, -122f), new Vector2(480f, 238f), SectionFillColor, DimGoldColor, 3f);
            CreateText("AlertsTitle", root, "ALERTS & REPORTS", 28, TextAlignmentOptions.Left, new Vector2(552f, -144f), new Vector2(396f, 36f), GoldColor);
            CreateRowGuides(root, new Vector2(548f, -194f), 436f, 3);
            TMP_Text alertsBodyText = CreateText("AlertsBody", root, "No priority alerts", 28, TextAlignmentOptions.Left, new Vector2(570f, -198f), new Vector2(386f, 132f), TextPrimaryColor);

            RectTransform narrationBlock = CreateFramedBlock("NarrationBlock", root, new Vector2(32f, -386f), new Vector2(280f, 142f), SectionFillColor, CyanColor, 2f);
            CreateText("NarrationTitle", root, "ARIA VOICE", 24, TextAlignmentOptions.Left, new Vector2(54f, -408f), new Vector2(180f, 32f), CyanColor);
            CreateWaveform(root, new Vector2(54f, -456f));
            TMP_Text narrationSubtitleText = CreateText("NarrationSubtitle", root, "No active narration", 24, TextAlignmentOptions.Left, new Vector2(54f, -500f), new Vector2(230f, 36f), TextPrimaryColor);

            RectTransform recommendationBlock = CreateFramedBlock("RecommendationBlock", root, new Vector2(336f, -386f), new Vector2(672f, 204f), SectionFillColor, DimGoldColor, 3f);
            CreateText("RecommendationTitle", root, "RECOMMENDED NEXT ACTION", 28, TextAlignmentOptions.Left, new Vector2(366f, -410f), new Vector2(430f, 38f), GoldColor);
            Image previewPulse = CreatePulse("PreviewPulse", root, new Vector2(352f, -450f), new Vector2(626f, 116f));
            TMP_Text recommendationBodyText = CreateText("RecommendationBody", root, "ARIA is waiting for live battlefield context.", 30, TextAlignmentOptions.Left, new Vector2(372f, -460f), new Vector2(370f, 104f), TextPrimaryColor);
            CreateTargetLockGraphic(root, new Vector2(772f, -424f), new Vector2(178f, 148f));

            CreateLine(root, "CommandRailTop", new Vector2(36f, -606f), new Vector2(968f, 2f), DimGoldColor);

            _nextActionButton = CreatePanelButton("NextActionButton", root, "SHOW ME", new Vector2(40f, -622f), out TMP_Text nextActionLabelText);
            _giveControlButton = CreatePanelButton("GiveControlButton", root, "CONTROL LOCKED", new Vector2(280f, -622f), out TMP_Text giveControlLabelText);
            _stopButton = CreatePanelButton("StopButton", root, "STOP", new Vector2(520f, -622f), out TMP_Text stopLabelText);
            _closeButton = CreatePanelButton("CloseButton", root, "CLOSE", new Vector2(760f, -622f), out _);
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
                narrationSubtitleText,
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
            root.sizeDelta = PanelButtonSize;

            Image background = root.gameObject.AddComponent<Image>();
            bool primary = name == "NextActionButton" || name == "GiveControlButton";
            bool warning = name == "StopButton";
            background.color = primary
                ? new Color(0.70f, 0.48f, 0.16f, 1f)
                : warning
                    ? new Color(0.10f, 0.075f, 0.070f, 1f)
                    : new Color(0.09f, 0.12f, 0.13f, 1f);
            background.raycastTarget = true;
            CreateBorder(root, "Border", warning ? WarningColor : GoldColor, primary ? 3f : 2f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            labelText = CreateText("Label", root, label, 26, TextAlignmentOptions.Center, Vector2.zero, root.sizeDelta, primary ? new Color(0.07f, 0.055f, 0.035f, 1f) : new Color(0.94f, 0.86f, 0.62f, 1f));
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 20f;
            labelText.fontSizeMax = 26f;
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

        private static RectTransform CreateFramedBlock(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color borderColor, float borderThickness)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;

            Image image = root.gameObject.AddComponent<Image>();
            image.color = fillColor;
            image.raycastTarget = false;
            CreateBorder(root, "Border", borderColor, borderThickness);
            return root;
        }

        private static void CreateBorder(RectTransform parent, string name, Color color, float thickness)
        {
            CreateLine(parent, name + "Top", Vector2.zero, new Vector2(parent.sizeDelta.x, thickness), color);
            CreateLine(parent, name + "Bottom", new Vector2(0f, -parent.sizeDelta.y + thickness), new Vector2(parent.sizeDelta.x, thickness), color);
            CreateLine(parent, name + "Left", Vector2.zero, new Vector2(thickness, parent.sizeDelta.y), color);
            CreateLine(parent, name + "Right", new Vector2(parent.sizeDelta.x - thickness, 0f), new Vector2(thickness, parent.sizeDelta.y), color);
        }

        private static Image CreateLine(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            RectTransform root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;

            Image image = root.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateRowGuides(RectTransform parent, Vector2 firstPosition, float width, int count)
        {
            for (int i = 0; i < count; i++)
            {
                RectTransform row = CreateFramedBlock("TargetLockRowGuide" + i, parent, new Vector2(firstPosition.x, firstPosition.y - i * 50f), new Vector2(width, 40f), RowFillColor, new Color(0.25f, 0.21f, 0.12f, 1f), 1f);
                CreateLine(row, "IconChip", new Vector2(12f, -8f), new Vector2(24f, 24f), new Color(0.16f, 0.21f, 0.16f, 0.9f));
                CreateLine(row, "StatusChip", new Vector2(width - 44f, -8f), new Vector2(24f, 24f), new Color(0.16f, 0.21f, 0.16f, 0.9f));
            }
        }

        private static void CreateWaveform(RectTransform parent, Vector2 anchoredPosition)
        {
            float[] heights = { 12f, 24f, 18f, 34f, 26f, 16f, 30f, 20f, 14f, 28f, 18f, 10f };
            for (int i = 0; i < heights.Length; i++)
            {
                float height = heights[i];
                CreateLine(parent, "VoiceWave" + i, new Vector2(anchoredPosition.x + i * 12f, anchoredPosition.y - (34f - height) * 0.5f), new Vector2(4f, height), CyanColor);
            }
        }

        private static void CreateReticleBadge(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform root = CreateFramedBlock(name, parent, anchoredPosition, size, new Color(0.030f, 0.050f, 0.056f, 0.85f), CyanColor, 2f);
            CreateLine(root, "ReticleHorizontal", new Vector2(10f, -size.y * 0.5f), new Vector2(size.x - 20f, 2f), CyanColor);
            CreateLine(root, "ReticleVertical", new Vector2(size.x * 0.5f, -10f), new Vector2(2f, size.y - 20f), CyanColor);
            CreateLine(root, "ReticleCore", new Vector2(size.x * 0.5f - 5f, -size.y * 0.5f + 5f), new Vector2(10f, 10f), CyanColor);
        }

        private static void CreateTargetLockGraphic(RectTransform parent, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform root = CreateFramedBlock("TargetLockGraphic", parent, anchoredPosition, size, new Color(0.020f, 0.040f, 0.046f, 0.65f), CyanColor, 1f);
            CreateLine(root, "TargetSweep", new Vector2(18f, -22f), new Vector2(size.x - 36f, 3f), new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.55f));
            CreateLine(root, "TargetHorizontal", new Vector2(18f, -size.y * 0.5f), new Vector2(size.x - 36f, 2f), new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.9f));
            CreateLine(root, "TargetVertical", new Vector2(size.x * 0.5f, -16f), new Vector2(2f, size.y - 32f), new Color(CyanColor.r, CyanColor.g, CyanColor.b, 0.9f));
            CreateLine(root, "TargetOuterTop", new Vector2(40f, -42f), new Vector2(size.x - 80f, 2f), CyanColor);
            CreateLine(root, "TargetOuterBottom", new Vector2(40f, -size.y + 42f), new Vector2(size.x - 80f, 2f), CyanColor);
            CreateLine(root, "TargetCoreHorizontal", new Vector2(size.x * 0.5f - 30f, -size.y * 0.5f), new Vector2(60f, 3f), WarningColor);
            CreateLine(root, "TargetCoreVertical", new Vector2(size.x * 0.5f, -size.y * 0.5f + 30f), new Vector2(3f, 60f), WarningColor);
            CreateLine(root, "TargetDot", new Vector2(size.x * 0.5f - 7f, -size.y * 0.5f + 7f), new Vector2(14f, 14f), WarningColor);
            CreateLine(root, "TelemetryMarkerA", new Vector2(30f, -size.y + 28f), new Vector2(12f, 12f), GoldColor);
            CreateLine(root, "TelemetryMarkerB", new Vector2(size.x - 42f, -size.y + 52f), new Vector2(12f, 12f), CyanColor);
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
            UiShellRuntimeGateway.TryEnqueueAssistantCommandIntent(UiAssistantCommandIntentKind.ExecuteRecommendation, fromTakeover: true);
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
            if (open && _panelRoot != null)
                _panelRoot.SetAsLastSibling();

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
