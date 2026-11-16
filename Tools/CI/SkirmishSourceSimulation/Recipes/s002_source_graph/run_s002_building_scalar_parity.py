#!/usr/bin/env python3
"""Run the transient S002 building scalar parity check through the repo Unity wrapper."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import time

ROOT = Path(__file__).resolve().parents[5]
WRAPPER = ROOT / "Tools/CI/invoke_unity_macos.sh"
RTK = "/opt/homebrew/bin/rtk"
EXECUTE_METHOD = "Game.Editor.Skirmish.S002BuildingDefinitionScalarMetadataParityValidation.RunFocusedValidation"
PASS_MARKER = (
    "[S002BuildingDefinitionScalarMetadataParity] result=Passed cases=7 "
    "scope=TransientManagedMetadataOnly noScene=true noWorld=true noBaker=true "
    "sourceFitVerified=false missionReady=false configPriceNonpositiveNotTested=true"
)

SOURCE_PATHS = (
    "Assets/Game/Scripts/Components/BuildingDefinitionAuthoringScalarProjection.cs",
    "Assets/Game/Scripts/Components/BuildingDefinitionAuthoringScalarProjection.cs.meta",
    "Assets/Game/Scripts/Components/Game.Components.asmdef",
    "Assets/Game/Scripts/Components/Game.Components.asmdef.meta",
    "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs",
    "Assets/Game/Scripts/Authorings/BuildingDefinitionAuthoring.cs.meta",
    "Assets/Game/Scripts/Authorings/Game.Authoring.asmdef",
    "Assets/Game/Scripts/Authorings/Game.Authoring.asmdef.meta",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs",
    "Assets/Game/Scripts/Configs/GameplayConfigModels.cs.meta",
    "Assets/Game/Scripts/Configs/Game.Configs.asmdef",
    "Assets/Game/Scripts/Configs/Game.Configs.asmdef.meta",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs.meta",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef",
    "Assets/Game/Scripts/Composition/Game.Composition.asmdef.meta",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs",
    "Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs.meta",
    "Assets/Game/Scripts/Game.Runtime.asmdef",
    "Assets/Game/Scripts/Game.Runtime.asmdef.meta",
    "Assets/Game/Scripts/Editor/Skirmish/S002BuildingDefinitionScalarMetadataParityValidation.cs",
    "Assets/Game/Scripts/Editor/Skirmish/S002BuildingDefinitionScalarMetadataParityValidation.cs.meta",
    "Assets/Game/Scripts/Editor/Game.Editor.asmdef",
    "Assets/Game/Scripts/Editor/Game.Editor.asmdef.meta",
    "Tools/CI/invoke_unity_macos.sh",
    "Tools/CI/SkirmishSourceSimulation/Recipes/s002_source_graph/run_s002_building_scalar_parity.py",
)
ASSEMBLIES = (
    "Library/ScriptAssemblies/Game.Components.dll",
    "Library/ScriptAssemblies/Game.Authoring.dll",
    "Library/ScriptAssemblies/Game.Configs.dll",
    "Library/ScriptAssemblies/Game.Composition.dll",
    "Library/ScriptAssemblies/Game.Runtime.dll",
    "Library/ScriptAssemblies/Game.Editor.dll",
)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def snapshot(root: Path, paths: tuple[str, ...], *, require: bool = True) -> dict[str, str]:
    values = {}
    for rel in paths:
        path = root / rel
        if not path.is_file() or path.is_symlink():
            if require:
                raise ValueError("Required pinned file missing or symlinked: " + rel)
            continue
        values[rel] = digest(path)
    return values


def exact_marker_count(log_path: Path) -> int:
    if not log_path.is_file():
        return 0
    return sum(1 for line in log_path.read_text(errors="replace").splitlines()
               if line.strip() == PASS_MARKER)


def build_wrapper_command(log_path: Path, timeout: int) -> list[str]:
    return [RTK, "proxy", str(WRAPPER), "--timeout", str(timeout), "--log", str(log_path), "--",
            "-quit", "-executeMethod", EXECUTE_METHOD]


def validate_result(wrapper_exit_code: int | None, marker_count: int,
                    source_before: dict[str, str], source_after: dict[str, str],
                    assembly_before: dict[str, str], assembly_after: dict[str, str]) -> None:
    if wrapper_exit_code != 0:
        raise ValueError("Repository Unity wrapper must exit zero")
    if marker_count != 1:
        raise ValueError("Exact scalar parity pass marker must occur exactly once")
    if source_before != source_after:
        raise ValueError("Pinned source changed during parity execution")
    if set(assembly_before) != set(ASSEMBLIES) or set(assembly_after) != set(ASSEMBLIES):
        raise ValueError("All six compiled assemblies must be pinned before and after execution")
    if assembly_before != assembly_after:
        raise ValueError("Compiled assembly bytes changed during parity execution; preserve this failed epoch and rerun after compilation stabilizes")


def run(output_dir: Path, timeout: int) -> dict:
    root = ROOT.resolve(strict=True)
    output = output_dir.resolve()
    output.relative_to(root)
    if output.exists():
        raise ValueError("Fresh output directory required")
    output.mkdir(parents=True)
    log_path = output / "unity-wrapper.log"
    manifest_path = output / "pin-manifest.json"
    receipt_path = output / "receipt.json"
    command = build_wrapper_command(log_path, timeout)
    started = time.time()
    source_before: dict[str, str] = {}
    source_after: dict[str, str] = {}
    assembly_before: dict[str, str] = {}
    assembly_after: dict[str, str] = {}
    completed = None
    launch_failure = None
    try:
        source_before = snapshot(root, SOURCE_PATHS)
        assembly_before = snapshot(root, ASSEMBLIES)
        manifest = {
            "kind": "S002BuildingDefinitionScalarParityPinManifest",
            "source_paths": list(SOURCE_PATHS),
            "source_pins_before": source_before,
            "assembly_paths": list(ASSEMBLIES),
            "assembly_pins_before": assembly_before,
            "assembly_bytes_may_change_during_unity_compile": True,
        }
        manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        completed = subprocess.run(command, cwd=root, text=True, stdout=subprocess.PIPE,
                                   stderr=subprocess.STDOUT, check=False)
    except Exception as exc:
        launch_failure = str(exc)
    try:
        source_after = snapshot(root, SOURCE_PATHS)
        assembly_after = snapshot(root, ASSEMBLIES)
    except Exception as exc:
        launch_failure = launch_failure or ("Post-run pin snapshot failed: " + str(exc))
    marker_count = exact_marker_count(log_path)
    failure = launch_failure
    try:
        validate_result(completed.returncode if completed is not None else None, marker_count,
                        source_before, source_after, assembly_before, assembly_after)
    except Exception as exc:
        failure = failure or str(exc)

    result = {
        "kind": "S002BuildingDefinitionScalarParityReceipt",
        "status": "Passed" if failure is None else "Failed",
        "scope": "TransientManagedMetadataOnly_NoScene_NoWorld_NoBaker_NoSourceFit_NoReadiness",
        "execute_method": EXECUTE_METHOD,
        "command": command,
        "wrapper_exit_code": completed.returncode if completed is not None else None,
        "required_pass_marker": PASS_MARKER,
        "pass_marker_count": marker_count,
        "source_paths": list(SOURCE_PATHS),
        "pin_manifest_path": str(manifest_path.relative_to(root)),
        "pin_manifest_sha256": digest(manifest_path) if manifest_path.is_file() else None,
        "source_pins_before": source_before,
        "source_pins_after": source_after,
        "assembly_paths": list(ASSEMBLIES),
        "assembly_pins_before": assembly_before,
        "assembly_pins_after": assembly_after,
        "assembly_bytes_may_change_during_unity_compile": True,
        "log_path": str(log_path.relative_to(root)),
        "log_sha256": digest(log_path) if log_path.is_file() else None,
        "claims": {"native_metadata_owner_parity": failure is None,
                   "scene_loaded": False, "world_created": False, "baker_invoked": False,
                   "source_fit_verified": False, "mission_ready": False},
        "failure": failure,
        "elapsed_seconds": round(time.time() - started, 3),
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
        print(json.dumps({"status": "FailedBeforeReceipt", "failure": str(exc)}, indent=2))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
