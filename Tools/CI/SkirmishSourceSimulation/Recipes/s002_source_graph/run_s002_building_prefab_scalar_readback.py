#!/usr/bin/env python3
"""Read original building scalar metadata from isolated contents of eight T1 prefabs."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time

import s002_building_prefab_scalar_readback as checks

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
WRAPPER = ROOT / "Tools/CI/invoke_unity_macos.sh"
RTK = "/opt/homebrew/bin/rtk"
OBSERVER = "Assets/Game/Scripts/Editor/Skirmish/S002BuildingPrefabScalarMetadataReadback.cs"
OBSERVER_META = OBSERVER + ".meta"
RUNNER = "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_building_prefab_scalar_readback.py"
HELPER = "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/s002_building_prefab_scalar_readback.py"
TEST = "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/test_s002_building_prefab_scalar_readback.py"
WRAPPER_REL = "Tools/CI/invoke_unity_macos.sh"
EXECUTE_METHOD = "Game.Editor.Skirmish.S002BuildingPrefabScalarMetadataReadback.Run"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot(root: Path, paths: tuple[str, ...] | list[str]) -> dict[str, str]:
    result = {}
    for rel in paths:
        path = checks.safe_path(root, rel)
        if not path.is_file() or path.is_symlink():
            raise ValueError("Required pinned input missing or symlinked: " + rel)
        result[rel] = digest(path)
    return dict(sorted(result.items()))


def build_command(log: Path, observation: Path, manifest: Path, timeout: int) -> list[str]:
    return [RTK, "proxy", str(WRAPPER), "--timeout", str(timeout), "--log", str(log), "--",
            "-quit", "-executeMethod", EXECUTE_METHOD,
            "-s002-building-scalar-readback-output", str(observation),
            "-s002-building-scalar-readback-pins", str(manifest)]


def marker_count(log: Path) -> int:
    if not log.is_file():
        return 0
    return sum(1 for line in log.read_text(errors="replace").splitlines()
               if line.strip() == checks.PASS_MARKER)


def prepare(root: Path) -> tuple[dict, list[dict]]:
    receipt, projection, inputs = checks.load_t1(root)
    manifest = checks.make_manifest(root, receipt, projection, inputs,
                                    OBSERVER, OBSERVER_META, RUNNER, HELPER, TEST, WRAPPER_REL)
    return manifest, checks.build_candidates(root, projection, inputs)


def run(output_dir: Path, timeout: int) -> dict:
    root = ROOT.resolve(strict=True)
    output_dir = output_dir.resolve()
    output_dir.relative_to(root)
    if output_dir.exists():
        raise ValueError("Fresh output directory required")
    output_dir.mkdir(parents=True)
    observation = output_dir / "observation.json"
    manifest_path = output_dir / "pin-manifest.json"
    log = output_dir / "unity-wrapper.log"
    receipt_path = output_dir / "receipt.json"
    manifest, candidates = prepare(root)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    paths = list(manifest["source_pins"])
    source_before: dict[str, str] = {}
    source_after: dict[str, str] = {}
    assembly_before: dict[str, str] = {}
    assembly_after: dict[str, str] = {}
    completed = None
    failure = None
    try:
        source_before = snapshot(root, paths)
        assembly_before = snapshot(root, list(checks.ASSEMBLIES))
    except Exception as exc:
        failure = "Pre-run pin snapshot failed: " + str(exc)

    command = build_command(log, observation, manifest_path, timeout)
    started = time.time()
    if failure is None:
        try:
            completed = subprocess.run(command, cwd=root, text=True, stdout=subprocess.PIPE,
                                       stderr=subprocess.STDOUT, check=False)
        except Exception as exc:
            failure = "Repository Unity wrapper launch failed: " + str(exc)
        try:
            source_after = snapshot(root, paths)
        except Exception as exc:
            failure = failure or ("Post-run source pin check failed: " + str(exc))
        try:
            assembly_after = snapshot(root, list(checks.ASSEMBLIES))
        except Exception as exc:
            failure = failure or ("Post-run assembly pin check failed: " + str(exc))

    marker = marker_count(log)
    try:
        if completed is None:
            raise ValueError(failure or "Repository wrapper did not return a process result")
        if completed.returncode != 0:
            raise ValueError("Repository Unity wrapper exit code " + str(completed.returncode))
        if marker != 1:
            raise ValueError("Exact pass marker must occur once")
        if source_before != source_after:
            raise ValueError("Pinned source changed during readback")
        if assembly_before != assembly_after:
            raise ValueError("Pinned Unity assemblies changed during readback")
        parsed = json.loads(observation.read_text())
        checks.validate_observation(parsed, candidates, manifest["source_pins"])
    except Exception as exc:
        failure = failure or str(exc)
    result = {
        "kind": "S002BuildingPrefabScalarReadbackReceipt",
        "status": "Passed" if failure is None else "Failed",
        "scope": "ImportedPrefabContentsPreviewScenes_NoAuthoredOrMissionScene_NoWorld_NoBaker_NoPrefabSave_NoFit_NoReadiness",
        "t1_receipt_path": checks.T1_RECEIPT.as_posix(), "t1_receipt_sha256": checks.T1_RECEIPT_SHA256,
        "t1_projection_path": checks.T1_PROJECTION.as_posix(), "t1_projection_sha256": checks.T1_PROJECTION_SHA256,
        "t1_inputs_path": checks.T1_INPUTS.as_posix(), "t1_inputs_sha256": checks.T1_INPUTS_SHA256,
        "manifest_path": manifest_path.relative_to(root).as_posix(),
        "manifest_sha256": digest(manifest_path),
        "observation_path": observation.relative_to(root).as_posix() if observation.is_file() else None,
        "observation_sha256": digest(observation) if observation.is_file() else None,
        "log_path": log.relative_to(root).as_posix() if log.is_file() else None,
        "log_sha256": digest(log) if log.is_file() else None,
        "command": command,
        "wrapper_exit_code": completed.returncode if completed is not None else None,
        "required_pass_marker": checks.PASS_MARKER, "pass_marker_count": marker,
        "source_pins_before": source_before, "source_pins_after": source_after,
        "assembly_pins_before": assembly_before, "assembly_pins_after": assembly_after,
        "claims": {"temporary_prefab_contents_preview_scene_used": True,
                   "authored_or_mission_scene_loaded": False, "world_created": False,
                   "baker_invoked": False, "prefab_asset_saved": False,
                   "source_fit_verified": False, "mission_ready": False},
        "failure": failure,
        "elapsed_seconds": round(time.time() - started, 3),
        "rtk_binary_sha256": digest(Path(RTK).resolve()),
        "rtk_binary_is_source_authority": False,
    }
    receipt_path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()
    try:
        result = run(args.output_dir, args.timeout)
        print(json.dumps({"status": result["status"], "receipt": str(args.output_dir / "receipt.json"),
                          "failure": result["failure"]}, indent=2))
        return 0 if result["status"] == "Passed" else 1
    except Exception as exc:
        print(json.dumps({"status": "FailedBeforeReceipt", "failure": str(exc)}, indent=2), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
