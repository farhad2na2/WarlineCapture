using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleHudRuntimeFeedbackView : MonoBehaviour
{
    [SerializeField] private BattleHudTacticalFeedbackView tacticalFeedback;
    [SerializeField] private MatchOverlayCommandTabGroupView[] commandTabGroups;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Image feedbackIcon;
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
    public GameObject FeedbackActionsRoot => feedbackActionsRoot;
    public Button BoardAllButton => boardAllButton;
    public Button CancelButton => cancelButton;
    internal TacticalCommandMode CurrentCommandMode
    {
        get => _currentCommandMode;
        set => _currentCommandMode = value;
    }

    internal TacticalCommandMode StickyCommandMode
    {
        get => _stickyCommandMode;
        set => _stickyCommandMode = value;
    }

    internal TacticalCommandResult LastCommandResult
    {
        get => _lastCommandResult;
        set => _lastCommandResult = value;
    }

    internal bool HasLastCommandResult
    {
        get => _hasLastCommandResult;
        set => _hasLastCommandResult = value;
    }

    internal BattleHudRuntimeFeedbackState RuntimeFeedbackState =>
        new(_currentCommandMode, _stickyCommandMode, _lastCommandResult, _hasLastCommandResult);

    private void Awake()
    {
        BindUnityEvents();
        HideFeedbackMessage();
    }

    private void OnEnable()
    {
        BindUnityEvents();
        ResetRuntimeFeedbackState();
        BattleHudRuntimeFeedbackSystem.ClearCommandMode(this);
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

    internal void ApplyPersistentCommandFeedback(
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

    internal void ApplyTransientCommandFeedback(MatchHudCommandFeedbackModel model, float now)
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

    internal void ClearPersistentCommandFeedback()
    {
        _hasPersistentCommandFeedback = false;
        _persistentCommandFeedback = MatchHudCommandFeedbackModel.Hidden;
        _persistentCommandFeedbackActions = MatchHudCommandFeedbackActionsModel.Hidden;
    }

    internal void TickFeedbackLifetime(float now)
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
