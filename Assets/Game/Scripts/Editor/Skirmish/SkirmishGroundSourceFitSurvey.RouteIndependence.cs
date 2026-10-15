#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Game.Components;
using Game.Configs;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace Game.Editor
{
    public static partial class SkirmishGroundSourceFitSurvey
    {
        // Explicit conservative engineering probe policy, not player acceptance.
        const int SharedEndpointRadiusCells = 16;
        const int MinimumUniqueInteriorCells = 32;

        static void RecordPilotRouteIndependence(JObject report, SkirmishMapLayoutConfig layout, GridConfig grid)
        {
            var frame = layout.WorldBinding;
            Require(math.abs(frame.ForwardX - 1) < .0001f && math.abs(frame.ForwardZ) < .0001f &&
                math.abs(frame.AcrossX) < .0001f && math.abs(frame.AcrossZ - 1) < .0001f,
                "This bounded endpoint-zone adapter requires the actual axis-aligned AP/CC authored frame");
            var zoneMin = new int2[2]; var zoneMax = new int2[2]; var zoneRows = new JArray();
            for (byte faction = 1; faction <= 2; faction++)
            {
                Require(layout.TryGetPad(faction, SkirmishLegalPadKind.GroundStaging, out var pad), "Actual typed compound entry pad required");
                Require(layout.TryProjectLocalToMap(pad.CenterX, pad.CenterZ, out float x, out float z), "Exact typed entry projection required");
                var center = GridUtils.WorldToCell(grid, new float3(x, 0, z));
                var half = (int2)math.floor(new float2(pad.WidthMetres, pad.DepthMetres) / (2 * grid.CellSize));
                half = math.min(half, new int2(SharedEndpointRadiusCells));
                Require(math.all(half > 0), "Positive bounded authored entry zone required");
                zoneMin[faction - 1] = center - half; zoneMax[faction - 1] = center + half;
                zoneRows.Add(new JObject { ["faction"] = faction, ["actualTypedPadId"] = pad.PadId,
                    ["minCellInclusive"] = new JArray((center - half).x, (center - half).y),
                    ["maxCellInclusive"] = new JArray((center + half).x, (center + half).y),
                    ["scope"] = "Actual source-pinned GroundStaging pad clipped to maximum16 cells; not a new clear compound" });
            }
            var groups = new List<JObject[]>();
            var source = (string)report["group"];
            var retained = ((JArray)report["endpointCandidateRouteQueries"]).OfType<JObject>()
                .Where(r => (string)r["routeId"] == (source == "AP" ? "route.skirmish.db.main" : "route.skirmish.db.flank_a")).ToArray();
            groups.Add(retained);
            foreach (JObject proposal in (JArray)report["failedRouteDetourProposals"])
                groups.Add(((JArray)proposal["nativeLegQueries"]).OfType<JObject>().ToArray());
            var rows = new JArray(); report["nativeThreeApproachIndependence"] = rows;
            foreach (var role in new[] { SkirmishRoleKind.Tank, SkirmishRoleKind.Siege, SkirmishRoleKind.ApcHeavy })
            {
                var paths = new List<List<int2>>(); int clearance = 0; bool complete = groups.Count == 3;
                var failures = new JArray();
                for (int route = 0; route < groups.Count; route++)
                {
                    var legs = groups[route].Where(r => (string)r["role"] == role.ToString()).OrderBy(r => (int)r["leg"]).ToArray();
                    if (!TryAssembleExactNativeLegs(legs, out var path, out int radius, out string error))
                    { complete = false; failures.Add(new JObject { ["routeIndex"] = route, ["error"] = error }); }
                    paths.Add(path); clearance = Math.Max(clearance, radius);
                }
                var result = new JObject { ["role"] = role.ToString(), ["completeExactNativePaths"] = complete,
                    ["retainedRouteIndex"] = 0, ["retainedRouteId"] = source == "AP" ? "route.skirmish.db.main" : "route.skirmish.db.flank_a",
                    ["detourRouteIndices"] = new JArray(1, 2), ["assemblyFailures"] = failures,
                    ["scope"] = "StaticNativeCenterlineClearanceProbe_ExcludesLiveAndSceneryOccupancy",
                    ["sourceFitCertified"] = false };
                rows.Add(result);
                if (!complete) { result["independentUnderDeclaredProbePolicy"] = false; continue; }
                result["actualAuthoredEndpointZones"] = zoneRows.DeepClone();
                var comparison = ComparePilotApproaches(paths, clearance, zoneMin[0], zoneMax[0], zoneMin[1], zoneMax[1]);
                foreach (var property in comparison.Properties()) result[property.Name] = property.Value.DeepClone();
            }
        }

        static bool TryAssembleExactNativeLegs(JObject[] legs, out List<int2> points, out int radius, out string error)
        {
            points = new List<int2>(); radius = 0; error = null;
            if (legs.Length == 0) { error = "No native legs"; return false; }
            for (int i = 0; i < legs.Length; i++)
            {
                var row = legs[i];
                if ((int?)row["leg"] != i + 1 || (string)row["status"] != "NativeQueryFoundExactGoal" ||
                    (int?)row["failureCode"] != 0 || !(row["pathCells"] is JArray cells) || cells.Count < 2 ||
                    !(row["requestedStartCell"] is JArray from) || !(row["requestedGoalCell"] is JArray to) ||
                    !((int?)row["footprintX"] >= 1) || !((int?)row["footprintZ"] >= 1))
                { error = "Missing, failed or noncontiguous exact native leg"; return false; }
                var a = new int2((int)from[0], (int)from[1]); var b = new int2((int)to[0], (int)to[1]);
                var first = new int2((int)cells[0][0], (int)cells[0][1]);
                var last = new int2((int)cells[cells.Count - 1][0], (int)cells[cells.Count - 1][1]);
                if (!first.Equals(a) || !last.Equals(b) || (i > 0 && !points[points.Count - 1].Equals(a)))
                { error = "Native endpoints or cross-leg join differ"; return false; }
                radius = Math.Max(radius, (Math.Max((int)row["footprintX"], (int)row["footprintZ"]) + 1) / 2);
                for (int n = i == 0 ? 0 : 1; n < cells.Count; n++) points.Add(new int2((int)cells[n][0], (int)cells[n][1]));
            }
            return true;
        }

        static JObject ComparePilotApproaches(List<List<int2>> paths, int clearance,
            int2 entryMin, int2 entryMax, int2 exitMin, int2 exitMax)
        {
            Require(paths.Count == 3 && paths.All(p => p.Count >= 2) && clearance >= 0 && clearance <= 16,
                "Three bounded native path centerlines required");
            var start = paths[0][0]; var goal = paths[0][paths[0].Count - 1];
            Require(paths.All(p => p[0].Equals(start) && p[p.Count - 1].Equals(goal)), "Approaches must share exact native entry and exit");
            bool InZone(int2 p, int2 min, int2 max) => math.all(p >= min) && math.all(p <= max);
            if (!InZone(start, entryMin, entryMax) || !InZone(goal, exitMin, exitMax))
                return new JObject { ["independentUnderDeclaredProbePolicy"] = false, ["rejection"] = "Native path endpoint is outside actual authored compound entry/exit" };
            var centers = new List<HashSet<int2>>(); var corridors = new List<HashSet<int2>>();
            foreach (var path in paths)
            {
                var center = RasterizePilotCenterline(path);
                center.RemoveWhere(p => (InZone(p, entryMin, entryMax) && math.lengthsq(p - start) <= SharedEndpointRadiusCells * SharedEndpointRadiusCells) ||
                    (InZone(p, exitMin, exitMax) && math.lengthsq(p - goal) <= SharedEndpointRadiusCells * SharedEndpointRadiusCells));
                centers.Add(center); var corridor = new HashSet<int2>();
                foreach (var cell in center) for (int x = -clearance; x <= clearance; x++) for (int z = -clearance; z <= clearance; z++)
                    corridor.Add(cell + new int2(x, z));
                corridors.Add(corridor);
            }
            var pairs = new JArray(); var uniqueRows = new JArray(); bool independent = true;
            for (int i = 0; i < 3; i++)
            {
                var unique = new HashSet<int2>(centers[i]);
                for (int j = 0; j < 3; j++) if (i != j) unique.ExceptWith(corridors[j]);
                uniqueRows.Add(new JObject { ["routeIndex"] = i, ["uniqueInteriorCenterlineCells"] = unique.Count });
                independent &= unique.Count >= MinimumUniqueInteriorCells;
                for (int j = i + 1; j < 3; j++)
                {
                    var overlap = new HashSet<int2>(corridors[i]); overlap.IntersectWith(corridors[j]);
                    pairs.Add(new JObject { ["firstRouteIndex"] = i, ["secondRouteIndex"] = j, ["interiorClearanceOverlapCells"] = overlap.Count,
                        ["overlapWitnessCells"] = new JArray(overlap.OrderBy(p => p.x).ThenBy(p => p.y).Take(64).Select(p => new JArray(p.x, p.y))) });
                    independent &= overlap.Count == 0;
                }
            }
            return new JObject { ["probePolicy"] = "ThreeStaticInteriorCorridors.v1", ["sharedEndpointRadiusCells"] = SharedEndpointRadiusCells,
                ["conservativeChebyshevClearanceCells"] = clearance, ["minimumUniqueInteriorCells"] = MinimumUniqueInteriorCells,
                ["uniqueInteriorRows"] = uniqueRows, ["mergeAndOverlapPairs"] = pairs,
                ["independentUnderDeclaredProbePolicy"] = independent };
        }

        static HashSet<int2> RasterizePilotCenterline(List<int2> points)
        {
            var cells = new HashSet<int2>();
            for (int i = 1; i < points.Count; i++)
            {
                int x = points[i - 1].x, z = points[i - 1].y, endX = points[i].x, endZ = points[i].y;
                int dx = Math.Abs(endX - x), dz = -Math.Abs(endZ - z), sx = x < endX ? 1 : -1, sz = z < endZ ? 1 : -1, error = dx + dz;
                Require(dx - dz <= 10000, "Native compressed path segment exceeds bounded source probe");
                while (true)
                {
                    cells.Add(new int2(x, z)); if (x == endX && z == endZ) break;
                    int twice = error * 2; if (twice >= dz) { error += dz; x += sx; } if (twice <= dx) { error += dx; z += sz; }
                }
            }
            return cells;
        }

        static void RunPilotRouteIndependenceControls()
        {
            var main = new List<int2> { new int2(0, 0), new int2(200, 0) };
            var north = new List<int2> { new int2(0, 0), new int2(20, 20), new int2(180, 20), new int2(200, 0) };
            var south = new List<int2> { new int2(0, 0), new int2(20, -20), new int2(180, -20), new int2(200, 0) };
            var entryMin = new int2(-16, -16); var entryMax = new int2(16, 16);
            var exitMin = new int2(184, -16); var exitMax = new int2(216, 16);
            Require((bool)ComparePilotApproaches(new List<List<int2>> { main, north, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Separated synthetic native-path policy control must pass");
            Require(!(bool)ComparePilotApproaches(new List<List<int2>> { main, main, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Renamed identical route must reject");
            var merged = new List<int2> { new int2(0, 0), new int2(20, 20), new int2(100, 20), new int2(120, 0), new int2(200, 0) };
            Require(!(bool)ComparePilotApproaches(new List<List<int2>> { main, merged, south }, 2, entryMin, entryMax, exitMin, exitMax)["independentUnderDeclaredProbePolicy"],
                "Distinct route that merges into an interior chokepoint must reject");
        }
    }
}
#endif
