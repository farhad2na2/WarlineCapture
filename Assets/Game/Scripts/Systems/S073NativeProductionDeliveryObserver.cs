#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Runtime
{
    // Opt-in, passive observation of real S073 production ticks. Observer failures
    // are swallowed by every entry point so they cannot change production behavior.
    internal static class S073NativeProductionDeliveryObserver
    {
        private const string OutputKey = "WARLINE_S073_PRODUCTION_DELIVERY_OUTPUT";
        private const int MaxDropSearchObservationsPerSession = 32;
        private const int MaxStaticBoundsObservationsPerSession = 32;
        private static bool enabled => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(OutputKey));
        private static string currentSession;
        private static bool currentTickIsS073;
        private static TickSnapshot current;
        private static bool currentRandomStateHooksAvailable;
        private static Func<uint> currentRandomStateGetter;
        private static bool tickRecorded;
        private static bool currentTickHasLaunch;
        private static bool noEventTickRecorded;
        private static string staticBoundsSession;
        private static int staticBoundsObservationCount;
        private static int staticBoundsSkippedCount;
        private static string ownedOutputPath;
        private static int observerErrorCount;
        private static string lastObserverError;
        private static string dropSearchSession;
        private static int dropSearchObservationCount;

        internal static bool IsEnabled => enabled;
        internal static int ObserverErrorCount => observerErrorCount;
        internal static string LastObserverError => lastObserverError;
        internal static int StaticBoundsObservationCountFor(string session) =>
            string.Equals(staticBoundsSession, session, StringComparison.Ordinal) ? staticBoundsObservationCount : 0;
        internal static int StaticBoundsSkippedCountFor(string session) =>
            string.Equals(staticBoundsSession, session, StringComparison.Ordinal) ? staticBoundsSkippedCount : 0;

        internal readonly struct LaunchInputs
        {
            internal readonly bool randomStateObserved;
            internal readonly uint randomStateBefore;
            internal readonly bool cameraObserved;
            internal readonly bool cameraPresent;
            internal readonly Vector3 cameraPosition;
            internal readonly Vector3 cameraForward;
            internal readonly Vector3 cameraRight;

            internal LaunchInputs(bool randomObserved, uint randomBefore, bool cameraWasObserved,
                bool hasCamera, Vector3 position, Vector3 forward, Vector3 right)
            {
                randomStateObserved = randomObserved;
                randomStateBefore = randomBefore;
                cameraObserved = cameraWasObserved;
                cameraPresent = hasCamera;
                cameraPosition = position;
                cameraForward = forward;
                cameraRight = right;
            }
        }

        internal static LaunchInputs CaptureHelicopterLaunchInputs(Camera camera, uint randomState)
        {
            if (!enabled) return default;
            try
            {
                if (camera == null)
                    return new LaunchInputs(true, randomState, true, false, default, default, default);
                return new LaunchInputs(true, randomState, true, true,
                    camera.transform.position, camera.transform.forward, camera.transform.right);
            }
            catch (Exception error)
            {
                RecordObserverError("LaunchInputs", error);
                return new LaunchInputs(true, randomState, false, false, default, default, default);
            }
        }

        internal static HelicopterDropSearchDiagnostics CreateDropSearchDiagnostics()
        {
            try
            {
                if (!enabled) return null;
                return new HelicopterDropSearchDiagnostics();
            }
            catch (Exception error)
            {
                RecordObserverError("DropSearchDiagnosticsCreate", error);
                return null;
            }
        }

        [Serializable]
        private sealed class TickSnapshot
        {
            public int schema_version = 1;
            public string kind = "S073NativeProductionDeliveryObservation";
            public string scope = "PassiveSameSessionOriginalProductionTick_NotReadinessOrMissionAcceptance";
            public string session_id;
            public string world_name;
            public int frame = -1;
            public string tick_owner;
            public string phase;
            public float unity_time;
            public float delta_time;
            public uint random_state_before;
            public uint random_state_after;
            public bool random_state_observed;
            public bool random_state_source_hooks_available;
            public bool random_state_getter_before_observed;
            public uint random_state_getter_before;
            public bool random_state_getter_before_matches_tick_input;
            public bool random_state_getter_after_observed;
            public uint random_state_getter_after;
            public bool random_state_getter_after_matches_tick_output;
            public bool random_state_getter_verification_passed;
            public uint launch_random_state_before;
            public uint launch_random_state_after;
            public bool launch_random_state_observed;
            public bool launch_random_state_local_value_observed;
            public bool launch_random_state_source_hooks_available;
            public int launch_frame = -1;
            public bool launch_camera_present;
            public float[] launch_camera_position;
            public float[] launch_camera_forward;
            public float[] launch_camera_right;
            public bool active_transport_after_tick;
            public bool production_event_present;
            public string production_event_status;
            public string transport_event;
            public float transport_event_match_time;
            public int transport_runtime_building_id = -1;
            public string transport_producer;
            public int transport_owner_faction_id;
            public string transport_unit_prefab;
            public int transport_production_index = -1;
            public float transport_production_started_at;
            public float transport_production_ready_at;
            public string transport_mode;
            public byte transport_phase;
            public int transport_delivered_unit_count;
            public int transport_drop_cell_x;
            public int transport_drop_cell_y;
            public float[] transport_drop_position;
            public bool transport_spawn_succeeded;
            public int delivered_entity_index = -1;
            public int delivered_entity_version;
            public int delivered_cell_x;
            public int delivered_cell_y;
            public float[] delivered_position;
            public bool static_render_bounds_observed;
            public int static_render_bounds_count;
            public string static_render_bounds_event = "NotObservedInThisTick";
            public float[] static_bounds_area;
            public float static_bounds_min_extent;
            public float static_bounds_max_extent;
            public BoundsSnapshot[] static_bounds;
            public string static_bounds_provider_type;
            public int static_bounds_provider_identity;
            public bool static_bounds_provider_reference_stable;
            public int static_bounds_grid_entity_index = -1;
            public int static_bounds_grid_entity_version;
            public int static_bounds_grid_width;
            public int static_bounds_grid_height;
            public float static_bounds_grid_cell_size;
            public float[] static_bounds_grid_origin;
            public int static_bounds_preferred_cell_x;
            public int static_bounds_preferred_cell_y;
            public int static_bounds_blocker_grid_size;
            public ulong static_bounds_blocker_storage_epoch;
            public ulong static_bounds_blocker_applied_storage_epoch;
            public ulong static_bounds_blocker_applied_revision;
            public bool static_bounds_blocker_component_present;
            public int drop_search_runtime_building_id = -1;
            public string drop_search_producer;
            public string drop_search_unit_prefab;
            public float drop_search_started_at;
            public float drop_search_ready_at;
            public float drop_search_tick_time;
            public int drop_search_start_radius;
            public bool drop_search_exhausted_budget;
            public int drop_search_next_radius;
            public int drop_search_cell_x;
            public int drop_search_cell_y;
            public float[] drop_search_position;
            public bool drop_search_inputs_available;
            public float[] drop_search_preferred_world;
            public int drop_search_preferred_cell_x;
            public int drop_search_preferred_cell_y;
            public int drop_search_grid_width;
            public int drop_search_grid_height;
            public float drop_search_grid_cell_size;
            public float[] drop_search_grid_origin;
            public int drop_search_footprint_x;
            public int drop_search_footprint_y;
            public int drop_search_candidate_checks;
            public int drop_search_candidate_accepted;
            public int drop_search_rejected_out_of_grid;
            public int drop_search_rejected_walkable;
            public int drop_search_rejected_blocked;
            public int drop_search_rejected_occupied;
            public int drop_search_rejected_reserved;
            public int drop_search_rejected_surface_blocked;
            public int drop_search_rejected_surface_reserved;
            public int drop_search_rejected_surface_infantry;
            public int drop_search_rejected_height;
            public float drop_search_rejected_height_delta_min;
            public float drop_search_rejected_height_delta_max;
            public float drop_search_height_limit;
            public int drop_search_rejected_height_non_road;
            public string drop_search_rejection_counting_policy = "FirstFailingOriginalPredicatePerCandidate";
            public float[] camera_position;
            public float[] camera_forward;
            public float[] camera_right;
            public BuildingSnapshot[] buildings;
        }

        [Serializable]
        private sealed class BoundsSnapshot
        {
            public float[] min;
            public float[] max;
        }

        [Serializable]
        private sealed class BuildingSnapshot
        {
            public int runtime_building_id;
            public int combat_entity_index;
            public int combat_entity_version;
            public string producer;
            public int pending_count;
            public PendingSnapshot[] pending;
            public bool has_active_transport;
            public byte active_transport_phase;
            public int delivered_unit_count;
            public int committed_drop_cell_x;
            public int committed_drop_cell_y;
            public bool has_committed_drop_position;
        }

        [Serializable]
        private sealed class PendingSnapshot
        {
            public int production_index;
            public string unit_prefab;
            public float started_at;
            public float ready_at;
            public float transport_arrival_seconds;
            public float transport_hold_seconds;
            public int remaining_quantity;
            public string transport_mode;
        }

        internal static void BeginTick(BuildingProductionRuntimeTickCompositionSystemHelper.Context context,
            uint randomState, string tickOwner)
        {
            if (!enabled) return;
            try
            {
                if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em) ||
                    !TryGetS073Match(em, out SkirmishMatchState match))
                {
                    currentTickIsS073 = false;
                    currentRandomStateGetter = null;
                    return;
                }

                string session = match.SessionId.ToString();
                if (!string.IsNullOrEmpty(currentSession) && currentSession != session)
                {
                    currentTickIsS073 = false;
                    current = null;
                    currentRandomStateGetter = null;
                    return;
                }
                if (string.IsNullOrEmpty(currentSession))
                {
                    tickRecorded = false;
                    noEventTickRecorded = false;
                }
                currentSession = session;
                currentTickIsS073 = true;
                currentTickHasLaunch = false;
                currentRandomStateGetter = context.GetRandomState;
                currentRandomStateHooksAvailable = context.GetRandomState != null && context.SetRandomState != null;
                bool getterBeforeObserved = false;
                uint getterBefore = 0u;
                bool getterBeforeMatchesTickInput = false;
                if (context.GetRandomState != null)
                {
                    try
                    {
                        getterBefore = context.GetRandomState();
                        getterBeforeObserved = true;
                        getterBeforeMatchesTickInput = getterBefore == randomState;
                    }
                    catch (Exception error) { RecordObserverError("RandomStateGetterBefore", error); }
                }
                var transport = context.ProductionUpdateContext.TransportContext;
                Camera camera = transport.WorldCamera;
                current = new TickSnapshot
                {
                    session_id = session,
                    world_name = em.World.Name,
                    frame = Time.frameCount,
                    tick_owner = tickOwner,
                    phase = match.Phase.ToString(),
                    unity_time = Time.time,
                    delta_time = Time.deltaTime,
                    random_state_before = randomState,
                    random_state_observed = false,
                    random_state_source_hooks_available = currentRandomStateHooksAvailable,
                    random_state_getter_before_observed = getterBeforeObserved,
                    random_state_getter_before = getterBefore,
                    random_state_getter_before_matches_tick_input = getterBeforeMatchesTickInput,
                    camera_position = camera != null ? Vector(camera.transform.position) : null,
                    camera_forward = camera != null ? Vector(camera.transform.forward) : null,
                    camera_right = camera != null ? Vector(camera.transform.right) : null,
                    buildings = SnapshotBuildings(context.RuntimeBuildings)
                };
            }
            catch (Exception error)
            {
                RecordObserverError("BeginTick", error);
                currentTickIsS073 = false;
                current = null;
                currentRandomStateGetter = null;
            }
        }

        internal static void EndTick(uint randomState, bool activeTransport)
        {
            if (!currentTickIsS073 || current == null) return;
            try
            {
                current.random_state_after = randomState;
                bool getterAfterObserved = false;
                uint getterAfter = 0u;
                bool getterAfterMatchesTickOutput = false;
                // The composition helper writes the local post-tick state through
                // SetRandomState before calling EndTick. Re-read the owner here to
                // verify that persistence without affecting the gameplay state.
                // The source getter is a passive field read of BuildingSpawnRandomState.
                current.random_state_source_hooks_available = currentRandomStateHooksAvailable;
                // The getter delegate is available iff the paired hooks were
                // available at BeginTick; avoid retaining the gameplay context.
                if (currentRandomStateHooksAvailable)
                {
                    try
                    {
                        // EndTick's caller passed the same source getter through
                        // the context; this delegate is retained only for this
                        // observation window.
                        getterAfter = currentRandomStateGetter();
                        getterAfterObserved = true;
                        getterAfterMatchesTickOutput = getterAfter == randomState;
                    }
                    catch (Exception error) { RecordObserverError("RandomStateGetterAfter", error); }
                }
                current.random_state_getter_after_observed = getterAfterObserved;
                current.random_state_getter_after = getterAfter;
                current.random_state_getter_after_matches_tick_output = getterAfterMatchesTickOutput;
                current.random_state_getter_verification_passed = currentRandomStateHooksAvailable &&
                    current.random_state_getter_before_observed && current.random_state_getter_before_matches_tick_input &&
                    getterAfterObserved && getterAfterMatchesTickOutput;
                current.random_state_observed = current.random_state_getter_verification_passed;
                current.active_transport_after_tick = activeTransport;
                bool hasProduction = activeTransport;
                if (current.buildings != null)
                    for (int i = 0; i < current.buildings.Length; i++)
                        hasProduction |= current.buildings[i].pending_count > 0 || current.buildings[i].has_active_transport;
                if (hasProduction)
                {
                    current.production_event_present = true;
                    current.production_event_status = "PendingProductionOrActiveTransportObserved";
                    if (!tickRecorded || currentTickHasLaunch)
                    {
                        Write(current);
                        tickRecorded = true;
                    }
                }
                else if (!noEventTickRecorded)
                {
                    current.production_event_present = false;
                    current.production_event_status = "NoPendingProductionOrActiveTransportOnThisNaturalTick";
                    Write(current);
                    noEventTickRecorded = true;
                }
            }
            catch (Exception error) { RecordObserverError("EndTick", error); }
            finally
            {
                currentTickIsS073 = false;
                current = null;
                currentRandomStateGetter = null;
            }
        }

        internal static void ObserveStaticRenderBounds(EntityManager em,
            Game.Rendering.Contracts.IRenderBoundsPresentation provider,
            Entity gridEntity, GridConfig grid, DynamicBlockerComponent blockerData,
            bool blockerComponentPresent, Unity.Mathematics.int2 preferredCell,
            Rect area, float minExtent, float maxExtent,
            Unity.Collections.NativeList<Bounds> bounds)
        {
            if (!enabled) return;
            try
            {
                if (!TryGetS073Match(em, out SkirmishMatchState match)) return;
                string session = match.SessionId.ToString();
                if (!string.Equals(staticBoundsSession, session, StringComparison.Ordinal))
                {
                    staticBoundsSession = session;
                    staticBoundsObservationCount = 0;
                    staticBoundsSkippedCount = 0;
                }
                if (staticBoundsObservationCount >= MaxStaticBoundsObservationsPerSession)
                {
                    staticBoundsSkippedCount++;
                    return;
                }
                var observation = new TickSnapshot
                {
                    kind = "S073NativeProductionStaticRenderBoundsObservation",
                    scope = "ActualOriginalReserveStaticRenderDropBuffersCollectStaticBoundsCall",
                    session_id = session,
                    world_name = em.World.Name,
                    frame = Time.frameCount,
                    tick_owner = "ReserveStaticRenderDropBuffers",
                    phase = match.Phase.ToString(),
                    unity_time = Time.time,
                    delta_time = Time.deltaTime,
                    static_render_bounds_observed = true,
                    static_render_bounds_count = bounds.Length,
                    static_render_bounds_event = "Observed",
                    static_bounds_area = new[] { area.xMin, area.yMin, area.width, area.height },
                    static_bounds_min_extent = minExtent,
                    static_bounds_max_extent = maxExtent,
                    static_bounds = new BoundsSnapshot[bounds.Length],
                    static_bounds_provider_type = provider != null ? provider.GetType().AssemblyQualifiedName : null,
                    static_bounds_provider_identity = provider != null ? RuntimeHelpers.GetHashCode(provider) : 0,
                    static_bounds_provider_reference_stable = provider != null &&
                        ReferenceEquals(provider, Game.Rendering.Contracts.RenderBoundsGateway.Provider),
                    static_bounds_grid_entity_index = gridEntity == Entity.Null ? -1 : gridEntity.Index,
                    static_bounds_grid_entity_version = gridEntity == Entity.Null ? 0 : gridEntity.Version,
                    static_bounds_grid_width = grid.Width,
                    static_bounds_grid_height = grid.Height,
                    static_bounds_grid_cell_size = grid.CellSize,
                    static_bounds_grid_origin = new[] { grid.Origin.x, grid.Origin.y, grid.Origin.z },
                    static_bounds_preferred_cell_x = preferredCell.x,
                    static_bounds_preferred_cell_y = preferredCell.y,
                    static_bounds_blocker_grid_size = blockerData.GridSize,
                    static_bounds_blocker_storage_epoch = blockerData.StorageEpoch,
                    static_bounds_blocker_applied_storage_epoch = blockerData.AppliedStorageEpoch,
                    static_bounds_blocker_applied_revision = blockerData.AppliedRevision,
                    static_bounds_blocker_component_present = blockerComponentPresent,
                    buildings = Array.Empty<BuildingSnapshot>()
                };
                for (int i = 0; i < bounds.Length; i++)
                {
                    Bounds b = bounds[i];
                    observation.static_bounds[i] = new BoundsSnapshot { min = Vector(b.min), max = Vector(b.max) };
                }
                if (Write(observation)) staticBoundsObservationCount++;
            }
            catch (Exception error) { RecordObserverError("StaticBounds", error); }
        }

        internal static void ObserveHelicopterDropSearch(
            BuildingProductionTransportPresentationSystemHelper.Context context,
            RuntimeBuildingEntity building,
            RuntimeBuildingEntity.PendingProduction pending,
            float now,
            LaunchInputs launchInputs,
            uint randomState,
            int searchStartRadius,
            bool resolved,
            bool exhaustedSearchBudget,
            int nextSearchRadius,
            Unity.Mathematics.int2 cell,
            Vector3 position,
            HelicopterDropSearchDiagnostics diagnostics)
        {
            try
            {
                if (!enabled) return;
                var tryGetEntityManager = context.TransportBridgeContext.TryGetEntityManager;
                if (tryGetEntityManager == null || !tryGetEntityManager(out EntityManager em) ||
                    !TryGetS073Match(em, out SkirmishMatchState match)) return;
                string session = match.SessionId.ToString();
                if (!string.Equals(dropSearchSession, session, StringComparison.Ordinal))
                {
                    dropSearchSession = session;
                    dropSearchObservationCount = 0;
                }
                if (dropSearchObservationCount >= MaxDropSearchObservationsPerSession) return;
                if (currentTickIsS073 && current != null &&
                    string.Equals(currentSession, session, StringComparison.Ordinal) &&
                    string.Equals(current.world_name, em.World.Name, StringComparison.Ordinal))
                    currentTickHasLaunch = true;
                var observation = new TickSnapshot
                {
                    kind = "S073NativeProductionDropSearchObservation",
                    scope = "ActualOriginalTryResolveClearHelicopterDropPositionResult",
                    session_id = session,
                    world_name = em.World.Name,
                    frame = Time.frameCount,
                    tick_owner = "TryEnsureActiveProductionTransport",
                    phase = match.Phase.ToString(),
                    unity_time = Time.time,
                    delta_time = Time.deltaTime,
                    random_state_before = randomState,
                    random_state_after = randomState,
                    random_state_observed = false,
                    launch_random_state_before = launchInputs.randomStateBefore,
                    launch_random_state_after = randomState,
                    launch_random_state_observed = false,
                    launch_random_state_local_value_observed = launchInputs.randomStateObserved,
                    launch_random_state_source_hooks_available = currentRandomStateHooksAvailable,
                    launch_frame = Time.frameCount,
                    launch_camera_present = launchInputs.cameraObserved && launchInputs.cameraPresent,
                    camera_position = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraPosition) : null,
                    camera_forward = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraForward) : null,
                    camera_right = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraRight) : null,
                    launch_camera_position = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraPosition) : null,
                    launch_camera_forward = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraForward) : null,
                    launch_camera_right = launchInputs.cameraObserved && launchInputs.cameraPresent ? Vector(launchInputs.cameraRight) : null,
                    production_event_present = true,
                    production_event_status = resolved ? "ClearDropCellResolved" : "NoClearDropCellResolved",
                    drop_search_runtime_building_id = building != null ? building.Id : -1,
                    drop_search_producer = building?.Definition != null ? building.Definition.DisplayName : null,
                    drop_search_unit_prefab = pending?.Prefab != null ? pending.Prefab.name : null,
                    drop_search_started_at = pending != null ? pending.StartedAt : 0f,
                    drop_search_ready_at = pending != null ? pending.ReadyAt : 0f,
                    drop_search_tick_time = now,
                    drop_search_start_radius = searchStartRadius,
                    drop_search_exhausted_budget = exhaustedSearchBudget,
                    drop_search_next_radius = nextSearchRadius,
                    drop_search_cell_x = cell.x,
                    drop_search_cell_y = cell.y,
                    drop_search_position = resolved ? Vector(position) : null,
                    drop_search_inputs_available = diagnostics != null && diagnostics.searchStarted
                };
                if (diagnostics != null)
                {
                    observation.drop_search_preferred_world = new[]
                    {
                        diagnostics.preferredWorldX, diagnostics.preferredWorldY, diagnostics.preferredWorldZ
                    };
                    observation.drop_search_preferred_cell_x = diagnostics.preferredCellX;
                    observation.drop_search_preferred_cell_y = diagnostics.preferredCellY;
                    observation.drop_search_grid_width = diagnostics.gridWidth;
                    observation.drop_search_grid_height = diagnostics.gridHeight;
                    observation.drop_search_grid_cell_size = diagnostics.gridCellSize;
                    observation.drop_search_grid_origin = new[]
                    {
                        diagnostics.gridOriginX, diagnostics.gridOriginY, diagnostics.gridOriginZ
                    };
                    observation.drop_search_footprint_x = diagnostics.footprintX;
                    observation.drop_search_footprint_y = diagnostics.footprintY;
                    observation.drop_search_candidate_checks = diagnostics.candidateChecks;
                    observation.drop_search_candidate_accepted = diagnostics.candidateAccepted;
                    observation.drop_search_rejected_out_of_grid = diagnostics.outOfGrid;
                    observation.drop_search_rejected_walkable = diagnostics.walkableRejected;
                    observation.drop_search_rejected_blocked = diagnostics.blockedRejected;
                    observation.drop_search_rejected_occupied = diagnostics.occupiedRejected;
                    observation.drop_search_rejected_reserved = diagnostics.reservedRejected;
                    observation.drop_search_rejected_surface_blocked = diagnostics.surfaceBlockedRejected;
                    observation.drop_search_rejected_surface_reserved = diagnostics.surfaceReservedRejected;
                    observation.drop_search_rejected_surface_infantry = diagnostics.surfaceInfantryRejected;
                    observation.drop_search_rejected_height = diagnostics.heightRejected;
                    observation.drop_search_rejected_height_delta_min = diagnostics.heightRejectedDeltaMin;
                    observation.drop_search_rejected_height_delta_max = diagnostics.heightRejectedDeltaMax;
                    observation.drop_search_height_limit = diagnostics.heightLimit;
                    observation.drop_search_rejected_height_non_road = diagnostics.heightRejectedNonRoad;
                }
                dropSearchObservationCount++;
                Write(observation);
            }
            catch (Exception error) { RecordObserverError("DropSearch", error); }
        }

        // Emits only from the original transport lifecycle after its existing state
        // transition or spawn has completed. This observer never advances gameplay.
        internal static void ObserveTransportEvent(
            BuildingProductionTransportPresentationSystemHelper.Context context,
            RuntimeBuildingEntity building,
            RuntimeBuildingEntity.ActiveProductionTransport transport,
            RuntimeBuildingEntity.PendingProduction pending,
            string eventName,
            float now,
            bool spawnSucceeded = false)
        {
            try
            {
                if (!enabled) return;
                var tryGetEntityManager = context.TransportBridgeContext.TryGetEntityManager;
                if (tryGetEntityManager == null || !tryGetEntityManager(out EntityManager em) ||
                    !TryGetS073Match(em, out SkirmishMatchState match)) return;

                RuntimeBuildingEntity.PendingProduction observedPending = pending;
                if (observedPending == null && building?.PendingProductions != null)
                {
                    for (int i = 0; i < building.PendingProductions.Count; i++)
                    {
                        RuntimeBuildingEntity.PendingProduction candidate = building.PendingProductions[i];
                        if (candidate != null && candidate.TransportPrefab == transport?.Prefab)
                        {
                            observedPending = candidate;
                            break;
                        }
                    }
                }

                var observation = new TickSnapshot
                {
                    kind = "S073NativeProductionTransportEvent",
                    scope = "PassiveOriginalTransportLifecycle_NotReadinessOrMissionAcceptance",
                    session_id = match.SessionId.ToString(),
                    world_name = em.World.Name,
                    frame = Time.frameCount,
                    tick_owner = "UpdateActiveProductionTransport",
                    phase = match.Phase.ToString(),
                    unity_time = Time.time,
                    delta_time = Time.deltaTime,
                    random_state_observed = false,
                    production_event_present = true,
                    production_event_status = eventName,
                    transport_event = eventName,
                    transport_event_match_time = now,
                    transport_runtime_building_id = building != null ? building.Id : -1,
                    transport_producer = building?.Definition != null ? building.Definition.DisplayName : null,
                    transport_owner_faction_id = building != null ? building.OwnerFactionId : 0,
                    transport_unit_prefab = observedPending?.Prefab != null ? observedPending.Prefab.name : null,
                    transport_production_index = observedPending?.ProductionIndex ?? -1,
                    transport_production_started_at = observedPending?.StartedAt ?? 0f,
                    transport_production_ready_at = observedPending?.ReadyAt ?? 0f,
                    transport_mode = transport != null ? transport.Mode.ToString() : null,
                    transport_phase = transport != null ? transport.Phase : (byte)0,
                    transport_delivered_unit_count = transport != null ? transport.DeliveredUnitCount : 0,
                    transport_drop_cell_x = transport != null ? transport.CommittedDropCell.x : 0,
                    transport_drop_cell_y = transport != null ? transport.CommittedDropCell.y : 0,
                    transport_drop_position = transport != null && transport.HasCommittedDropPosition
                        ? Vector(transport.CommittedDropPosition) : null,
                    transport_spawn_succeeded = spawnSucceeded
                };

                if (spawnSucceeded && building != null &&
                    BuildingProductionTransportBridgeCompositionSystemHelper.TryGetNewestProducedUnit(context.TransportBridgeContext, building, em, out Entity delivered) &&
                    em.Exists(delivered))
                {
                    observation.delivered_entity_index = delivered.Index;
                    observation.delivered_entity_version = delivered.Version;
                    if (em.HasComponent<UnitGrid>(delivered))
                    {
                        UnitGrid cell = em.GetComponentData<UnitGrid>(delivered);
                        observation.delivered_cell_x = cell.Cell.x;
                        observation.delivered_cell_y = cell.Cell.y;
                    }
                    if (em.HasComponent<Unity.Transforms.LocalTransform>(delivered))
                    {
                        var position = em.GetComponentData<Unity.Transforms.LocalTransform>(delivered).Position;
                        observation.delivered_position = new[] { position.x, position.y, position.z };
                    }
                }
                Write(observation);
            }
            catch (Exception error) { RecordObserverError("TransportEvent", error); }
        }

        private static void RecordObserverError(string stage, Exception error)
        {
            try
            {
                observerErrorCount++;
                lastObserverError = stage + ":" + error.GetType().Name + ":" + error.Message.Replace('\n', ' ').Replace('\r', ' ');
            }
            catch { }
        }

        private static bool TryGetS073Match(EntityManager em, out SkirmishMatchState match)
        {
            match = default;
            if (em.World == null || !em.World.IsCreated) return false;
            using var query = em.CreateEntityQuery(typeof(SkirmishMatchState));
            if (query.CalculateEntityCount() != 1) return false;
            match = query.GetSingleton<SkirmishMatchState>();
            return match.ScenarioIndex == 3 && match.Seed == 104729 && match.Phase == SkirmishPhase.Playing;
        }

        private static BuildingSnapshot[] SnapshotBuildings(IReadOnlyDictionary<int, RuntimeBuildingEntity> buildings)
        {
            if (buildings == null) return Array.Empty<BuildingSnapshot>();
            var rows = new List<BuildingSnapshot>(buildings.Count);
            foreach (var pair in buildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null) continue;
                var pending = building.PendingProductions;
                var pendingRows = new PendingSnapshot[pending != null ? pending.Count : 0];
                for (int i = 0; i < pendingRows.Length; i++)
                {
                    var item = pending[i];
                    pendingRows[i] = new PendingSnapshot
                    {
                        production_index = item?.ProductionIndex ?? -1,
                        unit_prefab = item?.Prefab != null ? item.Prefab.name : null,
                        started_at = item?.StartedAt ?? 0f,
                        ready_at = item?.ReadyAt ?? 0f,
                        transport_arrival_seconds = item?.TransportArrivalSeconds ?? 0f,
                        transport_hold_seconds = item?.TransportHoldForNextReadySeconds ?? 0f,
                        remaining_quantity = item?.RemainingQuantity ?? 0,
                        transport_mode = item != null ? item.TransportMode.ToString() : null
                    };
                }
                var active = building.ActiveTransport;
                rows.Add(new BuildingSnapshot
                {
                    runtime_building_id = building.Id,
                    combat_entity_index = building.CombatEntity.Index,
                    combat_entity_version = building.CombatEntity.Version,
                    producer = building.Definition != null ? building.Definition.DisplayName : null,
                    pending_count = pendingRows.Length,
                    pending = pendingRows,
                    has_active_transport = active != null,
                    active_transport_phase = active != null ? active.Phase : (byte)0,
                    delivered_unit_count = active != null ? active.DeliveredUnitCount : 0,
                    committed_drop_cell_x = active != null ? active.CommittedDropCell.x : 0,
                    committed_drop_cell_y = active != null ? active.CommittedDropCell.y : 0,
                    has_committed_drop_position = active != null && active.HasCommittedDropPosition
                });
            }
            return rows.ToArray();
        }

        private static float[] Vector(Vector3 v) => new[] { v.x, v.y, v.z };

        private static bool Write(TickSnapshot observation)
        {
            string configured = Environment.GetEnvironmentVariable(OutputKey);
            if (string.IsNullOrEmpty(configured)) return false;
            string path = Path.GetFullPath(configured);
            if (!path.StartsWith("/private/tmp/", StringComparison.Ordinal)) return false;
            if (ownedOutputPath == null)
            {
                if (File.Exists(path)) return false;
                ownedOutputPath = path;
            }
            if (!string.Equals(ownedOutputPath, path, StringComparison.Ordinal)) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string row = JsonUtility.ToJson(observation);
            File.AppendAllText(path, row + "\n", Encoding.UTF8);
            return true;
        }
    }
}
#endif
