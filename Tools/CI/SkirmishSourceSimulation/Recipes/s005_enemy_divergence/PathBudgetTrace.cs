using System;
using System.Collections.Generic;
using System.IO;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Bounded in-memory tap of the existing post-decision observation. It does not
// read IsCompleted again or change budget state; flushing still affects timing.
internal sealed class S005PathBudgetTrace : IDisposable
{
    private readonly List<JObject> rows = new List<JObject>();
    private bool overflow;
    private S005PathBudgetTrace() { }
    internal static S005PathBudgetTrace Attach()
    {
        if (UnitPathfindingBudget.CompletionObserved != null) throw new InvalidOperationException("Path observation already bound");
        var trace = new S005PathBudgetTrace();
        UnitPathfindingBudget.CompletionObserved = trace.Observe;
        return trace;
    }
    private void Observe(PathRequestBudgetCompletionObservation value)
    {
        if (rows.Count >= 2048) { overflow = true; return; }
        rows.Add(new JObject { ["frame"] = UnityEngine.Time.frameCount, ["simulation_time"] = UnityEngine.Time.time,
            ["pending_frames"] = value.PendingFrames, ["pending_wall_seconds"] = value.PendingWallTime,
            ["request_count"] = value.RequestCount, ["manual_request_count"] = value.ManualRequestCount,
            ["vehicle_like_count"] = value.VehicleLikeCount, ["pending_request_budget"] = value.PendingRequestBudget,
            ["before_budget"] = value.Before.AdaptiveRequestsPerFrame, ["after_budget"] = value.After.AdaptiveRequestsPerFrame,
            ["before_stable_batches"] = value.Before.StableOneFrameBatchCount, ["after_stable_batches"] = value.After.StableOneFrameBatchCount,
            ["before_pending_reduced"] = value.Before.PendingBudgetReduced, ["after_pending_reduced"] = value.After.PendingBudgetReduced });
    }
    public void Dispose()
    {
        UnitPathfindingBudget.CompletionObserved = null;
        var directory = Environment.GetEnvironmentVariable("WARLINE_S005_ROAD_RECEIPT_DIR");
        if (String.IsNullOrEmpty(directory)) throw new InvalidOperationException("Diagnostic output directory required");
        var path = Path.Combine(directory, "path-budget-completions.jsonl");
        using var stream = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write));
        foreach (var row in rows) stream.WriteLine(row.ToString(Formatting.None));
        File.WriteAllText(Path.Combine(directory, "path-budget-window.json"), new JObject {
            ["row_count"] = rows.Count, ["overflow"] = overflow,
            ["scope"] = "SourceHostOriginalBudgetCompletionObservation_NotNativeParity" }.ToString(Formatting.Indented));
    }
}
