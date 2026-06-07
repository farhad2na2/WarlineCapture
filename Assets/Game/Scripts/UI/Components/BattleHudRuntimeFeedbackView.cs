using TMPro;
using UnityEngine;

public enum TacticalCommandMode
{
    None,
    Select,
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
            TacticalCommandMode.Select => "SELECT SQUAD",
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

    public static string ToInstructionText(TacticalCommandMode mode)
    {
        return mode switch
        {
            TacticalCommandMode.Move => "Choose destination",
            TacticalCommandMode.Attack => "Choose target",
            _ => string.Empty
        };
    }
}

[DisallowMultipleComponent]
public sealed class BattleHudRuntimeFeedbackView : MonoBehaviour
{
    [SerializeField] private BattleHudTacticalFeedbackSystem tacticalFeedback;
    [SerializeField] private MatchOverlayCommandTabGroupView[] commandTabGroups;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;

    public BattleHudTacticalFeedbackSystem TacticalFeedback => tacticalFeedback;
    public MatchOverlayCommandTabGroupView[] CommandTabGroups => commandTabGroups;
    public GameObject FeedbackPanel => feedbackPanel;
    public TMP_Text FeedbackText => feedbackText;

    private void Awake()
    {
        HideFeedbackMessage();
    }

    public void ShowFeedbackMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            HideFeedbackMessage();
            return;
        }

        if (feedbackText != null)
            feedbackText.text = message;
        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
    }

    public void HideFeedbackMessage()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }
}
