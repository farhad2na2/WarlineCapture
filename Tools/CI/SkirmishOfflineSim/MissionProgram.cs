using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Game.Runtime;
using Game.Skirmish.Contracts;
using static Program;
using K = Game.Runtime.SkirmishMissionOrchestrationKernel;

// Explicit world-fact replay. No AI, economy, native tombstones, persistence or scene startup.
static class MissionProgram
{
    sealed class Mission
    {
        public string SessionId, SetupHash;
        public SkirmishObjectiveKind Kind;
        public SkirmishSessionPhase Phase;
        public bool Legacy;
        public float Elapsed;
        public int Deadline, SetupDeadline;
        public byte Playing, Paused;
        public double CollectorLast, CollectorFraction;
        public SkirmishFrontlineState Frontline;
        public SkirmishBreakthroughState Breakthrough;
        public SkirmishConvoyState Convoy;
        public SkirmishBaseAssaultFacts BaseFacts;
        public SkirmishOutcomeKind Outcome;
        public SkirmishEndReasonKind Reason;
        public bool Frozen;
        public int FreezeCount;
        public readonly HashSet<string> Departed = new(StringComparer.Ordinal);

        public object Snapshot() => new {
            Phase, ElapsedSeconds = Elapsed, DeadlineSeconds = Deadline, Playing, Paused,
            CollectorLastSeconds = CollectorLast, CollectorFractionalTicks = CollectorFraction,
            Outcome, Reason, Terminal = Outcome != SkirmishOutcomeKind.None, Frozen, FreezeCount,
            Result = Frozen ? new { SessionId, SetupHash, Outcome, Reason, SaveAcknowledged = false } : null,
            Departed = Departed.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            Objective = Kind == SkirmishObjectiveKind.FrontlineControl ? (object)Frontline :
                Kind == SkirmishObjectiveKind.Breakthrough ? Breakthrough :
                Kind == SkirmishObjectiveKind.ConvoyEscort ? Convoy : BaseFacts
        };
    }

    static T WorldFacts<T>(JsonElement data, params string[] derived)
    {
        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !derived.Contains(f.Name)).ToArray();
        Shape(data, fields.Select(f => f.Name).ToArray());
        foreach (var field in fields) FactsShape(data.GetProperty(field.Name), field.FieldType);
        return data.Deserialize<T>(Json);
    }

    static SkirmishSessionPhase Phase(JsonElement value)
    {
        Need(value.ValueKind == JsonValueKind.String && Enum.GetNames(typeof(SkirmishSessionPhase)).Contains(value.GetString()), "Exact named mission phase required");
        return Enum.Parse<SkirmishSessionPhase>(value.GetString());
    }

    static object Replay(JsonElement scenario)
    {
        Shape(scenario, new[] { "id", "family", "size", "session_id", "setup_hash", "initial_phase", "legacy", "deadline_seconds", "area_ids", "original_ids", "steps", "expect" });
        Need(!string.IsNullOrWhiteSpace(scenario.GetProperty("id").GetString()), "Case ID required");
        Need(SkirmishObjectiveIds.TryParse(scenario.GetProperty("family").GetString(), out var kind), "Unknown objective family");
        Need(SkirmishIdParsing.TryParseSize(scenario.GetProperty("size").GetString(), out var size), "Invalid size");
        var mission = new Mission {
            Kind = kind, SessionId = scenario.GetProperty("session_id").GetString(), SetupHash = scenario.GetProperty("setup_hash").GetString(),
            Phase = Phase(scenario.GetProperty("initial_phase")), Legacy = scenario.GetProperty("legacy").GetBoolean(),
            SetupDeadline = scenario.GetProperty("deadline_seconds").GetInt32()
        };
        Need(!string.IsNullOrWhiteSpace(mission.SessionId) && !string.IsNullOrWhiteSpace(mission.SetupHash), "Session/setup identity required");
        Need(mission.SetupDeadline >= 0, "Invalid setup deadline");
        if (kind == SkirmishObjectiveKind.FrontlineControl) {
            var areas = Strings(scenario.GetProperty("area_ids"), 3, "area_ids");
            Strings(scenario.GetProperty("original_ids"), 0, "original_ids");
            mission.Frontline = SkirmishFrontlineReducer.Create(size, areas[0], areas[1], areas[2]);
        } else if (kind == SkirmishObjectiveKind.Breakthrough) {
            var areas = Strings(scenario.GetProperty("area_ids"), 2, "area_ids");
            var ids = Strings(scenario.GetProperty("original_ids"), SkirmishBreakthroughReducer.OriginalCount, "original_ids");
            mission.Breakthrough = SkirmishBreakthroughReducer.Create(size, areas[0], areas[1], ids);
        } else if (kind == SkirmishObjectiveKind.ConvoyEscort) {
            var areas = Strings(scenario.GetProperty("area_ids"), 1, "area_ids");
            var ids = Strings(scenario.GetProperty("original_ids"), SkirmishConvoyReducer.OriginalCount, "original_ids");
            mission.Convoy = SkirmishConvoyReducer.Create(size, areas[0], ids);
        } else {
            Strings(scenario.GetProperty("area_ids"), 0, "area_ids");
            Strings(scenario.GetProperty("original_ids"), 0, "original_ids");
        }
        var rows = scenario.GetProperty("steps");
        Need(rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() > 0 && rows.GetArrayLength() <= 10000, "1..10000 steps required");
        var snapshots = new List<object>();
        int index = 0, frames = 0;
        foreach (var row in rows.EnumerateArray()) {
            try {
                Shape(row, new[] { "delta_seconds", "paused", "surrender", "world_facts" }, "repeat", "expect", "phase");
                float dt = row.GetProperty("delta_seconds").GetSingle();
                Need(float.IsFinite(dt) && dt >= 0, "Finite nonnegative delta required");
                bool paused = row.GetProperty("paused").GetBoolean(), surrender = row.GetProperty("surrender").GetBoolean();
                int repeat = row.TryGetProperty("repeat", out var count) ? count.GetInt32() : 1;
                Need(repeat >= 1 && repeat <= 100000 && frames <= 100000-repeat, "Per-case frame limit is 100000");
                frames += repeat;
                if (row.TryGetProperty("phase", out var phase)) {
                    Need(!mission.Frozen, "Cannot override a frozen mission phase");
                    mission.Phase = Phase(phase);
                }
                var world = row.GetProperty("world_facts");
                var ba = default(SkirmishBaseAssaultFacts);
                var fc = default(SkirmishFrontlineTickFacts);
                var bt = default(SkirmishBreakthroughTickFacts);
                var ce = default(SkirmishConvoyTickFacts);
                if (kind == SkirmishObjectiveKind.BaseAssault)
                    ba = WorldFacts<SkirmishBaseAssaultFacts>(world, "Playing", "Paused", "Surrender", "ElapsedSeconds", "DeadlineSeconds");
                else if (kind == SkirmishObjectiveKind.FrontlineControl)
                    fc = WorldFacts<SkirmishFrontlineTickFacts>(world, "Playing", "Paused", "SimulationDeltaTicks");
                else if (kind == SkirmishObjectiveKind.Breakthrough)
                    bt = WorldFacts<SkirmishBreakthroughTickFacts>(world, "Playing", "Paused", "SimulationDeltaTicks");
                else ce = WorldFacts<SkirmishConvoyTickFacts>(world, "Playing", "Paused", "SimulationDeltaTicks");
                var departures = new List<string>();
                for (int i = 0; i < repeat; i++) {
                    K.ProjectClock(mission.Legacy, mission.Phase, paused, dt, mission.SetupDeadline,
                        ref mission.Elapsed, ref mission.Deadline, ref mission.Playing, ref mission.Paused);
                    Need(float.IsFinite(mission.Elapsed), "Mission clock overflow");
                    if (kind == SkirmishObjectiveKind.BaseAssault && K.ShouldRefreshFacts(mission.Legacy, mission.Phase)) {
                        mission.BaseFacts = ba;
                        mission.BaseFacts.Playing = mission.Playing != 0;
                        mission.BaseFacts.Paused = mission.Paused != 0;
                        mission.BaseFacts.ElapsedSeconds = mission.Elapsed;
                        mission.BaseFacts.DeadlineSeconds = mission.Deadline;
                        // Native fact projection publishes BA terminal facts before the BA system reads surrender.
                        if (mission.Phase == SkirmishSessionPhase.Playing && mission.Outcome == SkirmishOutcomeKind.None) {
                            if (!K.TryEvaluateBaseAssault(in mission.BaseFacts, out mission.Outcome, out mission.Reason)) {
                                var later = mission.BaseFacts; later.Surrender = surrender;
                                K.TryEvaluateBaseAssault(in later, out mission.Outcome, out mission.Reason);
                            }
                        }
                    }
                    if (K.ShouldTickNativeObjective(mission.Legacy, kind, mission.Phase, mission.Outcome != SkirmishOutcomeKind.None)) {
                        double nextLast = mission.CollectorLast, nextFraction = mission.CollectorFraction;
                        if (!surrender) {
                            Need(K.TryProjectCollectorClock(mission.CollectorLast, mission.CollectorFraction, mission.Elapsed,
                                mission.Playing, mission.Paused, out nextLast, out nextFraction, out long delta, out string error), error);
                            fc.Playing = bt.Playing = ce.Playing = mission.Playing != 0;
                            fc.Paused = bt.Paused = ce.Paused = mission.Paused != 0;
                            fc.SimulationDeltaTicks = bt.SimulationDeltaTicks = ce.SimulationDeltaTicks = delta;
                        }
                        Need(K.TryDispatch(kind, surrender, ref mission.Frontline, ref mission.Breakthrough, ref mission.Convoy,
                            in fc, in bt, in ce, out var leaving, out string dispatchError), dispatchError);
                        mission.CollectorLast = nextLast; mission.CollectorFraction = nextFraction;
                        foreach (string id in leaving) {
                            Need(mission.Departed.Add(id), "Duplicate successful departure: " + id);
                            departures.Add(id);
                        }
                        Need(K.TryGetOutcome(kind, in mission.Frontline, in mission.Breakthrough, in mission.Convoy,
                            out mission.Outcome, out mission.Reason), "Unsupported objective result");
                    }
                    var freeze = K.DecideFreeze(mission.Legacy, mission.Outcome != SkirmishOutcomeKind.None, mission.Frozen);
                    if (freeze != SkirmishResultFreezeDecision.None) {
                        if (freeze == SkirmishResultFreezeDecision.CreateFrozen) { mission.Frozen = true; mission.FreezeCount++; }
                        mission.Phase = SkirmishSessionPhase.Finished;
                        mission.Playing = 0; mission.Paused = 1;
                    }
                }
                var snapshot = JsonSerializer.SerializeToElement(mission.Snapshot(), Json);
                if (row.TryGetProperty("expect", out var expected)) Expect(expected, snapshot, "step " + index);
                snapshots.Add(new { step = index, state = snapshot, departures });
            } catch (Exception error) { throw new ArgumentException("step " + index + ": " + error.Message, error); }
            index++;
        }
        var final = JsonSerializer.SerializeToElement(mission.Snapshot(), Json);
        var expectation = scenario.GetProperty("expect");
        Need(expectation.ValueKind == JsonValueKind.Object && expectation.EnumerateObject().Any(), "Nonempty final expectation required");
        Expect(expectation, final, "final");
        return new { frames, state = final, snapshots };
    }

    public static int Main(string[] args)
    {
        var watch = Stopwatch.StartNew();
        try {
            Need(args.Length == 1, "Expected trace JSON path");
            Need(new System.IO.FileInfo(args[0]).Length <= 16*1024*1024, "Trace exceeds 16 MiB");
            using var document = JsonDocument.Parse(System.IO.File.ReadAllBytes(args[0]));
            var root = document.RootElement; UniqueKeys(root); Shape(root, new[] { "schema_version", "cases" });
            Need(root.GetProperty("schema_version").GetInt32() == 1, "Unsupported schema");
            var cases = root.GetProperty("cases");
            Need(cases.ValueKind == JsonValueKind.Array && cases.GetArrayLength() >= 1 && cases.GetArrayLength() <= 10000, "1..10000 cases required");
            var ids = new HashSet<string>(); var results = new List<object>(); int failed = 0;
            foreach (var scenario in cases.EnumerateArray()) {
                string id = scenario.ValueKind == JsonValueKind.Object && scenario.TryGetProperty("id", out var identifier) ? identifier.ToString() : "<invalid>";
                try { Need(ids.Add(id), "Duplicate case ID"); results.Add(new { id, status = "passed", replay = Replay(scenario) }); }
                catch (Exception error) { failed++; results.Add(new { id, status = "failed", error = error.Message }); }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { schema_version = 1, scope = "ProductionMissionOrchestrationReplay",
                result = failed == 0 ? "passed" : "failed", passed = results.Count-failed, failed,
                elapsed_seconds = watch.Elapsed.TotalSeconds, native_runs = 0, full_battle_simulated = false,
                mission_qualified = false, world_facts_supplied = true, persistence_executed = false, cases = results }, Json));
            return failed == 0 ? 0 : 1;
        } catch (Exception error) {
            Console.WriteLine(JsonSerializer.Serialize(new { result = "failed", error = error.Message,
                native_runs = 0, full_battle_simulated = false, mission_qualified = false }, Json)); return 1;
        }
    }
}
