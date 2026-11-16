using System;
using System.IO;
using System.Linq;
using System.Text;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Newtonsoft.Json.Linq;
using Unity.Entities;

// The candidate is an external logical layout only; no live placement or fit is claimed.
internal static class S074AlignedPadControl
{
    internal static void Run(SkirmishResolvedSetup setup, SkirmishScenarioDefinitionConfig definition,
        EntityManager em, Entity gridEntity, Entity boundary, GeneratedBuildingDefinitions definitions,
        string directory, string sourceRoot, string candidatePath, string candidateSha)
    {
        if (setup.CatalogId != "S074" || setup.SetupHash != 977354998u) throw new InvalidDataException("Exact baseline setup required");
        var graphPath = Path.Combine(sourceRoot, "T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json");
        var graph = JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(graphPath,
            "edc4081763a3252716679fc4e36dae2b8341aa6e9c8426fa552e143096d510cb", 16 * 1024 * 1024)));
        var materializer = new SourceConfigMaterializer(graph);
        var candidate = materializer.LogicalLayout(JObject.Parse(Encoding.UTF8.GetString(AutonomousWorld.PinnedFile(candidatePath, candidateSha, 1024 * 1024))));
        var grid = em.GetComponentData<GridConfig>(gridEntity); var requests = em.GetBuffer<BuildingRuntimeSpawnRequest>(boundary);
        if (requests.Length != setup.Structures.Length || requests.Length != 20) throw new InvalidOperationException("Actual original pending grant domain required");
        int baseline = 0, aligned = 0; var rows = new JArray();
        for (int i = 0; i < requests.Length; i++)
        {
            var request = requests[i];
            if (request.Status != BuildingRuntimeSpawnRequest.Pending || request.EntryIndex != i ||
                !SkirmishStartingGrantPlacementConstraint.TryResolvePad(definition.MapLayout, setup, i, out var original) ||
                !SkirmishStartingGrantPlacementConstraint.TryResolvePad(candidate, setup, i, out var updated))
                throw new InvalidOperationException("Actual pending grant and exact typed pad required at " + i);
            var d = definitions.Definitions.Single(x => x.Prefab.name == request.BuildingId.ToString());
            var rect = new RectInt(request.PreferredOrigin.x, request.PreferredOrigin.y, d.FootprintCells.x, d.FootprintCells.y);
            bool before = SkirmishStartingGrantPlacementConstraint.ContainsFootprint(definition.MapLayout, original, grid, definition.CampaignMap.Bounds, rect);
            bool after = SkirmishStartingGrantPlacementConstraint.ContainsFootprint(candidate, updated, grid, definition.CampaignMap.Bounds, rect);
            if (before) baseline++; if (after) aligned++;
            rows.Add(new JObject { ["index"] = i, ["building"] = request.BuildingId.ToString(),
                ["faction"] = request.FactionId, ["baseline_contained"] = before, ["candidate_contained"] = after });
        }
        if (baseline != 4 || aligned != 20) throw new InvalidOperationException("Actual original pad-containment comparison changed");
        var result = new JObject { ["kind"] = "S074AlignedPadSourceOwnerControl", ["status"] = "OriginalContainmentCompared",
            ["baseline_setup_hash"] = setup.SetupHash, ["baseline_preferred_contained"] = baseline,
            ["candidate_preferred_contained"] = aligned, ["candidate"] = new JObject { ["path"] = candidatePath, ["sha256"] = candidateSha },
            ["rows"] = rows, ["candidate_compiled"] = false, ["placement_performed"] = false,
            ["source_fit_certified"] = false, ["mission_ready"] = false,
            ["scope"] = "Actual original typed-pad containment on the baseline compiled setup and external pad-only candidate; no placement, candidate compilation, or native readiness." };
        File.WriteAllText(Path.Combine(directory, "s074-aligned-pad-control.json"), result.ToString() + Environment.NewLine);
        Console.WriteLine("[S074AlignedPadControl] result=Passed baseline=4 candidate=20 candidateCompiled=0 placement=0 readiness=0");
    }
}
