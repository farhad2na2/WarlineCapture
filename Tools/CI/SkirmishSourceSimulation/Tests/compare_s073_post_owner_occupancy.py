#!/usr/bin/env python3
"""Compare the independently captured S073 occupancy mask with the source mask after its authored-vehicle projection."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path


def load_module(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    assert spec and spec.loader
    spec.loader.exec_module(module)
    return module


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_source_run(result_path: Path, run_receipt_path: Path, project_root: Path) -> tuple[dict, dict, dict]:
    result_path = result_path.resolve(strict=True)
    run_receipt_path = run_receipt_path.resolve(strict=True)
    receipt = json.loads(run_receipt_path.read_text(encoding="utf-8"))
    worker = receipt.get("worker_result") or {}
    need(worker.get("path") == str(result_path) and worker.get("sha256") == sha(result_path) and
         worker.get("bytes") == result_path.stat().st_size,
         "Source run receipt does not bind the actual projection result bytes")
    build_pin = receipt.get("build_receipt") or {}
    build_path = Path(build_pin.get("path", "")).resolve(strict=True)
    need(build_pin.get("sha256") == sha(build_path) and build_pin.get("bytes") == build_path.stat().st_size,
         "Source run receipt build pin mismatch")
    build = json.loads(build_path.read_text(encoding="utf-8"))
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
         build.get("mission") == "S073" and build.get("seed") == 104729 and
         build.get("compile_exit") == 0,
         "Qualified S073 source build receipt required")
    executable = build.get("executable") or {}
    executable_path = Path(executable.get("path", "")).resolve(strict=True)
    need(executable.get("sha256") == sha(executable_path) and
         receipt.get("source_executable", executable).get("sha256", executable.get("sha256")) == sha(executable_path),
         "Built source executable hash mismatch")
    result = json.loads(result_path.read_text(encoding="utf-8"))
    need(result.get("kind") == "SkirmishSourceSimulationRunReceipt" and
         result.get("mission") == "S073" and result.get("seed") == 104729 and
         result.get("session_id") and result.get("mission_ready") is False and
         result.get("status") == "PreparingPhysicalSourceRequired",
         "Expected unready S073 source diagnostic result required")
    pins = build.get("source_and_reference_pins")
    need(isinstance(pins, dict) and len(pins) > 0, "Source build source pins required")
    # Verify the source files that implement and validate this importer. The full
    # build pin manifest remains available in build.json as the build provenance.
    importer_paths = {
        "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073NeutralOccupancyProjectionImporter.cs",
        "Assets/Game/Scripts/Systems/DynamicOccupancyRebuildSystem.cs",
        "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs",
    }
    selected = {}
    for raw_path in importer_paths:
        path = (project_root / raw_path).resolve(strict=True)
        recorded = pins.get(str(path))
        need(isinstance(recorded, str) and recorded == sha(path), "Source build pin mismatch: " + raw_path)
        selected[raw_path] = recorded
    return receipt, build, {"result": result, "selected_source_pins_sha256": selected,
                            "build_source_pin_count": len(pins), "build_path": str(build_path)}


def occupancy_diff(actual: list[int], expected: set[int], cell_count: int) -> tuple[list[int], list[int]]:
    need(isinstance(actual, list) and len(actual) == len(set(actual)) and
         all(isinstance(i, int) and 0 <= i < cell_count for i in actual),
         "Captured actual source occupancy mask must contain unique in-range cell indices")
    actual_set = set(actual)
    return sorted(expected - actual_set), sorted(actual_set - expected)


def compare(args: argparse.Namespace) -> dict:
    root = args.project_root.resolve(strict=True)
    raw_compare = load_module("s073_raw_occupancy_compare",
        root / "Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_blocker_occupancy.py")
    projection_compare = load_module("s073_vehicle_projection_compare",
        root / "Tools/CI/SkirmishSourceSimulation/Tests/compare_s073_authored_vehicle_projection.py")

    source_run, source_build, source_run_data = verify_source_run(
        args.source_result, args.source_run_receipt, root)
    # Reuse strict validators for independent raw-capture/session receipt chains,
    # projection input/source pins, native census qualification, and both logs.
    raw_result = raw_compare.compare(args.native_raw, args.native_raw_receipt,
        args.source_baseline_raw, args.source_baseline_receipt, args.native_wrapper_receipt,
        args.native_snapshot, args.source_baseline_run_receipt, max_coordinates=0)
    projection_result = projection_compare.compare(args.projection_capture_receipt,
        args.projection_input, args.projection, args.native_wrapper_receipt,
        args.native_snapshot, root)

    native_receipt, native = raw_compare.load_capture(args.native_raw, args.native_raw_receipt, "native")
    source_receipt, baseline = raw_compare.load_capture(args.source_baseline_raw,
        args.source_baseline_receipt, "source")
    need((native["width"], native["height"], native["cell_size"], native["origin"]) ==
         (baseline["width"], baseline["height"], baseline["cell_size"], baseline["origin"]),
         "Source baseline and native census grid frames differ")
    result = source_run_data["result"]
    import_record = ((result.get("building_and_unit_startup_diagnostic") or {})
                     .get("neutral_occupancy_projection_import") or {})
    union_record = import_record.get("occupancy_union_comparison") or {}
    need(import_record.get("status") == "Passed" and
         import_record.get("original_dynamic_occupancy_owner_invoked_after_import") is True and
         import_record.get("source_state_imported") is False and import_record.get("mission_ready") is False and
         import_record.get("scheduler_admitted") is False and
         union_record.get("mismatch_count") == 0,
         "Actual post-owner occupancy projection owner evidence must pass with admission/readiness inactive")
    source_artifacts = import_record.get("source_artifacts") or {}
    for artifact_name, expected_path in (("projection", args.projection), ("inputs", args.projection_input),
            ("capture_receipt", args.projection_capture_receipt),
            ("editor_log", args.projection_capture_receipt.parent / "editor.log")):
        desc = source_artifacts.get(artifact_name) or {}
        path = expected_path.resolve(strict=True)
        need(Path(desc.get("path", "")).resolve(strict=True) == path and desc.get("sha256") == sha(path),
             "Source runtime result projection artifact hash/path mismatch: " + artifact_name)
    need(source_run.get("mission") == "S073" and source_run.get("seed") == 104729 and
         source_run_data["result"].get("session_id") == import_record.get("initial_and_admission_flags_before", {}).get("session_id") and
         import_record.get("initial_and_admission_flags_before") == import_record.get("initial_and_admission_flags_after"),
         "Source run/session identity or guarded flags changed")

    projection = json.loads(args.projection.read_text(encoding="utf-8"))
    count = native["width"] * native["height"]
    projected_cells = set(projection.get("occupied_row_major_cells", []))
    need(all(isinstance(i, int) and 0 <= i < count for i in projected_cells),
         "Projection contains an invalid row-major cell index")

    baseline_cells = {i for i in range(count) if raw_compare.bit(baseline["occupied"], i)}
    native_cells = {i for i in range(count) if raw_compare.bit(native["occupied"], i)}
    expected_post_owner = baseline_cells | projected_cells
    actual_source_cells = union_record.get("actual_occupied_row_major_cells")
    source_missing, source_extra = occupancy_diff(actual_source_cells, expected_post_owner, count)
    actual_source_set = set(actual_source_cells)
    source_native_only = sorted(actual_source_set - native_cells)
    native_only = sorted(native_cells - actual_source_set)
    exact = not source_missing and not source_extra and not source_native_only and not native_only and \
        len(actual_source_set) == union_record.get("actual_after_owner_occupied_cell_count") == 197
    return {
        "kind": "S073IndependentSourcePostOwnerOccupancyMaskComparison",
        "schema_version": 1,
        "status": "ExactMaskMatch" if exact else "DifferencesObserved",
        "source_run": {"run_receipt_sha256": sha(args.source_run_receipt),
            "result_sha256": sha(args.source_result), "session_id": result["session_id"],
            "build_receipt_sha256": sha(Path(source_run["build_receipt"]["path"])),
            "build_source_pin_count": source_run_data["build_source_pin_count"],
            "verified_importer_source_pins_sha256": source_run_data["selected_source_pins_sha256"]},
        "source_baseline_capture": {"raw_sha256": source_receipt["raw_sha256"],
            "receipt_sha256": sha(args.source_baseline_receipt),
            "session_id": source_receipt["source_session_id"], "occupied_cell_count": len(baseline_cells)},
        "source_projection_capture": {"input_sha256": sha(args.projection_input),
            "projection_sha256": sha(args.projection), "capture_receipt_sha256": sha(args.projection_capture_receipt),
            "occupied_cell_count": len(projected_cells), "independent_capture": True},
        "native_capture": {"raw_sha256": native_receipt["raw_sha256"],
            "raw_receipt_sha256": sha(args.native_raw_receipt),
            "wrapper_receipt_sha256": sha(args.native_wrapper_receipt),
            "snapshot_sha256": sha(args.native_snapshot),
            "session_id": native_receipt["native_session_id"], "occupied_cell_count": len(native_cells)},
        "source_and_native_sessions_are_independent": True,
        "grid": {"width": native["width"], "height": native["height"],
                 "cell_size": native["cell_size"], "origin": list(native["origin"])},
        "mask_comparison": {"basis": "actual source post-owner occupancy bitset captured after original owner rebuild",
            "source_baseline_occupied_cell_count": len(baseline_cells),
            "projected_occupied_cell_count": len(projected_cells),
            "overlap_count": len(baseline_cells & projected_cells),
            "expected_source_post_owner_union_count": len(expected_post_owner),
            "actual_source_post_owner_occupied_cell_count": len(actual_source_set),
            "native_raw_occupied_cell_count": len(native_cells),
            "shared_cell_count": len(actual_source_set & native_cells),
            "source_vs_expected_missing_count": len(source_missing), "source_vs_expected_extra_count": len(source_extra),
            "source_vs_native_only_count": len(source_native_only), "native_only_count": len(native_only),
            "source_vs_expected_missing_sample": source_missing[:64], "source_vs_expected_extra_sample": source_extra[:64],
            "source_vs_native_only_sample": source_native_only[:64], "native_only_sample": native_only[:64]},
        "validation": {"raw_capture_receipt_chain": "Passed", "authored_projection_capture_and_source_pins": "Passed",
            "native_raw_and_wrapper_receipt_chain": "Passed", "native_qualified_census_comparison": projection_result["status"],
            "prior_source_vs_native_raw_status": raw_result["status"]},
        "scope": "Actual source-host post-owner mask and independent native raw census compared offline; captures are separate sessions/times",
        "actual_source_post_owner_mask_captured": True,
        "source_post_owner_mask_is_reconstructed_from_independent_evidence": False,
        "same_session_or_same_time": False,
        "identity_join_performed": False, "ownership_inferred": False, "full_parity_claim": False,
        "state_transfer": False, "source_validation": False, "scheduler_admitted": False, "mission_ready": False,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--native-raw", type=Path, required=True)
    parser.add_argument("--native-raw-receipt", type=Path, required=True)
    parser.add_argument("--native-wrapper-receipt", type=Path, required=True)
    parser.add_argument("--native-snapshot", type=Path, required=True)
    parser.add_argument("--source-baseline-raw", type=Path, required=True)
    parser.add_argument("--source-baseline-receipt", type=Path, required=True)
    parser.add_argument("--source-run-receipt", type=Path, required=True)
    parser.add_argument("--source-baseline-run-receipt", type=Path, required=True)
    parser.add_argument("--source-result", type=Path, required=True)
    parser.add_argument("--projection-capture-receipt", type=Path, required=True)
    parser.add_argument("--projection-input", type=Path, required=True)
    parser.add_argument("--projection", type=Path, required=True)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = compare(args)
    with args.output.open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"],
        "expected_source_post_owner_union_count": result["mask_comparison"]["expected_source_post_owner_union_count"],
        "native_raw_occupied_cell_count": result["mask_comparison"]["native_raw_occupied_cell_count"],
        "actual_source_post_owner_mask_captured": True, "mission_ready": False}))


if __name__ == "__main__":
    main()
