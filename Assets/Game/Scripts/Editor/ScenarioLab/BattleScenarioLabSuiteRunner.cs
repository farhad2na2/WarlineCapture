#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class BattleScenarioLabSuiteRunner
{
    public const string SuiteIndexPath = "/private/tmp/warline-scenario-lab-suite-index.json";
    private const string ScenarioAssetSearchRoot = "Assets/Game/Configs/ScenarioLab";

    [MenuItem("Warline Capture/Scenario Lab/Run Scenario Suite")]
    public static void RunScenarioSuite()
    {
        try
        {
            BattleScenarioSuiteEntry[] entries = RunSuite();
            File.WriteAllText(SuiteIndexPath, ToJson(entries));

            bool failed = false;
            for (int i = 0; i < entries.Length; i++)
                failed |= !entries[i].Passed && !entries[i].Skipped;

            if (failed)
            {
                Debug.LogError($"[BattleScenarioLab] Scenario suite failed. Index: {SuiteIndexPath}");
                Exit(1);
                return;
            }

            Debug.Log($"[BattleScenarioLab] Scenario suite passed. Index: {SuiteIndexPath}");
            Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleScenarioLab] Scenario suite exception: {ex}");
            Exit(1);
        }
    }

    public static BattleScenarioSuiteEntry[] RunSuite()
    {
        string[] guids = AssetDatabase.FindAssets("t:BattleScenarioDefinition", new[] { ScenarioAssetSearchRoot });
        var entries = new List<BattleScenarioSuiteEntry>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            BattleScenarioDefinition definition = AssetDatabase.LoadAssetAtPath<BattleScenarioDefinition>(assetPath);
            if (definition == null)
                continue;

            entries.Add(RunDefinition(definition, assetPath));
        }

        entries.Sort((a, b) => string.CompareOrdinal(a.ScenarioId, b.ScenarioId));
        return entries.ToArray();
    }

    private static BattleScenarioSuiteEntry RunDefinition(BattleScenarioDefinition definition, string assetPath)
    {
        string scenarioId = definition.ScenarioId;
        string reportPath = BuildReportPath(scenarioId);
        BattleScenarioResult result;
        try
        {
            result = BattleScenarioLabRuntimeRunner.RunDefinition(definition);
        }
        catch (NotSupportedException)
        {
            return new BattleScenarioSuiteEntry(
                scenarioId,
                assetPath,
                string.Empty,
                true,
                true,
                "NoRunnerRegistered");
        }

        File.WriteAllText(reportPath, BattleScenarioReportJson.ToJson(result));
        return new BattleScenarioSuiteEntry(
            result.ScenarioId,
            assetPath,
            reportPath,
            result.Passed,
            false,
            result.FailureReason.ToString());
    }

    private static string BuildReportPath(string scenarioId)
    {
        string fileName = SanitizeFileName(string.IsNullOrWhiteSpace(scenarioId) ? "unknown-scenario" : scenarioId);
        return $"/private/tmp/warline-scenario-lab-{fileName}.json";
    }

    private static string ToJson(BattleScenarioSuiteEntry[] entries)
    {
        var builder = new StringBuilder(1024);
        builder.Append("{\n");
        builder.Append("  \"GeneratedAtUtc\": \"").Append(Escape(DateTime.UtcNow.ToString("O"))).Append("\",\n");
        builder.Append("  \"SuiteIndexPath\": \"").Append(Escape(SuiteIndexPath)).Append("\",\n");
        builder.Append("  \"Scenarios\": [\n");
        for (int i = 0; i < entries.Length; i++)
        {
            if (i > 0)
                builder.Append(",\n");

            BattleScenarioSuiteEntry entry = entries[i];
            builder.Append("    {\n");
            builder.Append("      \"ScenarioId\": \"").Append(Escape(entry.ScenarioId)).Append("\",\n");
            builder.Append("      \"AssetPath\": \"").Append(Escape(entry.AssetPath)).Append("\",\n");
            builder.Append("      \"ReportPath\": \"").Append(Escape(entry.ReportPath)).Append("\",\n");
            builder.Append("      \"Passed\": ").Append(entry.Passed ? "true" : "false").Append(",\n");
            builder.Append("      \"Skipped\": ").Append(entry.Skipped ? "true" : "false").Append(",\n");
            builder.Append("      \"FailureReason\": \"").Append(Escape(entry.FailureReason)).Append("\"\n");
            builder.Append("    }");
        }
        builder.Append("\n  ]\n");
        builder.Append("}\n");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return value;
    }

    private static void Exit(int code)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(code);
    }
}

public readonly struct BattleScenarioSuiteEntry
{
    public readonly string ScenarioId;
    public readonly string AssetPath;
    public readonly string ReportPath;
    public readonly bool Passed;
    public readonly bool Skipped;
    public readonly string FailureReason;

    public BattleScenarioSuiteEntry(
        string scenarioId,
        string assetPath,
        string reportPath,
        bool passed,
        bool skipped,
        string failureReason)
    {
        ScenarioId = scenarioId;
        AssetPath = assetPath;
        ReportPath = reportPath;
        Passed = passed;
        Skipped = skipped;
        FailureReason = failureReason;
    }
}
#endif
