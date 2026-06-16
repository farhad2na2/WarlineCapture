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
    InsufficientResources,
    InvalidTransport,
    InvalidPassenger,
    TransportFull,
    NoEligiblePassengers,
    NoDisembarkCell,
    TransportPassengerMissing
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
        DurationSeconds = visible ? ClampNonNegative(durationSeconds) : 0f;
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

    private static float ClampNonNegative(float value)
    {
        return value > 0f ? value : 0f;
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

public readonly struct BattleHudRuntimeFeedbackState
{
    public BattleHudRuntimeFeedbackState(
        TacticalCommandMode currentCommandMode,
        TacticalCommandMode stickyCommandMode,
        TacticalCommandResult lastCommandResult,
        bool hasLastCommandResult)
    {
        CurrentCommandMode = currentCommandMode;
        StickyCommandMode = stickyCommandMode;
        LastCommandResult = lastCommandResult;
        HasLastCommandResult = hasLastCommandResult;
    }

    public TacticalCommandMode CurrentCommandMode { get; }
    public TacticalCommandMode StickyCommandMode { get; }
    public TacticalCommandResult LastCommandResult { get; }
    public bool HasLastCommandResult { get; }

    public static BattleHudRuntimeFeedbackState Empty =>
        new(TacticalCommandMode.None, TacticalCommandMode.None, TacticalCommandResult.Success(), false);
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
            TacticalCommandReasonCode.InvalidTransport => "Select a transport vehicle or aircraft first.",
            TacticalCommandReasonCode.InvalidPassenger => "Select soldiers that can board.",
            TacticalCommandReasonCode.TransportFull => "Transport is full.",
            TacticalCommandReasonCode.NoEligiblePassengers => "No nearby soldiers can board this transport.",
            TacticalCommandReasonCode.NoDisembarkCell => "No clear exit point for passengers.",
            TacticalCommandReasonCode.TransportPassengerMissing => "Passenger is not inside this transport.",
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
            TacticalCommandMode.Hold => "Hold position and return fire.",
            TacticalCommandMode.Stop => "Stop selected units and clear orders.",
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
            TacticalCommandMode.Hold or
            TacticalCommandMode.Scan or
            TacticalCommandMode.Board or
            TacticalCommandMode.Build => CommandFeedbackSeverity.Ready,
            TacticalCommandMode.Stop => CommandFeedbackSeverity.Warning,
            TacticalCommandMode.Select or
            TacticalCommandMode.Special => CommandFeedbackSeverity.Neutral,
            _ => CommandFeedbackSeverity.Neutral
        };
    }
}
