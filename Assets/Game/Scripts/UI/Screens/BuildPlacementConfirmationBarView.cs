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
        [SerializeField] private TMP_Text oilCostText;
        [SerializeField] private TMP_Text fuelCostText;
        [SerializeField] private TMP_Text footprintText;
        [SerializeField] private Image buildingPortrait;
        [SerializeField] private GameObject validityPanelPrefab;
        [SerializeField] private GameObject validStatusIndicator;
        [SerializeField] private GameObject invalidStatusIndicator;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rotateButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Sprite panelFrameSprite;
        [SerializeField] private Sprite statusChipSprite;
        [SerializeField] private Sprite secondaryButtonSprite;
        [SerializeField] private Sprite goldActionButtonSprite;
        [SerializeField] private Sprite squareButtonSprite;
        [SerializeField] private Sprite instructionStripSprite;
        [UnityEngine.Serialization.FormerlySerializedAs("creditsIconSprite"), SerializeField]
        private Sprite materialsIconSprite;
        [SerializeField] private Sprite timeIconSprite;
        [SerializeField] private Sprite cancelIconSprite;
        [SerializeField] private Sprite rotateIconSprite;
        [SerializeField] private Sprite confirmIconSprite;
        [SerializeField] private Sprite infoIconSprite;

        private CanvasGroup _canvasGroup;
        private IBuildingUiCommand _commandSystem;
        private IGameTextResolver _gameTextResolver = FallbackGameTextResolver.Instance;
        private BattleHudRuntimeFeedbackView _runtimeFeedbackView;
        private UnityAction _cancelListener;
        private UnityAction _rotateListener;
        private UnityAction _confirmListener;
        private float _nextRefreshAt;
        private Canvas _cachedCanvas;
        private BuildPlacementValidityPanelView _validityPanel;
        private GameObject _suppressedThreatPanel;
        private bool _restoreThreatPanelWhenHidden;

        public RectTransform Root => root != null ? root : transform as RectTransform;
        public TMP_Text TitleText => titleText;
        public TMP_Text StatusText => statusText;
        public TMP_Text MaterialsCostText => costText;
        public TMP_Text OilCostText => oilCostText;
        public TMP_Text FuelCostText => fuelCostText;
        public TMP_Text FootprintText => footprintText;
        public Image BuildingPortrait => buildingPortrait;
        public GameObject ValidityPanelPrefab => validityPanelPrefab;
        public BuildPlacementValidityPanelView ValidityPanel => _validityPanel;
        public Button CancelButton => cancelButton;
        public Button RotateButton => rotateButton;
        public Button ConfirmButton => confirmButton;
        public Sprite MaterialsIconSprite => materialsIconSprite;
        public bool HasPendingPlacement =>
            _commandSystem != null && _commandSystem.HasPendingBuildingPlacement;

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
                view.EnsureValidityPanel(parent);
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

        public void BindRuntimeCommands(
            IBuildingUiCommand commandSystem,
            BattleHudRuntimeFeedbackView runtimeFeedbackView = null,
            IGameTextResolver gameTextResolver = null)
        {
            _commandSystem = commandSystem;
            _runtimeFeedbackView = runtimeFeedbackView;
            _gameTextResolver = gameTextResolver ?? FallbackGameTextResolver.Instance;
            EnsureValidityPanel(transform.parent as RectTransform);
            WireButtons();
            Refresh(force: true);
        }

        internal bool TryInvokeConfirmFromGuidance()
        {
            Refresh(force: true);
            if (!HasPendingPlacement ||
                !_commandSystem.CanConfirmBuildingPlacement ||
                confirmButton == null ||
                !confirmButton.IsActive() ||
                !confirmButton.IsInteractable())
            {
                return false;
            }

            confirmButton.onClick.Invoke();
            return !HasPendingPlacement;
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
            _validityPanel?.Hide();
            RestoreThreatPanel();
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
                _validityPanel?.ApplyValidityState(false, false);
                Hide();
                return;
            }

            Show();

            string rawStatus = _commandSystem.PlacementStatusText;
            SplitPlacementStatus(rawStatus, out string title, out string status);
            string buildingTitle = string.IsNullOrWhiteSpace(title)
                ? _gameTextResolver.Get("build.placement.title.default", "BUILDING")
                : title.ToUpperInvariant();
            SetText(titleText, $"BUILD {buildingTitle}");
            SetText(costText, FormatResource(_commandSystem.ActivePlacementCost));
            SetText(oilCostText, FormatResource(_commandSystem.ActivePlacementCreditsCost));
            if (fuelCostText != null && string.IsNullOrWhiteSpace(fuelCostText.text))
                SetText(fuelCostText, "0");
            if (footprintText != null && string.IsNullOrWhiteSpace(footprintText.text))
                SetText(footprintText, "3x3");
            SetText(durationText, FormatDuration(_commandSystem.ActivePlacementDurationSeconds));
            SetText(instructionText, _gameTextResolver.Get("build.placement.instruction.confirm", "DRAG TO POSITION, CONFIRM TO BUILD"));

            bool canConfirm = _commandSystem.CanConfirmBuildingPlacement;
            EnsureValidityPanel(transform.parent as RectTransform);
            _validityPanel?.ApplyValidityState(true, canConfirm);
            SetText(statusText, string.IsNullOrWhiteSpace(status)
                ? _gameTextResolver.Get("build.placement.status.drag_to_position", "DRAG TO POSITION")
                : status.ToUpperInvariant());
            if (statusText != null)
                SetTextColor(statusText, canConfirm
                    ? new Color(0.62f, 0.98f, 0.35f)
                    : new Color(1f, 0.35f, 0.22f));
            if (validStatusIndicator != null)
                validStatusIndicator.SetActive(canConfirm);
            if (invalidStatusIndicator != null)
                invalidStatusIndicator.SetActive(!canConfirm);

            if (confirmButton != null && confirmButton.interactable != canConfirm)
                confirmButton.interactable = canConfirm;
            if (confirmButton != null && confirmButton.targetGraphic is V3GradientGraphic confirmGradient)
            {
                confirmGradient.Configure(
                    canConfirm ? new Color32(16, 142, 41, 255) : new Color32(94, 101, 103, 255),
                    canConfirm ? new Color32(3, 67, 24, 255) : new Color32(43, 48, 50, 255),
                    canConfirm ? new Color32(48, 228, 73, 255) : new Color32(112, 123, 126, 255),
                    3f);
            }
            if (cancelButton != null && !cancelButton.interactable)
                cancelButton.interactable = true;
            if (rotateButton != null && !rotateButton.interactable)
                rotateButton.interactable = true;
        }

        private void OnCancelClicked()
        {
            _commandSystem?.CancelBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ClearStickyCommandMode(_runtimeFeedbackView, TacticalCommandMode.Build);
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.placement_cancelled", "PLACEMENT CANCELLED")), _gameTextResolver);
            Refresh(force: true);
        }

        private void OnRotateClicked()
        {
            bool rotated = _commandSystem != null && _commandSystem.RotateBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, rotated
                ? TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.rotated_90", "ROTATED 90 DEGREES"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, _gameTextResolver.Get("build.feedback.no_active_placement", "No active building placement.")), _gameTextResolver);
            Refresh(force: true);
        }

        private void OnConfirmClicked()
        {
            bool placed = _commandSystem != null && _commandSystem.ConfirmBuildingPlacement();
            BattleHudRuntimeFeedbackUiSystemHelper.ApplyCommandResult(_runtimeFeedbackView, placed
                ? TacticalCommandResult.Success(_gameTextResolver.Get("build.feedback.building_placed", "BUILDING PLACED"))
                : TacticalCommandResult.Rejected(TacticalCommandReasonCode.BuildUnavailable, _gameTextResolver.Get("build.feedback.place_on_valid_ground", "Place on valid ground.")), _gameTextResolver);
            Refresh(force: true);
        }

        private void Show()
        {
            SuppressThreatPanel();
            if (Root != null && !Root.gameObject.activeSelf)
                Root.gameObject.SetActive(true);

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                return;

            SetCanvasGroup(_canvasGroup, 1f, true, true);
        }

        private void Hide()
        {
            _validityPanel?.Hide();
            RestoreThreatPanel();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                return;

            SetCanvasGroup(_canvasGroup, 0f, false, false);
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
                if (background.color != Color.white)
                    background.color = Color.white;
                if (!background.raycastTarget)
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

        private void EnsureValidityPanel(RectTransform parent)
        {
            if (_validityPanel != null || validityPanelPrefab == null || parent == null)
                return;
            _validityPanel = BuildPlacementValidityPanelView.Ensure(validityPanelPrefab, parent);
        }

        private void SuppressThreatPanel()
        {
            if (_suppressedThreatPanel == null)
            {
                RectTransform[] candidates = transform.root.GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < candidates.Length; i++)
                {
                    RectTransform candidate = candidates[i];
                    if (candidate != null && candidate.name == "ThreatJumpPanel")
                    {
                        _suppressedThreatPanel = candidate.gameObject;
                        break;
                    }
                }
            }

            if (_suppressedThreatPanel == null)
                return;
            if (!_restoreThreatPanelWhenHidden)
                _restoreThreatPanelWhenHidden = _suppressedThreatPanel.activeSelf;
            if (_suppressedThreatPanel.activeSelf)
                _suppressedThreatPanel.SetActive(false);
        }

        private void RestoreThreatPanel()
        {
            if (_suppressedThreatPanel != null && _restoreThreatPanelWhenHidden &&
                !_suppressedThreatPanel.activeSelf)
            {
                _suppressedThreatPanel.SetActive(true);
            }
            _suppressedThreatPanel = null;
            _restoreThreatPanelWhenHidden = false;
        }

        private static void ApplyDefaultPlacementAnchors(RectTransform rect)
        {
            if (rect == null)
                return;

            // The prefab owns a full 1672x941 responsive section. Its visible
            // panel is the only hit target and begins after the squad tray.
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
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

            if (image.sprite != sprite)
                image.sprite = sprite;
            if (image.type != type)
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

        private static string FormatCost(int creditsCost, int materialsCost) =>
            $"{Mathf.Max(0, creditsCost).ToString("N0", CultureInfo.InvariantCulture)} CR / " +
            $"{Mathf.Max(0, materialsCost).ToString("N0", CultureInfo.InvariantCulture)} MAT";

        internal static string FormatCostForTests(int creditsCost, int materialsCost) =>
            FormatCost(creditsCost, materialsCost);

        private static string FormatResource(int value) =>
            Mathf.Max(0, value).ToString("N0", CultureInfo.InvariantCulture);

        private static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);
            return seconds <= 0f ? "00:00" : $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text == null)
                return;

            value ??= string.Empty;
            if (text.text != value)
                text.text = value;
        }

        private static void SetTextColor(TMP_Text text, Color color)
        {
            if (text.color != color)
                text.color = color;
        }

        private static void SetCanvasGroup(CanvasGroup canvasGroup, float alpha, bool interactable, bool blocksRaycasts)
        {
            if (!Mathf.Approximately(canvasGroup.alpha, alpha))
                canvasGroup.alpha = alpha;
            if (canvasGroup.interactable != interactable)
                canvasGroup.interactable = interactable;
            if (canvasGroup.blocksRaycasts != blocksRaycasts)
                canvasGroup.blocksRaycasts = blocksRaycasts;
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
