using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Tactical.Contracts;
using Game.UI.Contracts;

namespace Game.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BattleHudRuntimeFeedbackView : MonoBehaviour, IBattleHudRuntimeFeedbackView
    {
        private static readonly MatchOverlayCommandTabFeedbackUiSystemHelper CommandTabFeedbackHelper = new();

        [SerializeField] private BattleHudTacticalFeedbackView tacticalFeedback;
        [SerializeField] private MatchOverlayCommandTabGroupView[] commandTabGroups;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Image feedbackIcon;
        [SerializeField] private MatchHudCurrentOrderBannerView currentOrderBanner;
        [SerializeField] private MatchOverlayCommandControlsView commandIconSource;
        [SerializeField] private GameObject feedbackActionsRoot;
        [SerializeField] private Button boardAllButton;
        [SerializeField] private TMP_Text boardAllButtonLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelButtonLabel;
        [SerializeField] private Sprite neutralIcon;
        [SerializeField] private Sprite readyIcon;
        [SerializeField] private Sprite warningIcon;
        [SerializeField] private Sprite errorIcon;
        private TacticalCommandMode _currentCommandMode = TacticalCommandMode.None;
        private TacticalCommandMode _stickyCommandMode = TacticalCommandMode.None;
        private TacticalCommandResult _lastCommandResult = TacticalCommandResult.Success();
        private bool _hasLastCommandResult;
        private bool _hasPersistentCommandFeedback;
        private MatchHudCommandFeedbackModel _persistentCommandFeedback = MatchHudCommandFeedbackModel.Hidden;
        private MatchHudCommandFeedbackActionsModel _persistentCommandFeedbackActions = MatchHudCommandFeedbackActionsModel.Hidden;
        private bool _hasPersistentCurrentOrderBanner;
        private MatchHudCurrentOrderBannerModel _persistentCurrentOrderBanner = MatchHudCurrentOrderBannerModel.Hidden;
        private bool _transientCurrentOrderBannerActive;
        private float _transientCurrentOrderBannerExpiresAt;
        private bool _transientFeedbackActive;
        private float _transientFeedbackExpiresAt;
        private System.Action _boardAllRequested;
        private System.Action _cancelRequested;
        private Button _boundBoardAllButton;
        private Button _boundCancelButton;

        public BattleHudTacticalFeedbackView TacticalFeedback => tacticalFeedback;
        public MatchOverlayCommandTabGroupView[] CommandTabGroups => commandTabGroups;
        public GameObject FeedbackPanel => feedbackPanel;
        public TMP_Text FeedbackText => feedbackText;
        public Image FeedbackIcon => feedbackIcon;
        public MatchHudCurrentOrderBannerView CurrentOrderBanner => currentOrderBanner;
        public MatchOverlayCommandControlsView CommandIconSource => commandIconSource;
        public GameObject FeedbackActionsRoot => feedbackActionsRoot;
        public Button BoardAllButton => boardAllButton;
        public Button CancelButton => cancelButton;
        public TacticalCommandMode CurrentCommandMode
        {
            get => _currentCommandMode;
            set => _currentCommandMode = value;
        }

        public TacticalCommandMode StickyCommandMode
        {
            get => _stickyCommandMode;
            set => _stickyCommandMode = value;
        }

        public TacticalCommandResult LastCommandResult
        {
            get => _lastCommandResult;
            set => _lastCommandResult = value;
        }

        public bool HasLastCommandResult
        {
            get => _hasLastCommandResult;
            set => _hasLastCommandResult = value;
        }

        public BattleHudRuntimeFeedbackState RuntimeFeedbackState =>
            new(_currentCommandMode, _stickyCommandMode, _lastCommandResult, _hasLastCommandResult);

        public Sprite ResolveCommandIconSprite(TacticalCommandMode mode)
        {
            return commandIconSource != null ? commandIconSource.ResolveCommandIconSprite(mode) : null;
        }

        public void BindCurrentOrderBanner(MatchHudCurrentOrderBannerView bannerView)
        {
            currentOrderBanner = bannerView;
            if (currentOrderBanner != null)
                currentOrderBanner.Apply(_persistentCurrentOrderBanner);
        }

        public void ApplyCurrentOrderBanner(MatchHudCurrentOrderBannerModel model)
        {
            _transientCurrentOrderBannerActive = false;
            _hasPersistentCurrentOrderBanner = model.Visible;
            _persistentCurrentOrderBanner = model.Visible ? model : MatchHudCurrentOrderBannerModel.Hidden;
            currentOrderBanner?.Apply(model);
        }

        public void ApplyTransientCurrentOrderBanner(MatchHudCurrentOrderBannerModel model, float now, float durationSeconds)
        {
            if (!model.Visible)
                return;

            _transientCurrentOrderBannerActive = true;
            _transientCurrentOrderBannerExpiresAt = now + Mathf.Max(0f, durationSeconds);
            currentOrderBanner?.Apply(model);
        }

        public void HideCurrentOrderBanner()
        {
            _hasPersistentCurrentOrderBanner = false;
            _persistentCurrentOrderBanner = MatchHudCurrentOrderBannerModel.Hidden;
            _transientCurrentOrderBannerActive = false;
            currentOrderBanner?.Hide();
        }

        private void Awake()
        {
            BindUnityEvents();
            HideFeedbackMessage();
        }

        private void OnEnable()
        {
            BindUnityEvents();
            ResetRuntimeFeedbackState();
            BattleHudRuntimeFeedbackUiSystemHelper.ClearCommandMode(this);
        }

        private void OnDestroy()
        {
            ClearFeedbackActionCallbacks();
            RemoveUnityEvents();
        }

        public void BindFeedbackActionCallbacks(System.Action boardAllRequested, System.Action cancelRequested)
        {
            BindUnityEvents();
            _boardAllRequested = boardAllRequested;
            _cancelRequested = cancelRequested;
        }

        public void ClearFeedbackActionCallbacks()
        {
            _boardAllRequested = null;
            _cancelRequested = null;
        }

        public void ShowFeedbackMessage(string message)
        {
            ShowFeedbackMessage(message, CommandFeedbackSeverity.Neutral);
        }

        public void ShowFeedbackMessage(string message, CommandFeedbackSeverity severity)
        {
            ApplyCommandFeedback(MatchHudCommandFeedbackModel.Show(message, severity));
        }

        public void ApplyCommandFeedback(MatchHudCommandFeedbackModel model)
        {
            ApplyPersistentCommandFeedback(model, MatchHudCommandFeedbackActionsModel.Hidden);
        }

        public void ApplyPersistentCommandFeedback(
            MatchHudCommandFeedbackModel model,
            MatchHudCommandFeedbackActionsModel actionsModel)
        {
            _transientFeedbackActive = false;

            if (!model.Visible || string.IsNullOrWhiteSpace(model.Message))
            {
                ClearPersistentCommandFeedback();
                HideFeedbackMessage();
                return;
            }

            _hasPersistentCommandFeedback = true;
            _persistentCommandFeedback = model;
            _persistentCommandFeedbackActions = actionsModel;
            ApplyFeedbackVisuals(model);
            ApplyCommandFeedbackActions(actionsModel);
        }

        public void ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel model, float now)
        {
            if (!model.Visible || string.IsNullOrWhiteSpace(model.Message))
                return;

            _transientFeedbackActive = model.Lifetime == CommandFeedbackLifetime.Transient;
            _transientFeedbackExpiresAt = now + Mathf.Max(0f, model.DurationSeconds);
            ApplyFeedbackVisuals(model);
            ApplyCommandFeedbackActions(MatchHudCommandFeedbackActionsModel.Hidden);
        }

        public void HideFeedbackMessage()
        {
            _transientFeedbackActive = false;
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);
            ApplyCommandFeedbackActions(MatchHudCommandFeedbackActionsModel.Hidden);
        }

        public void ClearPersistentCommandFeedback()
        {
            _hasPersistentCommandFeedback = false;
            _persistentCommandFeedback = MatchHudCommandFeedbackModel.Hidden;
            _persistentCommandFeedbackActions = MatchHudCommandFeedbackActionsModel.Hidden;
        }

        public void TickFeedbackLifetime(float now)
        {
            TickCommandFeedbackLifetime(now);
            TickCurrentOrderBannerLifetime(now);
        }

        private void TickCommandFeedbackLifetime(float now)
        {
            if (!_transientFeedbackActive || now < _transientFeedbackExpiresAt)
                return;

            _transientFeedbackActive = false;
            if (_hasPersistentCommandFeedback && _persistentCommandFeedback.Visible)
            {
                ApplyFeedbackVisuals(_persistentCommandFeedback);
                ApplyCommandFeedbackActions(_persistentCommandFeedbackActions);
                return;
            }

            HideFeedbackMessage();
        }

        private void TickCurrentOrderBannerLifetime(float now)
        {
            if (!_transientCurrentOrderBannerActive || now < _transientCurrentOrderBannerExpiresAt)
                return;

            _transientCurrentOrderBannerActive = false;
            if (_hasPersistentCurrentOrderBanner && _persistentCurrentOrderBanner.Visible)
            {
                currentOrderBanner?.Apply(_persistentCurrentOrderBanner);
                return;
            }

            currentOrderBanner?.Hide();
        }

        public void ApplyCommandFeedbackActions(MatchHudCommandFeedbackActionsModel model)
        {
            if (feedbackActionsRoot != null)
                feedbackActionsRoot.SetActive(model.Visible);

            ApplyButtonState(boardAllButton, boardAllButtonLabel, model.BoardAllVisible, model.BoardAllInteractable, model.BoardAllLabel);
            ApplyButtonState(cancelButton, cancelButtonLabel, model.CancelVisible, true, model.CancelLabel);
        }

        internal void ResetRuntimeFeedbackState()
        {
            _currentCommandMode = TacticalCommandMode.None;
            _stickyCommandMode = TacticalCommandMode.None;
            _lastCommandResult = TacticalCommandResult.Success();
            _hasLastCommandResult = false;
            ClearPersistentCommandFeedback();
            _transientFeedbackActive = false;
            _hasPersistentCurrentOrderBanner = false;
            _persistentCurrentOrderBanner = MatchHudCurrentOrderBannerModel.Hidden;
            _transientCurrentOrderBannerActive = false;
        }

        private void ApplyFeedbackVisuals(MatchHudCommandFeedbackModel model)
        {
            if (feedbackText != null)
                feedbackText.text = model.Message;
            ApplyFeedbackIcon(model.Severity);
            if (feedbackPanel != null)
                feedbackPanel.SetActive(true);
        }

        private void ApplyFeedbackIcon(CommandFeedbackSeverity severity)
        {
            if (feedbackIcon == null)
                return;

            Sprite icon = ResolveFeedbackIcon(severity);
            feedbackIcon.sprite = icon;
            feedbackIcon.enabled = icon != null;
        }

        private Sprite ResolveFeedbackIcon(CommandFeedbackSeverity severity)
        {
            return severity switch
            {
                CommandFeedbackSeverity.Ready => readyIcon != null ? readyIcon : neutralIcon,
                CommandFeedbackSeverity.Warning => warningIcon != null ? warningIcon : neutralIcon,
                CommandFeedbackSeverity.Error => errorIcon != null ? errorIcon : warningIcon != null ? warningIcon : neutralIcon,
                _ => neutralIcon
            };
        }

        public bool ContainsFeedbackActionScreenPoint(Vector2 screenPosition)
        {
            return ContainsScreenPoint(boardAllButton != null ? boardAllButton.transform as RectTransform : null, screenPosition) ||
                   ContainsScreenPoint(cancelButton != null ? cancelButton.transform as RectTransform : null, screenPosition);
        }

        public void ApplyCommandModeTabs(TacticalCommandMode mode)
        {
            CommandTabFeedbackHelper.ApplyCommandMode(commandTabGroups, mode);
        }

        public void ClearCommandModeTabs()
        {
            CommandTabFeedbackHelper.ClearCommandMode(commandTabGroups);
        }

        public void ShowSelectedEntity(string displayName, string status)
        {
            ResolveTacticalFeedback()?.ShowSelectedEntity(displayName, status);
        }

        public void HideSelectedEntity()
        {
            ResolveTacticalFeedback()?.HideSelectedEntity();
        }

        public void ShowCommandMode(string mode)
        {
            ResolveTacticalFeedback()?.ShowCommandMode(mode);
        }

        public void HideCommandMode()
        {
            ResolveTacticalFeedback()?.HideCommandMode();
        }

        public void ShowInvalidCommand(string reason)
        {
            ResolveTacticalFeedback()?.ShowInvalidCommand(reason);
        }

        public void HideInvalidCommand()
        {
            ResolveTacticalFeedback()?.HideInvalidCommand();
        }

        public void SetWorldMarkersVisible(bool visible)
        {
            ResolveTacticalFeedback()?.SetWorldMarkersVisible(visible);
        }

        private BattleHudTacticalFeedbackView ResolveTacticalFeedback()
        {
            return tacticalFeedback != null
                ? tacticalFeedback
                : GetComponent<BattleHudTacticalFeedbackView>();
        }

        private void BindUnityEvents()
        {
            BindButton(boardAllButton, ref _boundBoardAllButton, HandleBoardAll);
            BindButton(cancelButton, ref _boundCancelButton, HandleCancel);
        }

        private void RemoveUnityEvents()
        {
            UnbindButton(ref _boundBoardAllButton, HandleBoardAll);
            UnbindButton(ref _boundCancelButton, HandleCancel);
        }

        private void HandleBoardAll()
        {
            _boardAllRequested?.Invoke();
        }

        private void HandleCancel()
        {
            _cancelRequested?.Invoke();
        }

        private static void ApplyButtonState(Button button, TMP_Text label, bool visible, bool interactable, string text)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
                button.interactable = visible && interactable;
            }

            if (label != null)
                label.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        private static void BindButton(Button button, ref Button boundButton, UnityEngine.Events.UnityAction action)
        {
            if (boundButton == button)
                return;

            UnbindButton(ref boundButton, action);
            boundButton = button;
            if (boundButton != null)
                boundButton.onClick.AddListener(action);
        }

        private static void UnbindButton(ref Button boundButton, UnityEngine.Events.UnityAction action)
        {
            if (boundButton == null)
                return;

            boundButton.onClick.RemoveListener(action);
            boundButton = null;
        }

        private static bool ContainsScreenPoint(RectTransform rectTransform, Vector2 screenPosition)
        {
            return rectTransform != null &&
                   rectTransform.gameObject.activeInHierarchy &&
                   RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
        }
    }
}
