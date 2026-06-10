using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TacticalCommandMode
{
    None,
    Select,
    Move,
    Attack,
    Hold,
    Stop,
    Build,
    Special,
    Scan,
    Board
}

public enum TacticalCommandReasonCode
{
    None,
    NoSelection,
    TargetOutOfBounds,
    TargetBlocked,
    TargetUnreachable,
    TargetNotEnemy,
    TargetNotAttackable,
    CommandUnavailable,
    BuildUnavailable,
    CameraJumpUnavailable,
    ScanUnavailable,
    ScanCooldown,
    InsufficientResources
}

public enum CommandFeedbackSeverity
{
    Neutral,
    Ready,
    Warning,
    Error
}

public enum CommandFeedbackLifetime
{
    Hidden,
    Persistent,
    Transient
}

public readonly struct MatchHudCommandFeedbackModel
{
    public MatchHudCommandFeedbackModel(
        bool visible,
        string message,
        CommandFeedbackSeverity severity,
        CommandFeedbackLifetime lifetime = CommandFeedbackLifetime.Persistent,
        float durationSeconds = 0f)
    {
        Visible = visible;
        Message = message;
        Severity = severity;
        Lifetime = visible ? lifetime : CommandFeedbackLifetime.Hidden;
        DurationSeconds = visible ? Mathf.Max(0f, durationSeconds) : 0f;
    }

    public bool Visible { get; }
    public string Message { get; }
    public CommandFeedbackSeverity Severity { get; }
    public CommandFeedbackLifetime Lifetime { get; }
    public float DurationSeconds { get; }

    public static MatchHudCommandFeedbackModel Hidden => new(false, string.Empty, CommandFeedbackSeverity.Neutral, CommandFeedbackLifetime.Hidden);

    public static MatchHudCommandFeedbackModel Show(string message, CommandFeedbackSeverity severity)
    {
        return string.IsNullOrWhiteSpace(message)
            ? Hidden
            : new MatchHudCommandFeedbackModel(true, message, severity);
    }

    public static MatchHudCommandFeedbackModel ShowTransient(string message, CommandFeedbackSeverity severity, float durationSeconds)
    {
        return string.IsNullOrWhiteSpace(message)
            ? Hidden
            : new MatchHudCommandFeedbackModel(true, message, severity, CommandFeedbackLifetime.Transient, durationSeconds);
    }
}

public readonly struct MatchHudCommandFeedbackActionsModel
{
    public MatchHudCommandFeedbackActionsModel(
        bool visible,
        bool boardAllVisible,
        bool boardAllInteractable,
        string boardAllLabel,
        bool cancelVisible,
        string cancelLabel)
    {
        Visible = visible;
        BoardAllVisible = boardAllVisible;
        BoardAllInteractable = boardAllInteractable;
        BoardAllLabel = boardAllLabel;
        CancelVisible = cancelVisible;
        CancelLabel = cancelLabel;
    }

    public bool Visible { get; }
    public bool BoardAllVisible { get; }
    public bool BoardAllInteractable { get; }
    public string BoardAllLabel { get; }
    public bool CancelVisible { get; }
    public string CancelLabel { get; }

    public static MatchHudCommandFeedbackActionsModel Hidden =>
        new(false, false, false, string.Empty, false, string.Empty);

    public static MatchHudCommandFeedbackActionsModel BoardPassengerSelection(bool boardAllInteractable)
    {
        return new(true, true, boardAllInteractable, "BOARD ALL", true, "CANCEL");
    }

    public static MatchHudCommandFeedbackActionsModel CancelOnly =>
        new(true, false, false, string.Empty, true, "CANCEL");
}

public readonly struct TacticalCommandResult
{
    public bool Accepted { get; }
    public TacticalCommandReasonCode ReasonCode { get; }
    public string Message { get; }

    private TacticalCommandResult(bool accepted, TacticalCommandReasonCode reasonCode, string message)
    {
        Accepted = accepted;
        ReasonCode = reasonCode;
        Message = message;
    }

    public static TacticalCommandResult Success(string message = "") => new(true, TacticalCommandReasonCode.None, message);

    public static TacticalCommandResult Rejected(TacticalCommandReasonCode reasonCode, string message = "")
    {
        return new(false, reasonCode, message);
    }
}

public static class TacticalCommandFeedbackText
{
    public static string ToDisplayText(TacticalCommandMode mode)
    {
        return mode switch
        {
            TacticalCommandMode.Select => "SELECT SQUAD",
            TacticalCommandMode.Move => "MOVE ORDER",
            TacticalCommandMode.Attack => "ATTACK ORDER",
            TacticalCommandMode.Hold => "HOLD POSITION",
            TacticalCommandMode.Stop => "STOP ORDER",
            TacticalCommandMode.Scan => "SCAN ORDER",
            TacticalCommandMode.Board => "BOARD ORDER",
            TacticalCommandMode.Build => "BUILD MODE",
            TacticalCommandMode.Special => "SPECIAL ORDER",
            _ => string.Empty
        };
    }

    public static string ToDisplayText(TacticalCommandReasonCode reasonCode)
    {
        return reasonCode switch
        {
            TacticalCommandReasonCode.NoSelection => "Select units or a building first.",
            TacticalCommandReasonCode.TargetOutOfBounds => "Target is outside the playable area.",
            TacticalCommandReasonCode.TargetBlocked => "Route is blocked.",
            TacticalCommandReasonCode.TargetUnreachable => "Target is unreachable.",
            TacticalCommandReasonCode.TargetNotEnemy => "Target is not hostile.",
            TacticalCommandReasonCode.TargetNotAttackable => "Target cannot be attacked.",
            TacticalCommandReasonCode.CommandUnavailable => "Command unavailable.",
            TacticalCommandReasonCode.BuildUnavailable => "Building unavailable.",
            TacticalCommandReasonCode.CameraJumpUnavailable => "Camera focus unavailable.",
            TacticalCommandReasonCode.ScanUnavailable => "Scan unavailable.",
            TacticalCommandReasonCode.ScanCooldown => "Scan cooling down.",
            TacticalCommandReasonCode.InsufficientResources => "Insufficient resources.",
            _ => string.Empty
        };
    }

    public static string ToInstructionText(TacticalCommandMode mode)
    {
        return mode switch
        {
            TacticalCommandMode.Select => "Select units or a building.",
            TacticalCommandMode.Move => "Choose destination.",
            TacticalCommandMode.Attack => "Tap hostile target.",
            TacticalCommandMode.Scan => "Tap scan area.",
            TacticalCommandMode.Board => "Tap a transport.",
            TacticalCommandMode.Build => "Choose what to build, produce, or recruit.",
            TacticalCommandMode.Special => "Choose special command.",
            _ => string.Empty
        };
    }

    public static CommandFeedbackSeverity ToInstructionSeverity(TacticalCommandMode mode)
    {
        return mode switch
        {
            TacticalCommandMode.Move or
            TacticalCommandMode.Attack or
            TacticalCommandMode.Scan or
            TacticalCommandMode.Board or
            TacticalCommandMode.Build => CommandFeedbackSeverity.Ready,
            TacticalCommandMode.Select or
            TacticalCommandMode.Special => CommandFeedbackSeverity.Neutral,
            _ => CommandFeedbackSeverity.Neutral
        };
    }
}

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
