using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Runtime
{
    public static class BalanceReportWriter
    {
        public readonly struct ReportPaths
        {
            public readonly string DirectoryPath;
            public readonly string JsonPath;
            public readonly string MarkdownPath;

            public ReportPaths(string directoryPath, string jsonPath, string markdownPath)
            {
                DirectoryPath = directoryPath;
                JsonPath = jsonPath;
                MarkdownPath = markdownPath;
            }
        }

        public static ReportPaths WriteProjectReport(BalanceMetrics metrics)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string reportDirectory = Path.Combine(projectRoot, "Library", "BalanceReports");
            return WriteReport(metrics, reportDirectory);
        }

        public static ReportPaths WriteReport(BalanceMetrics metrics, string reportDirectory)
        {
            if (metrics == null)
                throw new System.ArgumentNullException(nameof(metrics));

            if (string.IsNullOrWhiteSpace(reportDirectory))
                throw new System.ArgumentException("Report directory is required.", nameof(reportDirectory));

            Directory.CreateDirectory(reportDirectory);

            string safeProbeId = SanitizeFileName(metrics.ProbeId);
            string jsonPath = Path.Combine(reportDirectory, $"{safeProbeId}.json");
            string markdownPath = Path.Combine(reportDirectory, $"{safeProbeId}.md");

            File.WriteAllText(jsonPath, JsonUtility.ToJson(metrics, true), Encoding.UTF8);
            File.WriteAllText(markdownPath, BuildMarkdown(metrics), Encoding.UTF8);

            return new ReportPaths(reportDirectory, jsonPath, markdownPath);
        }

        private static string BuildMarkdown(BalanceMetrics metrics)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"# Balance Probe: {metrics.ProbeDisplayName}");
            builder.AppendLine();
            builder.AppendLine($"- Probe id: `{metrics.ProbeId}`");
            builder.AppendLine($"- Scenario: `{metrics.ScenarioId}`");
            if (!string.IsNullOrWhiteSpace(metrics.ProbeDescription))
                builder.AppendLine($"- Description: {metrics.ProbeDescription}");
            builder.AppendLine($"- Seed: `{metrics.Seed}`");
            builder.AppendLine($"- Enemy: `{metrics.EnemyType}` x{metrics.EnemyCount}");
            builder.AppendLine($"- Difficulty: `{metrics.Difficulty}`");
            builder.AppendLine($"- Starting Credits: `{metrics.StartingCredits}`");
            builder.AppendLine($"- Winner: `{metrics.Winner}`");
            builder.AppendLine($"- Result: {metrics.ResultReason}");
            builder.AppendLine($"- Overall classification: `{metrics.OverallClassification}`");
            builder.AppendLine();
            builder.AppendLine("## Classifications");
            builder.AppendLine();
            builder.AppendLine($"- Match duration: `{metrics.MatchDurationClassification}`");
            builder.AppendLine($"- Economy activity: `{metrics.EconomyActivityClassification}`");
            builder.AppendLine($"- Casualties: `{metrics.CasualtyClassification}`");
            builder.AppendLine();
            builder.AppendLine("## Runtime Snapshot");
            builder.AppendLine();
            builder.AppendLine($"- Sampled duration seconds: `{metrics.SampledDurationSeconds:0.##}`");
            builder.AppendLine($"- Oil extracted: `{metrics.OilExtracted}`");
            builder.AppendLine($"- Fuel produced: `{metrics.FuelProduced}`");
            builder.AppendLine($"- Buildings built: `{metrics.BuildingsBuilt}`");
            builder.AppendLine($"- Soldiers ordered: `{metrics.SoldiersOrdered}`");
            builder.AppendLine($"- Vehicles ordered: `{metrics.VehiclesOrdered}`");
            builder.AppendLine($"- Ammo ordered: `{metrics.AmmoOrdered}`");
            builder.AppendLine($"- Own soldiers dead: `{metrics.OwnSoldiersDead}`");
            builder.AppendLine($"- Enemy soldiers dead: `{metrics.EnemySoldiersDead}`");
            builder.AppendLine();
            builder.AppendLine("## Materials And Field Fabrication");
            builder.AppendLine();
            builder.AppendLine($"- Current / capacity: `{metrics.MaterialsCurrent} / {metrics.MaterialsCapacity}`");
            builder.AppendLine($"- Fabricated: `{metrics.MaterialsFabricated}`");
            builder.AppendLine($"- Imported: `{metrics.MaterialsImported}`");
            builder.AppendLine($"- Rewarded: `{metrics.MaterialsRewarded}`");
            builder.AppendLine($"- Exported: `{metrics.MaterialsExported}`");
            builder.AppendLine($"- Gross spent including exports: `{metrics.MaterialsGrossSpent}`");
            builder.AppendLine($"- Construction spent: `{metrics.MaterialsConstructionSpent}`");
            builder.AppendLine($"- Repair spent: `{metrics.MaterialsRepairSpent}`");
            builder.AppendLine($"- Infrastructure spent: `{metrics.MaterialsInfrastructureSpent}`");
            builder.AppendLine($"- Upgrade spent: `{metrics.MaterialsUpgradeSpent}`");
            builder.AppendLine($"- Fabrication active seconds: `{metrics.FabricationActiveSeconds:0.##}`");
            builder.AppendLine($"- Fabrication blocked seconds: `{metrics.FabricationBlockedSeconds:0.##}`");
            builder.AppendLine($"- Blocked, no Oil input: `{metrics.FabricationNoOilInputBlockedSeconds:0.##}`");
            builder.AppendLine($"- Blocked, Materials capacity full: `{metrics.FabricationMaterialsCapacityFullBlockedSeconds:0.##}`");
            builder.AppendLine($"- Blocked, no Oil route: `{metrics.FabricationNoOilRouteBlockedSeconds:0.##}`");
            builder.AppendLine($"- Production disabled seconds: `{metrics.FabricationProductionDisabledSeconds:0.##}`");
            builder.AppendLine($"- Building disabled seconds: `{metrics.FabricationBuildingDisabledSeconds:0.##}`");
            builder.AppendLine();
            builder.AppendLine("## Fuel Logistics");
            builder.AppendLine();
            builder.AppendLine($"- Faction id: `{metrics.FuelLogisticsFactionId}`");
            builder.AppendLine($"- Telemetry version: `{metrics.FuelLogisticsTelemetryVersion}`");
            builder.AppendLine($"- Tray route assignments: `{metrics.TrayRouteAssignmentCount}`");
            builder.AppendLine($"- Tray route reassignments: `{metrics.TrayRouteReassignmentCount}`");
            builder.AppendLine($"- Tray route failures: `{metrics.TrayRouteFailureCount}`");
            builder.AppendLine($"- Oil delivered to refineries: `{metrics.OilDeliveredToRefineries:0.##}`");
            builder.AppendLine($"- Oil delivered to fabrication depots: `{metrics.OilDeliveredToFabricationDepots:0.##}`");
            builder.AppendLine();
            builder.AppendLine("## Resource Exchange");
            builder.AppendLine();
            builder.AppendLine($"- Source mode: `{metrics.ResourceExchangeSourceMode ?? "Unspecified"}`");
            builder.AppendLine($"- Route summary: `{metrics.ResourceExchangeRouteSummary ?? "None"}`");
            builder.AppendLine($"- Started jobs: `{metrics.ResourceExchangeStartedCount}`");
            builder.AppendLine($"- Completed jobs: `{metrics.ResourceExchangeCompletedCount}`");
            builder.AppendLine($"- Cancelled jobs: `{metrics.ResourceExchangeCancelledCount}`");
            builder.AppendLine($"- Blocked jobs: `{metrics.ResourceExchangeBlockedCount}`");
            builder.AppendLine($"- Rush actions: `{metrics.ResourceExchangeRushCount}`");
            builder.AppendLine($"- Input amount planned: `{metrics.ResourceExchangeInputAmount}`");
            builder.AppendLine($"- Output amount planned: `{metrics.ResourceExchangeOutputAmount}`");
            builder.AppendLine($"- Total duration seconds: `{metrics.ResourceExchangeDurationSeconds:0.##}`");
            builder.AppendLine($"- Completion rate: `{metrics.ResourceExchangeCompletionRatePercent:0.##}%`");
            builder.AppendLine($"- Credits delta: `{metrics.ResourceExchangeCreditsDelta}`");
            builder.AppendLine($"- Materials delta: `{metrics.ResourceExchangeMaterialsDelta}`");
            builder.AppendLine($"- Oil delta: `{metrics.ResourceExchangeOilDelta}`");
            builder.AppendLine($"- Fuel delta: `{metrics.ResourceExchangeFuelDelta}`");
            builder.AppendLine($"- Rush Tickets delta: `{metrics.ResourceExchangeRushTicketsDelta}`");
            builder.AppendLine($"- Net resource delta: `{metrics.ResourceExchangeNetResourceDelta}`");
            builder.AppendLine();
            builder.AppendLine("This report is a balance tuning artifact, not a build-validation gate.");
            return builder.ToString();
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "balance_probe";

            string result = value.Trim().ToLowerInvariant();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');

            return result.Replace(' ', '_');
        }
    }
}
