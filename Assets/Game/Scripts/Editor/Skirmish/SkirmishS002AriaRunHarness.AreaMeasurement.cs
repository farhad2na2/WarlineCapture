#if UNITY_EDITOR
// =============================================================================
// RunCatalogAreaMeasurementAndExit - read-only measurement of a FC/BT/CE draft on the
// Campaign source that the launched host mission uses (Desert Base: S006 or S002).
// Without WARLINE_SKIRMISH_CATALOG, select a probe host by the draft's campaign_source.
//
//   WARLINE_SKIRMISH_CATALOG=S006 WARLINE_MISSION_DRAFT=Tools/Missions/drafts/<draft>.json \
//   WARLINE_AREA_MEASUREMENT_OUT=Design/AgentReports/SkirmishExpansion/<id>/area-measurement.json \
//   Tools/CI/invoke_unity_macos.sh --timeout 900 --log <log> -- \
//     -executeMethod Game.Editor.SkirmishS002AriaRunHarness.RunCatalogAreaMeasurementAndExit
// (no -quit: the harness quits after the startup-only map review). Pass marker in the log:
//   [SkirmishAreaMeasurement] catalog=<id> result=Completed ...
//
// It launches the host through the generic dispatch in startup-only map review (no ARIA, no touches,
// no order, no truck driven), then reads the live grid/surface once, writes JSON and lets the
// harness finish. Nothing in the preview, actors, terrain or assets is modified. The JSON is the
// `area_measurement` artifact a draft names: it measures, it does not certify source fit, ARIA play
// or readiness. Coordinates are the draft's world X/Z metres on the host's own source.
//
// Reachability uses the native passability rules (GridWalkable, live DynamicBlocker bits, surface
// movement mask, diagonal corner rule) in a flood fill over the logical playable window; live unit
// occupancy is ignored. Truck cells additionally need the native truck footprint (origin-anchored).
// Truck route segments also go through the native PathfindBatchJob (manual move, the same query the
// source-fit survey uses) and report its own failure code when it refuses.
// =============================================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Components;
using Game.Configs;
using Game.Runtime;
using Game.Runtime.Pathfinding;
using Game.Skirmish.Contracts;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool areaMeasurementActive;
        private static JObject areaMeasurementDraft;
        private static string areaMeasurementDraftPath;

        public static void RunCatalogAreaMeasurementAndExit()
        {
            string draftArgument = Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT");
            string outArgument = Environment.GetEnvironmentVariable("WARLINE_AREA_MEASUREMENT_OUT");
            if (string.IsNullOrEmpty(draftArgument) || string.IsNullOrEmpty(outArgument))
                throw new InvalidOperationException("RunCatalogAreaMeasurementAndExit requires WARLINE_MISSION_DRAFT and WARLINE_AREA_MEASUREMENT_OUT.");
            areaMeasurementDraft = ReadMeasurementDraft(draftArgument, out areaMeasurementDraftPath);
            if (!(areaMeasurementDraft["anchors"] is JObject)) throw new InvalidOperationException("Draft has no anchors{}.");
            // Keep the existing catalog dispatch, including coded-host fallbacks, for existing callers.
            RunMeasurementHost(string.IsNullOrEmpty(Environment.GetEnvironmentVariable(MeasurementCatalogVariable))
                ? areaMeasurementDraft : null, StartAreaMeasurement);
        }

        private static void StartAreaMeasurement()
        {
            areaMeasurementActive = true;

            // Same startup-only map-review setup as the catalog Airport source-fit probe.
            SessionState.SetBool("S004EditorJourney.OptionsSaved", true);
            SessionState.SetBool("S004EditorJourney.OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt("S004EditorJourney.Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", true);
            nativeReviewStage = 0;
            nativeReviewSizeApplied = false;
            Environment.SetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW", "1");
            Environment.SetEnvironmentVariable("WARLINE_MAP_REVIEW_ONLY", "1");
            Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", "1"); // the existing map-review tick gate
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Environment.SetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY", null);
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_S002_SEED")))
                Environment.SetEnvironmentVariable("WARLINE_S002_SEED", "104733");
            Environment.SetEnvironmentVariable("WARLINE_S002_LOCALE", "en");
            try { RunFocusedAriaAndExit(); }
            catch
            {
                areaMeasurementActive = false;
                AssetDatabase.AllowAutoRefresh();
                SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", false);
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool("S004EditorJourney.OptionsEnabled", false);
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt("S004EditorJourney.Options", 0);
                SessionState.SetBool("S004EditorJourney.OptionsSaved", false);
                Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", null);
                Environment.SetEnvironmentVariable("WARLINE_S003_NATIVE_REVIEW", null);
                Environment.SetEnvironmentVariable("WARLINE_MAP_REVIEW_ONLY", null);
                Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", null);
                throw;
            }
        }

        // A source-selected host may load the right physical map and still fail its opening
        // grants. The live grid can be measured, with that failure recorded in the receipt.
        private static bool TryMeasureAreaAfterStartupFailure(EntityManager em)
        {
            if (!areaMeasurementActive || measurementSource == null || !LayoutDumpSessionFailed(em, out string reason))
                return false;
            TickAreaMeasurement(reason);
            Finish(false, "areaMeasurementOnly:sessionStartupFailure=" + reason);
            return true;
        }

        // Called from the map-review tick (see SkirmishS002AriaRunHarness.cs). Runs once; true = finished.
        private static bool TickAreaMeasurement(string hostStartupFailure = null)
        {
            deadline = EditorApplication.timeSinceStartup + 900d; // one-shot computation, longer than the opening watchdog
            try
            {
                string outPath = Environment.GetEnvironmentVariable("WARLINE_AREA_MEASUREMENT_OUT");
                var result = MeasureAreaDraft(areaMeasurementDraft);
                if (hostStartupFailure != null) result["hostStartupFailure"] = hostStartupFailure;
                outPath = Path.GetFullPath(outPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllText(outPath, result.ToString(Newtonsoft.Json.Formatting.Indented));
                File.WriteAllText(LiveTracePath() + ".area-measurement.json", result.ToString(Newtonsoft.Json.Formatting.Indented));
                Debug.Log("[SkirmishAreaMeasurement] catalog=" + catalogId + " result=Completed draft=" + (string)result["draft"] +
                    " areas=" + ((JArray)result["areas"]).Count + " pads=" + ((JArray)result["pads"]).Count + " routes=" + ((JArray)result["routes"]).Count +
                    (hostStartupFailure == null ? "" : " hostStartupFailure=" + hostStartupFailure) +
                    " out=" + outPath + " previewMutated=0 physicalSourceModified=0 readiness=0");
                return true;
            }
            finally
            {
                areaMeasurementActive = false;
                Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", null);
            }
        }

        // ---- measurement ------------------------------------------------------------------------------------

        private sealed class AreaGridModel
        {
            public int X0, Z0, W, H;
            public float Cell;
            public bool[] Infantry, Vehicle, Truck;
        }

        private static JObject MeasureAreaDraft(JObject draft)
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var grids = em.CreateEntityQuery(typeof(GridConfig), typeof(GridWalkable), typeof(DynamicBlockerComponent),
                typeof(GridRoad), typeof(GridRoadSidewalk), typeof(GridRoadDirt));
            using var surfaces = em.CreateEntityQuery(typeof(MapSurfaceComponent));
            if (grids.CalculateEntityCount() != 1 || surfaces.CalculateEntityCount() != 1)
                throw new InvalidOperationException("Area measurement needs exactly one live grid and one map surface.");
            var gridEntity = grids.GetSingletonEntity();
            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var walk = em.GetBuffer<GridWalkable>(gridEntity, true);
            var blockers = em.GetComponentData<DynamicBlockerComponent>(gridEntity);
            var surface = em.GetComponentData<MapSurfaceComponent>(surfaces.GetSingletonEntity());
            if (!blockers.Blocked.IsCreated || grid.CellSize <= 0f || !surface.SurfaceBlob.IsCreated)
                throw new InvalidOperationException("Live blocker bits or surface blob unavailable.");
            if (measurementSource != null)
            {
                if (!TryReadLayoutDumpBounds(em, out _, out _, out string sourceId))
                    throw new InvalidOperationException("Cannot identify the live map for area measurement.");
                RequireMeasurementSource(sourceId);
            }
            // Coded hosts (S002) carry no expansion-catalog definition, so the session may expose no logical map; the draft's own
            // campaign_source then supplies the playable bounds. Either way the host must run the draft's source.
            OperationMapDefinition map = null;
            string mapOrigin = "session logical map";
            try { map = Game.Runtime.SkirmishS004Environment.LoadMapForSession(em); }
            catch (Exception loadError) { mapOrigin = "draft campaign_source (session map unavailable: " + loadError.Message + ")"; }
            var draftMap = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>((string)draft["campaign_source"]);
            if (map == null) { map = draftMap; if (mapOrigin == "session logical map") mapOrigin = "draft campaign_source (host session has no logical map)"; }
            if (map == null) throw new InvalidOperationException("No playable bounds: neither the host session nor the draft campaign_source resolves to an OperationMapDefinition.");
            if (draftMap != null && map != draftMap && map.OperationMapId != draftMap.OperationMapId)
                throw new InvalidOperationException("Host map " + map.OperationMapId + " differs from the draft's campaign_source " + draftMap.OperationMapId + "; host a mission on the draft's map.");

            var anchors = new Dictionary<string, Vector2>(StringComparer.Ordinal);
            foreach (var property in ((JObject)draft["anchors"]).Properties())
                anchors[property.Name] = new Vector2((float)property.Value[0], (float)property.Value[1]);

            var root = new JObject
            {
                ["schema"] = "warline.area-measurement/v1",
                ["draft"] = areaMeasurementDraftPath == null ? "" : Path.GetFileName(areaMeasurementDraftPath),
                ["catalogHost"] = catalogId,
                ["draftCatalog"] = (string)draft["catalog_id"],
                ["campaignSource"] = (string)draft["campaign_source"],
                ["scope"] = "ReadOnlyStartupOnlyGridMeasurement_NoTruckDriven_NoOrders",
                ["boundsSource"] = mapOrigin,
                ["limitation"] = "Measures passability, footprints and static route lengths on the host's source; it does not certify native source fit, ARIA play, a winnable mission or readiness. " +
                    "Live unit occupancy is ignored; reachability is a flood fill over the logical playable window using the native passability rules.",
                ["cellSize"] = grid.CellSize, ["gridWidth"] = grid.Width, ["gridHeight"] = grid.Height,
                ["playableMin"] = new JArray(map.Bounds.PlayableMin.x, map.Bounds.PlayableMin.z),
                ["playableMax"] = new JArray(map.Bounds.PlayableMax.x, map.Bounds.PlayableMax.z)
            };

            // Footprints from the native definitions.
            var truckInfo = new JObject();
            int2 truckFootprint = new int2(0, 0);
            Entity truckPrefab = FindAreaProbePrefab(em, SkirmishConvoyTruckDefinition.ExistingChassisKey);
            var truckDefinition = SkirmishConvoyTruckDefinition.Load();
            if (truckPrefab != Entity.Null && em.HasComponent<UnitFootprint>(truckPrefab))
            {
                truckFootprint = em.GetComponentData<UnitFootprint>(truckPrefab).Size;
                truckInfo["footprintSource"] = "NativeBakedUnitFootprint";
            }
            else if (truckDefinition != null && truckDefinition.ChassisConfig != null)
            {
                var configured = truckDefinition.ChassisConfig.FootprintCells;
                truckFootprint = new int2(configured.x, configured.y);
                truckInfo["footprintSource"] = "ConfiguredChassisConfigNotNativeBaked" + (truckDefinition.ChassisConfig.AutoCalculateFootprint ? "_AutoCalculateOn_UNRELIABLE" : "");
            }
            else truckInfo["footprintSource"] = "Unavailable";
            truckInfo["nativePrefabInHost"] = truckPrefab != Entity.Null;
            truckInfo["footprintCells"] = new JArray(truckFootprint.x, truckFootprint.y);
            truckInfo["footprintMetres"] = new JArray(truckFootprint.x * grid.CellSize, truckFootprint.y * grid.CellSize);
            if (truckDefinition != null && truckDefinition.ChassisConfig != null)
            {
                truckInfo["speedMetresPerSecond"] = truckDefinition.ChassisConfig.Speed;
                truckInfo["roadSpeedMultiplier"] = truckDefinition.ChassisConfig.RoadSpeedMultiplier;
            }
            root["truck"] = truckInfo;
            var towerInfo = new JObject();
            Vector2Int tower = Vector2Int.zero;
            var towerPaths = AssetDatabase.FindAssets("Building_GuardTower t:Prefab", new[] { "Assets/Game/Prefabs/Buildings" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileNameWithoutExtension(p) == "Building_GuardTower").ToArray();
            if (towerPaths.Length == 1)
            {
                var authoring = AssetDatabase.LoadAssetAtPath<GameObject>(towerPaths[0])?.GetComponent<BuildingDefinitionAuthoring>();
                if (authoring != null) tower = authoring.ConfiguredFootprintCells;
            }
            towerInfo["footprintSource"] = tower == Vector2Int.zero ? "Unavailable" : "Building_GuardTower prefab BuildingDefinitionAuthoring.ConfiguredFootprintCells";
            towerInfo["footprintCells"] = new JArray(tower.x, tower.y);
            towerInfo["footprintMetres"] = new JArray(tower.x * grid.CellSize, tower.y * grid.CellSize);
            root["guardTower"] = towerInfo;

            // Passability over the logical playable window.
            var min = GridUtils.WorldToCell(grid, map.Bounds.PlayableMin);
            var max = GridUtils.WorldToCell(grid, map.Bounds.PlayableMax);
            var model = new AreaGridModel
            {
                X0 = math.max(0, min.x), Z0 = math.max(0, min.y), Cell = grid.CellSize
            };
            model.W = math.min(grid.Width, max.x) - model.X0;
            model.H = math.min(grid.Height, max.y) - model.Z0;
            if (model.W <= 0 || model.H <= 0) throw new InvalidOperationException("Empty playable window.");
            int total = model.W * model.H;
            model.Infantry = new bool[total]; model.Vehicle = new bool[total]; model.Truck = new bool[total];
            for (int z = 0; z < model.H; z++)
                for (int x = 0; x < model.W; x++)
                {
                    int gx = model.X0 + x, gz = model.Z0 + z, g = gz * grid.Width + gx, i = z * model.W + x;
                    if (g >= walk.Length || walk[g].Value == 0 || (g < blockers.Blocked.Length && blockers.Blocked.IsSet(g))) continue;
                    var world = GridUtils.CellToWorldCenter(grid, new int2(gx, gz));
                    var sc = new int2((int)math.floor((world.x - surface.GridOrigin.x) / surface.CellSize),
                        (int)math.floor((world.z - surface.GridOrigin.z) / surface.CellSize));
                    if (!MapSurfaceBlobAccess.TryGetPrimarySurface(ref surface.SurfaceBlob.Value, sc, out var sample)) continue;
                    model.Infantry[i] = (sample.MovementMask & MapSurfaceMovementMask.Infantry) != 0;
                    model.Vehicle[i] = (sample.MovementMask & (MapSurfaceMovementMask.WheeledVehicle | MapSurfaceMovementMask.TrackedVehicle)) != 0;
                }
            if (truckFootprint.x > 0 && truckFootprint.y > 0)
            {
                // Origin-anchored footprint erosion with a 2D prefix sum of vehicle-passable cells.
                var prefix = new int[(model.W + 1) * (model.H + 1)];
                for (int z = 0; z < model.H; z++)
                    for (int x = 0; x < model.W; x++)
                        prefix[(z + 1) * (model.W + 1) + x + 1] = (model.Vehicle[z * model.W + x] ? 1 : 0) +
                            prefix[z * (model.W + 1) + x + 1] + prefix[(z + 1) * (model.W + 1) + x] - prefix[z * (model.W + 1) + x];
                for (int z = 0; z + truckFootprint.y <= model.H; z++)
                    for (int x = 0; x + truckFootprint.x <= model.W; x++)
                    {
                        int x1 = x + truckFootprint.x, z1 = z + truckFootprint.y;
                        int sum = prefix[z1 * (model.W + 1) + x1] - prefix[z * (model.W + 1) + x1] - prefix[z1 * (model.W + 1) + x] + prefix[z * (model.W + 1) + x];
                        model.Truck[z * model.W + x] = sum == truckFootprint.x * truckFootprint.y;
                    }
            }
            root["window"] = new JObject { ["minCell"] = new JArray(model.X0, model.Z0), ["cells"] = new JArray(model.W, model.H),
                ["infantryPassableCells"] = model.Infantry.Count(v => v), ["vehiclePassableCells"] = model.Vehicle.Count(v => v),
                ["truckPassableCells"] = model.Truck.Count(v => v) };

            // Sources: both sides' base and staging.
            var distances = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var sourceRows = new JArray();
            var sourceCells = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string role in new[] { "base.player", "staging.player", "base.enemy", "staging.enemy" })
            {
                var row = new JObject { ["role"] = role };
                sourceRows.Add(row);
                if (!anchors.TryGetValue(role, out var at)) { row["status"] = "MissingFromDraft"; continue; }
                row["world"] = new JArray(at.x, at.y);
                foreach (var pair in new[] { ("infantry", model.Infantry), ("truck", model.Truck) })
                {
                    var cell = WorldToWindow(grid, model, at);
                    int snapped = NearestPassable(pair.Item2, model, cell.x, cell.y, 16);
                    var detail = new JObject();
                    row[pair.Item1] = detail;
                    if (snapped < 0) { detail["status"] = "NoPassableCellWithin16Cells"; continue; }
                    detail["snappedCell"] = new JArray(model.X0 + snapped % model.W, model.Z0 + snapped / model.W);
                    detail["snapDistanceMetres"] = Vector2.Distance(new Vector2(cell.x, cell.y), new Vector2(snapped % model.W, snapped / model.W)) * grid.CellSize;
                    distances[role + "|" + pair.Item1] = AreaDijkstra(pair.Item2, model.W, model.H, snapped, -1);
                    sourceCells[role + "|" + pair.Item1] = snapped;
                    detail["reachableCells"] = distances[role + "|" + pair.Item1].Count(d => d != int.MaxValue);
                }
            }
            root["sources"] = sourceRows;

            // Areas.
            var areaRows = new JArray();
            if (draft["objective_areas"] is JObject areaObject)
                foreach (var property in areaObject.Properties())
                {
                    var row = new JObject { ["role"] = property.Name };
                    areaRows.Add(row);
                    var spec = property.Value as JObject;
                    bool rectangle = spec?["rectangle"] != null;
                    var vertices = ReadAreaPoints(spec?["rectangle"] ?? spec?["polygon"]);
                    row["shape"] = rectangle ? "rectangle" : "polygon";
                    row["points"] = new JArray(vertices.Select(v => new JArray(v.x, v.y)));
                    if (vertices.Count < (rectangle ? 2 : 3)) { row["status"] = "InvalidGeometry"; continue; }
                    row["insidePlayable"] = vertices.All(v => v.x >= map.Bounds.PlayableMin.x && v.x <= map.Bounds.PlayableMax.x &&
                        v.y >= map.Bounds.PlayableMin.z && v.y <= map.Bounds.PlayableMax.z);
                    var cells = new List<int>();
                    var lo = new Vector2(vertices.Min(v => v.x), vertices.Min(v => v.y));
                    var hi = new Vector2(vertices.Max(v => v.x), vertices.Max(v => v.y));
                    var cLo = WorldToWindow(grid, model, lo); var cHi = WorldToWindow(grid, model, hi);
                    int outside = 0;
                    for (int z = cLo.y; z <= cHi.y; z++)
                        for (int x = cLo.x; x <= cHi.x; x++)
                        {
                            var centre = GridUtils.CellToWorldCenter(grid, new int2(model.X0 + x, model.Z0 + z));
                            if (!AreaContains(rectangle, vertices, centre.x, centre.z)) continue;
                            if (x < 0 || z < 0 || x >= model.W || z >= model.H) { outside++; continue; }
                            cells.Add(z * model.W + x);
                        }
                    row["cellsInside"] = cells.Count;
                    row["cellsOutsidePlayableWindow"] = outside;
                    row["areaSquareMetres"] = (cells.Count + outside) * grid.CellSize * grid.CellSize;
                    row["infantryPassableCells"] = cells.Count(i => model.Infantry[i]);
                    row["vehiclePassableCells"] = cells.Count(i => model.Vehicle[i]);
                    row["truckFootprintCells"] = cells.Count(i => model.Truck[i]);
                    var reach = new JArray();
                    foreach (string role in new[] { "base.player", "staging.player", "base.enemy", "staging.enemy" })
                        foreach (string kind in new[] { "infantry", "truck" })
                        {
                            var entry = new JObject { ["from"] = role, ["kind"] = kind };
                            reach.Add(entry);
                            if (!distances.TryGetValue(role + "|" + kind, out var dist)) { entry["status"] = "NoSource"; continue; }
                            var ok = kind == "infantry" ? model.Infantry : model.Truck;
                            int reachable = 0, nearest = int.MaxValue;
                            foreach (int i in cells)
                                if (ok[i] && dist[i] != int.MaxValue) { reachable++; nearest = math.min(nearest, dist[i]); }
                            entry["reachableCellsInside"] = reachable;
                            entry["nearestPathMetres"] = nearest == int.MaxValue ? JValue.CreateNull() : (JToken)(nearest / 10f * grid.CellSize);
                        }
                    row["reachability"] = reach;
                }
            root["areas"] = areaRows;

            // Pads whose size must come from the native definitions.
            var padRows = new JArray();
            var padSpecs = draft["pads"] as JObject;
            Vector2 forward = Vector2.right, across = Vector2.up;
            if (anchors.TryGetValue("base.player", out var playerBase) && anchors.TryGetValue("base.enemy", out var enemyBase) &&
                Vector2.Distance(playerBase, enemyBase) >= 1f)
            {
                forward = (enemyBase - playerBase).normalized; across = new Vector2(-forward.y, forward.x);
            }
            foreach (string role in new[] { "convoy.origin.1", "convoy.origin.2", "convoy.origin.3", "defense.approach.a", "defense.approach.b" })
            {
                if (!anchors.TryGetValue(role, out var at)) continue;
                var row = new JObject { ["role"] = role, ["at"] = new JArray(at.x, at.y) };
                padRows.Add(row);
                var spec = padSpecs?[role] as JObject;
                if (spec?["at"] != null) at = new Vector2((float)spec["at"][0], (float)spec["at"][1]);
                if (spec?["size"] == null) { row["status"] = "NoDraftSize"; continue; }
                float width = (float)spec["size"][0], depth = (float)spec["size"][1];
                row["draftSizeMetres"] = new JArray(width, depth);
                bool truckBay = role.StartsWith("convoy.", StringComparison.Ordinal);
                var footprint = truckBay ? new Vector2(truckFootprint.x, truckFootprint.y) : new Vector2(tower.x, tower.y);
                var metres = footprint * grid.CellSize;
                row["nativeFootprintMetres"] = new JArray(metres.x, metres.y);
                row["fitsAsIs"] = metres.x > 0 && metres.y > 0 && metres.x <= width && metres.y <= depth;
                row["fitsRotated"] = metres.x > 0 && metres.y > 0 && metres.y <= width && metres.x <= depth;
                int inside = 0, insideFootprint = 0;
                var corners = new[] { at + forward * (width / 2) + across * (depth / 2), at + forward * (width / 2) - across * (depth / 2),
                    at - forward * (width / 2) + across * (depth / 2), at - forward * (width / 2) - across * (depth / 2) };
                var lo = new Vector2(corners.Min(c => c.x), corners.Min(c => c.y));
                var hi = new Vector2(corners.Max(c => c.x), corners.Max(c => c.y));
                var cLo = WorldToWindow(grid, model, lo); var cHi = WorldToWindow(grid, model, hi);
                var passable = truckBay ? model.Truck : model.Vehicle;
                for (int z = cLo.y; z <= cHi.y; z++)
                    for (int x = cLo.x; x <= cHi.x; x++)
                    {
                        if (x < 0 || z < 0 || x >= model.W || z >= model.H) continue;
                        var centre = GridUtils.CellToWorldCenter(grid, new int2(model.X0 + x, model.Z0 + z));
                        var d = new Vector2(centre.x, centre.z) - at;
                        if (Mathf.Abs(Vector2.Dot(d, forward)) > width / 2 || Mathf.Abs(Vector2.Dot(d, across)) > depth / 2) continue;
                        inside++;
                        if (passable[z * model.W + x]) insideFootprint++;
                    }
                row["padCells"] = inside;
                row[truckBay ? "truckFootprintCellsInPad" : "vehiclePassableCellsInPad"] = insideFootprint;
                row["padFrame"] = "forward=base.player->base.enemy (layout local x), across=perpendicular (layout local z)";
            }
            root["pads"] = padRows;

            // Static route lengths for vehicle routes (no truck is driven).
            var routeRows = new JArray();
            var scratch = new UnitPathScratchWorkspace();
            bool scratchReady = false;
            try
            {
                int routeNumber = 0;
                foreach (var routeToken in draft["routes"] as JArray ?? new JArray())
                {
                    routeNumber++;
                    var route = routeToken as JObject; if (route == null) continue;
                    bool vehicles = route["vehicles"] == null || (bool)route["vehicles"];
                    var row = new JObject { ["route"] = routeNumber, ["kind"] = (string)route["kind"], ["vehicles"] = vehicles };
                    routeRows.Add(row);
                    if (!vehicles) { row["status"] = "NotAVehicleRoute"; continue; }
                    var via = ((JArray)route["via"]).Select(x => (string)x).ToArray();
                    row["via"] = new JArray(via);
                    var segments = new JArray();
                    double straight = 0, flood = 0, native = 0; bool floodComplete = true, nativeComplete = truckPrefab != Entity.Null;
                    for (int i = 1; i < via.Length; i++)
                    {
                        var segment = new JObject { ["from"] = via[i - 1], ["to"] = via[i] };
                        segments.Add(segment);
                        if (!anchors.TryGetValue(via[i - 1], out var a) || !anchors.TryGetValue(via[i], out var b)) { segment["status"] = "MissingAnchor"; floodComplete = nativeComplete = false; continue; }
                        straight += Vector2.Distance(a, b);
                        var ca = WorldToWindow(grid, model, a); var cb = WorldToWindow(grid, model, b);
                        int sa = NearestPassable(model.Truck, model, ca.x, ca.y, 16), sb = NearestPassable(model.Truck, model, cb.x, cb.y, 16);
                        if (sa < 0 || sb < 0) { segment["floodStatus"] = "NoTruckCellNearWaypoint"; floodComplete = nativeComplete = false; continue; }
                        var dist = AreaDijkstra(model.Truck, model.W, model.H, sa, sb);
                        if (dist[sb] == int.MaxValue) { segment["floodStatus"] = "Unreachable"; floodComplete = false; }
                        else { segment["floodStatus"] = "Reached"; segment["floodMetres"] = dist[sb] / 10f * grid.CellSize; flood += dist[sb] / 10f * grid.CellSize; }
                        if (truckPrefab != Entity.Null)
                        {
                            if (!scratchReady) { scratch.Initialize(); scratch.Ensure(grid.Width * grid.Height, out _, out _); scratchReady = true; }
                            var native2 = NativeTruckQuery(em, gridEntity, grid, surface, truckPrefab, ref scratch, blockers.Blocked,
                                new int2(model.X0 + sa % model.W, model.Z0 + sa / model.W), new int2(model.X0 + sb % model.W, model.Z0 + sb / model.W), grid.CellSize);
                            segment["native"] = native2;
                            if ((string)native2["status"] == "NativeQueryFoundExactGoal") native += (double)native2["pathMetres"]; else nativeComplete = false;
                        }
                    }
                    row["segments"] = segments;
                    row["straightLineMetres"] = straight;
                    row["floodPathMetres"] = floodComplete ? (JToken)flood : JValue.CreateNull();
                    row["nativePathMetres"] = nativeComplete ? (JToken)native : JValue.CreateNull();
                    double speed = truckDefinition?.ChassisConfig != null ? truckDefinition.ChassisConfig.Speed : 0;
                    double basis = nativeComplete ? native : floodComplete ? flood : 0;
                    row["lengthBasis"] = nativeComplete ? "native" : floodComplete ? "flood" : "none";
                    row["nominalTruckSeconds"] = basis > 0 && speed > 0 ? (JToken)(basis / speed) : JValue.CreateNull();
                    row["timeNote"] = "length / ChassisConfig.Speed; ignores road multiplier and traffic. The convoy deadline is not changed by this probe.";
                }
            }
            finally { if (scratchReady) scratch.Dispose(); }
            root["routes"] = routeRows;
            return root;
        }

        private static JObject NativeTruckQuery(EntityManager em, Entity gridEntity, GridConfig grid, MapSurfaceComponent surface, Entity prefab,
            ref UnitPathScratchWorkspace scratch, NativeBitArray dynamicBlocked, int2 from, int2 to, float cellSize)
        {
            var allocations = new List<IDisposable>();
            using var stream = new NativeStream(1, Allocator.Persistent);
            var result = new JObject();
            try
            {
                NativeArray<T> Make<T>(int count) where T : unmanaged { var value = new NativeArray<T>(count, Allocator.Persistent); allocations.Add(value); return value; }
                var entities = Make<Entity>(1); var units = Make<UnitGrid>(1); units[0] = new UnitGrid { Cell = from };
                var sizes = Make<UnitFootprint>(1); sizes[0] = em.GetComponentData<UnitFootprint>(prefab);
                var behaviors = Make<UnitMovementBehavior>(1); behaviors[0] = em.GetComponentData<UnitMovementBehavior>(prefab);
                var factions = Make<byte>(1); factions[0] = 1;
                var manual = Make<byte>(1); manual[0] = 1;
                var skipped = Make<byte>(1); skipped[0] = 1;
                var requests = Make<UnitPathRequest>(1); requests[0] = new UnitPathRequest { Goal = to };
                var goals = Make<int2>(1); goals[0] = to;
                var status = Make<byte>(1); var failures = Make<int>(1); var expansions = Make<int>(1);
                var job = new PathfindBatchJob
                {
                    Grid = grid, DynamicBlocked = dynamicBlocked, Walkable = em.GetBuffer<GridWalkable>(gridEntity, true).AsNativeArray(),
                    Roads = em.GetBuffer<GridRoad>(gridEntity, true).AsNativeArray(), Sidewalks = em.GetBuffer<GridRoadSidewalk>(gridEntity, true).AsNativeArray(),
                    DirtRoads = em.GetBuffer<GridRoadDirt>(gridEntity, true).AsNativeArray(),
                    MapSurface = surface, HasMapSurface = 1, SurfaceValidation = new MapSurfaceTraversalValidation(),
                    SurfacePathCost = new MapSurfacePathCost(), SurfaceRoadPriority = new MapSurfaceRoadPriorityPolicy(),
                    Entities = entities, UnitGrids = units, Footprints = sizes, MovementBehaviors = behaviors, Factions = factions, ManualMoves = manual,
                    IgnoredOccupancyEntities = Make<Entity>(1), IgnoredOccupancyCells = Make<int2>(1), IgnoredOccupancySizes = Make<int2>(1),
                    RequestedGoals = requests, Goals = goals, Segmented = Make<byte>(1), CheapSegmentModes = Make<byte>(1),
                    AlternateSearchSkipped = skipped, AlternateAttempts = Make<int>(1), Output = stream.AsWriter(), Status = status, FailureCodes = failures,
                    ExpansionCounts = expansions, GridSize = grid.Width * grid.Height, ScratchCameFrom = scratch.CameFrom, ScratchGScore = scratch.GScore,
                    ScratchClosed = scratch.Closed, ScratchInOpen = scratch.InOpen, ScratchEpoch = scratch.Epoch, ScratchOpen = scratch.Open,
                    ScratchPath = scratch.Path, SearchEpochBase = scratch.ReserveEpochs(1)
                };
                job.Execute(0);
                var reader = stream.AsReader(); int count = reader.BeginForEachIndex(0);
                var cells = new List<int2>();
                for (int i = 0; i < count; i++) cells.Add(reader.Read<int2>());
                reader.EndForEachIndex();
                double metres = 0;
                for (int i = 1; i < cells.Count; i++)
                    metres += (cells[i].x != cells[i - 1].x && cells[i].y != cells[i - 1].y ? 1.41421356 : 1.0) * cellSize;
                bool found = status[0] == 1 && goals[0].Equals(to);
                result["status"] = found ? "NativeQueryFoundExactGoal" : "NativeQueryFailed";
                result["failureCode"] = failures[0]; result["expansions"] = expansions[0]; result["pathCells"] = cells.Count;
                if (found) result["pathMetres"] = metres;
                result["queryScope"] = "ManualMoveVehicle_StaticBlockersAndSurface_NoLiveOccupancy";
            }
            finally { foreach (var value in allocations) value.Dispose(); }
            return result;
        }

        private static Entity FindAreaProbePrefab(EntityManager em, string key)
        {
            using var query = em.CreateEntityQuery(typeof(UnitPrefabRegistryEntry));
            using var owners = query.ToEntityArray(Allocator.Temp);
            string wanted = BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(key);
            var found = Entity.Null;
            foreach (var owner in owners)
                foreach (var entry in em.GetBuffer<UnitPrefabRegistryEntry>(owner, true))
                    if (em.Exists(entry.Prefab) && em.HasComponent<UnitSourcePrefabKey>(entry.Prefab) &&
                        BuildingDefinitionPrefabSystemHelper.NormalizeSpawnableKey(em.GetComponentData<UnitSourcePrefabKey>(entry.Prefab).Value.ToString()) == wanted)
                        found = entry.Prefab;
            return found;
        }

        private static List<Vector2> ReadAreaPoints(JToken token)
        {
            var points = new List<Vector2>();
            if (token is JArray array)
                foreach (var item in array)
                    if (item is JArray pair && pair.Count == 2) points.Add(new Vector2((float)pair[0], (float)pair[1]));
            return points;
        }

        private static bool AreaContains(bool rectangle, List<Vector2> points, float x, float z)
        {
            if (rectangle) return x >= points[0].x && x <= points[1].x && z >= points[0].y && z <= points[1].y;
            bool inside = false;
            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
                if ((points[i].y > z) != (points[j].y > z) &&
                    x < (points[j].x - points[i].x) * (z - points[i].y) / (points[j].y - points[i].y) + points[i].x) inside = !inside;
            return inside;
        }

        // Window-relative cell of a world point (may be outside the window).
        private static Vector2Int WorldToWindow(GridConfig grid, AreaGridModel model, Vector2 world)
        {
            var cell = GridUtils.WorldToCell(grid, new float3(world.x, 0f, world.y));
            return new Vector2Int(cell.x - model.X0, cell.y - model.Z0);
        }

        private static int NearestPassable(bool[] ok, AreaGridModel model, int cx, int cz, int radius)
        {
            int best = -1; float bestDistance = float.MaxValue;
            for (int z = cz - radius; z <= cz + radius; z++)
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || z < 0 || x >= model.W || z >= model.H || !ok[z * model.W + x]) continue;
                    float d = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                    if (d < bestDistance) { bestDistance = d; best = z * model.W + x; }
                }
            return best;
        }

        // Dijkstra with the kernel's costs (10 orthogonal, 14 diagonal) and diagonal corner rule; stops at target when >= 0.
        private static int[] AreaDijkstra(bool[] ok, int w, int h, int start, int target)
        {
            var dist = new int[w * h];
            for (int i = 0; i < dist.Length; i++) dist[i] = int.MaxValue;
            var heap = new List<long>();
            void Push(long value)
            {
                heap.Add(value);
                int child = heap.Count - 1;
                while (child > 0)
                {
                    int parent = (child - 1) / 2;
                    if (heap[parent] <= heap[child]) break;
                    (heap[parent], heap[child]) = (heap[child], heap[parent]); child = parent;
                }
            }
            long Pop()
            {
                long top = heap[0]; long last = heap[heap.Count - 1]; heap.RemoveAt(heap.Count - 1);
                if (heap.Count > 0)
                {
                    heap[0] = last; int parent = 0;
                    while (true)
                    {
                        int left = parent * 2 + 1, right = left + 1, smallest = parent;
                        if (left < heap.Count && heap[left] < heap[smallest]) smallest = left;
                        if (right < heap.Count && heap[right] < heap[smallest]) smallest = right;
                        if (smallest == parent) break;
                        (heap[parent], heap[smallest]) = (heap[smallest], heap[parent]); parent = smallest;
                    }
                }
                return top;
            }
            dist[start] = 0; Push((long)0 << 32 | (uint)start);
            while (heap.Count > 0)
            {
                long entry = Pop(); int index = (int)(entry & 0xFFFFFFFFL); int cost = (int)(entry >> 32);
                if (cost > dist[index]) continue;
                if (index == target) break;
                int cx = index % w, cz = index / w;
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = cx + dx, nz = cz + dz;
                        if (nx < 0 || nz < 0 || nx >= w || nz >= h || !ok[nz * w + nx]) continue;
                        bool diagonal = dx != 0 && dz != 0;
                        if (diagonal && (!ok[cz * w + nx] || !ok[nz * w + cx])) continue;
                        int next = nz * w + nx, step = cost + (diagonal ? 14 : 10);
                        if (step >= dist[next]) continue;
                        dist[next] = step; Push((long)step << 32 | (uint)next);
                    }
            }
            return dist;
        }
    }
}
#endif
