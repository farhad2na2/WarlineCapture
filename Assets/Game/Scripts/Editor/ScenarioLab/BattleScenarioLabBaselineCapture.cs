using Game.Runtime;
using Game.Composition;
namespace Game.Editor
{
    #if UNITY_EDITOR
    using System;
    using System.IO;
    using System.Text;
    using UnityEditor;
    using UnityEngine;

    public static class BattleScenarioLabBaselineCapture
    {
        public const string Ad001BaselineMarkdownPath = "Design/Architecture/battle_scenario_lab_ad001_baseline_metrics.md";
        public const string Ad001BaselineJsonPath = "Design/Architecture/battle_scenario_lab_ad001_baseline_metrics.json";

        [MenuItem("Warline Capture/Scenario Lab/Capture AD-001 Baseline Metrics")]
        public static void CaptureAd001BaselineMetrics()
        {
            try
            {
                BattleScenarioDefinition definition =
                    AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(BattleScenarioLabValidationRunner.Ad001DefinitionPath);
                if (definition == null)
                    throw new InvalidOperationException($"Missing AD-001 definition asset: {BattleScenarioLabValidationRunner.Ad001DefinitionPath}");

                BattleScenarioResult result = BattleScenarioAd001Runner.RunDefinition(definition);
                File.WriteAllText(Ad001BaselineJsonPath, BattleScenarioReportJson.ToJson(result));
                File.WriteAllText(Ad001BaselineMarkdownPath, ToMarkdown(result));

                if (!result.Passed)
                {
                    Debug.LogError($"[BattleScenarioLab] AD-001 baseline capture failed scenario criteria: {Ad001BaselineMarkdownPath}");
                    Exit(1);
                    return;
                }

                Debug.Log($"[BattleScenarioLab] AD-001 baseline metrics captured: {Ad001BaselineMarkdownPath}");
                Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BattleScenarioLab] AD-001 baseline capture exception: {ex}");
                Exit(1);
            }
        }

        private static string ToMarkdown(BattleScenarioResult result)
        {
            var builder = new StringBuilder(4096);
            builder.AppendLine("# AD-001 Baseline Metrics");
            builder.AppendLine();
            builder.AppendLine("This file records the current measured AD-001 behavior before any Phase 6 tuning.");
            builder.AppendLine("It is a measurement artifact only; it does not change live combat balance.");
            builder.AppendLine();
            builder.Append("- Scenario: `").Append(result.ScenarioId).AppendLine("`");
            builder.Append("- Generated UTC: `").Append(result.GeneratedAtUtc).AppendLine("`");
            builder.Append("- Fixed delta time: `").Append(result.FixedDeltaTime.ToString("0.###")).AppendLine("`");
            builder.Append("- Passed: `").Append(result.Passed ? "true" : "false").AppendLine("`");
            builder.Append("- Failure reason: `").Append(result.FailureReason).AppendLine("`");
            builder.AppendLine();
            builder.AppendLine("## Variant Metrics");
            builder.AppendLine();
            builder.AppendLine("| Variant | Radar | Detected | Detection | Locked | Lock | Launched | Launch | Intercepted | Intercept | Closest Distance | Effective Range | Effective Lock | Tracking Quality |");
            builder.AppendLine("| --- | --- | --- | ---: | --- | ---: | --- | ---: | --- | ---: | ---: | ---: | ---: | ---: |");
            for (int i = 0; i < result.Variants.Length; i++)
            {
                BattleScenarioMetrics metrics = result.Variants[i];
                builder
                    .Append("| `").Append(metrics.VariantId).Append("` ")
                    .Append("| ").Append(metrics.RadarProviderUsed ? "yes" : "no").Append(' ')
                    .Append("| ").Append(metrics.Detected ? "yes" : "no").Append(' ')
                    .Append("| ").Append(FormatSeconds(metrics.DetectionTimeSeconds)).Append(' ')
                    .Append("| ").Append(metrics.Locked ? "yes" : "no").Append(' ')
                    .Append("| ").Append(FormatSeconds(metrics.LockTimeSeconds)).Append(' ')
                    .Append("| ").Append(metrics.InterceptorLaunched ? "yes" : "no").Append(' ')
                    .Append("| ").Append(FormatSeconds(metrics.LaunchTimeSeconds)).Append(' ')
                    .Append("| ").Append(metrics.Intercepted ? "yes" : "no").Append(' ')
                    .Append("| ").Append(FormatSeconds(metrics.InterceptTimeSeconds)).Append(' ')
                    .Append("| ").Append(FormatFloat(metrics.ClosestInterceptorDistanceToThreat)).Append(' ')
                    .Append("| ").Append(FormatFloat(metrics.LauncherEffectiveRange)).Append(' ')
                    .Append("| ").Append(FormatSeconds(metrics.LauncherEffectiveLockSeconds)).Append(' ')
                    .Append("| ").Append(FormatFloat(metrics.LauncherEffectiveTrackingQuality)).AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Radar Comparisons");
            builder.AppendLine();
            builder.AppendLine("| Baseline | Supported | Detection Delta | Lock Delta | Detection Improved | Lock Improved | Outcome Improved/Matched |");
            builder.AppendLine("| --- | --- | ---: | ---: | --- | --- | --- |");
            for (int i = 0; i < result.Comparisons.Length; i++)
            {
                BattleScenarioComparison comparison = result.Comparisons[i];
                builder
                    .Append("| `").Append(comparison.BaselineVariantId).Append("` ")
                    .Append("| `").Append(comparison.SupportedVariantId).Append("` ")
                    .Append("| ").Append(FormatDeltaSeconds(comparison.DetectionTimeDeltaSeconds)).Append(' ')
                    .Append("| ").Append(FormatDeltaSeconds(comparison.LockTimeDeltaSeconds)).Append(' ')
                    .Append("| ").Append(comparison.RadarImprovedDetectionTime ? "yes" : "no").Append(' ')
                    .Append("| ").Append(comparison.RadarImprovedLockTime ? "yes" : "no").Append(' ')
                    .Append("| ").Append(comparison.RadarImprovedOrMatchedOutcome ? "yes" : "no").AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Current Target Outcomes");
            builder.AppendLine();
            builder.AppendLine("- Normal radar-near variant should intercept.");
            builder.AppendLine("- Radar-near normal should improve lock time and match or improve the no-support normal outcome.");
            builder.AppendLine("- Radar-near fast-threat should improve detection and match or improve the no-support fast-threat outcome.");
            builder.AppendLine("- No tuning is approved by this baseline capture.");
            return builder.ToString();
        }

        private static string FormatSeconds(float value)
        {
            return value < 0f ? "n/a" : value.ToString("0.00") + "s";
        }

        private static string FormatFloat(float value)
        {
            return value < 0f ? "n/a" : value.ToString("0.###");
        }

        private static string FormatDeltaSeconds(float value)
        {
            return (value >= 0f ? "+" : string.Empty) + value.ToString("0.00") + "s";
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
    #endif
}
