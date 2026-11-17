#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Game.Components;
using Game.Composition;
using Game.Configs;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Read-only layout dump of a launched Skirmish source: base anchors, playable bounds, grid, a 4 m
    /// blocked/road/water/scenery bitmap and native IsPlacementValid candidate origins per pad kind.
    /// It rides the same startup-only map-review hook as the Airport source-fit scan
    /// (TickReadOnlyS005AirportSourceFit hands over while layoutDump is armed), so it never touches the
    /// preview, actors, camera, resources, terrain or any asset. Candidates are coarse-stride native
    /// validator hits, not capacity or route certificates.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const int LayoutBitRoad = 1, LayoutBitWater = 2, LayoutBitScenery = 4, LayoutBitInvalid = 8, LayoutBitNotLevel = 16;
        private const int LayoutMaxCandidatesPerDefinition = 4000;

        [Serializable] private sealed class LayoutDumpPoint { public string role; public int faction; public float x, z; }
        [Serializable] private sealed class LayoutDumpDefinition { public string name, kind; public int footprintX, footprintY; public bool scanned; }
        [Serializable]
        private sealed class LayoutDumpCandidate
        {
            public string kind, definition;
            public bool rotated;
            public int originX, originZ, rectX, rectZ, rectW, rectH;
            public float centreX, centreZ;
        }
        [Serializable]
        private sealed class LayoutDump
        {
            public string catalog, scope = "ReadOnlyLayoutDumpNativeValidator";
            public string limitation = "Coarse sampled candidates from the native placement validator; not an exhaustive or capacity/route proof.";
            public string bitmapFlags = "1=road 2=water 4=authoredScenery 8=cachedInvalidCell 16=notSkirmishLevel; runs are value,count pairs, row-major, row 0 = lowest z";
            public bool previewMutated, physicalSourceModified, playerReady, candidatesTruncated;
            public float cellSize;
            public int gridWidth, gridHeight, stride, samples, tested, valid, frames, blockCells, bitmapColumns, bitmapRows;
            public Vector3 gridOrigin, playableMin, playableMax;
            public Vector2Int logicalMinCell, logicalMaxCell, bitmapOriginCell;
            public List<LayoutDumpPoint> bases = new();
            public List<LayoutDumpPoint> buildings = new();
            public List<LayoutDumpDefinition> definitions = new();
            public List<int> bitmapRuns = new();
            public List<LayoutDumpCandidate> candidates = new();
        }

        private static readonly (string kind, string[] keys)[] LayoutDumpKinds =
        {
            ("barracks", new[] { "barrack", "soldier base" }),
            ("staging", new[] { "staging", "soldier tent" }),
            ("supply", new[] { "oil pump", "refinery", "fuel" }),
            ("service", new[] { "factory", "fabrication", "depot" }),
            ("helipad", new[] { "helipad" }),
            ("airport", new[] { "airport" })
        };

        private static LayoutDump layoutDump;
        private static bool layoutDumpDone;
        private static int layoutDumpPhase, layoutBlockCursor, layoutTargetIndex, layoutCandidateCursor, layoutTargetCandidates;
        private static int layoutRunValue = -1, layoutRunCount;
        private static readonly List<(string kind, BuildingDefinition definition)> layoutTargets = new();

        /// <summary>
        /// Layout dump for ANY authored catalog named by WARLINE_SKIRMISH_CATALOG (generic dispatch).
        /// Optional: WARLINE_LAYOUT_DUMP_OUT (copy path), WARLINE_LAYOUT_DUMP_STRIDE (cells, default 16),
        /// WARLINE_LAYOUT_DUMP_KINDS (comma list of barracks,staging,supply,service,helipad,airport).
        /// </summary>
        public static void RunCatalogLayoutDumpAndExit()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WARLINE_SKIRMISH_CATALOG")))
                throw new InvalidOperationException("RunCatalogLayoutDumpAndExit requires WARLINE_SKIRMISH_CATALOG.");
            RunExistingCampaignAirfieldSourceFit(true, RunFocusedAriaAndExit, "ReadOnlyLayoutDumpViaGenericCatalogDispatch", true);
        }

        /// <summary>Layout dump of the source S004 already binds (CityEdgeAirfield, the AP/DB source).</summary>
        public static void RunExistingCampaignLayoutDumpAndExit() =>
            RunExistingCampaignAirfieldSourceFit(true, null, "ReadOnlyLayoutDumpViaProtectedS004Binding", true);

        private static void ResetLayoutDump(bool armed)
        {
            layoutDump = armed ? new LayoutDump() : null;
            layoutDumpDone = false;
            layoutDumpPhase = layoutBlockCursor = layoutTargetIndex = layoutCandidateCursor = layoutTargetCandidates = 0;
            layoutRunValue = -1;
            layoutRunCount = 0;
            layoutTargets.Clear();
        }

        private static bool LayoutDumpHasStarted() => layoutDump != null && layoutDump.frames > 0;

        private static bool TickLayoutDump()
        {
            if (layoutDumpDone) return true;
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var source = FindAirportPlacementSource(scene?.MatchBootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,
                new HashSet<object>(), 0);
            if (source == null || !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em) ||
                !ReadAirportGrid(source, out _, out var grid, out var roads, out var blockers))
                throw new InvalidOperationException("Layout dump has no native placement source/grid.");
            var map = Game.Runtime.SkirmishS004Environment.LoadMapForSession(em);
            if (map == null)
                throw new InvalidOperationException("Layout dump has no launched logical map.");
            var logicalMin = GridUtils.WorldToCell(grid, map.Bounds.PlayableMin);
            var logicalMax = GridUtils.WorldToCell(grid, map.Bounds.PlayableMax);
            int startX = math.max(0, logicalMin.x), startZ = math.max(0, logicalMin.y);
            int endX = math.min(grid.Width, logicalMax.x), endZ = math.min(grid.Height, logicalMax.y);
            var dump = layoutDump;
            var cache = source.BuildingPlacementInvalidCellCacheCompositionSystemHelper;
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
            cache.EnsureAuthoredGeometry(em, source.BuildingGameplayEcsQueryCompositionSystemHelper.SurfaceQuery, grid);
            using var surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            bool hasSurface = surfaceQuery.CalculateEntityCount() == 1;
            var surface = hasSurface ? surfaceQuery.GetSingleton<MapSurfaceComponent>() : default;
            var budget = System.Diagnostics.Stopwatch.StartNew();
            if (dump.frames == 0) deadline = EditorApplication.timeSinceStartup + 1500d;
            dump.frames++;
            if (dump.frames % 60 == 0)
                Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " phase=Progress step=" + layoutDumpPhase +
                    " blocks=" + layoutBlockCursor + " candidates=" + dump.candidates.Count + " frames=" + dump.frames);

            if (layoutDumpPhase == 0)
            {
                dump.catalog = catalogId;
                dump.cellSize = grid.CellSize;
                dump.gridWidth = grid.Width;
                dump.gridHeight = grid.Height;
                dump.gridOrigin = grid.Origin;
                dump.playableMin = map.Bounds.PlayableMin;
                dump.playableMax = map.Bounds.PlayableMax;
                dump.logicalMinCell = new Vector2Int(logicalMin.x, logicalMin.y);
                dump.logicalMaxCell = new Vector2Int(logicalMax.x, logicalMax.y);
                CollectLayoutDumpContext(em, source, dump);
                dump.blockCells = math.max(1, (int)math.round(4f / math.max(grid.CellSize, 0.01f)));
                dump.bitmapOriginCell = new Vector2Int(startX, startZ);
                dump.bitmapColumns = (endX - startX + dump.blockCells - 1) / dump.blockCells;
                dump.bitmapRows = (endZ - startZ + dump.blockCells - 1) / dump.blockCells;
                if (!int.TryParse(Environment.GetEnvironmentVariable("WARLINE_LAYOUT_DUMP_STRIDE"), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out dump.stride) || dump.stride < 4 || dump.stride > 64)
                    dump.stride = 16;
                layoutDumpPhase = 1;
                return false;
            }

            if (layoutDumpPhase == 1)
            {
                int blockTotal = dump.bitmapColumns * dump.bitmapRows;
                int bc = dump.blockCells, iterations = 0;
                while (layoutBlockCursor < blockTotal)
                {
                    if ((++iterations & 63) == 0 && budget.Elapsed.TotalMilliseconds >= 8) break;
                    int index = layoutBlockCursor++;
                    int x = startX + (index % dump.bitmapColumns) * bc, z = startZ + (index / dump.bitmapColumns) * bc;
                    var rect = new RectInt(x, z, math.min(bc, grid.Width - x), math.min(bc, grid.Height - z));
                    int value = 0;
                    if (cache.HasRoadInFootprint(source.BuildingPlacementStartupSystemHelper, grid, rect.position, rect.size)) value |= LayoutBitRoad;
                    if (cache.RoadSurfaces.OverlapsWater(grid, rect.position, rect.size)) value |= LayoutBitWater;
                    if (cache.AuthoredBuildings.Overlaps(grid, rect.position, rect.size)) value |= LayoutBitScenery;
                    if (cache.HasCachedInvalidCellInFootprint(rect.position, rect.size)) value |= LayoutBitInvalid;
                    if (hasSurface && !BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect)) value |= LayoutBitNotLevel;
                    if (value == layoutRunValue) layoutRunCount++;
                    else
                    {
                        if (layoutRunCount > 0) { dump.bitmapRuns.Add(layoutRunValue); dump.bitmapRuns.Add(layoutRunCount); }
                        layoutRunValue = value;
                        layoutRunCount = 1;
                    }
                }
                if (layoutBlockCursor < blockTotal) return false;
                if (layoutRunCount > 0) { dump.bitmapRuns.Add(layoutRunValue); dump.bitmapRuns.Add(layoutRunCount); }
                layoutDumpPhase = 2;
                return false;
            }

            // Phase 2: native IsPlacementValid candidates per resolved definition (full validator is the only acceptance test).
            int columns = (endX - startX + dump.stride - 1) / dump.stride;
            int rows = (endZ - startZ + dump.stride - 1) / dump.stride;
            int total = columns * rows * 2;
            int frameSamples = 0;
            while (layoutTargetIndex < layoutTargets.Count)
            {
                var (kind, definition) = layoutTargets[layoutTargetIndex];
                while (layoutCandidateCursor < total && frameSamples++ < 64 && budget.Elapsed.TotalMilliseconds < 8)
                {
                    int index = layoutCandidateCursor++;
                    bool rotated = (index & 1) != 0;
                    int cell = index / 2;
                    var origin = new Vector2Int(startX + (cell % columns) * dump.stride, startZ + (cell / columns) * dump.stride);
                    dump.samples++;
                    var rect = AirportEffectiveRect(source, definition, origin, grid, rotated);
                    if (rect.xMin < logicalMin.x || rect.yMin < logicalMin.y || rect.xMax > logicalMax.x || rect.yMax > logicalMax.y ||
                        rect.xMin < 0 || rect.yMin < 0 || rect.xMax > grid.Width || rect.yMax > grid.Height) continue;
                    dump.tested++;
                    if (cache.HasCachedInvalidCellInFootprint(rect.position, rect.size)) continue;
                    if (!source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source, definition,
                        origin, definition.FootprintCells, rotated, grid, roads, blockers, AirportEffectiveRect, AirportOverlapsOccupant))
                        continue;
                    dump.valid++;
                    if (layoutTargetCandidates >= LayoutMaxCandidatesPerDefinition) { dump.candidatesTruncated = true; continue; }
                    layoutTargetCandidates++;
                    var footprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(definition, rotated);
                    Vector3 centre = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, grid,
                        source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                    dump.candidates.Add(new LayoutDumpCandidate
                    {
                        kind = kind, definition = definition.DisplayName, rotated = rotated, originX = origin.x, originZ = origin.y,
                        rectX = rect.x, rectZ = rect.y, rectW = rect.width, rectH = rect.height, centreX = centre.x, centreZ = centre.z
                    });
                }
                if (layoutCandidateCursor < total) return false;
                layoutTargetIndex++;
                layoutCandidateCursor = 0;
                layoutTargetCandidates = 0;
            }

            string json = JsonUtility.ToJson(dump, false);
            string path = LiveTracePath() + ".layout-dump.json";
            File.WriteAllText(path, json);
            string outPath = Environment.GetEnvironmentVariable("WARLINE_LAYOUT_DUMP_OUT");
            if (!string.IsNullOrEmpty(outPath))
            {
                outPath = Path.GetFullPath(outPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                File.WriteAllText(outPath, json);
                Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " copy=" + outPath);
                Environment.SetEnvironmentVariable("WARLINE_LAYOUT_DUMP_OUT", null);
            }
            Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " result=Completed stride=" + dump.stride + " bases=" + dump.bases.Count +
                " bitmapRuns=" + dump.bitmapRuns.Count / 2 + " definitions=" + dump.definitions.Count + " samples=" + dump.samples +
                " tested=" + dump.tested + " nativeValid=" + dump.valid + " candidates=" + dump.candidates.Count +
                " frames=" + dump.frames + " previewMutated=0 physicalSourceModified=0 playerReady=0 path=" + path);
            Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", null);
            layoutDumpDone = true;
            return true;
        }

        private static void CollectLayoutDumpContext(EntityManager em, BuildingGameplaySourceCompositionSystemHelper source, LayoutDump dump)
        {
            if (SkirmishLaunchProjection.TryGet(em, out _, out var match))
            {
                if (em.Exists(match.PlayerMainBase) && em.HasComponent<LocalTransform>(match.PlayerMainBase))
                {
                    var p = em.GetComponentData<LocalTransform>(match.PlayerMainBase).Position;
                    dump.bases.Add(new LayoutDumpPoint { role = "base.player", faction = 1, x = p.x, z = p.z });
                }
                if (em.Exists(match.EnemyMainBase) && em.HasComponent<LocalTransform>(match.EnemyMainBase))
                {
                    var p = em.GetComponentData<LocalTransform>(match.EnemyMainBase).Position;
                    dump.bases.Add(new LayoutDumpPoint { role = "base.enemy", faction = 2, x = p.x, z = p.z });
                }
            }
            using (var query = em.CreateEntityQuery(typeof(RuntimeBuildingCombatInfo), typeof(SkirmishAttemptOwnedComponent), typeof(LocalTransform)))
            using (var entities = query.ToEntityArray(Allocator.Temp))
                foreach (var entity in entities)
                {
                    if (dump.buildings.Count >= 512) break;
                    var p = em.GetComponentData<LocalTransform>(entity).Position;
                    dump.buildings.Add(new LayoutDumpPoint { role = "building", faction = em.GetComponentData<SkirmishAttemptOwnedComponent>(entity).FactionId,
                        x = p.x, z = p.z });
                }
            // Every configured definition is listed; only keyword-matched ones are scanned. Nothing is guessed:
            // a kind with no matching display name simply has no candidates.
            var wanted = Environment.GetEnvironmentVariable("WARLINE_LAYOUT_DUMP_KINDS");
            var filter = string.IsNullOrEmpty(wanted) ? null : new HashSet<string>(wanted.ToLowerInvariant().Split(','));
            for (int i = 0; i < 256; i++)
            {
                if (!source.BuildingDefinitionPrefabSystemHelper.TryGetConfiguredDefinition(i, out var definition)) continue;
                string name = (definition.DisplayName ?? string.Empty).ToLowerInvariant();
                string matched = null;
                foreach (var (kind, keys) in LayoutDumpKinds)
                {
                    foreach (var key in keys) if (name.Contains(key)) { matched = kind; break; }
                    if (matched != null) break;
                }
                bool scan = matched != null && (filter == null || filter.Contains(matched));
                dump.definitions.Add(new LayoutDumpDefinition { name = definition.DisplayName, kind = matched ?? string.Empty,
                    footprintX = definition.FootprintCells.x, footprintY = definition.FootprintCells.y, scanned = scan });
                if (scan) layoutTargets.Add((matched, definition));
            }
        }
    }
}
#endif
