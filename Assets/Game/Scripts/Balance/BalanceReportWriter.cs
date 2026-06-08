using System.IO;
using System.Text;
using UnityEngine;

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
