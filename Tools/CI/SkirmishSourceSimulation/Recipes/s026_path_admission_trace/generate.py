"""Expose the existing path owner's computed admission in an experimental source host."""
import hashlib
from pathlib import Path

SOURCE = 'Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs'
OUTPUT = 'SourcePathfindingAdmissionObservation.cs'
BUDGET_SOURCE = 'Assets/Game/Scripts/Systems/UnitPathfindingBudget.cs'
BUDGET_OUTPUT = 'SourcePathfindingBudgetObservation.cs'
ANCHOR = '#if UNITY_EDITOR\n            AdmissionObserved?.Invoke(Time.frameCount, admission);\n#endif'
BUDGET_ANCHOR = '            PathRequestBudgetKernel.ReportCompletedJob(ref _state,pendingFrames,pendingWallTime,requestCount,manualRequestCount,vehicleLikeCount,pendingRequestBudget);'
PROBE = '''
namespace Game.Runtime
{
    // Opt-in source-host observation of the already computed admission. It never
    // reads the pending job handle and writes buffered observations only at exit.
    internal static class SourcePathAdmissionTrace
    {
        private const int FirstFrame = 1300, LastFrame = 1500, Capacity = 256;
        private struct Row { internal int Frame; internal Pathfinding.PathJobAdmission Admission; }
        private struct BudgetRow { internal int Frame, PendingFrames, Requests, Manual, Vehicles, PendingBudget, Before, After;
            internal double WallSeconds; }
        private static readonly bool Enabled = System.Environment.GetEnvironmentVariable("WARLINE_SOURCE_PATH_ADMISSION_TRACE") == "1";
        private static readonly Row[] Rows = Enabled ? new Row[Capacity] : null;
        private static readonly BudgetRow[] Budgets = Enabled ? new BudgetRow[Capacity] : null;
        private static int count;
        private static int budgetCount;
        private static bool overflow, budgetOverflow;
        static SourcePathAdmissionTrace()
        {
            if (Enabled) System.AppDomain.CurrentDomain.ProcessExit += (sender, args) => Write();
        }
        internal static void Observe(int frame, Pathfinding.PathJobAdmission admission)
        {
            if (!Enabled || frame < FirstFrame || frame > LastFrame) return;
            if (count == Capacity) { overflow = true; return; }
            Rows[count++] = new Row { Frame = frame, Admission = admission };
        }
        internal static void ObserveBudget(int frame, Pathfinding.PathRequestBudgetState before,
            Pathfinding.PathRequestBudgetState after, int pendingFrames, double wallSeconds,
            int requests, int manual, int vehicles, int pendingBudget)
        {
            if (!Enabled || frame < FirstFrame || frame > LastFrame) return;
            if (budgetCount == Capacity) { budgetOverflow = true; return; }
            Budgets[budgetCount++] = new BudgetRow { Frame = frame, PendingFrames = pendingFrames,
                Requests = requests, Manual = manual, Vehicles = vehicles, PendingBudget = pendingBudget,
                Before = before.AdaptiveRequestsPerFrame, After = after.AdaptiveRequestsPerFrame, WallSeconds = wallSeconds };
        }
        private static void Write()
        {
            try
            {
                var rows = new Newtonsoft.Json.Linq.JArray();
                for (int i = 0; i < count; i++) rows.Add(new Newtonsoft.Json.Linq.JObject {
                    ["engine_frame"] = Rows[i].Frame, ["actual_admission"] = Rows[i].Admission.ToString() });
                var budgets = new Newtonsoft.Json.Linq.JArray();
                for (int i = 0; i < budgetCount; i++) budgets.Add(new Newtonsoft.Json.Linq.JObject {
                    ["engine_frame"] = Budgets[i].Frame, ["pending_frames"] = Budgets[i].PendingFrames,
                    ["pending_wall_seconds"] = Budgets[i].WallSeconds, ["request_count"] = Budgets[i].Requests,
                    ["manual_request_count"] = Budgets[i].Manual, ["vehicle_count"] = Budgets[i].Vehicles,
                    ["pending_request_budget"] = Budgets[i].PendingBudget,
                    ["adaptive_budget_before"] = Budgets[i].Before, ["adaptive_budget_after"] = Budgets[i].After });
                var output = new Newtonsoft.Json.Linq.JObject { ["kind"] = "ActualSourcePathAdmissionObservation",
                    ["first_frame"] = FirstFrame, ["last_frame"] = LastFrame, ["overflow"] = overflow,
                    ["budget_overflow"] = budgetOverflow, ["budget_rows"] = budgets,
                    ["additional_completion_reads"] = 0, ["scope"] = "Original computed admission; source host only",
                    ["rows"] = rows };
                System.Console.Error.WriteLine("[SourcePathAdmissionTrace] " + output.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (System.Exception error) { System.Console.Error.WriteLine("[SourcePathAdmissionTrace] failure=" + error); }
        }
    }
}
'''

def generate(repo, output):
    repo = Path(repo).resolve(strict=True)
    source = repo / SOURCE
    budget_source = repo / BUDGET_SOURCE
    raw, budget_raw = source.read_bytes(), budget_source.read_bytes()
    text = raw.decode('utf-8').replace('\r\n', '\n')
    if text.count(ANCHOR) != 1:
        raise ValueError('Original single-read admission observation anchor changed')
    text = text.replace(ANCHOR, ANCHOR + '\n            SourcePathAdmissionTrace.Observe(Time.frameCount, admission);', 1)
    budget = budget_raw.decode('utf-8').replace('\r\n', '\n')
    if budget.count(BUDGET_ANCHOR) != 1:
        raise ValueError('Original completed-job budget anchor changed')
    budget = budget.replace(BUDGET_ANCHOR,
        '            var sourceObservedBefore = _state;\n' + BUDGET_ANCHOR +
        '\n            SourcePathAdmissionTrace.ObserveBudget(UnityEngine.Time.frameCount, sourceObservedBefore, _state, pendingFrames, pendingWallTime, requestCount, manualRequestCount, vehicleLikeCount, pendingRequestBudget);', 1)
    if source.read_bytes() != raw or budget_source.read_bytes() != budget_raw:
        raise ValueError('Path owner or budget changed during extraction')
    output = Path(output)
    output.mkdir(parents=True, exist_ok=False)
    generated = (text + PROBE).encode('utf-8')
    generated_budget = budget.encode('utf-8')
    (output / OUTPUT).write_bytes(generated)
    (output / BUDGET_OUTPUT).write_bytes(generated_budget)
    return {'source_pins': {str(source): hashlib.sha256(raw).hexdigest(), str(budget_source): hashlib.sha256(budget_raw).hexdigest()},
            'outputs': {OUTPUT: hashlib.sha256(generated).hexdigest(), BUDGET_OUTPUT: hashlib.sha256(generated_budget).hexdigest()}, 'experimental_only': True,
            'scope': 'Actual source admission and budget inputs/results; zero additional job completion reads; source inline jobs and wall clock are not native timing'}
