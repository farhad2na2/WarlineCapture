using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BuildPlacementConfirmationBarView : MonoBehaviour
    {
        private const float RefreshIntervalSeconds = 0.1f;

        [SerializeField] private RectTransform root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Sprite panelFrameSprite;
        [SerializeField] private Sprite statusChipSprite;
        [SerializeField] private Sprite secondaryButtonSprite;
        [SerializeField] private Sprite goldActionButtonSprite;
        [SerializeField] private Sprite squareButtonSprite;
        [SerializeField] private Sprite instructionStripSprite;
        [SerializeField] private Sprite creditsIconSprite;
        [SerializeField] private Sprite timeIconSprite;
        [SerializeField] private Sprite cancelIconSprite;
        [SerializeField] private Sprite rotateIconSprite;
        [SerializeField] private Sprite confirmIconSprite;
        [SerializeField] private Sprite infoIconSprite;

        private CanvasGroup _canvasGroup;
        private IBuildingUiCommand _commandSystem;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private UnityAction _cancelListener;
        private UnityAction _rotateListener;
        private UnityAction _confirmListener;
        private float _nextRefreshAt;
        private Canvas _cachedCanvas;

        public RectTransform Root => root != null ? root : transform as RectTransform;

        private void Awake()
        {
            CacheSerializedLayout();
        }

        private void OnTransformParentChanged()
        {
            _cachedCanvas = null;
        }

        public static BuildPlacementConfirmationBarView Ensure(GameObject prefab, RectTransform parent)
        {
            if (parent == null)
                return null;

            BuildPlacementConfirmationBarView existing = parent.GetComponentInChildren<BuildPlacementConfirmationBarView>(true);
            if (existing != null)
                return existing;

            if (prefab != null)
            {
                GameObject instance = Instantiate(prefab, parent, false);
                instance.name = prefab.name;
                ApplyDefaultPlacementAnchors(instance.GetComponent<RectTransform>());

                BuildPlacementConfirmationBarView view = instance.GetComponent<BuildPlacementConfirmationBarView>();
                if (view == null || !view.HasRequiredLayoutReferences())
                {
                    Destroy(instance);
                    return null;
                }

                view.CacheSerializedLayout();
                view.Hide();
                return view;
            }

            return null;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            RectTransform rect = Root;
            if (rect == null || !rect.gameObject.activeInHierarchy)
                return false;
            if (_canvasGroup != null && (!_canvasGroup.blocksRaycasts || _canvasGroup.alpha <= 0.01f))
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, ResolveEventCamera());
        }

        public void BindRuntimeCommands(IBuildingUiCommand commandSystem, BattleHudRuntimeFeedbackView runtimeFeedbackView = null)
        {
            _commandSystem = commandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            WireButtons();
            Refresh(force: true);
        }

        private void OnEnable()
        {
            CacheSerializedLayout();
            WireButtons();
            Refresh(force: true);
        }

        private void OnDisable()
        {
            UnwireButtons();
            _nextRefreshAt = 0f;
        }

        private void Update()
        {
            Refresh(force: false);
        }

        private void WireButtons()
        {
            if (_cancelListener == null)
                _cancelListener = OnCancelClicked;
            if (_rotateListener == null)
                _rotateListener = OnRotateClicked;
            if (_confirmListener == null)
                _confirmListener = OnConfirmClicked;

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(_cancelListener);
                cancelButton.onClick.AddListener(_cancelListener);
            }

            if (rotateButton != null)
            {
                rotateButton.onClick.RemoveListener(_rotateListener);
                rotateButton.onClick.AddListener(_rotateListener);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(_confirmListener);
                confirmButton.onClick.AddListener(_confirmListener);
            }
        }

        private void UnwireButtons()
        {
            if (_cancelListener != null)
                cancelButton?.onClick.RemoveListener(_cancelListener);
            if (_rotateListener != null)
                rotateButton?.onClick.RemoveListener(_rotateListener);
            if (_confirmListener != null)
                confirmButton?.onClick.RemoveListener(_confirmListener);
        }

        private void Refresh(bool force)
        {
            if (!force && Time.unscaledTime < _nextRefreshAt)
                return;

            _nextRefreshAt = Time.unscaledTime + RefreshIntervalSeconds;
            bool hasPlacement = _commandSystem != null && _commandSystem.HasPendingBuildingPlacement;
            if (!hasPlacement)
            {
                Hide();
                return;
            }

            Show();

            string rawStatus = _commandSystem.PlacementStatusText;
            SplitPlacementStatus(rawStatus, out string title, out string status);
            SetText(titleText, string.IsNullOrWhiteSpace(title) ? "PLACE BUILDING" : $"PLACE {title.ToUpperInvariant()}");
            SetText(costText, FormatCost(_commandSystem.ActivePlacementCost));
            SetText(durationText, FormatDuration(_commandSystem.ActivePlacementDurationSeconds));
            SetText(instructionText, "DRAG TO POSITION, CONFIRM TO BUILD");

            bool canConfirm = _commandSystem.CanConfirmBuildingPlacement;
            SetText(statusText, string.IsNullOrWhiteSpace(status) ? "DRAG TO POSITION" : status.ToUpperInvariant());
            if (statusText != null)
                statusText.color = canConfirm
                    ? new Color(0.62f, 0.98f, 0.35f)
                    : new Color(1f, 0.35f, 0.22f);

            if (confirmButton != null)
                confirmButton.interactable = canConfirm;
            if (cancelButton != null)
                cancelButton.interactable = true;
            if (rotateButton != null)
                rotateButton.interactable = true;
        }

        private void OnCancelClicked()
        {
            _commandSystem?.CancelBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success("PLACEMENT CANCELLED"));
            Refresh(force: true);
        }

        private void OnRotateClicked()
        {
            bool rotated = _commandSystem != null && _commandSystem.RotateBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, rotated
                ? TacticalCommandResult.Success("ROTATED 90 DEGREES")
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, "No active building placement."));
            Refresh(force: true);
        }

        private void OnConfirmClicked()
        {
            bool placed = _commandSystem != null && _commandSystem.ConfirmBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, placed
                ? TacticalCommandResult.Success("BUILDING PLACED")
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, "Place on valid ground."));
            Refresh(force: true);
        }

        private void Show()
        {
            if (Root != null && !Root.gameObject.activeSelf)
                Root.gameObject.SetActive(true);

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        private void Hide()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void CacheSerializedLayout()
        {
            if (root == null)
                root = transform as RectTransform;
            if (root == null)
                return;

            _canvasGroup ??= GetComponent<CanvasGroup>();
            Image background = GetComponent<Image>();
            if (background != null && panelFrameSprite != null)
            {
                ApplySprite(background, panelFrameSprite, Image.Type.Sliced);
                background.color = Color.white;
                background.raycastTarget = true;
            }
        }

        private bool HasRequiredLayoutReferences()
        {
            return titleText != null &&
                   statusText != null &&
                   costText != null &&
                   durationText != null &&
                   instructionText != null &&
                   cancelButton != null &&
                   rotateButton != null &&
                   confirmButton != null;
        }

        private static void ApplyDefaultPlacementAnchors(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = new Vector2(0.255f, 0.205f);
            rect.anchorMax = new Vector2(0.725f, 0.385f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private static void ApplySprite(Image image, Sprite sprite, Image.Type type)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = type;
        }

        private static void SplitPlacementStatus(string rawStatus, out string title, out string status)
        {
            title = "BUILDING";
            status = rawStatus;
            if (string.IsNullOrWhiteSpace(rawStatus))
                return;

            int separator = rawStatus.IndexOf(':');
            if (separator < 0)
                return;

            title = rawStatus.Substring(0, separator).Trim();
            status = rawStatus.Substring(separator + 1).Trim();
        }

        private static string FormatCost(int cost)
        {
            return cost > 0
                ? cost.ToString("N0", CultureInfo.InvariantCulture)
                : "0";
        }

        private static string FormatDuration(float seconds)
        {
            if (seconds <= 0f)
                return "00:00";

            int totalSeconds = Mathf.CeilToInt(seconds);
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
                text.text = value ?? string.Empty;
        }

        private Camera ResolveEventCamera()
        {
            Canvas canvas = ResolveCanvas();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return null;

            return canvas.worldCamera;
        }

        private Canvas ResolveCanvas()
        {
            if (_cachedCanvas == null)
                _cachedCanvas = GetComponentInParent<Canvas>();
            return _cachedCanvas;
        }
    }
}
