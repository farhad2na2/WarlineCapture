"""Fail-closed qualification for passive S073 native launch-input observations."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path


KIND = "S073NativeProductionDeliveryWindow"
REQUIRED_MARKER = (
    "[S073NativeProductionWindow] result=ObservationWindowElapsed "
    "observer_status=NaturalProductionStateObserved"
)
REQUIRED_SHUTDOWN_MARKER = "[S073NativeProductionWindow] shutdown result=EditModeConfirmed exit_code=0"
TIMEOUT_SHUTDOWN_MARKER = "[S073NativeProductionWindow] shutdown result=TimedOutExit"
REQUIRED_SOURCE_PINS = {
    "Assets/Game/Resources/SkirmishAircraftRotorGeometry_V1.asset",
    "Assets/Game/Resources/SkirmishCanonicalAircraftGeometry_V1.asset",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishAircraftRotorGeometryAuthoring.cs",
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishCanonicalAircraftGeometryAuthoring.cs",
    "Assets/Game/Scenes/Match.unity",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/InitialForces.asset",
    "Assets/Game/Configs/Skirmish/IndustrialBasin/OperationMap_IndustrialBasin.asset",
    "Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md",
    "Assets/Game/Scripts/Components/OperationMapComponents.cs",
    "Assets/Game/Scripts/Components/SceneLifecycleComponents.cs",
    "Assets/Game/Scripts/Systems/S073NativeProductionDeliveryObserver.cs",
    "Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTickCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionRuntimeTickCompositionSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
    "Assets/Game/Scripts/Editor/Skirmish/S073NativeProductionDeliveryWindow.cs",
    "Tools/CI/invoke_unity_macos.sh",
}


def sha(data):
    return hashlib.sha256(data).hexdigest()


def finite_vector(value, length):
    return (isinstance(value, list) and len(value) == length and
            all(type(item) in (int, float) and math.isfinite(item) for item in value))


def safe_vector(value, length):
    return value if finite_vector(value, length) else None


def safe_finite_number(value):
    return value if type(value) in (int, float) and math.isfinite(value) else None


def nonnegative_int(value):
    return type(value) is int and value >= 0


def uint32(value):
    return nonnegative_int(value) and value <= 0xFFFFFFFF


def _read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig"))


def _artifact(directory, standard_name, pattern):
    standard = directory / standard_name
    if standard.is_file():
        return standard
    matches = sorted(directory.glob(pattern))
    return matches[0] if len(matches) == 1 else None


def _verify_pins(repo, pins_path, wrapper_command):
    if pins_path is None or not Path(pins_path).is_file():
        return {"status": "Unknown", "source_pin_count": None, "sha256": None,
                "verified_paths": [], "missing_paths": sorted(REQUIRED_SOURCE_PINS),
                "errors": []}
    raw = Path(pins_path).read_bytes()
    document = json.loads(raw)
    pins = document.get("source_pins_sha256") if isinstance(document, dict) else None
    if not isinstance(pins, dict):
        return {"status": "Failed", "source_pin_count": 0, "sha256": sha(raw),
                "verified_paths": [], "missing_paths": sorted(REQUIRED_SOURCE_PINS),
                "errors": ["source_pins_sha256 mapping required"]}
    output_paths = (document.get("window_output_path"), document.get("delivery_output_path"))
    provenance = (document.get("kind") == "S073NativeDeliveryLaunchPreRunReceipt" and
                  document.get("schema_version") == 1 and
                  document.get("captured_before_wrapper") is True and
                  document.get("output_paths_absent_at_capture") is True and
                  document.get("wrapper_command") == wrapper_command and
                  all(isinstance(path, str) and Path(path).is_absolute() and
                      Path(path).parent == Path("/private/tmp") for path in output_paths) and
                  output_paths[0] != output_paths[1])
    if not provenance:
        return {"status": "Failed", "source_pin_count": len(pins), "sha256": sha(raw),
                "verified_paths": [], "missing_paths": sorted(REQUIRED_SOURCE_PINS - set(pins)),
                "errors": ["Pre-wrapper receipt must bind distinct private-tmp output paths to the checked wrapper"]}
    missing = sorted(REQUIRED_SOURCE_PINS - set(pins))
    verified = []
    errors = []
    for relative, expected in sorted(pins.items(), key=lambda item: str(item[0])):
        if not isinstance(relative, str) or not isinstance(expected, str) or len(expected) != 64:
            errors.append("Malformed pin entry")
            continue
        source = (repo / relative).resolve()
        if not source.is_relative_to(repo):
            errors.append("Pin path escapes repository: " + relative)
            continue
        try:
            actual = sha(source.read_bytes())
        except OSError:
            errors.append("Pinned source missing: " + relative)
            continue
        if actual != expected.lower():
            errors.append("Pinned source changed: " + relative)
            continue
        verified.append(relative)
    if errors:
        status = "Failed"
    elif missing:
        status = "Incomplete"
    else:
        status = "Passed"
    return {"status": status, "source_pin_count": len(pins), "sha256": sha(raw),
            "window_output_path": output_paths[0], "delivery_output_path": output_paths[1],
            "verified_paths": verified, "missing_paths": missing, "errors": errors}


def _valid_bounds(row):
    bounds = row.get("static_bounds")
    if not isinstance(bounds, list) or not bounds or row.get("static_render_bounds_count") != len(bounds):
        return False
    for bound in bounds:
        if not isinstance(bound, dict) or not finite_vector(bound.get("min"), 3) or not finite_vector(bound.get("max"), 3):
            return False
        if any(bound["min"][axis] > bound["max"][axis] for axis in range(3)):
            return False
    return True


def qualify(repo, directory, wrapper_exit, pre_run_receipt_path=None,
            wrapper_command="Tools/CI/invoke_unity_macos.sh"):
    repo = Path(repo).resolve(strict=True)
    directory = Path(os.path.abspath(directory))
    result_path = _artifact(directory, "result.json", "*-result.json")
    delivery_path = _artifact(directory, "delivery.jsonl", "*-delivery.jsonl")
    log_path = _artifact(directory, "editor.log", "*-editor.log")
    gates = {
        "checked_wrapper_exit_zero": wrapper_exit == 0 and wrapper_command == "Tools/CI/invoke_unity_macos.sh",
        "completion_marker": None,
        "edit_mode_shutdown": None,
        "pre_run_output_path_binding": None,
        "output_artifact_byte_identity": None,
        "native_session": None,
        "same_session_frame_bounds_launch": None,
        "static_bounds_capture_accounting": None,
        "ordered_bounds_and_provider_stability": None,
        "local_and_tick_rng_flags": None,
        "same_session_scene_operation_map": None,
        "finite_camera_grid_and_epochs": None,
        "source_pin_hashes": None,
        "no_readiness_claim": None,
    }
    errors = []
    result = None
    if gates["checked_wrapper_exit_zero"] is False:
        errors.append("Checked Unity wrapper did not exit successfully")
    rows = []
    log = b""
    try:
        if result_path is None:
            raise FileNotFoundError("unique result artifact unavailable")
        result_raw = result_path.read_bytes()
        result = json.loads(result_raw)
    except (OSError, ValueError) as error:
        errors.append("result.json unavailable or malformed: " + str(error))
    try:
        if log_path is None:
            raise FileNotFoundError("unique editor log unavailable")
        log = log_path.read_bytes()
        log_text = log.decode("utf-8-sig", errors="strict")
        marker_count = log_text.splitlines().count(REQUIRED_MARKER)
        gates["completion_marker"] = marker_count == 1
        if marker_count != 1:
            errors.append("Required completion marker must occur exactly once")
        shutdown_marker_count = log_text.splitlines().count(REQUIRED_SHUTDOWN_MARKER)
        timeout_marker_count = sum(TIMEOUT_SHUTDOWN_MARKER in line for line in log_text.splitlines())
        gates["edit_mode_shutdown"] = shutdown_marker_count == 1 and timeout_marker_count == 0
        if shutdown_marker_count != 1:
            errors.append("Confirmed Edit Mode shutdown marker must occur exactly once")
        if timeout_marker_count:
            errors.append("Timed out shutdown marker is forbidden for a passing capture")
    except (OSError, UnicodeError) as error:
        errors.append("editor.log unavailable or malformed: " + str(error))
    try:
        if delivery_path is None:
            raise FileNotFoundError("unique delivery artifact unavailable")
        delivery_raw = delivery_path.read_bytes()
        rows = [json.loads(line) for line in delivery_raw.decode("utf-8-sig").splitlines() if line.strip()]
        if any(not isinstance(row, dict) for row in rows):
            raise ValueError("delivery rows must be JSON objects")
    except (OSError, UnicodeError, ValueError) as error:
        errors.append("delivery.jsonl unavailable or malformed: " + str(error))

    session = result.get("session_id") if isinstance(result, dict) else None
    observer_summary = result.get("production_delivery_observer") if isinstance(result, dict) else None
    s073_result = (isinstance(result, dict) and result.get("kind") == KIND and
                   result.get("schema_version") == 3 and
                   result.get("mission") == "S073" and result.get("scenario_index") == 3 and
                   result.get("seed") == 104729 and result.get("terminal_match_phase") == "Playing" and
                   result.get("result") == "ObservationWindowElapsed" and
                   isinstance(observer_summary, dict) and
                   observer_summary.get("status") == "NaturalProductionStateObserved" and
                   observer_summary.get("observer_error_count") == 0 and
                   observer_summary.get("same_session_rows") == len(rows) and
                   result.get("observer_error_count") == 0 and
                   isinstance(session, str) and bool(session))
    all_same_session = bool(rows) and all(row.get("session_id") == session for row in rows)
    gates["native_session"] = None if result is None or not rows else s073_result and all_same_session
    if gates["native_session"] is False:
        errors.append("S073 identity and one same-session event stream required")
    elif gates["native_session"] is None:
        errors.append("Native session identity is Unknown")

    launch_rows = [row for row in rows if row.get("kind") == "S073NativeProductionDropSearchObservation"]
    bounds_rows = [row for row in rows if row.get("kind") == "S073NativeProductionStaticRenderBoundsObservation"]
    launches_with_bounds = []
    reused_bounds = set()
    unmatched_complete_launch = False
    matching_launches = []
    for launch in launch_rows:
        launch_frame = launch.get("launch_frame", launch.get("frame"))
        same_frame_bounds = [bounds for bounds in bounds_rows
                             if launch.get("session_id") == bounds.get("session_id") == session and
                             type(launch_frame) is int and bounds.get("frame") == launch_frame]
        launch_time = launch.get("unity_time")
        if not same_frame_bounds:
            continue
        if type(launch_time) not in (int, float) or not math.isfinite(launch_time):
            continue
        time_matches = [bounds for bounds in same_frame_bounds
                        if type(bounds.get("unity_time")) in (int, float) and
                        math.isfinite(bounds["unity_time"]) and launch_time == bounds["unity_time"]]
        cell_x = launch.get("drop_search_preferred_cell_x")
        cell_y = launch.get("drop_search_preferred_cell_y")
        if type(cell_x) is int and type(cell_y) is int:
            time_matches = [bounds for bounds in time_matches
                            if bounds.get("static_bounds_preferred_cell_x") == cell_x and
                            bounds.get("static_bounds_preferred_cell_y") == cell_y]
        if len(time_matches) > 1:
            matching_launches.append((launch, time_matches, True))
        elif len(time_matches) == 1:
            launches_with_bounds.append((launch, time_matches[0]))
        elif all(type(bounds.get("unity_time")) in (int, float) and math.isfinite(bounds["unity_time"])
                 for bounds in same_frame_bounds):
            unmatched_complete_launch = True

    for launch, bounds in launches_with_bounds:
        identity = id(bounds)
        if identity in reused_bounds:
            matching_launches.append((launch, [bounds], True))
        reused_bounds.add(identity)
    ambiguous_pair = bool(matching_launches)
    if launches_with_bounds:
        gates["same_session_frame_bounds_launch"] = False if ambiguous_pair else True
    elif ambiguous_pair or unmatched_complete_launch:
        gates["same_session_frame_bounds_launch"] = False
    else:
        gates["same_session_frame_bounds_launch"] = None
    if gates["same_session_frame_bounds_launch"] is False:
        errors.append("Launch and static-bounds observations are contradictory or ambiguously correlated")
    elif gates["same_session_frame_bounds_launch"] is None:
        errors.append("Same-session/frame/time launch-to-bounds linkage is Unknown")

    static_bounds_count = observer_summary.get("static_bounds_observation_count") if isinstance(observer_summary, dict) else None
    static_bounds_skipped = observer_summary.get("static_bounds_skipped_overflow_count") if isinstance(observer_summary, dict) else None
    accounting_fields_present = (isinstance(observer_summary, dict) and
                                 "static_bounds_observation_count" in observer_summary and
                                 "static_bounds_skipped_overflow_count" in observer_summary)
    bounds_rows_accounted = (nonnegative_int(static_bounds_count) and
                             nonnegative_int(static_bounds_skipped) and
                             static_bounds_count == len(bounds_rows))
    gates["static_bounds_capture_accounting"] = (
        bounds_rows_accounted if accounting_fields_present else None)
    if gates["static_bounds_capture_accounting"] is False:
        errors.append("Window bounds capture counts must match successful rows and report overflow")
    elif gates["static_bounds_capture_accounting"] is None:
        errors.append("Static-bounds capture accounting is Unknown")

    launches_with_bounds.sort(key=lambda pair: (pair[0].get("unity_time", float("inf")),
                                                pair[0].get("launch_frame", pair[0].get("frame", 2**31))))
    chosen_launch, chosen_bounds = launches_with_bounds[0] if launches_with_bounds else (None, None)
    bounds_valid = bool(chosen_bounds and _valid_bounds(chosen_bounds))
    provider_valid = bool(chosen_bounds and isinstance(chosen_bounds.get("static_bounds_provider_type"), str) and
                          chosen_bounds.get("static_bounds_provider_type") and
                          type(chosen_bounds.get("static_bounds_provider_identity")) is int and
                          chosen_bounds.get("static_bounds_provider_reference_stable") is True)
    gates["ordered_bounds_and_provider_stability"] = None if chosen_bounds is None else bounds_valid and provider_valid
    bounds_digest = None
    if bounds_valid:
        ordered = json.dumps(chosen_bounds["static_bounds"], separators=(",", ":"), allow_nan=False).encode()
        bounds_digest = sha(ordered)
    if gates["ordered_bounds_and_provider_stability"] is False:
        errors.append("Ordered finite collector bounds and stable identified provider required")
    elif gates["ordered_bounds_and_provider_stability"] is None:
        errors.append("Provider and ordered bounds facts are Unknown")

    launch_local_rng = bool(chosen_launch and chosen_launch.get("launch_random_state_local_value_observed") is True and
                            uint32(chosen_launch.get("launch_random_state_before")) and
                            uint32(chosen_launch.get("launch_random_state_after")))
    source_rng_hooks = bool(chosen_launch and chosen_launch.get("launch_random_state_source_hooks_available") is True)
    same_frame_ticks = [row for row in rows if row.get("kind") == "S073NativeProductionDeliveryObservation" and
                        row.get("session_id") == session and chosen_launch is not None and
                        row.get("frame") == chosen_launch.get("launch_frame", chosen_launch.get("frame"))]
    verified_tick_rng_rows = [row for row in same_frame_ticks if row.get("random_state_observed") is True and
                   row.get("random_state_source_hooks_available") is True and
                   row.get("random_state_getter_verification_passed") is True and
                   row.get("random_state_getter_before_observed") is True and
                   row.get("random_state_getter_before_matches_tick_input") is True and
                   row.get("random_state_getter_after_observed") is True and
                   row.get("random_state_getter_after_matches_tick_output") is True and
                   row.get("random_state_getter_before") == row.get("random_state_before") and
                   row.get("random_state_getter_after") == row.get("random_state_after") and
                   uint32(row.get("random_state_before")) and
                   uint32(row.get("random_state_after")) and
                   row.get("random_state_before") == chosen_launch.get("launch_random_state_before") and
                   row.get("random_state_after") == chosen_launch.get("launch_random_state_after")
                   ]
    verified_tick_rng = verified_tick_rng_rows[0] if verified_tick_rng_rows else None
    tick_rng = verified_tick_rng is not None
    rng_verification_fields = (
        "random_state_source_hooks_available", "random_state_getter_verification_passed",
        "random_state_getter_before_observed", "random_state_getter_before",
        "random_state_getter_before_matches_tick_input", "random_state_getter_after_observed",
        "random_state_getter_after", "random_state_getter_after_matches_tick_output")
    tick_rng_facts_available = any(all(key in row for key in rng_verification_fields)
                                   for row in same_frame_ticks)
    tick_rng_status = True if tick_rng else False if tick_rng_facts_available else None
    gates["local_and_tick_rng_flags"] = (None if chosen_launch is None or tick_rng_status is None else
                                         launch_local_rng and source_rng_hooks and tick_rng)
    if gates["local_and_tick_rng_flags"] is False:
        errors.append("Exact local launch RNG plus successful same-frame source tick RNG observation required")
    elif gates["local_and_tick_rng_flags"] is None:
        errors.append("Same-frame source tick RNG relationship is Unknown")

    baseline_map = result.get("scene_operation_map_at_playing") if isinstance(result, dict) else None
    latest_map = result.get("scene_operation_map_latest_same_world_session") if isinstance(result, dict) else None

    def valid_scene_map(snapshot):
        if not isinstance(snapshot, dict) or snapshot.get("capture_status") == "Unavailable":
            return None
        required = ("session_id", "world_name", "unique_consistent_operation_map_identity_available",
                    "unique_operation_map_id", "unique_scenario_id", "unique_mission_id",
                    "unique_operation_map_generation", "unique_metadata_hash", "unique_metadata_generation",
                    "unique_identity_matches_metadata", "loaded_scene_sequence")
        if snapshot.get("capture_status") != "Observed" or any(key not in snapshot for key in required):
            return None
        if snapshot.get("unique_consistent_operation_map_identity_available") is not True:
            return False
        if (snapshot.get("session_id") != session or not isinstance(snapshot.get("world_name"), str) or
                not snapshot.get("world_name")):
            return False
        if not all(isinstance(snapshot.get(key), str) and snapshot.get(key) for key in
                   ("unique_operation_map_id", "unique_scenario_id", "unique_mission_id", "unique_metadata_hash")):
            return False
        if (not nonnegative_int(snapshot.get("unique_operation_map_generation")) or
                snapshot.get("unique_metadata_generation") != snapshot.get("unique_operation_map_generation") or
                snapshot.get("unique_identity_matches_metadata") is not True):
            return False
        scenes = snapshot.get("loaded_scene_sequence")
        if not isinstance(scenes, list):
            return None
        return any(isinstance(scene, dict) and scene.get("is_loaded") is True and
                   scene.get("path") == "Assets/Game/Scenes/Match.unity" for scene in scenes)

    baseline_scene_map_valid = valid_scene_map(baseline_map)
    latest_scene_map_valid = valid_scene_map(latest_map)
    map_fields = ("unique_operation_map_id", "unique_scenario_id", "unique_mission_id",
                  "unique_operation_map_generation", "unique_metadata_generation", "unique_metadata_hash")
    same_scene_map_identity = bool(
        baseline_scene_map_valid is True and latest_scene_map_valid is True and
        baseline_map.get("session_id") == latest_map.get("session_id") == session and
        baseline_map.get("world_name") == latest_map.get("world_name") and
        baseline_map.get("loaded_scene_sequence") == latest_map.get("loaded_scene_sequence") and
        all(baseline_map.get(key) == latest_map.get(key) for key in map_fields))
    gates["same_session_scene_operation_map"] = (
        None if result is None or baseline_scene_map_valid is None or latest_scene_map_valid is None
        else same_scene_map_identity)
    if gates["same_session_scene_operation_map"] is False:
        errors.append("Schema-3 baseline/latest must correlate the same session/world, loaded Match scene, and unique map metadata identity")
    elif gates["same_session_scene_operation_map"] is None:
        errors.append("Scene and operation-map identity is Unknown")

    camera_valid = bool(chosen_launch and chosen_launch.get("launch_camera_present") is True and
                        finite_vector(chosen_launch.get("launch_camera_position"), 3) and
                        finite_vector(chosen_launch.get("launch_camera_forward"), 3) and
                        finite_vector(chosen_launch.get("launch_camera_right"), 3))
    grid_valid = bool(chosen_bounds and type(chosen_bounds.get("static_bounds_grid_width")) is int and
                      chosen_bounds.get("static_bounds_grid_width", 0) > 0 and
                      type(chosen_bounds.get("static_bounds_grid_height")) is int and
                      chosen_bounds.get("static_bounds_grid_height", 0) > 0 and
                      type(chosen_bounds.get("static_bounds_grid_entity_index")) is int and
                      chosen_bounds.get("static_bounds_grid_entity_index", -1) >= 0 and
                      nonnegative_int(chosen_bounds.get("static_bounds_grid_entity_version")) and
                      finite_vector(chosen_bounds.get("static_bounds_grid_origin"), 3) and
                      type(chosen_bounds.get("static_bounds_grid_cell_size")) in (int, float) and
                      math.isfinite(chosen_bounds.get("static_bounds_grid_cell_size")) and
                      chosen_bounds.get("static_bounds_grid_cell_size") > 0)
    epochs_valid = bool(chosen_bounds and chosen_bounds.get("static_bounds_blocker_component_present") is True and
                        nonnegative_int(chosen_bounds.get("static_bounds_blocker_grid_size")) and
                        nonnegative_int(chosen_bounds.get("static_bounds_blocker_storage_epoch")) and
                        nonnegative_int(chosen_bounds.get("static_bounds_blocker_applied_storage_epoch")) and
                        nonnegative_int(chosen_bounds.get("static_bounds_blocker_applied_revision")))
    gates["finite_camera_grid_and_epochs"] = (None if chosen_launch is None or chosen_bounds is None else
                                              camera_valid and grid_valid and epochs_valid)
    if gates["finite_camera_grid_and_epochs"] is False:
        errors.append("Finite launch camera, actual grid identity/configuration, and blocker epochs required")
    elif gates["finite_camera_grid_and_epochs"] is None:
        errors.append("Launch camera, grid, or blocker epoch facts are Unknown")

    pins = _verify_pins(repo, pre_run_receipt_path, wrapper_command)
    gates["source_pin_hashes"] = (True if pins["status"] == "Passed" else
                                   False if pins["status"] == "Failed" else None)
    if gates["source_pin_hashes"] is not True:
        errors.append("Required source pin hashes are incomplete or invalid")

    window_output_path = pins.get("window_output_path")
    delivery_output_path = pins.get("delivery_output_path")
    if window_output_path and delivery_output_path:
        started_marker = ("[S073NativeProductionWindow] started windowOutput=" + window_output_path +
                          " deliveryOutput=" + delivery_output_path)
        started_count = log.decode("utf-8-sig", errors="replace").splitlines().count(started_marker)
        observer_path = (result.get("production_delivery_observer", {}).get("path")
                         if isinstance(result, dict) and isinstance(result.get("production_delivery_observer"), dict)
                         else None)
        gates["pre_run_output_path_binding"] = started_count == 1 and observer_path == delivery_output_path
        try:
            original_window_bytes = Path(window_output_path).read_bytes()
            original_delivery_bytes = Path(delivery_output_path).read_bytes()
            gates["output_artifact_byte_identity"] = (
                result_path is not None and delivery_path is not None and
                result_path.read_bytes() == original_window_bytes and
                delivery_path.read_bytes() == original_delivery_bytes)
        except OSError:
            gates["output_artifact_byte_identity"] = None
    else:
        started_count = None
        gates["pre_run_output_path_binding"] = None
        gates["output_artifact_byte_identity"] = None
    if gates["pre_run_output_path_binding"] is False:
        errors.append("Editor start line/result observer output must match the pre-run receipt paths exactly")
    elif gates["pre_run_output_path_binding"] is None:
        errors.append("Pre-run output path binding is Unknown")
    if gates["output_artifact_byte_identity"] is False:
        errors.append("Durable result and delivery copies differ from the exact pre-run output artifacts")
    elif gates["output_artifact_byte_identity"] is None:
        errors.append("Original output artifacts are unavailable for byte identity comparison")

    readiness_values = []
    for field in ("mission_ready", "source_fit_certified", "normal_input_playthrough", "player_readiness"):
        readiness_values.append(result.get(field) if isinstance(result, dict) and field in result else None)
    no_readiness = all(value is False for value in readiness_values)
    gates["no_readiness_claim"] = (True if no_readiness else
                                   False if any(value is True for value in readiness_values) else None)
    if gates["no_readiness_claim"] is not True:
        errors.append("Readiness fields must be explicitly false for this passive capture scope")

    known_false = any(value is False for value in gates.values())
    all_passed = all(value is True for value in gates.values())
    status = "Passed" if all_passed else "Failed" if known_false else "Incomplete"
    return {
        "kind": "S073NativeProductionLaunchInputReceipt",
        "schema_version": 1,
        "validation_status": status,
        "scope": "Passive same-session original launch input observation; not gameplay replay or readiness",
        "wrapper": {"command": wrapper_command, "exit_code": wrapper_exit,
                    "required_completion_marker": REQUIRED_MARKER,
                    "completion_marker_count": log.decode("utf-8-sig", errors="replace").splitlines().count(REQUIRED_MARKER),
                    "required_shutdown_marker": REQUIRED_SHUTDOWN_MARKER,
                    "shutdown_marker_count": log.decode("utf-8-sig", errors="replace").splitlines().count(REQUIRED_SHUTDOWN_MARKER),
                    "timeout_shutdown_marker_count": sum(TIMEOUT_SHUTDOWN_MARKER in line for line in log.decode("utf-8-sig", errors="replace").splitlines()),
                    "started_marker_count": started_count},
        "session": {"mission": result.get("mission") if isinstance(result, dict) else None,
                    "scenario_index": result.get("scenario_index") if isinstance(result, dict) else None,
                    "seed": result.get("seed") if isinstance(result, dict) else None,
                    "session_id": session,
                    "launch_frame": chosen_launch.get("launch_frame", chosen_launch.get("frame")) if chosen_launch else None,
                    "bounds_frame": chosen_bounds.get("frame") if chosen_bounds else None,
                    "scene_operation_map_at_playing": baseline_map,
                    "scene_operation_map_latest_same_world_session": latest_map},
        "capture": {
            "qualification_scope": "At least one fully linked launch; every additional launch is listed separately and is not inferred",
            "launch_observed": chosen_launch is not None,
            "launch_random_state_local_value_observed": launch_local_rng,
            "launch_random_state_source_hooks_available": source_rng_hooks,
            "launch_random_state_observed_field": chosen_launch.get("launch_random_state_observed") if chosen_launch else None,
            "same_frame_source_tick_random_state_observed": tick_rng_status,
            "same_frame_source_tick_rng_verification": ({
                "random_state_before": verified_tick_rng.get("random_state_before"),
                "random_state_after": verified_tick_rng.get("random_state_after"),
                "getter_before": verified_tick_rng.get("random_state_getter_before"),
                "getter_after": verified_tick_rng.get("random_state_getter_after"),
                "before_matches_tick_input": verified_tick_rng.get("random_state_getter_before_matches_tick_input"),
                "after_matches_tick_output": verified_tick_rng.get("random_state_getter_after_matches_tick_output"),
                "source_hooks_available": verified_tick_rng.get("random_state_source_hooks_available"),
                "verification_passed": verified_tick_rng.get("random_state_getter_verification_passed"),
            } if verified_tick_rng else None),
            "launch_camera_present": chosen_launch.get("launch_camera_present") if chosen_launch else None,
            "launch_camera_position": safe_vector(chosen_launch.get("launch_camera_position"), 3) if chosen_launch else None,
            "launch_camera_forward": safe_vector(chosen_launch.get("launch_camera_forward"), 3) if chosen_launch else None,
            "launch_camera_right": safe_vector(chosen_launch.get("launch_camera_right"), 3) if chosen_launch else None,
            "provider_type": chosen_bounds.get("static_bounds_provider_type") if chosen_bounds else None,
            "provider_identity": chosen_bounds.get("static_bounds_provider_identity") if chosen_bounds else None,
            "provider_reference_stable": chosen_bounds.get("static_bounds_provider_reference_stable") if chosen_bounds else None,
            "bounds_count": chosen_bounds.get("static_render_bounds_count") if chosen_bounds else None,
            "bounds_order": "collector_return_order_preserved" if bounds_valid else "Unknown",
            "ordered_bounds_sha256": bounds_digest,
            "grid_entity": ([chosen_bounds.get("static_bounds_grid_entity_index"),
                             chosen_bounds.get("static_bounds_grid_entity_version")] if chosen_bounds else None),
            "grid": ({"width": chosen_bounds.get("static_bounds_grid_width"),
                      "height": chosen_bounds.get("static_bounds_grid_height"),
                      "cell_size": safe_finite_number(chosen_bounds.get("static_bounds_grid_cell_size")),
                      "origin": safe_vector(chosen_bounds.get("static_bounds_grid_origin"), 3)} if chosen_bounds else None),
            "blocker": ({"grid_size": chosen_bounds.get("static_bounds_blocker_grid_size"),
                         "storage_epoch": chosen_bounds.get("static_bounds_blocker_storage_epoch"),
                         "applied_storage_epoch": chosen_bounds.get("static_bounds_blocker_applied_storage_epoch"),
                         "applied_revision": chosen_bounds.get("static_bounds_blocker_applied_revision")} if chosen_bounds else None),
            "additional_launch_rng_observations": [
                {"frame": row.get("launch_frame", row.get("frame")),
                 "local_value_observed": row.get("launch_random_state_local_value_observed"),
                 "source_hooks_available": row.get("launch_random_state_source_hooks_available"),
                 "same_frame_tick_pair": (True if any(
                     tick.get("kind") == "S073NativeProductionDeliveryObservation" and
                     tick.get("session_id") == session and
                     tick.get("frame") == row.get("launch_frame", row.get("frame")) and
                     tick.get("random_state_observed") is True and
                     tick.get("random_state_before") == row.get("launch_random_state_before") and
                     tick.get("random_state_after") == row.get("launch_random_state_after")
                     for tick in rows) else False if any(
                     tick.get("kind") == "S073NativeProductionDeliveryObservation" and
                     tick.get("session_id") == session and
                     tick.get("frame") == row.get("launch_frame", row.get("frame"))
                     for tick in rows) else None),
                 "bounds_same_frame": (True if any(
                     bound.get("kind") == "S073NativeProductionStaticRenderBoundsObservation" and
                     bound.get("session_id") == session and
                     bound.get("frame") == row.get("launch_frame", row.get("frame"))
                     for bound in rows) else None)}
                for row in launch_rows if row is not chosen_launch
            ],
        },
        "source_pins": pins,
        "source_artifacts": {
            "result_path": str(result_path) if result_path is not None else None,
            "result_sha256": sha(result_path.read_bytes()) if result_path is not None and result_path.is_file() else None,
            "delivery_path": str(delivery_path) if delivery_path is not None else None,
            "delivery_sha256": sha(delivery_path.read_bytes()) if delivery_path is not None and delivery_path.is_file() else None,
            "editor_log_path": str(log_path) if log_path is not None else None,
            "editor_log_sha256": sha(log_path.read_bytes()) if log_path is not None and log_path.is_file() else None,
        },
        "adjacent_observations": {"predicate_capture": "Unknown_NotCapturedByThisLaunchInputFinalizer",
                                  "mask_capture": "Unknown_NotCapturedByThisLaunchInputFinalizer"},
        "gates": gates,
        "unknown_or_failed_facts": errors,
        "mission_ready": False,
        "source_fit_certified": False,
        "normal_input_playthrough": False,
        "player_readiness": False,
        "readiness_claim": "None",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--directory", required=True)
    parser.add_argument("--wrapper-exit", required=True, type=int)
    parser.add_argument("--wrapper-command", default="Tools/CI/invoke_unity_macos.sh")
    parser.add_argument("--pre-run-receipt", help="Exclusive pre-wrapper receipt binding source hashes and output paths")
    args = parser.parse_args()
    directory = Path(os.path.abspath(args.directory))
    pins_path = args.pre_run_receipt or (directory / "pre-run-receipt.json")
    receipt = qualify(args.repo, directory, args.wrapper_exit, pins_path, args.wrapper_command)
    with (directory / "delivery-launch-receipt.json").open("x", encoding="utf-8") as output:
        json.dump(receipt, output, indent=2, allow_nan=False)
        output.write("\n")
    print(json.dumps(receipt, indent=2, allow_nan=False))
    raise SystemExit(0 if receipt["validation_status"] == "Passed" else 1)


if __name__ == "__main__":
    main()
