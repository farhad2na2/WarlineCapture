using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Operations.Contracts;
using Game.Operations.Tactical;
using static OperationsHost;

// External authoring data and public command traces. No combat rules, map generation or state injection.
internal static class OperationDraft
{
    private sealed record Group(int count, JsonElement spawn);
    private sealed record Order([property: JsonRequired] int at, [property: JsonRequired] string verb,
                                string actor = null, string target = null);
    private sealed record Probe(string name, OperationsOutcomeKind expect, Order[] orders);
    private sealed record Input(string mission, JsonElement graph, Group[] groups, Probe[] cases);
    internal sealed record Result(string name, bool passed, string expected, string outcome, string reason,
                                  int tick, List<string> commands, string error);

    internal static Result[] Run(JsonElement json, OperationsCatalogEntry entry)
    {
        var input = json.Deserialize<Input>(Json);
        var config = entry.ToDefinitionSchema();
        string id = entry.MissionId.Split('.')[1];
        string[] owned = { "GraphId", "MapId", "ForcePackage", "EnemyPackage", "Materials", "DeadlineTicks" };
        Require(!input.graph.EnumerateObject().Any(p => owned.Contains(p.Name)),
                "Map, graph ID, packages, materials and deadline come from the catalog; remove overrides");
        var graph = input.graph.Deserialize<OperationsTacticalAuthoring>(Json);
        var force = OperationsForcePackageSchema.Create(config.ForcePackage);
        var threat = OperationsThreatProfileSchema.Create(config.EnemyPackage);
        graph.GraphId = "graph.operations." + id;
        graph.MapId = entry.OperationMapId;
        graph.ForcePackage = force.Kind;
        graph.EnemyPackage = threat.Kind;
        graph.Materials = force.Materials;
        graph.DeadlineTicks = entry.HardDeadlineSeconds;
        var spawns = new List<OperationsTacticalSpawnAuthoring>(graph.Spawns);
        foreach (var group in input.groups ?? Array.Empty<Group>())
        {
            Require(group.count is >= 1 and <= 128 && spawns.Count + group.count <= 512, "Spawn group out of range");
            for (int i = 1; i <= group.count; i++)
            {
                var spawn = group.spawn.Deserialize<OperationsTacticalSpawnAuthoring>(Json);
                Require(!string.IsNullOrWhiteSpace(spawn.ObjectId), "Spawn group requires an ObjectId prefix");
                spawn.ObjectId += "." + i.ToString("00");
                spawns.Add(spawn);
            }
        }
        graph.Spawns = spawns.ToArray();
        var compile = OperationsTacticalCompiler.Compile(graph);
        Require(compile.Accepted, compile.Error);
        var def = compile.Definition;
        // The tactical compiler permits subsets. New mission drafts must preserve the catalog's full packages.
        CheckPackage(def, OperationsTacticalFaction.Player, force.Lines);
        CheckPackage(def, OperationsTacticalFaction.Hostile, threat.Lines);
        Require(input.cases is { Length: > 0 and <= 100 } &&
                input.cases.All(p => p != null && !string.IsNullOrWhiteSpace(p.name)) &&
                input.cases.Select(p => p.name).Distinct().Count() == input.cases.Length, "Distinct named cases required");
        // Keep the complete digest within the launch contract's 60-byte token limit.
        string hash = Convert.ToBase64String(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(graph, Json)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var launch = new OperationsLaunchPayload(OperationsIdentityRules.CurrentSchemaVersion,
            "run.operations.draft001", entry.DistrictId, "offer.operations.draft001", entry.MissionId,
            entry.ScenarioId, entry.OperationMapId, 0, 1, hash, OperationsDifficultyKind.Regular,
            entry.CanonicalSeed, "txn.operations.draft001", "session.operations.draft001", 1, hash, false);
        return input.cases.Select(p => Replay(p, new OperationsTacticalSession(def, launch), def.DeadlineTicks)).ToArray();
    }

    static void CheckPackage(OperationsCompiledTactical def, OperationsTacticalFaction side, OperationsForceRoleLine[] lines)
    {
        var team = def.Spawns.Where(s => s.Faction == side).ToArray();
        Require(team.Length == lines.Sum(l => l.Count) && lines.All(l => team.Count(s => s.RosterRole == l.Role) == l.Count),
                "Incomplete or changed fixed " + side + " package");
    }

    static Result Replay(Probe test, OperationsTacticalSession s, int deadline)
    {
        var commands = new List<string>();
        string error = null;
        try
        {
            Require(test.expect != OperationsOutcomeKind.None && test.orders is { Length: <= 2048 }, "Expected terminal outcome and orders required");
            foreach (var o in test.orders)
            {
                Require(o.at >= s.Tick && o.at < deadline && !s.IsTerminal, "Invalid or unreachable order tick: " + o.at);
                if (o.at > s.Tick) s.Advance(o.at - s.Tick);
                Require(!s.IsTerminal, "Mission already ended at " + s.Tick);
                bool needActor = o.verb is not ("conclude" or "withdraw" or "escort_go" or "escort_hold");
                bool needTarget = o.verb is not ("conclude" or "withdraw" or "extract" or "escort_hold");
                Require(needActor == !string.IsNullOrEmpty(o.actor) && needTarget == !string.IsNullOrEmpty(o.target),
                        "Wrong actor/target arguments for " + o.verb);
                var result = o.verb switch {
                    "move" => s.IssueMove(o.actor, o.target), "observe" => s.IssueObserve(o.actor, o.target),
                    "scan" => s.IssueScan(o.actor, o.target), "interact" => s.IssueInteract(o.actor, o.target),
                    "attack" => s.IssueAttack(o.actor, o.target), "repair" => s.IssueRepair(o.actor, o.target),
                    "hold" => s.IssueHold(o.actor, o.target), "escort_go" => s.IssueEscortGo(o.target),
                    "escort_hold" => s.IssueEscortHold(), "extract" => s.IssueExtract(o.actor),
                    "conclude" => s.IssueConclude(), "withdraw" => s.IssueWithdraw(),
                    _ => throw new ArgumentException("Unknown public order: " + o.verb)
                };
                commands.Add($"{s.Tick}: {o.verb} {o.actor} {o.target}: {result.Reason}");
                Require(result.Accepted, "Rejected order: " + commands.Last());
            }
            if (!s.IsTerminal && s.Tick < deadline) s.Advance(deadline - s.Tick);
            Require(s.IsTerminal && s.Outcome == test.expect, "Expected " + test.expect + "; observed " + s.Outcome);
        }
        catch (Exception ex) { error = ex.Message; }
        return new(test.name, error == null, test.expect.ToString(), s.Outcome.ToString(), s.TerminalReason,
                   s.Tick, commands, error);
    }
}
