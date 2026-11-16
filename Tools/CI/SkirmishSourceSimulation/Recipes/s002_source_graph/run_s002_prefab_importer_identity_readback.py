#!/usr/bin/env python3
"""Run a read-only Unity AssetDatabase identity check for two pinned prefab overrides.

This observes Unity importer identities only. It does not load a scene, run a
Baker, allocate a World, or establish provider membership or mission readiness.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
OBSERVER = ROOT / "Assets/Game/Scripts/Editor/Skirmish/S002PrefabImporterIdentityObserver.cs"
OBSERVER_META = Path(str(OBSERVER) + ".meta")
WRAPPER = ROOT / "Tools/CI/invoke_unity_macos.sh"
PROJECT_VERSION = ROOT / "ProjectSettings/ProjectVersion.txt"
MARKER = "[S002PrefabImporterIdentityObserver] result=Passed scope=ImporterObjectIdentityOnly"
PREFAB = "Assets/Game/Prefabs/Vehicles/Unit_Veh_Light_Armored_Car.prefab"
CONFIG = "Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Light_Armored_Car_Config.asset"
EXPECTED_ROWS = [
    {"row_index": 0, "property_path": "config", "target_guid": "5680e7910ed3741beba6f07116b6b03c",
     "target_file_id": 106347591433935587, "target_type": 3,
     "config_guid": "b0ffc9714da994462a1e48679b4863de", "config_file_id": 11400000,
     "target_prefab_path": "Assets/Game/Prefabs/Characters/Unit.prefab",
     "target_resolution": "serialized_expected_authoring_component"},
    {"row_index": 1, "property_path": "config", "target_guid": "9ce6998c034284d98a89b5badc6e2f24",
     "target_file_id": 391336776749024018, "target_type": 3,
     "config_guid": "b0ffc9714da994462a1e48679b4863de", "config_file_id": 11400000,
     "target_prefab_path": "Assets/Game/Prefabs/Vehicles/Unit_Veh.prefab",
     "target_resolution": "unresolved_local_id_not_in_serialized_document"},
]
EXPECTED_OBSERVATION_ROWS = [
    {"target_guid": row["target_guid"], "target_local_id": str(row["target_file_id"]),
     "target_path": row["target_prefab_path"],
     "config_guid": row["config_guid"], "config_local_id": str(row["config_file_id"]),
     "config_path": CONFIG, "property_path": "config"}
    for row in EXPECTED_ROWS
]


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def captured_text(value) -> str:
    if value is None:
        return ""
    return value.decode(errors="replace") if isinstance(value, bytes) else str(value)


def validate_evidence(evidence: dict) -> None:
    need(evidence.get("kind") == "S002PrefabConfigTargetPartialSourceEvidence" and
         evidence.get("status") == "PartialUnresolved" and evidence.get("prefab_path") == PREFAB and
         evidence.get("unity_importer_semantics") is False and evidence.get("complete_provider_join") is False,
         "Exact current partial source evidence required")
    rows = evidence.get("rows")
    need(isinstance(rows, list) and len(rows) == 2, "Exactly two current serialized config rows required")
    for actual, expected in zip(rows, EXPECTED_ROWS):
        for key, value in expected.items():
            need(actual.get(key) == value, "Current config row identity changed: " + key)
        need(actual.get("target_guid_hit_count") == 1, "Unique serialized target GUID reference required")


def load_partial_receipt(partial_receipt_path: Path) -> tuple[dict, dict, Path]:
    partial_receipt_path = partial_receipt_path if partial_receipt_path.is_absolute() else ROOT / partial_receipt_path
    partial_receipt_path = partial_receipt_path.resolve(strict=True)
    need(partial_receipt_path.is_relative_to(ROOT) and partial_receipt_path.is_file(),
         "Current in-project partial-target receipt is required")
    receipt = json.loads(partial_receipt_path.read_text(encoding="utf-8"))
    need(receipt.get("kind") == "S002PrefabConfigTargetPartialSourceEvidenceReceipt" and
         receipt.get("status") == "PartialUnresolved" and receipt.get("prefab_path") == PREFAB and
         receipt.get("unity_importer_semantics") is False and
         receipt.get("complete_provider_join") is False and receipt.get("mission_ready") is False,
         "Exact unresolved current partial-target receipt required")
    desc = receipt.get("evidence")
    need(isinstance(desc, dict) and isinstance(desc.get("path"), str), "Partial evidence descriptor required")
    evidence_raw = Path(desc["path"])
    evidence_path = (evidence_raw if evidence_raw.is_absolute() else ROOT / evidence_raw).resolve(strict=True)
    need(evidence_path.is_relative_to(ROOT) and sha(evidence_path) == desc.get("sha256"),
         "Partial source evidence hash/path mismatch")
    evidence = json.loads(evidence_path.read_text(encoding="utf-8"))
    validate_evidence(evidence)
    need(evidence.get("source_pins") == receipt.get("source_pins"), "Receipt/evidence pin sets differ")
    for raw, expected in receipt.get("source_pins", {}).items():
        path = Path(raw).resolve(strict=True)
        need(path.is_relative_to(ROOT) and sha(path) == expected, "Partial source pin changed: " + raw)
    partial_runner = HERE / "inspect_s002_prefab_config_targets.py"
    projection_runner = HERE / "run_s002_current_setup_provider_projection.py"
    need(receipt.get("runner") == {"path": str(partial_runner.resolve()), "sha256": sha(partial_runner)} and
         receipt.get("projection_runner") == {"path": str(projection_runner.resolve()), "sha256": sha(projection_runner)},
         "Partial receipt must be generated by the exact current inspectors")
    return receipt, evidence, partial_receipt_path


def current_pins(receipt: dict, evidence_path: Path, partial_receipt_path: Path) -> dict[str, str]:
    pins: dict[str, str] = {}
    for raw, expected in receipt["source_pins"].items():
        path = Path(raw).resolve(strict=True)
        need(path.is_relative_to(ROOT), "Partial source pin outside current project")
        need(sha(path) == expected, "Partial source pin changed: " + raw)
        pins[path.relative_to(ROOT).as_posix()] = expected
    extra = {
        CONFIG: ROOT / CONFIG,
        CONFIG + ".meta": ROOT / (CONFIG + ".meta"),
        str(OBSERVER.relative_to(ROOT)): OBSERVER,
        str(OBSERVER_META.relative_to(ROOT)): OBSERVER_META,
        str(Path(__file__).resolve().relative_to(ROOT)): Path(__file__).resolve(),
        str(WRAPPER.relative_to(ROOT)): WRAPPER,
        str(PROJECT_VERSION.relative_to(ROOT)): PROJECT_VERSION,
        str(partial_receipt_path.relative_to(ROOT)): partial_receipt_path,
        str(evidence_path.relative_to(ROOT)): evidence_path,
    }
    for relative, path in extra.items():
        path = path.resolve(strict=True)
        need(path.is_file() and path.is_relative_to(ROOT), "Required current source file missing: " + relative)
        digest = sha(path)
        prior = pins.get(relative)
        need(prior is None or prior == digest, "Conflicting source pin: " + relative)
        pins[relative] = digest
    return dict(sorted(pins.items()))


def snapshot(pins: dict[str, str]) -> dict[str, str]:
    current = {}
    for relative, expected in pins.items():
        path = ROOT / relative
        try:
            need(path.is_file() and not path.is_symlink(), "Pinned regular source file required: " + relative)
            current[relative] = sha(path)
        except (OSError, ValueError):
            current[relative] = "<unreadable>"
    return current


def wrapper_command(rtk_executable: Path, timeout: int, unity_log: Path,
                    observation_path: Path, pin_manifest: Path) -> list[str]:
    return [str(rtk_executable), "proxy", str(WRAPPER), "--timeout", str(timeout),
            "--log", str(unity_log), "--", "-quit", "-executeMethod",
            "Game.Tests.Editor.S002PrefabImporterIdentityObserver.Run",
            "-s002-prefab-importer-output", str(observation_path),
            "-s002-prefab-importer-pins", str(pin_manifest)]


def validate_observation(observation: dict) -> None:
    need(observation.get("kind") == "S002PrefabImporterIdentityObservation" and
         observation.get("status") == "Passed" and observation.get("scope") ==
         "ImporterObjectIdentityOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderJoin_NoReadiness",
         "Passed importer-only observer result required")
    rows = observation.get("config_overrides")
    need(isinstance(rows, list) and len(rows) == 2, "Exactly two importer-resolved config overrides required")
    normalized = []
    for row in rows:
        need(isinstance(row.get("target_type"), str) and row["target_type"] and
             isinstance(row.get("config_type"), str) and row["config_type"],
             "Unity-resolved target/config types must be recorded from actual importer results")
        normalized.append({key: row.get(key) for key in EXPECTED_OBSERVATION_ROWS[0]})
    need(sorted(normalized, key=lambda row: row["target_guid"]) ==
         sorted(EXPECTED_OBSERVATION_ROWS, key=lambda row: row["target_guid"]),
         "Importer-resolved targets/config references differ from both exact serialized rows")
    need(observation.get("identity_issues") == [], "Importer observer reported identity issues")
    need(observation.get("observer_loaded_or_modified_scene") is False and
         observation.get("observer_created_world") is False and
         observation.get("observer_invoked_baker") is False and
         observation.get("provider_join_complete") is False and observation.get("mission_ready") is False,
         "Observer must remain AssetDatabase-only and non-readiness evidence")


def run(partial_receipt: Path, output: Path, timeout: int) -> dict:
    need(timeout >= 30, "Explicit timeout >=30 seconds required")
    need(not output.exists(), "Fresh readback output directory required")
    output = output.resolve(strict=False)
    need(output.is_relative_to(ROOT) and not output.is_relative_to(ROOT / "Assets"),
         "Output must be a fresh in-project evidence directory outside Assets")
    need(output.parent.is_dir(), "Existing evidence parent directory required")
    partial, evidence, partial_path = load_partial_receipt(partial_receipt)
    evidence_raw = Path(partial["evidence"]["path"])
    evidence_path = (evidence_raw if evidence_raw.is_absolute() else ROOT / evidence_raw).resolve(strict=True)
    pins = current_pins(partial, evidence_path, partial_path)
    output.mkdir(parents=True)
    pin_manifest = output / "source-pin-manifest.json"
    manifest = {"kind": "S002PrefabImporterIdentityPinManifest", "scope": "ImporterObjectIdentityOnly",
                "source_pins": pins}
    pin_manifest.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    manifest_sha_before = sha(pin_manifest)
    before = snapshot(pins)
    unity_log = output / "unity.log"
    wrapper_log = output / "wrapper.log"
    observation_path = output / "observation.json"
    rtk_found = shutil.which("rtk")
    need(rtk_found is not None, "RTK executable required to invoke the repository Unity wrapper")
    rtk_executable = Path(rtk_found).resolve(strict=True)
    rtk_sha_before = sha(rtk_executable)
    command = wrapper_command(rtk_executable, timeout, unity_log, observation_path, pin_manifest)
    wrapper_exit = None
    wrapper_output = ""
    error_text = None
    try:
        completed = subprocess.run(command, cwd=ROOT, capture_output=True, text=True,
                                   timeout=timeout + 45, check=False)
        wrapper_exit = completed.returncode
        wrapper_output = completed.stdout + completed.stderr
    except subprocess.TimeoutExpired as error:
        wrapper_output = captured_text(error.stdout) + captured_text(error.stderr)
        error_text = "wrapper_process_timeout"
    wrapper_log.write_text(wrapper_output, encoding="utf-8")
    after = snapshot(pins)
    manifest_sha_after = sha(pin_manifest)
    rtk_sha_after = sha(rtk_executable)
    marker_present = unity_log.is_file() and MARKER in unity_log.read_text(encoding="utf-8", errors="replace")
    observation = None
    if observation_path.is_file():
        try:
            observation = json.loads(observation_path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            error_text = error_text or "observer_output_invalid_json"
    observation_valid = False
    if observation is not None:
        try:
            validate_observation(observation)
            observation_valid = True
        except ValueError:
            observation_valid = False
    passed = (wrapper_exit == 0 and marker_present and observation_valid and
              observation.get("source_pins_before") == before and observation.get("source_pins_after") == after and
              before == after == pins and manifest_sha_before == manifest_sha_after and
              rtk_sha_before == rtk_sha_after)
    receipt = {
        "kind": "S002PrefabImporterIdentityReadbackReceipt",
        "status": "Passed" if passed else "Failed",
        "scope": "ImporterObjectIdentityOnly_NoSceneLoad_NoWorldCreation_NoBake_NoProviderJoin_NoReadiness",
        "partial_source_receipt": {"path": str(partial_path.relative_to(ROOT)), "sha256": sha(partial_path)},
        "partial_source_evidence": {"path": str(evidence_path.relative_to(ROOT)), "sha256": sha(evidence_path)},
        "source_pins_before": before,
        "source_pins_after": after,
        "pin_manifest": {"path": str(pin_manifest.relative_to(ROOT)), "sha256_before": manifest_sha_before,
                         "sha256_after": manifest_sha_after},
        "wrapper": {"path": str(WRAPPER.relative_to(ROOT)), "exit_code": wrapper_exit,
                    "command": command, "log": str(wrapper_log.relative_to(ROOT)),
                    "log_sha256": sha(wrapper_log), "required_marker": MARKER, "marker_present": marker_present},
        "rtk_invocation": {"executable_path": str(rtk_executable), "command_prefix": [str(rtk_executable), "proxy"],
                           "binary_sha256_before": rtk_sha_before, "binary_sha256_after": rtk_sha_after,
                           "binary_hash_scope": "Execution tool integrity only; not source authority"},
        "unity_log": {"path": str(unity_log.relative_to(ROOT)) if unity_log.exists() else None,
                      "sha256": sha(unity_log) if unity_log.exists() else None},
        "observation": {"path": str(observation_path.relative_to(ROOT)) if observation_path.exists() else None,
                        "sha256": sha(observation_path) if observation_path.exists() else None},
        "provider_join_complete": False,
        "factory_execution": False,
        "observer_created_world": False,
        "observer_invoked_baker": False,
        "source_fit_verified": False,
        "mission_ready": False,
    }
    if error_text:
        receipt["failure_reason"] = error_text
    receipt_path = output / "receipt.json"
    receipt_path.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    return receipt


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--partial-receipt", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--timeout", type=int, required=True)
    args = parser.parse_args()
    try:
        result = run(args.partial_receipt, args.output, args.timeout)
        print(json.dumps(result, indent=2))
        return 0 if result["status"] == "Passed" else 2
    except (ValueError, OSError, KeyError, TypeError, json.JSONDecodeError, subprocess.SubprocessError) as error:
        print("S002 prefab importer identity readback failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
