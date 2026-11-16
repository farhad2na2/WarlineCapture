using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Operations.Content;
using Game.Operations.Contracts;
using Game.Operations.Loop;

// Authoring CLI only. Compile the game's Operations assemblies directly, with no Unity references.
internal static class OperationsHost
{
    internal static readonly JsonSerializerOptions Json = new()
    {
        IncludeFields = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    internal static void Require(bool ok, string why)
    {
        if (!ok) throw new ArgumentException(why);
    }

    static object Catalog(OperationsCatalogEntry e)
    {
        _ = e.ToDefinitionSchema(); // Exercise production ID, package and numerical validation.
        return new {
            mission_id = e.MissionId, scenario_id = e.ScenarioId, operation_map_id = e.OperationMapId,
            district_id = e.DistrictId, local_slot = e.LocalSlot, family = OperationsFamilyIds.ToCatalogToken(e.Family),
            force_package = e.ForcePackageToken, enemy_profile = e.EnemyPackageToken,
            hard_deadline_seconds = e.HardDeadlineSeconds, min_intel = e.MinIntel, canonical_seed = e.CanonicalSeed
        };
    }

    internal record ModelRun(bool ok, int seed, string failure, string outcome, string reason, int? tick,
                             int? settlement_revision, string[] intents, string difficulty, string result_hash, bool returned);

    static ModelRun AuthoredRun(OperationsCatalogEntry entry, int offset)
    {
        int seed = checked(entry.CanonicalSeed + offset);
        bool ok = OperationsAriaEvidenceHarness.TryRunMission(entry.MissionId, seed, "en", out var record, out var failure);
        // The production harness creates its record after CompleteReturn.
        return new(ok, seed, failure, record?.TerminalOutcome, record?.TerminalReason, record?.ObjectiveCompletionTick,
                   record?.SettledRevision, record?.IntentTrace, record?.Difficulty, record?.ResultHash, record != null);
    }

    public static int Main(string[] args)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(args.Single()));
            var root = doc.RootElement;
            var selected = root.GetProperty("selected").Deserialize<string[]>();
            var offsets = root.GetProperty("seed_offsets").Deserialize<int[]>();
            var drafts = root.GetProperty("drafts").EnumerateArray().ToDictionary(d => d.GetProperty("mission").GetString());
            var catalog = OperationsCatalogIndex.Entries;
            var byId = catalog.ToDictionary(e => e.MissionId, StringComparer.Ordinal);
            var missions = new List<object>();
            bool passed = true;
            foreach (string code in selected)
            {
                try
                {
                    var e = byId["operation." + code.ToLowerInvariant()];
                    bool authored = OperationsAuthoredMissions.TryCompile(e.MissionId, out var def, out _, out var error);
                    if (drafts.TryGetValue(code, out var draft))
                    {
                        Require(!authored && error == "missing_mission", code + ": a draft cannot replace an existing authored binding");
                        var runs = OperationDraft.Run(draft, e);
                        bool ok = runs.All(r => r.passed);
                        missions.Add(new { code, status = ok ? "draft_model" : "failed", runs, error = ok ? null : "draft expectations failed" });
                        passed &= ok;
                    }
                    else if (authored)
                    {
                        Require(def.MapId == e.OperationMapId && def.DeadlineTicks == e.HardDeadlineSeconds, code + ": authored map or deadline differs from catalog");
                        var runs = offsets.Select(offset => AuthoredRun(e, offset)).ToArray();
                        bool ok = runs.All(r => r.ok);
                        missions.Add(new { code, status = ok ? "host_model" : "failed", runs, error = ok ? null : "host planner/return failed; see runs" });
                        passed &= ok;
                    }
                    else
                    {
                        // The launch fallback can compile a generic map fixture even for a missing mission.
                        // Never count it as an authored mission or a completed run.
                        Require(error == "missing_mission", code + ": authored graph rejected: " + error);
                        missions.Add(new { code, status = "catalog_only", reason = error });
                    }
                }
                catch (Exception ex)
                {
                    passed = false;
                    missions.Add(new { code, status = "failed", error = ex.Message });
                }
            }
            Console.WriteLine(JsonSerializer.Serialize(new { catalog = catalog.Select(Catalog), missions }, Json));
            return passed ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            return 1;
        }
    }
}
