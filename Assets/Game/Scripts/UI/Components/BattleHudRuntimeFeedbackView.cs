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

public readonly struct MatchHudCommandFeedbackModel
{
    public MatchHudCommandFeedbackModel(bool visible, string message, CommandFeedbackSeverity severity)
    {
        Visible = visible;
        Message = message;
        Severity = severity;
    }

    public bool Visible { get; }
    public string Message { get; }
    public CommandFeedbackSeverity Severity { get; }

    public static MatchHudCommandFeedbackModel Hidden => new(false, string.Empty, CommandFeedbackSeverity.Neutral);

    public static MatchHudCommandFeedbackModel Show(string message, CommandFeedbackSeverity severity)
    {
        return string.IsNullOrWhiteSpace(message)
            ? Hidden
            : new MatchHudCommandFeedbackModel(true, message, severity);
    }
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
    [SerializeField] private Sprite neutralIcon;
    [SerializeField] private Sprite readyIcon;
    [SerializeField] private Sprite warningIcon;
    [SerializeField] private Sprite errorIcon;
    private TacticalCommandMode _currentCommandMode = TacticalCommandMode.None;
    private TacticalCommandMode _stickyCommandMode = TacticalCommandMode.None;
    private TacticalCommandResult _lastCommandResult = TacticalCommandResult.Success();
    private bool _hasLastCommandResult;

    public BattleHudTacticalFeedbackView TacticalFeedback => tacticalFeedback;
    public MatchOverlayCommandTabGroupView[] CommandTabGroups => commandTabGroups;
    public GameObject FeedbackPanel => feedbackPanel;
    public TMP_Text FeedbackText => feedbackText;
    public Image FeedbackIcon => feedbackIcon;
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
        HideFeedbackMessage();
    }

    private void OnEnable()
    {
        ResetRuntimeFeedbackState();
        BattleHudRuntimeFeedbackSystem.ClearCommandMode(this);
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
        if (!model.Visible || string.IsNullOrWhiteSpace(model.Message))
        {
            HideFeedbackMessage();
            return;
        }

        if (feedbackText != null)
            feedbackText.text = model.Message;
        ApplyFeedbackIcon(model.Severity);
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
    }

    public void HideFeedbackMessage()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    internal void ResetRuntimeFeedbackState()
    {
        _currentCommandMode = TacticalCommandMode.None;
        _stickyCommandMode = TacticalCommandMode.None;
        _lastCommandResult = TacticalCommandResult.Success();
        _hasLastCommandResult = false;
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
}
