#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Game.Components;
using Game.Composition;
using Game.Runtime;
using Game.Skirmish.Contracts;
using Game.UI.Contracts;
using Game.UI.Runtime;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public static partial class SkirmishS002AriaRunHarness
    {
        private static bool airportPlacementOnly, airportSiteTouched, airportRotated, airportJointPlacementOnly;
        private static int airportPlacementStage, airportSiteIndex;
        private static int airportCameraStep, airportCameraAttempts;
        private static int airportCameraCommandsBefore;
        private static Vector3 airportCameraSite;
        private static bool airportCameraPending;
        private static bool airportCameraNavigationOnly, airportCameraProbeStarted, airportCameraPreviewOnly, airportCameraPreviewOpened;
        private static MatchHudMinimapView observedAirportCameraMap;

        private static readonly List<Vector2Int> placementSearchOrigins = new();
        private static BuildingDefinition placementSearchDefinition;
        private static string placementSearchKind;
        private static int placementSearchCursor, placementSearchTested, placementSearchClear, placementSearchFrames;
        private static readonly List<Vector3> airportCandidateSites = new();
        private static readonly Dictionary<Vector3, bool> airportCandidateRotations = new();

        private static void ResetAirportPlacementProbe()
        {
            airportPlacementOnly = Environment.GetEnvironmentVariable("WARLINE_S005_AIRPORT_PLACEMENT_PROBE") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_AIRPORT_PLACEMENT_PROBE", null);
            airportJointPlacementOnly = Environment.GetEnvironmentVariable("WARLINE_S005_JOINT_AIRPORT_PLACEMENT_PROBE") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_JOINT_AIRPORT_PLACEMENT_PROBE", null);
            airportPlacementStage = airportSiteIndex = 0;
            airportSiteTouched = airportRotated = false;
            airportCameraStep = airportCameraAttempts = 0;
            airportCameraCommandsBefore = 0;
            airportCameraPending = false;
            airportCameraSite = default;
            airportCameraNavigationOnly = Environment.GetEnvironmentVariable("WARLINE_S005_AIRPORT_CAMERA_ONLY") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_AIRPORT_CAMERA_ONLY", null);
            airportCameraProbeStarted = false;
            airportCameraPreviewOnly = Environment.GetEnvironmentVariable("WARLINE_S005_CAMERA_PREVIEW_ONLY") == "1";
            Environment.SetEnvironmentVariable("WARLINE_S005_CAMERA_PREVIEW_ONLY", null);
            airportCameraPreviewOpened = false;
            observedAirportCameraMap = null;
            placementSearchDefinition = null;
            placementSearchOrigins.Clear();
            airportCandidateSites.Clear();
            airportCandidateRotations.Clear();
        }

        public static void RunS005EditorJointAirportPlacementProbe()
        {
            RunS005EditorAirportPlacementProbe();
            Environment.SetEnvironmentVariable("WARLINE_S005_JOINT_AIRPORT_PLACEMENT_PROBE", "1");
        }

        public static void RunS005EditorSouthernAirportCameraProbe()
        {
            RunS005EditorSaveResumeEn();
            Environment.SetEnvironmentVariable("WARLINE_S005_AIRPORT_CAMERA_ONLY", "1");
        }

        public static void RunS005EditorSouthernPlacementCameraProbe()
        {
            RunS005EditorSouthernAirportCameraProbe();
            Environment.SetEnvironmentVariable("WARLINE_S005_CAMERA_PREVIEW_ONLY", "1");
        }

        private static void TickAirportCameraNavigationProbe(double now)
        {
            if (airportCameraPreviewOnly && !airportCameraPreviewOpened)
            {
                var preview = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
                if (preview != null && preview.HasPendingPlacement) airportCameraPreviewOpened = true;
                else
                {
                    var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
                    var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                    var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                    var button = drawer != null && drawer.IsOpen
                        ? catalog?.ResolveCatalogTarget(BuildDrawerCategory.Buildings, "Building_Ammunition_Depot") : controls?.BuildButton;
                    if (!PlayerResumeTap(button)) PlayerResumeScrollTo(button);
                    return;
                }
            }
            if (!airportCameraProbeStarted)
            {
                if (!BeginAirportCameraFocus(new Vector3(1399f, 0f, 177.5f), false))
                    throw new InvalidOperationException("Southern Airport camera probe could not start.");
                airportCameraProbeStarted = true;
            }
            if (airportCameraPending) { FocusAirportSiteThroughMap(now); return; }
            Vector3 point = Camera.main != null ? Camera.main.WorldToScreenPoint(airportCameraSite) : Vector3.zero;
            bool visible = point.z > 0 && Screen.safeArea.Contains(point);
            if (airportCameraPreviewOnly)
            {
                var preview = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
                var group = preview != null ? preview.GetComponent<CanvasGroup>() : null;
                bool preserved = preview != null && preview.HasPendingPlacement;
                bool restored = group != null && group.alpha > .99f && group.blocksRaycasts && group.interactable &&
                    preview.CancelButton != null && preview.CancelButton.IsInteractable() &&
                    preview.RotateButton != null && preview.RotateButton.IsInteractable();
                Debug.Log("[S005PreviewAfterMapClose] preserved=" + preserved + " controlsRestored=" + restored);
                if (!preserved || !restored)
                    throw new InvalidOperationException("Full-map navigation lost the pending building preview or placement controls.");
            }
            Debug.Log("[S005SouthernAirportCamera] result=" + (visible ? "Passed" : "Failed") +
                " input=Touch target=" + airportCameraSite + " screen=" + point +
                " camera=" + (Camera.main != null ? Camera.main.transform.position : Vector3.zero) +
                " troopCommandsAdded=0 construction=NotClaimed playerReady=0");
            UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".southern-camera-result.png");
            if (!visible) throw new InvalidOperationException("Normal full-map southern camera target is outside the native view.");
            playerResumeProbeActive = false;
            playerResumeTouch.Dispose(); playerResumeTouch = null;
            Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
            Finish(false, "southernAirportCameraOnly");
        }

        private static void TickAirportPlacementProbe(EntityManager em, Entity session, double now)
        {
            var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
            var drawer = UnityEngine.Object.FindAnyObjectByType<BuildDrawerView>(FindObjectsInactive.Include);
            var placement = UnityEngine.Object.FindAnyObjectByType<BuildPlacementConfirmationBarView>(FindObjectsInactive.Include);
            if (hud == null) return;
            if (airportPlacementStage == 4) { TickFixedWingCycleProbe(em, session, now, hud, drawer); return; }
            if (airportPlacementStage == 0)
            {
                if (drawer != null && drawer.IsOpen) { PlayerResumeTap(drawer.CloseButton); return; }
                if (PlayerResumeTap(hud.PlayerFocusButton)) airportPlacementStage = 1;
                return;
            }
            if (airportPlacementStage == 1)
            {
                if (placement != null && placement.HasPendingPlacement)
                {
                    if (!FindAirportSourceCandidates()) return;
                    airportPlacementStage = 2;
                    return;
                }
                var catalog = UnityEngine.Object.FindAnyObjectByType<BuildDrawerCatalogRuntimeView>(FindObjectsInactive.Include);
                var controls = UnityEngine.Object.FindAnyObjectByType<MatchOverlayCommandControlsView>();
                var button = drawer != null && drawer.IsOpen
                    ? catalog?.ResolveCatalogTarget(Game.UI.Contracts.BuildDrawerCategory.Buildings, "Building_Airport")
                    : controls?.BuildButton;
                if (!PlayerResumeTap(button)) PlayerResumeScrollTo(button);
                return;
            }
            if (airportPlacementStage == 3)
            {
                if (placement != null && placement.HasPendingPlacement) return;
                if (!SkirmishProductionService.HasLivingProducer(em, session, SkirmishProducerKind.Airport, 1)) return;
                using var query = em.CreateEntityQuery(typeof(SkirmishStructureIdentityComponent), typeof(SkirmishAttemptOwnedComponent));
                using var actors = query.ToEntityArray(Allocator.Temp);
                var attempt = em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
                int liveAirports = 0;
                foreach (var actor in actors)
                {
                    var owner = em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                    var role = em.GetComponentData<SkirmishStructureIdentityComponent>(actor);
                    if (!owner.SessionId.Equals(attempt) || owner.FactionId != 1 || role.Producer != SkirmishProducerKind.Airport) continue;
                    if (!em.HasComponent<SkirmishSharedActorTag>(actor) || owner.IsStructure == 0 || role.DesignatedBase != 0)
                        throw new InvalidOperationException("Committed Airport has invalid native ownership.");
                    if (em.HasComponent<UnitHealth>(actor) && em.GetComponentData<UnitHealth>(actor).Current <= 0) continue;
                    liveAirports++;
                }
                if (liveAirports == 0) throw new InvalidOperationException("Committed Airport has no living native actor.");
                if (airportJointPlacementOnly && liveAirports < 2)
                {
                    Debug.Log("[S005JointAirportPlacement] phase=FirstCommitted nativeLivingAirports=" + liveAirports +
                        " input=Touch secondConstruction=Pending bilateralFactionCapacity=Pending");
                    airportPlacementStage = 1;
                    airportSiteIndex = 0;
                    airportSiteTouched = airportRotated = false;
                    return;
                }
                {
                    if (AriaCommandEvidence.Violations != 0 || playerResumeTouch.UnexpectedSamples != 0 ||
                        playerResumeTouch.PhysicalInterventions != 0)
                        throw new InvalidOperationException("Airport construction input audit failed.");
                    if (airportJointPlacementOnly)
                        Debug.Log("[S005JointAirportPlacement] result=Passed input=Touch nativeLivingAirports=" + liveAirports +
                            " sameFaction=1 jointNativePlacement=1 physicalMapModified=0 logicalBoundsModified=0 " +
                            "bilateralFactionCapacity=Pending taxiParkingCapacity=Pending completeMatch=NotClaimed playerReady=0");
                    Debug.Log("[S005AirportPlacement] result=Passed input=Touch nativeProducer=1 nativeActor=1 originalObjective=0 normalBudget=1 campaignSourceModified=0" +
                        " gestures=" + playerResumeTouch.CompletedGestures + " commands=" + AriaCommandEvidence.AcceptedCommands +
                        " receiptViolations=" + AriaCommandEvidence.Violations + " fixedWingLifecycle=Pending bilateralCapacity=Pending completeMatch=NotClaimed");
                    UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".airport-committed.png");
                    if (fixedWingCycleOnly) { airportPlacementStage = 4; return; }
                    playerResumeProbeActive = false;
                    airportPlacementOnly = false;
                    playerResumeTouch.Dispose(); playerResumeTouch = null;
                    // End this explicitly scoped construction fixture without fabricating
                    // a terminal mission result or adding it to the ARIA win sample.
                    Environment.SetEnvironmentVariable("WARLINE_SKIRMISH_STARTUP_ONLY", "1");
                    Finish(false, "paidAirportConstructionOnly");
                    return;
                }
                return;
            }
            if (placement == null || !placement.HasPendingPlacement)
                throw new InvalidOperationException("Airport preview closed before construction.");
            if (airportCameraPending)
            {
                FocusAirportSiteThroughMap(now);
                return;
            }
            if (airportSiteTouched && placement.ConfirmButton != null && placement.ConfirmButton.IsInteractable())
            {
                if (PlayerResumeTap(placement.ConfirmButton)) airportPlacementStage = 3;
                return;
            }
            if (airportSiteTouched)
                Debug.Log("[S005AirportPlacement] rejected=NativePlacementValidator sample=" + airportSiteIndex +
                    " status=" + (placement.StatusText != null ? placement.StatusText.text : "unavailable"));
            airportSiteTouched = false;
            if (airportSiteIndex >= airportCandidateSites.Count)
                throw new InvalidOperationException("Airport source-fit remains unresolved: sampled native-validator candidates could not be committed through normal input.");
            Vector3 site = airportCandidateSites[airportSiteIndex];
            bool requiredRotation = airportCandidateRotations[site];
            if (airportRotated != requiredRotation)
            {
                if (PlayerResumeTap(placement.RotateButton)) airportRotated = requiredRotation;
                return;
            }
            airportSiteIndex++;
            Vector3 point = Camera.main != null ? Camera.main.WorldToScreenPoint(site) : Vector3.zero;
            if (point.z <= 0 || !Screen.safeArea.Contains(point))
            {
                Debug.Log("[S005PlacementCamera] reason=OutsideView site=" + site + " screen=" + point +
                    " safeArea=" + Screen.safeArea);
                BeginAirportCameraFocus(site); return;
            }
            var hits = new List<RaycastResult>();
            if (EventSystem.current != null)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            if (hits.Count > 0)
            {
                Debug.Log("[S005PlacementCamera] reason=UiOcclusion site=" + site + " screen=" + point +
                    " hit=" + hits[0].gameObject.name);
                BeginAirportCameraFocus(site); return;
            }
            if (!playerResumeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                throw new InvalidOperationException("Airport ground gesture rejected.");
            airportSiteTouched = true;
            playerResumeDue = now + 1;
            Debug.Log("[S005AirportPlacement] phase=Site input=Touch site=" + site + " sample=" + airportSiteIndex);
        }

        private static BuildingGameplaySourceCompositionSystemHelper FindAirportPlacementSource(object value,
            HashSet<object> visited, int depth)
        {
            if (value is BuildingGameplaySourceCompositionSystemHelper source) return source;
            if (value == null || depth > 8 || !visited.Add(value)) return null;
            if (value is Delegate callback) return FindAirportPlacementSource(callback.Target, visited, depth + 1);
            if (value.GetType().Namespace?.StartsWith("Game.", StringComparison.Ordinal) != true) return null;
            foreach (var field in value.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            {
                var found = FindAirportPlacementSource(field.GetValue(value), visited, depth + 1);
                if (found != null) return found;
            }
            return null;
        }

        // Read-only source-fit diagnostic. Never changes the preview, actors, camera,
        // resources or terrain. The real touch and Confirm still decide construction.
        [Serializable]
        private sealed class AirportSourceFitSite
        {
            public Vector2Int origin;
            public bool rotated, insideLogicalPlayable;
            public RectInt effectiveRect;
            public Vector3 centre;
        }
        [Serializable]
        private sealed class AirportSourceFitScan
        {
            public int stride = 32, diagnosticWatchdogSeconds = 600, samples, tested, cachedClear, frames, pairChecks;
            public bool previewMutated, physicalSourceModified, capacityCertified, playerReady, logicalPlayableOnly;
            public string scope = "ReadOnlyWholeExistingSourceCoarseNativeValidator";
            public string limitation = "Coarse sampled sites are not an exhaustive impossibility proof. Non-overlapping footprints do not certify bilateral parking, taxi, launch or return capacity.";
            public List<AirportSourceFitSite> sites = new();
            public List<Vector2Int> playablePairs = new();
        }
        private static AirportSourceFitScan airportSourceFitScan;
        private static int airportSourceFitCursor;
        public static void RunS005AirSourceFitAndExit()
        {
            airportSourceFitScan = new AirportSourceFitScan();
            airportSourceFitCursor = 0;
            RunS005MapReviewAndExit();
            Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", "1");
        }
        public static void RunS005PlayableAirSourceFitAndExit()
        {
            RunS005AirSourceFitAndExit();
            airportSourceFitScan = new AirportSourceFitScan { stride = 16, logicalPlayableOnly = true,
                scope = "ReadOnlyLogicalPlayableCoarseNativeValidator" };
        }

        private static bool TickReadOnlyS005AirportSourceFit()
        {
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var source = FindAirportPlacementSource(scene?.MatchBootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,
                new HashSet<object>(), 0);
            // Read the native resolved definition; the prefab configuration is a different asset type.
            var definition = source?.BuildingDefinitionPrefabSystemHelper.FindConfiguredDefinition("Airport");
            if (source == null || definition == null ||
                !source.BuildingEntityManagerAccessSystem.TryGetEntityManager(out var em) ||
                !ReadAirportGrid(source, out _, out var grid, out var roads, out var blockers))
                throw new InvalidOperationException("Read-only Airport source-fit has no native definition/grid.");
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishOperationMap_S005.asset");
            if (map == null) throw new InvalidOperationException("Read-only Airport source-fit has no logical map.");
            var logicalMin = GridUtils.WorldToCell(grid, map.Bounds.PlayableMin);
            var logicalMax = GridUtils.WorldToCell(grid, map.Bounds.PlayableMax);
            var scan = airportSourceFitScan ??= new AirportSourceFitScan();
            int startX = scan.logicalPlayableOnly ? math.max(0, logicalMin.x) : 0;
            int startZ = scan.logicalPlayableOnly ? math.max(0, logicalMin.y) : 0;
            int endX = scan.logicalPlayableOnly ? math.min(grid.Width, logicalMax.x) : grid.Width;
            int endZ = scan.logicalPlayableOnly ? math.min(grid.Height, logicalMax.y) : grid.Height;
            int columns = (endX - startX + scan.stride - 1) / scan.stride;
            int rows = (endZ - startZ + scan.stride - 1) / scan.stride;
            int total = columns * rows * 2;
            // Count/refresh the unchanged native geometry before budgeting the
            // candidate enumeration itself. Otherwise its first census alone
            // exceeds8ms and leaves this read-only diagnostic at one site/tick.
            source.BuildingGameplayEcsQueryCompositionSystemHelper.EnsureEntityQueries(em);
            source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.EnsureAuthoredGeometry(
                em, source.BuildingGameplayEcsQueryCompositionSystemHelper.SurfaceQuery, grid);
            var budget = System.Diagnostics.Stopwatch.StartNew();
            int frameSamples = 0;
            // This observational fixture needs longer than the opening screenshot
            // watchdog. It never extends a match or a flight-cycle qualification.
            if (scan.frames == 0) deadline = UnityEditor.EditorApplication.timeSinceStartup + scan.diagnosticWatchdogSeconds;
            scan.frames++;
            if (scan.frames % 60 == 0)
                Debug.Log("[S005ReadOnlyAirSourceFit] phase=Progress cursor=" + airportSourceFitCursor +
                    " total=" + total + " nativeValid=" + scan.sites.Count + " frames=" + scan.frames);
            while (airportSourceFitCursor < total && frameSamples++ < 32 && budget.Elapsed.TotalMilliseconds < 8)
            {
                int index = airportSourceFitCursor++;
                bool rotated = (index & 1) != 0;
                int cell = index / 2;
                var origin = new Vector2Int(startX + (cell % columns) * scan.stride, startZ + (cell / columns) * scan.stride);
                scan.samples++;
                var rect = AirportEffectiveRect(source, definition, origin, grid, rotated);
                if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > grid.Width || rect.yMax > grid.Height) continue;
                if (scan.logicalPlayableOnly && (rect.xMin < logicalMin.x || rect.yMin < logicalMin.y ||
                    rect.xMax > logicalMax.x || rect.yMax > logicalMax.y)) continue;
                scan.tested++;
                if (source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasCachedInvalidCellInFootprint(rect.position, rect.size)) continue;
                scan.cachedClear++;
                if (!source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source, definition,
                    origin, definition.FootprintCells, rotated, grid, roads, blockers, AirportEffectiveRect, AirportOverlapsOccupant)) continue;
                var footprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(definition, rotated);
                scan.sites.Add(new AirportSourceFitSite { origin = origin, rotated = rotated, effectiveRect = rect,
                    insideLogicalPlayable = rect.xMin >= logicalMin.x && rect.yMin >= logicalMin.y && rect.xMax <= logicalMax.x && rect.yMax <= logicalMax.y,
                    centre = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint, grid,
                        source.BuildingPlacementStartupSystemHelper.BuildPlaneY) });
            }
            if (airportSourceFitCursor < total) return false;
            for (int a = 0; a < scan.sites.Count; a++)
            for (int b = a + 1; b < scan.sites.Count; b++)
            {
                scan.pairChecks++;
                if (scan.sites[a].insideLogicalPlayable && scan.sites[b].insideLogicalPlayable &&
                    !scan.sites[a].effectiveRect.Overlaps(scan.sites[b].effectiveRect))
                    scan.playablePairs.Add(new Vector2Int(a, b));
            }
            string path = LiveTracePath() + ".airport-source-fit.json";
            System.IO.File.WriteAllText(path, JsonUtility.ToJson(scan, true));
            Debug.Log("[S005ReadOnlyAirSourceFit] result=Completed stride=" + scan.stride + " samples=" + scan.samples +
                " tested=" + scan.tested + " nativeValid=" + scan.sites.Count + " playableNonOverlappingPairs=" + scan.playablePairs.Count +
                " frames=" + scan.frames + " previewMutated=0 physicalSourceModified=0 capacityCertified=0 playerReady=0 path=" + path);
            Environment.SetEnvironmentVariable("WARLINE_S005_READONLY_AIR_SOURCE_FIT", null);
            return true;
        }

        private static bool FindAirportSourceCandidates() =>
            FindPlacementSourceCandidates(airportCandidateSites, 16, true, "Airport", airportCandidateRotations);

        private static bool FindPlacementSourceCandidates(List<Vector3> candidates, int stride, bool includeRotations, string kind, Dictionary<Vector3, bool> rotations = null)
        {
            var scene = UnityEngine.Object.FindAnyObjectByType<MatchSceneView>();
            var source = FindAirportPlacementSource(scene?.MatchBootstrap.BuildingUiCommandContext.CanConfirmBuildingPlacement,
                new HashSet<object>(), 0);
            if (source == null) throw new InvalidOperationException("Missing live Airport placement composition.");
            var active = source.BuildingPlacementLifecycleCompositionSystemHelper.ActivePlacement;
            if (active?.Definition == null || !ReadAirportGrid(source, out _, out var grid, out var roads, out var blockers))
                throw new InvalidOperationException("Missing Airport preview/grid for source-fit diagnostic.");
            var map = UnityEditor.AssetDatabase.LoadAssetAtPath<Game.Configs.OperationMapDefinition>(
                "Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishOperationMap_S005.asset");
            if (map == null) throw new InvalidOperationException("Missing S005 logical map bounds.");
            var min = GridUtils.WorldToCell(grid, map.Bounds.PlayableMin);
            var max = GridUtils.WorldToCell(grid, map.Bounds.PlayableMax);
            if (placementSearchDefinition != active.Definition || placementSearchKind != kind)
            {
                candidates.Clear();
                rotations?.Clear();
                placementSearchOrigins.Clear();
                placementSearchDefinition = active.Definition;
                placementSearchKind = kind;
                placementSearchCursor = placementSearchTested = placementSearchClear = placementSearchFrames = 0;
                if (kind == "Airport" && fixedWingCycleOnly)
                {
                    // Revisit sites paid for through native input in prior runs
                    // on this same Campaign source. Recheck every current rule;
                    // fall back to the bounded search if none remains legal.
                    var recordedSites = new[]
                    {
                        (position: new Vector3(1415f, 0, 177.5f), rotated: false),
                        (position: new Vector3(1399f, 0, 177.5f), rotated: false),
                        (position: new Vector3(825.5f, 0, 671f), rotated: true)
                    };
                    foreach (var recorded in recordedSites)
                    {
                        if (recorded.rotated && !includeRotations) continue;
                        var footprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(active.Definition, recorded.rotated);
                        var relative = ((float3)recorded.position - grid.Origin) / grid.CellSize;
                        var origin = new Vector2Int(Mathf.RoundToInt(relative.x - footprint.x * .5f),
                            Mathf.RoundToInt(relative.z - footprint.y * .5f));
                        var rect = AirportEffectiveRect(source, active.Definition, origin, grid, recorded.rotated);
                        if (rect.xMin < min.x || rect.yMin < min.y || rect.xMax > max.x || rect.yMax > max.y ||
                            source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasCachedInvalidCellInFootprint(rect.position, rect.size) ||
                            !source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source, active.Definition,
                                origin, active.Definition.FootprintCells, recorded.rotated, grid, roads, blockers,
                                AirportEffectiveRect, AirportOverlapsOccupant)) continue;
                        var site = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint,
                            grid, source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                        candidates.Add(site);
                        if (rotations != null) rotations[site] = recorded.rotated;
                    }
                    if (candidates.Count > 0)
                    {
                        placementSearchDefinition = null;
                        Debug.Log("[S005PlacementSourceFit] kind=Airport scope=RecordedPaidSitesRevalidated nativeValid=" +
                            candidates.Count + " fullNativeClearance=1 inputCommitRequired=1 physicalSourceModified=0 capacityCertified=0");
                        return true;
                    }
                }
                for (int z = min.y; z < max.y; z += stride)
                    for (int x = min.x; x < max.x; x += stride)
                        placementSearchOrigins.Add(new Vector2Int(x, z));
                var hud = UnityEngine.Object.FindAnyObjectByType<SkirmishMatchView>();
                var focus = (kind is "Fabrication" or "OilPump" or "Refinery" or "FuelBladder") && paidIndustryPlacementFocus.HasValue
                    ? paidIndustryPlacementFocus.Value : hud != null ? hud.PlayerBaseObjectivePosition : Vector3.zero;
                if (kind == "Airport" && fixedWingCycleOnly && hud != null)
                {
                    // The known objective selects the existing southern legal
                    // Airport site; preserve all native source-clearance checks.
                    focus = hud.EnemyBaseObjectivePosition;
                    Debug.Log("[S005AirportStrategy] phase=SourceSearch focus=KnownEnemyBase" +
                        " nativeClearanceRequired=1 geometryModified=0 flightSavings=NotClaimed");
                }
                var home = GridUtils.WorldToCell(grid, focus);
                var searchFootprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(active.Definition, false);
                placementSearchOrigins.Sort((a, b) =>
                {
                    // Compare building centres, not lower-left footprint corners.
                    double ax = a.x + searchFootprint.x * .5 - home.x, az = a.y + searchFootprint.y * .5 - home.y;
                    double bx = b.x + searchFootprint.x * .5 - home.x, bz = b.y + searchFootprint.y * .5 - home.y;
                    int distance = (ax * ax + az * az).CompareTo(bx * bx + bz * bz);
                    return distance != 0 ? distance : a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x);
                });
            }
            // Qualification search yields to the real player loop. Test the same
            // full native rules, then choose a small nearby sample; do not freeze
            // simulation to exhaustively validate thousands of unused sites.
            // One Airport purchase needs a small fallback set. Waiting for32
            // forced an exhaustive scan when this sector had only8 legal sites.
            // This search does not certify bilateral aircraft capacity.
            int wantedCandidates = kind == "Airport" ? 3 : 32;
            int rotationCount = includeRotations ? 2 : 1;
            int totalSamples = placementSearchOrigins.Count * rotationCount;
            int frameSamples = 0;
            var budget = System.Diagnostics.Stopwatch.StartNew();
            placementSearchFrames++;
            while (placementSearchCursor < totalSamples && candidates.Count < wantedCandidates &&
                frameSamples++ < 128 && budget.Elapsed.TotalMilliseconds < 8)
            {
                int sample = placementSearchCursor++;
                bool rotated = includeRotations && sample % rotationCount == 1;
                var origin = placementSearchOrigins[sample / rotationCount];
                var rect = AirportEffectiveRect(source, active.Definition, origin, grid, rotated);
                if (rect.xMin < min.x || rect.yMin < min.y || rect.xMax > max.x || rect.yMax > max.y) continue;
                placementSearchTested++;
                if (source.BuildingPlacementInvalidCellCacheCompositionSystemHelper.HasCachedInvalidCellInFootprint(rect.position, rect.size)) continue;
                placementSearchClear++;
                if (!source.BuildingPlacementAdapterCompositionSystemHelper.IsPlacementValid(source, active.Definition,
                    origin, active.Definition.FootprintCells, rotated, grid, roads, blockers,
                    AirportEffectiveRect, AirportOverlapsOccupant)) continue;
                // The paid industry strategy must leave vehicle approach lanes
                // between purchases. Merely non-overlapping depots can enclose
                // the Oil Pump and stop otherwise healthy three-cell trucks.
                // This is candidate selection only; native placement rules and
                // the player's ability to choose other sites stay unchanged.
                if (kind is "Fabrication" or "OilPump" or "FuelBladder" or "Refinery")
                {
                    const int serviceLaneCells = 8;
                    var clearance = new RectInt(rect.xMin - serviceLaneCells, rect.yMin - serviceLaneCells,
                        rect.width + serviceLaneCells * 2, rect.height + serviceLaneCells * 2);
                    if (AirportOverlapsOccupant(source, clearance)) continue;
                    // Buying both Factories first must leave the compact paid
                    // supply sites observed in native851 available. This only
                    // filters the player's qualification strategy candidates;
                    // it creates no reservation, actor or physical map asset.
                    if (fixedWingCycleOnly && kind == "Fabrication" && !paidIndustryOilReady &&
                        (clearance.Overlaps(new RectInt(906, 562, 15, 4)) ||
                         clearance.Overlaps(new RectInt(954, 562, 25, 15)) ||
                         clearance.Overlaps(new RectInt(986, 534, 14, 10)))) continue;
                }
                var footprint = source.BuildingPlacementGridCameraSystemHelper.GetPlacementFootprint(active.Definition, rotated);
                var site = source.BuildingPlacementGridCameraSystemHelper.GetFootprintCenter(origin, footprint,
                    grid, source.BuildingPlacementStartupSystemHelper.BuildPlaneY);
                if (!candidates.Contains(site))
                {
                    candidates.Add(site);
                    if (rotations != null) rotations[site] = rotated;
                }
            }
            if (candidates.Count < wantedCandidates && placementSearchCursor < totalSamples) return false;
            bool exhausted = placementSearchCursor >= totalSamples;
            placementSearchDefinition = null;
            Debug.Log("[S005PlacementSourceFit] kind=" + kind + " scope=NearbyPlayableSamples stride=" + stride +
                " tested=" + placementSearchTested + " cachedClear=" + placementSearchClear + " nativeValid=" + candidates.Count +
                " frames=" + placementSearchFrames + " scanExhausted=" + (exhausted ? 1 : 0) +
                " previewMutated=0 physicalSourceModified=0 capacityCertified=0");
            if (candidates.Count == 0)
                throw new InvalidOperationException(kind + " source-fit unresolved: no full-validator candidate in coarse playable-sector scan; no global impossibility claim.");
            for (int index = 0; index < Math.Min(candidates.Count, 8); index++)
                Debug.Log("[S005PlacementSourceFit] kind=" + kind + " candidate=" + candidates[index]);
            return true;
        }

        private static bool ReadAirportGrid(BuildingGameplaySourceCompositionSystemHelper source, out Entity entity,
            out GridConfig grid, out DynamicBuffer<GridRoad> roads, out DynamicBlockerComponent blockers) =>
            source.BuildingGridCompositionSystem.TryGetGridData(source,
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager, out entity, out grid, out roads, out blockers);

        private static RectInt AirportEffectiveRect(BuildingGameplaySourceCompositionSystemHelper source,
            BuildingDefinition definition, Vector2Int origin, GridConfig grid, bool rotated) =>
            source.BuildingRuntimeQueryCompositionSystemHelper.GetEffectivePlacementRect(source, definition, origin, grid, rotated);

        private static bool AirportOverlapsOccupant(BuildingGameplaySourceCompositionSystemHelper source, RectInt rect) =>
            source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyRuntimeBuilding(source, rect, ReadAirportGrid, AirportEffectiveRect) ||
            source.BuildingRuntimeQueryCompositionSystemHelper.OverlapsAnyLiveUnitFootprint(source, rect,
                source.BuildingEntityManagerAccessSystem.TryGetEntityManager);

        private static bool BeginAirportCameraFocus(Vector3 site, bool retryAirportSite = true)
        {
            if (airportCameraSite != site) airportCameraAttempts = 0;
            if (airportCameraAttempts >= 3)
            {
                Debug.Log("[S005PlacementCamera] result=SiteSkipped reason=VisibilityAttemptsExhausted site=" + site);
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".airport-camera-failure-" + airportSiteIndex + ".png");
                return false;
            }
            if (retryAirportSite) airportSiteIndex--; // Retry this same site after moving only the camera.
            airportCameraSite = site;
            airportCameraStep = 0;
            airportCameraCommandsBefore = AriaCommandEvidence.AcceptedCommands;
            airportCameraPending = true;
            return true;
        }

        private static string ObserveCameraCanvases(GameObject node)
        {
            var values = new List<string>();
            foreach (var canvas in node.GetComponentsInParent<Canvas>())
                values.Add(canvas.name + ":order=" + canvas.sortingOrder + ":override=" + canvas.overrideSorting + ":mode=" + canvas.renderMode);
            return string.Join("|", values);
        }

        private static void FocusAirportSiteThroughMap(double now)
        {
            var popup = UnityEngine.Object.FindAnyObjectByType<MatchHudFullMapPopupView>(FindObjectsInactive.Include);
            if (popup == null || !popup.IsOpen)
            {
                if (airportCameraStep == 2)
                {
                    if (AriaCommandEvidence.AcceptedCommands != airportCameraCommandsBefore)
                        throw new InvalidOperationException("Airport camera guidance issued a troop command.");
                    airportCameraPending = false; airportCameraStep = 0; return;
                }
                foreach (var map in UnityEngine.Object.FindObjectsByType<MatchHudMinimapView>(FindObjectsSortMode.None))
                {
                    if (map.GetComponentInParent<MatchHudFullMapPopupView>() != null || map.MapRect == null) continue;
                    var canvas = map.GetComponentInParent<Canvas>();
                    Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(camera, map.MapRect.TransformPoint(map.MapRect.rect.center));
                    if (playerResumeTouch.TryGesture(point, point, .15f, 0f, Time.unscaledTime))
                    { airportCameraStep = 1; playerResumeDue = now + 1; }
                    return;
                }
                return;
            }
            if (airportCameraStep == 2)
            { PlayerResumeTap(popup.CloseAction); return; }
            if (airportCameraAttempts >= 3) throw new InvalidOperationException("Airport camera focus was not observed.");
            var fullMap = popup.Minimap;
            if (fullMap == null) return;
            if (airportCameraNavigationOnly && observedAirportCameraMap != fullMap)
            {
                observedAirportCameraMap = fullMap;
                fullMap.FocusRequested += normalized => Debug.Log("[S005CameraFocusObservation] normalized=" + normalized +
                    " cameraAfterNativeFocus=" + (Camera.main != null ? Camera.main.transform.position : Vector3.zero));
            }
            var fullCanvas = fullMap.GetComponentInParent<Canvas>();
            Camera fullCamera = fullCanvas != null && fullCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? fullCanvas.worldCamera : null;
            if (!fullMap.TryGetPresentedMapPoint(airportCameraSite, fullCamera, out var goal)) return;
            if (airportCameraNavigationOnly)
            {
                var raycasts = new List<RaycastResult>();
                EventSystem.current?.RaycastAll(new PointerEventData(EventSystem.current) { position = goal }, raycasts);
                // Internal presentation metadata is observed through reflection only.
                var corners = new Vector3[4]; fullMap.MapRect.GetWorldCorners(corners);
                Debug.Log("[S005CameraMapObservation] target=" + airportCameraSite + " point=" + goal +
                    " presentedProjection=" + typeof(MatchHudMinimapView).GetField("PresentedProjection",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.GetValue(fullMap) +
                    " corners=" + corners[0] + "/" + corners[2] +
                    " preview=" + airportCameraPreviewOnly +
                    " firstHit=" + (raycasts.Count > 0 ? raycasts[0].gameObject.name : "none") +
                    " mapCanvases=" + ObserveCameraCanvases(fullMap.gameObject) +
                    " hitCanvases=" + (raycasts.Count > 0 ? ObserveCameraCanvases(raycasts[0].gameObject) : "none"));
                UnityEngine.ScreenCapture.CaptureScreenshot(LiveTracePath() + ".southern-full-map.png");
            }
            Vector2 from = goal;
            bool drag = fullMap.ViewportRect != null && RectTransformUtility.RectangleContainsScreenPoint(fullMap.ViewportRect, goal, fullCamera);
            if (drag) from = RectTransformUtility.WorldToScreenPoint(fullCamera, fullMap.ViewportRect.TransformPoint(fullMap.ViewportRect.rect.center));
            Vector2 end = drag && (from - goal).sqrMagnitude <= 25f ? goal + Vector2.up * 12f : goal;
            int commandsBefore = AriaCommandEvidence.AcceptedCommands;
            if (!playerResumeTouch.TryGesture(from, end, .12f, drag ? .45f : 0f, Time.unscaledTime)) return;
            Debug.Log("[S005AirportPlacement] cameraInput=FullMapTouch site=" + airportCameraSite +
                " from=" + from + " to=" + end + " drag=" + drag +
                " acceptedCommandsBefore=" + commandsBefore);
            airportCameraAttempts++;
            airportCameraStep = 2;
            playerResumeDue = now + 1;
        }
    }
}
#endif
