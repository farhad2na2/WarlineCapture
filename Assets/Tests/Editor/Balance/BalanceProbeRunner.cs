using UnityEditor;
using UnityEngine;

public static class BalanceProbeRunner
{
    [MenuItem("WarlineCapture/Balance/Run All Balance Probes")]
    public static void RunAllBalanceProbes()
    {
        RunQuickCustomDefaultMedium();
        RunQuickCustomHardSwarm();
    }

    [MenuItem("WarlineCapture/Balance/Run Quick Custom Default Medium Probe")]
    public static void RunQuickCustomDefaultMedium()
    {
        BalanceReportWriter.ReportPaths paths = QuickCustomBalanceProbe.RunDefaultMediumReport();
        Debug.Log($"[Balance] Wrote Quick Custom Default Medium report: {paths.JsonPath} and {paths.MarkdownPath}");
    }

    [MenuItem("WarlineCapture/Balance/Run Quick Custom Hard Swarm Probe")]
    public static void RunQuickCustomHardSwarm()
    {
        BalanceReportWriter.ReportPaths paths = QuickCustomBalanceProbe.RunHardSwarmReport();
        Debug.Log($"[Balance] Wrote Quick Custom Hard Swarm report: {paths.JsonPath} and {paths.MarkdownPath}");
    }
}
