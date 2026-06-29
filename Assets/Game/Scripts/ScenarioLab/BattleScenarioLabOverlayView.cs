using System.Text;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleScenarioLabOverlayView : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text variantsText;
    [SerializeField] private Text comparisonsText;

    public void ShowPending(BattleScenarioDefinition definition)
    {
        SetText(titleText, definition != null ? definition.DisplayName : "Battle Scenario Lab");
        SetText(statusText, "Waiting for scenario run.");
        SetText(variantsText, string.Empty);
        SetText(comparisonsText, string.Empty);
    }

    public void ShowResult(BattleScenarioResult result)
    {
        if (result == null)
        {
            SetText(statusText, "No scenario result.");
            return;
        }

        if (TransportBoardingScenarioCatalog.TryGetScenario(result.ScenarioId, out TransportBoardingScenarioDescriptor transportScenario))
        {
            ShowTransportBoardingResult(result, transportScenario);
            return;
        }

        SetText(titleText, result.ScenarioId);
        SetText(statusText, result.Passed ? "PASS" : $"FAIL - {result.FailureReason}");
        SetText(variantsText, BuildVariantSummary(result));
        SetText(comparisonsText, BuildComparisonSummary(result));
    }

    public void ShowError(string message)
    {
        SetText(statusText, "ERROR");
        SetText(variantsText, message);
        SetText(comparisonsText, string.Empty);
    }

    private static string BuildVariantSummary(BattleScenarioResult result)
    {
        var builder = new StringBuilder(512);
        for (int i = 0; i < result.Variants.Length; i++)
        {
            BattleScenarioMetrics metrics = result.Variants[i];
            builder
                .Append(metrics.VariantId)
                .Append(": detected ")
                .Append(FormatSeconds(metrics.DetectionTimeSeconds))
                .Append(", lock ")
                .Append(FormatSeconds(metrics.LockTimeSeconds))
                .Append(", launch ")
                .Append(FormatSeconds(metrics.LaunchTimeSeconds))
                .Append(", intercept ")
                .Append(metrics.Intercepted ? FormatSeconds(metrics.InterceptTimeSeconds) : "no")
                .Append(", radar ")
                .Append(metrics.RadarProviderUsed ? "yes" : "no")
                .Append('\n');
        }

        return builder.ToString();
    }

    private static string BuildComparisonSummary(BattleScenarioResult result)
    {
        var builder = new StringBuilder(256);
        for (int i = 0; i < result.Comparisons.Length; i++)
        {
            BattleScenarioComparison comparison = result.Comparisons[i];
            builder
                .Append(comparison.BaselineVariantId)
                .Append(" -> ")
                .Append(comparison.SupportedVariantId)
                .Append(": detection delta ")
                .Append(FormatDeltaSeconds(comparison.DetectionTimeDeltaSeconds))
                .Append(", lock delta ")
                .Append(FormatDeltaSeconds(comparison.LockTimeDeltaSeconds))
                .Append(", outcome ")
                .Append(comparison.RadarImprovedOrMatchedOutcome ? "improved/matched" : "not improved")
                .Append('\n');
        }

        return builder.ToString();
    }

    private void ShowTransportBoardingResult(BattleScenarioResult result, TransportBoardingScenarioDescriptor scenario)
    {
        BattleScenarioFailureReason failureReason = result != null
            ? result.FailureReason
            : BattleScenarioFailureReason.None;

        SetText(titleText, scenario.DisplayName);
        SetText(statusText, scenario.VisualProofRequired
            ? "VISUAL PLAYBACK - production ECS"
            : "AUTOMATED AUDIT - no live visual playback");
        SetText(variantsText, BuildTransportBoardingSummary(scenario, failureReason));
        SetText(comparisonsText, scenario.Description);
    }

    private static string BuildTransportBoardingSummary(
        TransportBoardingScenarioDescriptor scenario,
        BattleScenarioFailureReason failureReason)
    {
        var builder = new StringBuilder(256);
        builder
            .Append("Phase: ")
            .Append(ResolveTransportPhase(scenario.Kind))
            .Append('\n')
            .Append("Passenger count: ")
            .Append(ResolveTransportPassengerCount(scenario.Kind))
            .Append('\n')
            .Append("Exit mode: ")
            .Append(scenario.ExitMode)
            .Append('\n')
            .Append("Failure reason: ")
            .Append(failureReason);
        return builder.ToString();
    }

    private static string ResolveTransportPhase(TransportBoardingScenarioKind kind)
    {
        return kind switch
        {
            TransportBoardingScenarioKind.GroundVehicleBoardAndExit => "board -> ground exit",
            TransportBoardingScenarioKind.HelicopterBoardAndRopeExit => "board -> rope exit",
            TransportBoardingScenarioKind.HelicopterAirPickup => "air pickup -> board -> rope exit",
            TransportBoardingScenarioKind.HelicopterGroundExitAudit => "ground-exit audit",
            TransportBoardingScenarioKind.PlaneRampBoardAndExit => "ramp board -> ground exit",
            TransportBoardingScenarioKind.PlaneSoldierAirdrop => "board -> soldier airdrop",
            TransportBoardingScenarioKind.PlaneVehicleCargoGroundExit => "vehicle cargo board -> ramp exit",
            TransportBoardingScenarioKind.PlaneVehicleCargoAirdrop => "vehicle cargo onboard -> cargo drop",
            TransportBoardingScenarioKind.PlaneMixedLoadAirdrop => "mixed load -> airdrop",
            TransportBoardingScenarioKind.RejectionCases => "negative-case checks",
            TransportBoardingScenarioKind.NextCleanup => "cleanup switching proof",
            TransportBoardingScenarioKind.CameraProofPath => "camera proof path",
            _ => "visual playback"
        };
    }

    private static string ResolveTransportPassengerCount(TransportBoardingScenarioKind kind)
    {
        return kind switch
        {
            TransportBoardingScenarioKind.GroundVehicleBoardAndExit => "1 soldier",
            TransportBoardingScenarioKind.HelicopterBoardAndRopeExit => "1 soldier",
            TransportBoardingScenarioKind.HelicopterAirPickup => "1 soldier",
            TransportBoardingScenarioKind.PlaneRampBoardAndExit => "1 soldier",
            TransportBoardingScenarioKind.PlaneSoldierAirdrop => "1 soldier",
            TransportBoardingScenarioKind.PlaneVehicleCargoGroundExit => "1 vehicle cargo",
            TransportBoardingScenarioKind.PlaneVehicleCargoAirdrop => "1 vehicle cargo",
            TransportBoardingScenarioKind.PlaneMixedLoadAirdrop => "1 soldier + 1 vehicle cargo",
            _ => "n/a"
        };
    }

    private static string FormatSeconds(float value)
    {
        return value < 0f ? "n/a" : $"{value:0.00}s";
    }

    private static string FormatDeltaSeconds(float value)
    {
        return $"{value:+0.00;-0.00;0.00}s";
    }

    private static void SetText(Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }
}
