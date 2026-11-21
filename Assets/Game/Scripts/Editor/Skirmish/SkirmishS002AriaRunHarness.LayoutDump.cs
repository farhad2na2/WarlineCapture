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
    /// The map bounds come from the runtime OperationMapBoundsComponent of the map the session actually loaded
    /// (any catalog, legacy or generic). When the session fails to start (e.g. BlockedSpawn on the starting
    /// buildings) the dump still runs against the loaded map; it is a read-only diagnostic and records no mission
    /// result. A watchdog quits the Editor after the dump or after a hard timeout.
    /// </summary>
    public static partial class SkirmishS002AriaRunHarness
    {
        private const int LayoutBitRoad = 1, LayoutBitWater = 2, LayoutBitScenery = 4, LayoutBitInvalid = 8, LayoutBitNotLevel = 16, LayoutBitSidewalk = 32;
        private const int LayoutMaxCandidatesPerDefinition = 4000;
        private const double LayoutDumpMaxWaitSeconds = 240d, LayoutDumpHardTimeoutSeconds = 1650d, LayoutDumpExitGraceSeconds = 25d;

        [Serializable] private sealed class LayoutDumpPoint { public string role; public int faction; public float x, z; }
        [Serializable] private sealed class LayoutDumpDefinition { public string name, kind; public int footprintX, footprintY; public bool scanned; public string source = "configured"; }
        [Serializable]
        private sealed class LayoutDumpCandidate
        {
            public string kind, definition, definitionSource = "configured";
            public bool rotated, level = true, nativeValidated = true;
            public int originX, originZ, rectX, rectZ, rectW, rectH;
            public float centreX, centreZ;
        }
        [Serializable]
        private sealed class LayoutDump
        {
            public string catalog, scope = "ReadOnlyLayoutDumpNativeValidator";
            public string limitation = "Coarse sampled candidates from the native placement validator; not an exhaustive or capacity/route proof.";
            public string bitmapFlags = "1=road 2=water 4=authoredScenery(map buildings/trees/props) 8=cachedInvalidCell 16=notSkirmishLevel 32=sidewalk; runs are value,count pairs, row-major, row 0 = lowest z";
            public string predicate = "candidate accepted only if: inside logical playable bounds and grid; not cached-invalid; no authored map building/scenery overlap (AuthoredBuildings.Overlaps = StartingGrant _skirmishBuildings.Overlaps); no road (RoadSurfaces.Overlaps) or water (OverlapsWater); no sidewalk cell and inside grid; then the native IsPlacementValid. Same caches and order as the starting-grant validator.";
            public string sourceOperationMapId = "", boundsSource = "OperationMapBoundsComponent", sessionFailure = "none", sessionPhase = "";
            public string missionResult = "NotRecorded";
            // Requested WARLINE_LAYOUT_DUMP_KINDS with no configured building definition in this session (nothing to scan).
            public string missingKinds = "";
            // Airport fallback: the session configures no Airport, so the definition is built read-only from the S004 air mission's
            // prefab asset (BuildingDefinitionPrefabSystemHelper.CreateDefinition, not registered anywhere). Empty when not used.
            public string airportAssetFallback = "", nativeValidationFailure = "";
            public int candidatesNotNativeValidated;
            public bool surfaceLevelEnforcedByValidator, authoredBuildingsReady, roadSurfacesReady, waterMaskKnown;
            public int rejectedCachedInvalid, rejectedBuilding, rejectedRoad, rejectedWater, rejectedSidewalk, rejectedNative, candidatesNotLevel;
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
        // -1 pending, 0 completed, 2 failed. The watchdog quits the Editor with it.
        private static int layoutDumpOutcome = -1;
        private static double layoutDumpWaitSince, layoutDumpArmedAt, layoutDumpIdleSince;
        private static bool layoutDumpSawPlay, layoutDumpDeadlineExtended;
        private static string layoutDumpWaitReason = string.Empty;
        private static readonly List<(string kind, BuildingDefinition definition)> layoutTargets = new();
        private static readonly HashSet<BuildingDefinition> layoutAssetDefinitions = new();
        private static bool layoutAssetNativeBroken;
        private const string LayoutAirportPrefabPath = "Assets/Game/Prefabs/Buildings/Building_Airport.prefab";

        /// <summary>
        /// Layout dump for ANY authored catalog named by WARLINE_SKIRMISH_CATALOG (generic dispatch, legacy S002
        /// included: set WARLINE_S002_SEED 104731|130365|155923 and WARLINE_S002_LOCALE for S002).
        /// Without a catalog, WARLINE_MISSION_DRAFT selects a probe host by campaign_source.
        /// Optional: WARLINE_LAYOUT_DUMP_OUT (copy path), WARLINE_LAYOUT_DUMP_STRIDE (cells, default 16),
        /// WARLINE_LAYOUT_DUMP_KINDS (comma list of barracks,staging,supply,service,helipad,airport).
        /// Run WITHOUT -quit. A session that fails to start (BlockedSpawn) does not stop the dump. The Editor
        /// quits by itself after the dump, a failure or the hard timeout. Records no mission result.
        /// </summary>
        public static void RunCatalogLayoutDumpAndExit()
        {
            string draftPath = Environment.GetEnvironmentVariable("WARLINE_MISSION_DRAFT");
            var draft = string.IsNullOrEmpty(Environment.GetEnvironmentVariable(MeasurementCatalogVariable)) &&
                !string.IsNullOrEmpty(draftPath) ? ReadMeasurementDraft(draftPath, out _) : null;
            RunMeasurementHost(draft, () => RunExistingCampaignAirfieldSourceFit(true, RunFocusedAriaAndExit,
                "ReadOnlyLayoutDumpViaGenericCatalogDispatch", true));
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
            layoutDumpOutcome = -1;
            layoutDumpWaitSince = layoutDumpIdleSince = 0d;
            layoutDumpSawPlay = layoutDumpDeadlineExtended = false;
            layoutDumpWaitReason = string.Empty;
            layoutTargets.Clear();
            layoutAssetDefinitions.Clear();
            layoutAssetNativeBroken = false;
            EditorApplication.update -= LayoutDumpWatchdogTick;
            if (!armed) return;
            layoutDumpArmedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += LayoutDumpWatchdogTick;
        }

        // "Started" includes waiting for the placement source/caches: the Tick must keep routing to the dump, not
        // fall back into the startup roster audit that would Finish the run before the dump begins.
        private static bool LayoutDumpHasStarted() => layoutDump != null && (layoutDump.frames > 0 || layoutDumpWaitSince > 0d);

        /// <summary>
        /// Guarantees the Editor quits. The normal RequestQuitIfAsked path did not fire after an aborted S002 dump
        /// (lane3-dump-S002, cause not proven), so this owns the exit: it restores the Editor play-mode options that
        /// the entry changed, then exits with 0 when the dump completed and 2 otherwise.
        /// </summary>
        private static void LayoutDumpWatchdogTick()
        {
            if (layoutDump == null) { EditorApplication.update -= LayoutDumpWatchdogTick; return; }
            double now = EditorApplication.timeSinceStartup;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { layoutDumpSawPlay = true; layoutDumpIdleSince = 0d; }
            else if (layoutDumpSawPlay)
            {
                if (layoutDumpIdleSince <= 0d) layoutDumpIdleSince = now;
                if (now - layoutDumpIdleSince >= LayoutDumpExitGraceSeconds)
                {
                    LayoutDumpWatchdogExit(layoutDumpOutcome == 0 ? 0 : 2, layoutDumpOutcome == 0 ? "afterDump" : "afterPlayEndedWithoutDump");
                    return;
                }
            }
            if (now - layoutDumpArmedAt >= LayoutDumpHardTimeoutSeconds)
            {
                if (layoutDumpOutcome < 0) layoutDumpOutcome = 2;
                LayoutDumpWatchdogExit(layoutDumpOutcome == 0 ? 0 : 2, "hardTimeout");
            }
        }

        private static void LayoutDumpWatchdogExit(int code, string reason)
        {
            EditorApplication.update -= LayoutDumpWatchdogTick;
            Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " watchdogExit=1 reason=" + reason + " code=" + code +
                " outcome=" + (layoutDumpOutcome == 0 ? "Completed" : layoutDumpOutcome < 0 ? "Pending" : "Failed"));
            try
            {
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
                RestoreS004EditorJourneyOptions();
                AssetDatabase.AllowAutoRefresh();
                SessionState.SetBool("Warline.MissionValidation.ResumeRefresh", false);
                SessionState.EraseBool(QuitPendingKey);
            }
            catch (Exception cleanupError) { Debug.LogError("[SkirmishLayoutDump] watchdogCleanupFailed " + cleanupError); }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// A session that failed to start (BlockedSpawn, content or timeout) never reaches the map-review path that
        /// normally runs the dump. The loaded map, its authored geometry caches and the placement source are still
        /// alive, so the dump runs here instead. Returns true when it consumed the tick. A healthy session returns
        /// false and the dump runs through CaptureNativeReview as before.
        /// </summary>
        private static bool TickLayoutDumpAfterStartupFailure(EntityManager em)
        {
            if (layoutDump == null || layoutDumpDone || !LayoutDumpSessionFailed(em, out string reason)) return false;
            layoutDump.sessionFailure = reason;
            if (!TickLayoutDump()) { next = EditorApplication.timeSinceStartup + 0.01d; return true; }
            Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " sessionFailure=" + reason +
                " dumpRanAfterStartupFailure=1 missionResult=NotRecorded countedWin=0 playerReady=0");
            Finish(false, "layoutDumpOnly:sessionStartupFailure=" + reason);
            return true;
        }

        private static bool LayoutDumpSessionFailed(EntityManager em, out string reason)
        {
            reason = string.Empty;
            using (var expanded = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent)))
                if (expanded.CalculateEntityCount() == 1)
                {
                    var failure = expanded.GetSingleton<SkirmishExpandedSessionComponent>().FailureCode;
                    if (failure != SkirmishReasonCode.None) { reason = failure.ToString(); return true; }
                }
            if (SkirmishLaunchProjection.TryGet(em, out _, out var match) && match.StartupFailure != SkirmishStartupFailureCode.None)
            {
                reason = "match." + match.StartupFailure;
                return true;
            }
            return false;
        }

        private static bool TickLayoutDump()
        {
            if (layoutDumpDone) return true;
            try { return TickLayoutDumpCore(); }
            catch (Exception failure)
            {
                layoutDumpOutcome = 2;
                Debug.LogError("[SkirmishLayoutDump] catalog=" + catalogId + " result=Failed reason=" + failure.GetType().Name + ": " + failure.Message);
                throw;
            }
        }

        private static bool LayoutDumpWaiting(string reason)
        {
            double now = EditorApplication.timeSinceStartup;
            if (layoutDumpWaitSince <= 0d) layoutDumpWaitSince = now;
            layoutDumpWaitReason = reason;
            if (now - layoutDumpWaitSince > LayoutDumpMaxWaitSeconds)
                throw new InvalidOperationException("Layout dump timed out after " + LayoutDumpMaxWaitSeconds + " s waiting for: " + reason);
            return false;
        }

        private static BuildingGameplaySourceCompositionSystemHelper TryFindLayoutDumpSource()
        {
            try
            {
                var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
                return FindAirportPlacementSource(scene?.MatchBootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,
                    new HashSet<object>(), 0);
            }
            catch (NullReferenceException) { return null; } // bootstrap not bound (yet)
        }

        /// <summary>
        /// The playable bounds of the map the session ACTUALLY loaded, read from the runtime singleton the map
        /// bootstrap publishes (OperationMapRuntimeBootstrapSceneSystemHelper). Works for every catalog: legacy S002/S003
        /// (preset map), S004 (environment resource) and generic authored missions (explicit source). The earlier
        /// S004-environment-only map lookup returned null for legacy S002 (lane3-dump-S002).
        /// </summary>
        private static bool TryReadLayoutDumpBounds(EntityManager em, out float3 playableMin, out float3 playableMax, out string sourceId)
        {
            playableMin = playableMax = default;
            sourceId = string.Empty;
            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<OperationMapBoundsComponent>());
            if (query.CalculateEntityCount() != 1) return false;
            var entity = query.GetSingletonEntity();
            var bounds = em.GetComponentData<OperationMapBoundsComponent>(entity);
            playableMin = bounds.PlayableMin;
            playableMax = bounds.PlayableMax;
            if (em.HasComponent<ActiveOperationMapComponent>(entity))
                sourceId = em.GetComponentData<ActiveOperationMapComponent>(entity).OperationMapId.ToString();
            return playableMax.x > playableMin.x && playableMax.z > playableMin.z;
        }

        // Same loop as the starting-grant validator (BuildingRuntimeProcessingCompositionSystemHelper sidewalk check).
        private static bool LayoutRectHasSidewalkOrLeavesGrid(bool[] sidewalks, GridConfig grid, RectInt rect)
        {
            for (int y = rect.yMin; y < rect.yMax; y++)
                for (int x = rect.xMin; x < rect.xMax; x++)
                    if (x < 0 || y < 0 || x >= grid.Width || y >= grid.Height || sidewalks != null && sidewalks[y * grid.Width + x])
                        return true;
            return false;
        }

        private static bool TickLayoutDumpCore()
        {
            var source = TryFindLayoutDumpSource();
            if (source == null || !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em) ||
                !ReadAirportGrid(source, out _, out var grid, out var roads, out var blockers))
                return LayoutDumpWaiting("native placement source/grid");
            if (!TryReadLayoutDumpBounds(em, out var playableMin, out var playableMax, out string sourceId))
                return LayoutDumpWaiting("OperationMapBoundsComponent of the loaded map");
            RequireMeasurementSource(sourceId);
            var cache = source.BuildingPlacementInvalidCellCacheCompositionSystemHelper;
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
            cache.EnsureAuthoredGeometry(em, source.BuildingGameplayEcsQueryCompositionSystemHelper.SurfaceQuery, grid);
            // The starting-grant validator refuses to run until these are ready (it `continue`s). A not-ready
            // AuthoredBuildings cache answers Overlaps=false, i.e. every cell looks free: never sample before it is ready.
            if (!cache.AuthoredBuildings.IsReady || !cache.RoadSurfaces.IsReady)
                return LayoutDumpWaiting("authored building/road geometry caches (buildings=" + cache.AuthoredBuildings.IsReady +
                    " roads=" + cache.RoadSurfaces.IsReady + ")");
            if (!cache.RoadSurfaces.HasKnownWaterMask) return LayoutDumpWaiting("water mask");
            layoutDumpWaitSince = 0d;

            var logicalMin = GridUtils.WorldToCell(grid, playableMin);
            var logicalMax = GridUtils.WorldToCell(grid, playableMax);
            int startX = math.max(0, logicalMin.x), startZ = math.max(0, logicalMin.y);
            int endX = math.min(grid.Width, logicalMax.x), endZ = math.min(grid.Height, logicalMax.y);
            var dump = layoutDump;
            using var surfaceQuery = em.CreateEntityQuery(ComponentType.ReadOnly<MapSurfaceComponent>());
            bool hasSurface = surfaceQuery.CalculateEntityCount() == 1;
            var surface = hasSurface ? surfaceQuery.GetSingleton<MapSurfaceComponent>() : default;
            var sidewalks = cache.GetRoadSidewalks();
            var budget = System.Diagnostics.Stopwatch.StartNew();
            if (dump.frames == 0) deadline = EditorApplication.timeSinceStartup + 1500d;
            dump.frames++;
            if (dump.frames % 60 == 0)
                Debug.Log("[SkirmishLayoutDump] catalog=" + catalogId + " phase=Progress step=" + layoutDumpPhase +
                    " blocks=" + layoutBlockCursor + " candidates=" + dump.candidates.Count + " frames=" + dump.frames);

            if (layoutDumpPhase == 0)
            {
                dump.catalog = catalogId;
                dump.sourceOperationMapId = sourceId;
                dump.cellSize = grid.CellSize;
                dump.gridWidth = grid.Width;
                dump.gridHeight = grid.Height;
                dump.gridOrigin = grid.Origin;
                dump.playableMin = playableMin;
                dump.playableMax = playableMax;
                dump.logicalMinCell = new Vector2Int(logicalMin.x, logicalMin.y);
                dump.logicalMaxCell = new Vector2Int(logicalMax.x, logicalMax.y);
                dump.authoredBuildingsReady = cache.AuthoredBuildings.IsReady;
                dump.roadSurfacesReady = cache.RoadSurfaces.IsReady;
                dump.waterMaskKnown = cache.RoadSurfaces.HasKnownWaterMask;
                dump.sessionPhase = SkirmishLaunchProjection.TryGet(em, out _, out var phaseMatch) ? phaseMatch.Phase.ToString() : "NoMatchState";
                // IsSkirmishSurfaceValid only enforces level ground while the match is Playing.
                dump.surfaceLevelEnforcedByValidator = dump.sessionPhase == nameof(SkirmishPhase.Playing);
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
                    if (LayoutRectHasSidewalkOrLeavesGrid(sidewalks, grid, rect)) value |= LayoutBitSidewalk;
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

            // Phase 2: candidate origins per resolved definition. A candidate must pass the same predicates, in the
            // same order, as the starting-grant validator (BuildingRuntimeProcessingCompositionSystemHelper): authored
            // map buildings, road, water, sidewalk/bounds, then the native IsPlacementValid.
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
                    if (cache.HasCachedInvalidCellInFootprint(rect.position, rect.size)) { dump.rejectedCachedInvalid++; continue; }
                    if (cache.AuthoredBuildings.Overlaps(grid, rect.position, rect.size)) { dump.rejectedBuilding++; continue; }
                    if (cache.RoadSurfaces.Overlaps(grid, rect.position, rect.size)) { dump.rejectedRoad++; continue; }
                    if (cache.RoadSurfaces.OverlapsWater(grid, rect.position, rect.size)) { dump.rejectedWater++; continue; }
                    if (LayoutRectHasSidewalkOrLeavesGrid(sidewalks, grid, rect)) { dump.rejectedSidewalk++; continue; }
                    bool fromAsset = layoutAssetDefinitions.Contains(definition);
                    bool nativeRan = !(fromAsset && layoutAssetNativeBroken), nativeOk = true;
                    if (nativeRan)
                    {
                        try
                        {
                            nativeOk = source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source, definition,
                                origin, definition.FootprintCells, rotated, grid, roads, blockers, AirportEffectiveRect, AirportOverlapsOccupant);
                        }
                        catch (Exception exception) when (fromAsset)
                        {
                            // The validator cannot take the asset-built definition: keep the other predicates only and say so.
                            layoutAssetNativeBroken = true;
                            nativeRan = false;
                            nativeOk = true;
                            dump.nativeValidationFailure = exception.GetType().Name + ": " + exception.Message;
                            Debug.LogWarning("[SkirmishLayoutDump] native IsPlacementValid cannot take the asset-built definition; continuing with the other predicates only (nativeValidated=0): " + dump.nativeValidationFailure);
                        }
                    }
                    if (nativeRan && !nativeOk) { dump.rejectedNative++; continue; }
                    if (!nativeRan) dump.candidatesNotNativeValidated++;
                    dump.valid++;
                    if (layoutTargetCandidates >= LayoutMaxCandidatesPerDefinition) { dump.candidatesTruncated = true; continue; }
                    layoutTargetCandidates++;
                    bool level = !hasSurface || BuildingPlacementAdapterCompositionSystemHelper.IsSkirmishFootprintLevel(surface, rect);
                    if (!level) dump.candidatesNotLevel++;
                    var footprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(definition, rotated);
                    Vector3 centre = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, grid,
                        source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                    dump.candidates.Add(new LayoutDumpCandidate
                    {
                        kind = kind, definition = definition.DisplayName, definitionSource = fromAsset ? "asset" : "configured", nativeValidated = nativeRan,
                        rotated = rotated, level = level, originX = origin.x, originZ = origin.y,
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
                " rejectedCachedInvalid=" + dump.rejectedCachedInvalid + " rejectedBuilding=" + dump.rejectedBuilding +
                " rejectedRoad=" + dump.rejectedRoad + " rejectedWater=" + dump.rejectedWater +
                " rejectedSidewalk=" + dump.rejectedSidewalk + " rejectedNative=" + dump.rejectedNative +
                " candidatesNotLevel=" + dump.candidatesNotLevel + " missingKinds=" + (dump.missingKinds.Length > 0 ? dump.missingKinds : "none") + " airportAsset=" +
                (dump.airportAssetFallback.Length > 0 ? dump.airportAssetFallback.Replace(' ', '_') : "notUsed") + " notNativeValidated=" + dump.candidatesNotNativeValidated + " source=" + dump.sourceOperationMapId +
                " sessionPhase=" + dump.sessionPhase + " sessionFailure=" + dump.sessionFailure +
                " frames=" + dump.frames + " previewMutated=0 physicalSourceModified=0 playerReady=0 missionResult=NotRecorded path=" + path);
            Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", null);
            layoutDumpDone = true;
            layoutDumpOutcome = 0;
            return true;
        }

        /// <summary>
        /// Ground missions do not configure the Airport, yet the air drafts need an Airport pad. Build the definition read-only
        /// from the same prefab and definition metadata the S004 air mission uses. CreateDefinition does not register the
        /// result in any configured list; it is used only as the scan target and flagged definitionSource=asset.
        /// </summary>
        private static void TryAddAirportDefinitionFromAsset(BuildingGameplaySourceCompositionSystemHelper source, LayoutDump dump)
        {
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LayoutAirportPrefabPath);
                if (prefab == null) { dump.airportAssetFallback = "FailedPrefabNotFound:" + LayoutAirportPrefabPath; return; }
                var definition = source.BuildingDefinitionPrefabSystemHelper.CreateDefinition(prefab, "Airport", "Operational building.", 500,
                    null, null, null, source.BuildingRunwaySystem);
                if (definition == null || definition.FootprintCells.x <= 0 || definition.FootprintCells.y <= 0)
                { dump.airportAssetFallback = "FailedEmptyDefinition"; return; }
                layoutAssetDefinitions.Add(definition);
                layoutTargets.Add(("airport", definition));
                dump.definitions.Add(new LayoutDumpDefinition { name = definition.DisplayName, kind = "airport", source = "asset",
                    footprintX = definition.FootprintCells.x, footprintY = definition.FootprintCells.y, scanned = true });
                dump.airportAssetFallback = "Used:" + LayoutAirportPrefabPath + " hasRunway=" + (definition.HasRunway ? 1 : 0);
                Debug.Log("[SkirmishLayoutDump] airport definition built from asset " + LayoutAirportPrefabPath + " footprint=" +
                    definition.FootprintCells + " hasRunway=" + definition.HasRunway + " (not in the session's configured list)");
            }
            catch (Exception exception)
            {
                dump.airportAssetFallback = "Failed:" + exception.GetType().Name + ": " + exception.Message;
                Debug.LogWarning("[SkirmishLayoutDump] airport asset definition could not be built: " + dump.airportAssetFallback);
            }
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
            // A session that failed to start has no spawned main bases: fall back to the resolved setup's base positions
            // (the authored layout values the starting grants were aimed at).
            bool hasPlayerBase = false, hasEnemyBase = false;
            foreach (var point in dump.bases) { hasPlayerBase |= point.faction == 1; hasEnemyBase |= point.faction == 2; }
            if (!hasPlayerBase || !hasEnemyBase)
            {
                using var setupQuery = em.CreateEntityQuery(typeof(SkirmishExpandedSessionComponent), typeof(SkirmishResolvedSetupRecord));
                var setup = setupQuery.CalculateEntityCount() == 1
                    ? em.GetComponentObject<SkirmishResolvedSetupRecord>(setupQuery.GetSingletonEntity()).Setup : null;
                if (setup != null)
                {
                    if (!hasPlayerBase && (setup.PlayerBaseWorldX != 0f || setup.PlayerBaseWorldZ != 0f))
                        dump.bases.Add(new LayoutDumpPoint { role = "base.player", faction = 1, x = setup.PlayerBaseWorldX, z = setup.PlayerBaseWorldZ });
                    if (!hasEnemyBase && (setup.EnemyBaseWorldX != 0f || setup.EnemyBaseWorldZ != 0f))
                        dump.bases.Add(new LayoutDumpPoint { role = "base.enemy", faction = 2, x = setup.EnemyBaseWorldX, z = setup.EnemyBaseWorldZ });
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
            if (filter != null && filter.Contains("airport"))
            {
                bool hasAirport = false;
                foreach (var target in layoutTargets) hasAirport |= target.kind == "airport";
                if (!hasAirport) TryAddAirportDefinitionFromAsset(source, dump);
            }
            if (filter != null)
                foreach (var requested in filter)
                {
                    bool found = false;
                    foreach (var target in layoutTargets) found |= target.kind == requested;
                    if (found || string.IsNullOrEmpty(requested)) continue;
                    dump.missingKinds += (dump.missingKinds.Length > 0 ? "," : "") + requested;
                    Debug.LogWarning("[SkirmishLayoutDump] requested kind '" + requested + "' has no configured building definition in this session; nothing was scanned for it.");
                }
        }
    }
}
#endif
