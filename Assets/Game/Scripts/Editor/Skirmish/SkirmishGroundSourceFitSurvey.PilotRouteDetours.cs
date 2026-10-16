#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Repair only logical joins. Original native failures and measured routes
        // remain in the report. Coordinates are proposed, never relabelled measured.
        static void ProbePilotFailedRouteDetours(EntityManager em, Entity gridEntity, GridConfig grid,
            MapSurfaceComponent surface, OperationMapDefinition source, SkirmishMapLayoutConfig layout, JObject report)
        {
            string group = (string)report["group"];
            if (group != "AP" && group != "CC") return;
            Require(Hash(PilotFacts) == "854b8b7cd7dbc47d2ab4dc6a4ab6829eee7b5f1830aabcc31f00dfb3890bbc2b",
                "Exact native02 failed-edge facts changed");
            var facts = JObject.Parse(File.ReadAllText(PilotFacts));
            var pin = ((JArray)facts["sources"]).OfType<JObject>().Single(r => (string)r["group"] == group);
            Require((string)pin["source_sha256"] == (string)report["sourceAssetSha256"] &&
                (string)pin["source_content_hash"] == source.ContentHash &&
                (string)pin["source_identity_hash"] == source.SourceIdentityHash,
                "Detour source changed since actual native02");
            int[][][] proposed = group == "AP" ? new[] {
                new[] { new[]{485,540}, new[]{497,596}, new[]{587,607}, new[]{676,612}, new[]{765,607}, new[]{855,596}, new[]{890,596}, new[]{890,534}, new[]{867,534} },
                new[] { new[]{485,540}, new[]{569,512}, new[]{735,512}, new[]{831,488}, new[]{867,534} }
            } : new[] {
                new[] { new[]{970,650}, new[]{1054,650}, new[]{1072,688}, new[]{1250,688}, new[]{1268,650}, new[]{1352,650} },
                new[] { new[]{970,650}, new[]{1006,598}, new[]{1054,600}, new[]{1161,600}, new[]{1220,582}, new[]{1316,598}, new[]{1352,650} }
            };
            string[] ids = group == "AP" ? new[] { "upper-final-join-v1", "lower-central-join-v1" } :
                new[] { "main-north-bypass-v1", "lower-central-join-v1" };
            var rows = new JArray(); report["failedRouteDetourProposals"] = rows;
            for (int route = 0; route < proposed.Length; route++)
            {
                var points = proposed[route]; var probes = new JArray();
                var row = new JObject { ["proposalId"] = group + "." + ids[route],
                    ["requestedGridCells"] = new JArray(points.Select(p => new JArray(p[0], p[1]))),
                    ["gridOriginWorld"] = new JArray(grid.Origin.x, grid.Origin.y, grid.Origin.z), ["cellSize"] = grid.CellSize,
                    ["sourceFactsSha256"] = Hash(PilotFacts), ["nativeLegQueries"] = probes,
                    ["coordinatesScope"] = "Explicit logical detour proposals; native exact query required for every leg",
                    ["independence"] = "Pending comparison with all three complete native paths; shared corridor cannot count twice",
                    ["sourceFitCertified"] = false };
                rows.Add(row);
                foreach (var role in new[] { SkirmishRoleKind.Tank, SkirmishRoleKind.Siege, SkirmishRoleKind.ApcHeavy })
                {
                    var prefab = FindNativePrefab(em, SkirmishRoleCatalogConfig.RuntimePrefabKey(role));
                    Require(prefab != Entity.Null, "Exact heavy actor template required for failed-edge repair probe");
                    for (int leg = 1; leg < points.Length; leg++)
                    {
                        var from = new int2(points[leg - 1][0], points[leg - 1][1]);
                        var to = new int2(points[leg][0], points[leg][1]);
                        Require(GridUtils.InBounds(from, grid.Width, grid.Height) && GridUtils.InBounds(to, grid.Width, grid.Height),
                            "Explicit proposed detour outside current exact source grid");
                        var result = new JObject { ["role"] = role.ToString(), ["leg"] = leg,
                            ["requestedStartCell"] = new JArray(from.x, from.y), ["requestedGoalCell"] = new JArray(to.x, to.y),
                            ["scope"] = "ActualNativeQueryOnProposedDetour_StaticGridSurfaceOnly" };
                        probes.Add(result); QueryPath(em, gridEntity, grid, surface, prefab, from, to, result);
                    }
                }
                row["nativeFailedLegs"] = probes.OfType<JObject>().Count(p => (string)p["status"] != "NativeQueryFoundExactGoal");
                row["allProposedLegsNativeExact"] = probes.Count == 3 * (points.Length - 1) && (int)row["nativeFailedLegs"] == 0;
            }
            RecordPilotRouteIndependence(report, layout, grid);
        }
    }
}
#endif
