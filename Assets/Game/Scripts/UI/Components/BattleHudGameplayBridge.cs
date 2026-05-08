using UnityEngine;

public enum TacticalCommandMode
{
    None,
    Move,
    Attack,
    Hold,
    Stop,
    Build,
    Special
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
    MissionDoesNotAllowBuild,
    CameraJumpUnavailable
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

    public static TacticalCommandResult Success() => new(true, TacticalCommandReasonCode.None, string.Empty);

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
            TacticalCommandMode.Move => "MOVE ORDER",
            TacticalCommandMode.Attack => "ATTACK ORDER",
            TacticalCommandMode.Hold => "HOLD POSITION",
            TacticalCommandMode.Stop => "STOP ORDER",
            TacticalCommandMode.Build => "BUILD MODE",
            TacticalCommandMode.Special => "SPECIAL ORDER",
            _ => string.Empty
        };
    }

    public static string ToDisplayText(TacticalCommandReasonCode reasonCode)
    {
        return reasonCode switch
        {
            TacticalCommandReasonCode.NoSelection => "Select a squad first.",
            TacticalCommandReasonCode.TargetOutOfBounds => "Target is outside the mission area.",
            TacticalCommandReasonCode.TargetBlocked => "Route is blocked.",
            TacticalCommandReasonCode.TargetUnreachable => "Target is unreachable.",
            TacticalCommandReasonCode.TargetNotEnemy => "Select a hostile target.",
            TacticalCommandReasonCode.TargetNotAttackable => "Target cannot be attacked.",
            TacticalCommandReasonCode.CommandUnavailable => "Command unavailable.",
            TacticalCommandReasonCode.MissionDoesNotAllowBuild => "Building unlocks in the next mission.",
            TacticalCommandReasonCode.CameraJumpUnavailable => "Camera focus unavailable.",
            _ => string.Empty
        };
    }
}

[DisallowMultipleComponent]
public sealed class BattleHudGameplayBridge : MonoBehaviour
{
    [SerializeField] private BattleHudTacticalFeedbackController tacticalFeedback;

    public BattleHudTacticalFeedbackController TacticalFeedback => tacticalFeedback;
    public TacticalCommandMode CurrentCommandMode { get; private set; } = TacticalCommandMode.None;
    public TacticalCommandResult LastCommandResult { get; private set; } = TacticalCommandResult.Success();
    public bool HasLastCommandResult { get; private set; }

    public static BattleHudGameplayBridge ResolveActive()
    {
        BattleHudGameplayBridge[] bridges = Resources.FindObjectsOfTypeAll<BattleHudGameplayBridge>();
        for (int i = 0; i < bridges.Length; i++)
        {
            BattleHudGameplayBridge bridge = bridges[i];
            if (bridge == null || !bridge.gameObject.scene.IsValid())
                continue;

            return bridge;
        }

        return null;
    }

    private void Awake()
    {
        if (tacticalFeedback == null)
            tacticalFeedback = GetComponent<BattleHudTacticalFeedbackController>();
    }

    public void ApplySelection(string displayName, string status)
    {
        if (tacticalFeedback == null)
            return;

        if (string.IsNullOrWhiteSpace(displayName))
            tacticalFeedback.HideSelectedEntity();
        else
            tacticalFeedback.ShowSelectedEntity(displayName, status);
    }

    public void ClearSelection()
    {
        tacticalFeedback?.HideSelectedEntity();
    }

    public void ApplyCommandMode(TacticalCommandMode mode)
    {
        CurrentCommandMode = mode;
        if (tacticalFeedback == null)
            return;

        string displayText = TacticalCommandFeedbackText.ToDisplayText(mode);
        if (string.IsNullOrEmpty(displayText))
            tacticalFeedback.HideCommandMode();
        else
            tacticalFeedback.ShowCommandMode(displayText);
    }

    public void ClearCommandMode()
    {
        CurrentCommandMode = TacticalCommandMode.None;
        tacticalFeedback?.HideCommandMode();
    }

    public void ApplyCommandResult(TacticalCommandResult result)
    {
        LastCommandResult = result;
        HasLastCommandResult = true;

        if (tacticalFeedback == null)
            return;

        if (result.Accepted)
        {
            tacticalFeedback.HideInvalidCommand();
            return;
        }

        string reason = !string.IsNullOrWhiteSpace(result.Message)
            ? result.Message
            : TacticalCommandFeedbackText.ToDisplayText(result.ReasonCode);
        tacticalFeedback.ShowInvalidCommand(reason);
    }

    public void SetWorldMarkersVisible(bool visible)
    {
        tacticalFeedback?.SetWorldMarkersVisible(visible);
    }
}
