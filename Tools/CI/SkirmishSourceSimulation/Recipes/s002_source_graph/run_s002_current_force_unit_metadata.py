#!/usr/bin/env python3
"""Run the opt-in imported-provider metadata observation through the repo wrapper."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time
from typing import Optional

import s002_current_force_unit_metadata as checks

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
T5_DIR = ROOT / "T Logs/SkirmishProduction/s002-current-provider-projection-20261103-T5"
T5_RECEIPT = T5_DIR / "receipt.json"
T5_PROJECTION = T5_DIR / "provider-projection.json"
WRAPPER = ROOT / "Tools/CI/invoke_unity_macos.sh"
RTK = "/opt/homebrew/bin/rtk"
PASS_MARKER = "[S002CurrentForceUnitMetadataObserver] result=Passed scope=ImportedProviderMetadataOnly"
EXECUTE_METHOD = "Game.Tests.Editor.S002CurrentForceUnitMetadataObserver.Run"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pins(root: Path, paths: set[str]) -> dict[str, str]:
    result = {}
    for rel in sorted(paths):
        p = checks.safe_repo_path(root, rel)
        if not p.is_file():
            raise ValueError(f"Pinned source missing: {rel}")
        result[rel] = digest(p)
    return result


def prepare_manifest(root: Path, receipt_path: Path, projection_path: Path) -> dict:
    receipt, projection = checks.load_t5(root, receipt_path, projection_path)
    specs = checks.build_provider_specs(root, projection)
    t5_source_pins = projection.get("source_pins")
    if not isinstance(t5_source_pins, dict) or len(t5_source_pins) < 300:
        raise ValueError("T5 projection must carry its complete source pin set")
    source_paths = set()
    for raw, expected_sha in t5_source_pins.items():
        p = checks.safe_repo_path(root, raw)
        if digest(p) != expected_sha:
            raise ValueError(f"Current T5 source differs from projection pin: {raw}")
        source_paths.add(p.relative_to(root.resolve()).as_posix())
    source_paths.update({
        receipt_path.relative_to(root.resolve()).as_posix(),
        projection_path.relative_to(root.resolve()).as_posix(),
        "Assets/Game/Scripts/Editor/Skirmish/S002CurrentForceUnitMetadataObserver.cs",
        "Assets/Game/Scripts/Editor/Skirmish/S002CurrentForceUnitMetadataObserver.cs.meta",
        "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
        "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
        "Assets/Game/Scripts/Authorings/Game.Authoring.asmdef",
        "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
        "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/s002_current_force_unit_metadata.py",
        "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_current_force_unit_metadata.py",
        "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/test_s002_current_force_unit_metadata.py",
        "Tools/CI/invoke_unity_macos.sh",
    })
    source_paths.update(checks.OWNER_SOURCES)
    source_paths.update(path + ".meta" for path in checks.OWNER_SOURCES)
    source_paths.update({
        "Assets/Game/Scripts/Editor/Game.Editor.asmdef.meta",
        "Assets/Game/Scripts/Composition/Game.Composition.asmdef.meta",
        "Assets/Game/Scripts/Authorings/Game.Authoring.asmdef.meta",
        "Assets/Game/Scripts/Configs/Game.Configs.asmdef.meta",
    })
    for spec in specs:
        source_paths.add(spec["path"])
        source_paths.add(spec["path"] + ".meta")
        for candidate in spec["config_candidates"]:
            source_paths.add(candidate["path"])
            source_paths.add(candidate["path"] + ".meta")
    return {
        "kind": "S002CurrentForceUnitMetadataPinManifest",
        "scope": "ImportedProviderMetadataOnly",
        "t5_receipt_path": receipt_path.relative_to(root.resolve()).as_posix(),
        "t5_receipt_sha256": digest(receipt_path),
        "t5_projection_path": projection_path.relative_to(root.resolve()).as_posix(),
        "t5_projection_sha256": digest(projection_path),
        "t5_setup": receipt["setup"],
        "source_pins": pins(root, source_paths),
        "providers": specs,
        "provider_count": len(specs),
        "claims": {
            "scene_loaded": False, "world_created": False, "baker_invoked": False,
            "provider_admission": False, "native_capture": False, "source_fit": False,
            "mission_readiness": False,
        },
    }


def build_wrapper_command(output: Path, manifest_path: Path, log: Path, timeout: int) -> list[str]:
    return [RTK, "proxy", str(WRAPPER), "--timeout", str(timeout), "--log", str(log), "--",
            "-quit", "-executeMethod", EXECUTE_METHOD,
            "-s002-current-force-metadata-output", str(output),
            "-s002-current-force-metadata-pins", str(manifest_path)]


def postrun_pin_snapshot(root: Path, source_pins: dict[str, str]) -> tuple[dict[str, str], Optional[str]]:
    try:
        return pins(root, set(source_pins)), None
    except Exception as exc:
        return {}, f"Post-run source pin check failed: {exc}"


def run(args: argparse.Namespace) -> int:
    root = ROOT.resolve()
    out_dir = Path(args.output_dir).resolve()
    out_dir.relative_to(root)
    out_dir.mkdir(parents=True, exist_ok=False)
    output = out_dir / "observation.json"
    manifest_path = out_dir / "pins.json"
    log = out_dir / "unity-wrapper.log"
    receipt_path = out_dir / "receipt.json"
    manifest = prepare_manifest(root, T5_RECEIPT, T5_PROJECTION)
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
    pinned_before = pins(root, set(manifest["source_pins"]))
    command = build_wrapper_command(output, manifest_path, log, args.timeout)
    started = time.time()
    completed = None
    launch_failure = None
    try:
        completed = subprocess.run(command, cwd=root, text=True, stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, check=False)
    except Exception as exc:
        launch_failure = f"Failed to execute repository wrapper: {exc}"
    pinned_after, pin_failure = postrun_pin_snapshot(root, manifest["source_pins"])
    try:
        marker_found = log.is_file() and PASS_MARKER in log.read_text(errors="replace")
    except Exception:
        marker_found = False
    failure = launch_failure or pin_failure
    try:
        if failure is not None:
            raise ValueError(failure)
        if completed is None:
            raise ValueError("Repository wrapper did not return a process result")
        if completed.returncode != 0:
            raise ValueError(f"Unity wrapper exit code {completed.returncode}")
        if not marker_found:
            raise ValueError("Required observer pass marker missing from wrapper log")
        if pinned_before != pinned_after:
            raise ValueError("Pinned source changed during readback")
        observation = json.loads(output.read_text())
        checks.validate_observation(observation, manifest["providers"], manifest["source_pins"])
    except Exception as exc:
        failure = str(exc)
    result = {
        "kind": "S002CurrentForceUnitMetadataReadbackReceipt",
        "status": "Passed" if failure is None else "Failed",
        "scope": "ImportedProviderMetadataOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderAdmission_NoReadiness",
        "t5_receipt_path": T5_RECEIPT.relative_to(root).as_posix(),
        "t5_receipt_sha256": digest(T5_RECEIPT),
        "t5_projection_path": T5_PROJECTION.relative_to(root).as_posix(),
        "t5_projection_sha256": digest(T5_PROJECTION),
        "manifest_path": manifest_path.relative_to(root).as_posix(),
        "manifest_sha256": digest(manifest_path),
        "observation_path": output.relative_to(root).as_posix() if output.exists() else None,
        "observation_sha256": digest(output) if output.exists() else None,
        "log_path": log.relative_to(root).as_posix() if log.exists() else None,
        "log_sha256": digest(log) if log.exists() else None,
        "command": command,
        "wrapper_exit_code": completed.returncode if completed is not None else None,
        "required_pass_marker": PASS_MARKER,
        "pass_marker_found": marker_found,
        "source_pins_before": pinned_before,
        "source_pins_after": pinned_after,
        "claims": manifest["claims"],
        "failure": failure,
        "elapsed_seconds": round(time.time() - started, 3),
        "rtk_binary_sha256_before": digest(Path(RTK).resolve()),
        "rtk_binary_used_for_command_wrapping": True,
        "rtk_binary_sha256_is_source_authority": False,
    }
    receipt_path.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({"status": result["status"], "receipt": str(receipt_path), "failure": failure}, indent=2))
    return 0 if failure is None else 1


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--timeout", type=int, default=600)
    args = parser.parse_args()
    try:
        return run(args)
    except Exception as exc:
        print(f"input validation failed: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
