using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Game.Runtime;
using static WorldJson;

static class SquadReplay
{
    struct Context : ISquadTargetContext
    {
        public SquadTargetCandidate[] Candidates;
        public SquadTargetMember[] Members;
        public int CandidateCount => Candidates.Length;
        public int MemberCount => Members.Length;
        public SquadTargetCandidate Candidate(int i) => Candidates[i];
        public SquadTargetMember Member(int i) => Members[i];
    }
    static T Read<T>(JsonElement value)
    {
        FactsShape(value, typeof(T));
        return value.Deserialize<T>(Json);
    }
    static void Finite(params float[] values) => Need(values.All(float.IsFinite), "Finite squad coordinates/times required");
    public static object Run(JsonElement scenario)
    {
        Shape(scenario, new[] { "id", "kind", "input", "candidates", "members", "expect" });
        Need(scenario.GetProperty("kind").GetString() == "squad_target", "Unsupported squad case kind");
        var input = Read<SquadTargetInput>(scenario.GetProperty("input"));
        Finite(input.OwnBaseX, input.OwnBaseZ, input.DefenseRadius, input.ElapsedSeconds, input.FirstAttackSeconds);
        Need((input.OpposingBase.Index == 0 && input.OpposingBase.Version == 0) || (input.OpposingBase.Index > 0 && input.OpposingBase.Version > 0), "Invalid opposing base identity");
        var candidates = Read<SquadTargetCandidate[]>(scenario.GetProperty("candidates"));
        var members = Read<SquadTargetMember[]>(scenario.GetProperty("members"));
        Need(candidates.Length <= 10000 && members.Length <= 10000 && (long)candidates.Length*(members.Length+1) <= 1000000, "Squad work limit exceeded");
        var ids = new HashSet<(int,int)>();
        foreach (var c in candidates) {
            Need(c.Entity.Index > 0 && c.Entity.Version > 0 && ids.Add((c.Entity.Index,c.Entity.Version)), "Distinct nonnull candidate identities required");
            Finite(c.X,c.Z);
        }
        foreach (var m in members) Finite(m.X,m.Z,m.Range);
        var context = new Context { Candidates=candidates, Members=members };
        var result = SkirmishSquadTargetingKernel.Select(ref context, input);
        var actual = JsonSerializer.SerializeToElement(result, Json);
        Expect(scenario.GetProperty("expect"), actual, "squad target");
        return new { selection=result, candidate_order_supplied=true, squad_orders_executed=false };
    }
}
