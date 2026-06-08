using System.Collections.Generic;
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
    Special,
    Scan
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
            TacticalCommandReasonCode.TargetOutOfBounds => "Target is outside the playable area.",
            TacticalCommandReasonCode.TargetBlocked => "Route is blocked.",
            TacticalCommandReasonCode.TargetUnreachable => "Target is unreachable.",
            TacticalCommandReasonCode.TargetNotEnemy => "Select a hostile target.",
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
            TacticalCommandMode.Move => "Choose destination",
            TacticalCommandMode.Attack => "TAP TARGET",
            TacticalCommandMode.Scan => "TAP SCAN AREA",
            _ => string.Empty
        };
    }
}

[DisallowMultipleComponent]
public sealed class BattleHudRuntimeFeedbackView : MonoBehaviour
{
    private static readonly List<BattleHudRuntimeFeedbackView> RegisteredInstances = new();

    [SerializeField] private BattleHudTacticalFeedbackSystem tacticalFeedback;
    [SerializeField] private MatchOverlayCommandTabGroupView[] commandTabGroups;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TMP_Text feedbackText;

    public BattleHudTacticalFeedbackSystem TacticalFeedback => tacticalFeedback;
    public MatchOverlayCommandTabGroupView[] CommandTabGroups => commandTabGroups;
    public GameObject FeedbackPanel => feedbackPanel;
    public TMP_Text FeedbackText => feedbackText;
    public static IReadOnlyList<BattleHudRuntimeFeedbackView> Instances => RegisteredInstances;

    private void Awake()
    {
        HideFeedbackMessage();
    }

    private void OnEnable()
    {
        if (!RegisteredInstances.Contains(this))
            RegisteredInstances.Add(this);
    }

    private void OnDisable()
    {
        RegisteredInstances.Remove(this);
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
