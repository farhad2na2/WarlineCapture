using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Runtime.Combat;
using Game.Runtime.Movement;
using static CombatProgram;

// Explicit snapshot/route replay, not an AI, pathfinder or native readiness verdict.
public static class SequenceProgram
{
    public struct Target
    {
        public string Id;
        public int Health;
        public bool Exists, HasHealth, CoverEligible;
        public ushort CoverPermille;
    }
    public struct Candidate
    {
        public string Target;
        public DirectDamageSource Source;
        public bool AttackerExists;
        public StandardAttackInput Input;
    }
    public struct DamageBatch
    {
        public float DeltaTime, CellSize;
        public Target[] Targets;
        public Candidate[] Candidates;
    }
    public struct Point { public int CellX, CellZ; public float X, Z; }
    public struct MovementFrame
    {
        public float DeltaTime;
        public bool PathAdmissionKnown, PathAdmitted, SettleCellKnown;
        public int SettleCellX, SettleCellZ;
    }
    public struct InfantryTrace
    {
        public InfantryPathState InitialState;
        public float Speed, ArriveDistance, CellSize;
        public bool GroupedManualMove;
        public Point[] Path;
        public MovementFrame[] Frames;
    }
    public struct MovementGate
    {
        public int Start, Length, PathIndex, PoolLength;
        public bool FactsKnown, CampaignGuidedMove, CellBlocked, OccupantBlocks;
        public float DeltaTime, CurrentSpeed, StallSeconds;
    }

    static T Read<T>(JsonElement input)
    {
        FactsShape(input, typeof(T));
        var value = input.Deserialize<T>(Json);
        FiniteFields(value);
        return value;
    }
    static void Timing(float dt, float cell)
    { Need(float.IsFinite(dt) && dt >= 0, "Nonnegative finite delta time required"); Need(float.IsFinite(cell) && cell > 0, "Positive finite cell size required"); }
    static void Count(int count, int max, string name) => Need(count >= 1 && count <= max, name + " count outside1.." + max);

    public static object Run(JsonElement row, string kind)
    {
        Shape(row, new[] { "id", "kind", "input", "expect" });
        Need(!string.IsNullOrWhiteSpace(row.GetProperty("id").GetString()), "Case ID required");
        object result = kind switch
        {
            "damage_sequence" => Damage(Read<DamageBatch>(row.GetProperty("input"))),
            "infantry_trace" => Movement(Read<InfantryTrace>(row.GetProperty("input"))),
            "movement_gate" => Gate(Read<MovementGate>(row.GetProperty("input"))),
            _ => throw new ArgumentException("Unknown replay kind: " + kind)
        };
        var snapshot = JsonSerializer.SerializeToElement(result, Json);
        var expected = row.GetProperty("expect");
        Need(expected.ValueKind == JsonValueKind.Object && expected.EnumerateObject().Any(), "Nonempty expectation required");
        Expect(expected, snapshot, kind);
        return snapshot;
    }

    static object Damage(DamageBatch batch)
    {
        Timing(batch.DeltaTime, batch.CellSize);
        Count(batch.Targets.Length, 10000, "Targets"); Count(batch.Candidates.Length, 10000, "Candidates");
        var targets = new Dictionary<string, Target>(StringComparer.Ordinal);
        var predicted = new Dictionary<string, int>(StringComparer.Ordinal);
        var aggregates = new Dictionary<string, DirectDamageAggregate>(StringComparer.Ordinal);
        foreach (var target in batch.Targets)
        {
            Need(!string.IsNullOrWhiteSpace(target.Id) && targets.TryAdd(target.Id, target), "Duplicate/empty target identity");
            predicted.Add(target.Id, target.Health);
        }
        var seen = new HashSet<(int, int)>();
        var shots = new List<object>();
        foreach (var candidate in batch.Candidates)
        {
            Need(targets.TryGetValue(candidate.Target, out var target), "Unknown target identity");
            Need(seen.Add((candidate.Source.Index, candidate.Source.Version)), "Repeated attacker in one native snapshot");
            var input = candidate.Input;
            Need(input.TargetHealth == target.Health && (input.TargetExists != 0) == target.Exists &&
                 (input.TargetHasHealth != 0) == target.HasHealth, "Candidate target facts disagree with frozen snapshot");
            Need(input.AttackerX == candidate.Source.X && input.AttackerZ == candidate.Source.Z, "Attacker positions disagree");
            var plan = StandardAttackPlanner.Plan(input, batch.DeltaTime, batch.CellSize);
            var decision = DirectDamageSequencer.Decide(plan.Kind, candidate.AttackerExists, predicted[target.Id]);
            var state = DirectDamageSequencer.CommittedState(input.State, plan, decision);
            int damage = 0;
            if (decision == StandardAttackCommit.Shot && plan.Damage > 0)
            {
                damage = DirectDamageSequencer.ApplyToTarget(plan.Damage, target.CoverEligible, target.CoverPermille);
                bool hasAggregate = aggregates.TryGetValue(target.Id, out var aggregate);
                predicted[target.Id] = DirectDamageSequencer.Accumulate(ref aggregate, hasAggregate,
                    predicted[target.Id], damage, candidate.Source, plan.ShowTracer);
                aggregates[target.Id] = aggregate;
            }
            shots.Add(new { Source = candidate.Source, candidate.Target, PlanKind = plan.Kind, Decision = decision,
                State = state, Damage = damage, ShowTracer = decision == StandardAttackCommit.Shot ? plan.ShowTracer : 0 });
        }
        var finalTargets = batch.Targets.Select(target =>
        {
            bool hasAggregate = aggregates.TryGetValue(target.Id, out var aggregate);
            bool applies = hasAggregate && target.Exists && target.HasHealth && target.Health > 0;
            return new { target.Id, InitialHealth = target.Health, PredictedHealth = predicted[target.Id],
                Health = applies ? DirectDamageSequencer.RemainingHealth(target.Health, aggregate.TotalDamage) : target.Health,
                HasAggregate = hasAggregate, Aggregate = aggregate };
        }).ToArray();
        return new { Shots = shots, Targets = finalTargets, SnapshotOrderPreserved = true };
    }

    static object Gate(MovementGate gate)
    {
        Timing(gate.DeltaTime, 1);
        Need(gate.FactsKnown, "Missing external grid/occupant facts");
        var range = GridPathMovementKernel.EvaluateRange(gate.Start, gate.Length, gate.PathIndex, gate.PoolLength);
        string action = "AdmitPathFollow";
        if (range == GridPathRangeDecision.NoWork) action = "NoWork";
        else if (range == GridPathRangeDecision.InvalidRange) { action = "Repath"; gate.CurrentSpeed = gate.StallSeconds = 0; }
        else if (GridPathMovementKernel.ShouldBlockPathCell(gate.CampaignGuidedMove, gate.CellBlocked))
        { action = "Repath"; gate.CurrentSpeed = gate.StallSeconds = 0; }
        else if (GridPathMovementKernel.ShouldBlockPathCell(gate.CampaignGuidedMove, gate.OccupantBlocks))
            action = GridPathMovementKernel.AdvanceOccupiedStall(false, gate.DeltaTime, ref gate.CurrentSpeed, ref gate.StallSeconds).ToString();
        return new { Range = range, Action = action, gate.CurrentSpeed, gate.StallSeconds };
    }

    static object Movement(InfantryTrace trace)
    {
        Timing(0, trace.CellSize); Count(trace.Path.Length, 10000, "Path"); Count(trace.Frames.Length, 10000, "Frames");
        Need(trace.Speed >= 0 && trace.ArriveDistance >= 0, "Nonnegative movement speed/arrival distance required");
        Need(trace.InitialState.PathIndex >= 0 && trace.InitialState.PathIndex <= trace.Path.Length, "Invalid initial path index");
        var state = trace.InitialState;
        var frames = new List<object>();
        var final = trace.Path[trace.Path.Length - 1];
        foreach (var frame in trace.Frames)
        {
            Timing(frame.DeltaTime, trace.CellSize);
            Need(frame.PathAdmissionKnown && frame.PathAdmitted, "Path-follow entry requires explicit admitted route facts");
            var steps = new List<InfantryWaypointResult>();
            state.RemainingDistance = GridPathMovementKernel.BeginInfantryDistance(trace.Speed, frame.DeltaTime);
            for (int i = 0; i < GridPathMovementKernel.InfantryMaxWaypointAdvancesPerFrame && state.RemainingDistance > 0; i++)
            {
                if ((uint)state.PathIndex >= (uint)trace.Path.Length) break;
                var point = trace.Path[state.PathIndex];
                var input = new InfantryWaypointInput
                {
                    PathLength = trace.Path.Length, TargetCellX = point.CellX, TargetCellZ = point.CellZ,
                    PoolIndexValid = true, FinalPoolIndexValid = true, GroupedManualMove = trace.GroupedManualMove,
                    TargetX = point.X, TargetZ = point.Z, FinalTargetX = final.X, FinalTargetZ = final.Z,
                    ArriveDistance = trace.ArriveDistance, CellSize = trace.CellSize
                };
                var result = GridPathMovementKernel.StepInfantryWaypoint(state, input);
                state = result.State;
                if (result.SettleCurrentCell)
                {
                    Need(frame.SettleCellKnown, "Grouped arrival requires externally resolved current grid cell");
                    state.CellX = frame.SettleCellX; state.CellZ = frame.SettleCellZ;
                }
                steps.Add(result);
                if (result.Action == InfantryWaypointAction.Stop || result.Action == InfantryWaypointAction.GroupedStop) break;
            }
            frames.Add(new { State = state, Steps = steps });
        }
        return new { State = state, Frames = frames, RouteFactsSupplied = true, RouteLegalityProven = false };
    }
}
