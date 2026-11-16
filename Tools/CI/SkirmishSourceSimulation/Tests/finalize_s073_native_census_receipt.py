#!/usr/bin/env python3
"""Bind a fresh native S073 pre-enqueue census to its Editor log and raw captures."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path


METHOD = "Game.Editor.SkirmishIndustrialBasinPlacementProbe.Launch"
MARKER_PREFIX = "[S073NativePathfindingCommandDiagnostic] result=Recorded scope=explicit_synthetic_command path="
RECEIPT_SCOPE = "IndependentInstrumentedNativeCapture_NotNormalInputOrReadiness"
SNAPSHOT_SCOPE = "RetainedOriginalS073World_ExplicitSyntheticCommand_NotNormalInputOrReadiness"
RAW_NAMES = (
    "native-blocker-occupancy-pre-enqueue-r1.bin",
    "native-blocker-occupancy-post-apply-r1.bin",
)


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def finalize(project_root: Path, snapshot_path: Path, raw_dir: Path, editor_log: Path,
             output_dir: Path, wrapper_exit: int) -> dict:
    root = project_root.resolve(strict=True)
    snapshot_path = snapshot_path.resolve(strict=True)
    raw_dir = raw_dir.resolve(strict=True)
    editor_log = editor_log.resolve(strict=True)
    output_dir = output_dir.resolve(strict=True)
    need(wrapper_exit == 0, "Unity wrapper exit must be zero")
    need(snapshot_path.parent == Path("/private/tmp") or Path("/private/tmp") in snapshot_path.parents,
         "Native snapshot must come from /private/tmp")
    need(raw_dir == Path("/private/tmp") or Path("/private/tmp") in raw_dir.parents,
         "Native raw snapshot directory must be under /private/tmp")

    snapshot = json.loads(snapshot_path.read_text(encoding="utf-8"))
    need(snapshot.get("kind") == "S073NativePathfindingCommandDiagnostic" and
         snapshot.get("schema_version") == 1 and snapshot.get("scope") == SNAPSHOT_SCOPE and
         snapshot.get("seed") == 104729 and snapshot.get("scenario_index") == 3 and
         snapshot.get("normal_input_playthrough") is False and snapshot.get("player_readiness") is False,
         "Exact unready original-S073 explicit-synthetic diagnostic snapshot required")
    session_id = snapshot.get("session_id")
    need(isinstance(session_id, str) and len(session_id) == 32,
         "Native S073 session id missing or malformed")
    observation = snapshot.get("pre_enqueue_input_observation") or {}
    census = observation.get("occupancy_contributor_census") or {}
    pins = census.get("source_pins_sha256")
    need(census.get("session_id") == session_id and census.get("read_only") is True and
         census.get("state_imported") is False and census.get("gameplay_state_mutated") is False and
         isinstance(pins, dict) and len(pins) >= 15,
         "Pre-enqueue native census identity/read-only/source pin contract failed")
    for relative, expected in pins.items():
        path = root / relative
        need(not Path(relative).is_absolute() and ".." not in Path(relative).parts and path.is_file() and
             sha(path) == expected, "Captured native source pin differs from current source: " + relative)

    log_text = editor_log.read_text(encoding="utf-8", errors="replace")
    exact_marker = MARKER_PREFIX + str(snapshot_path)
    need(log_text.count(exact_marker) == 1 and log_text.count(METHOD) == 1 and
         "-executeMethod\n" + METHOD in log_text,
         "Exact native diagnostic result marker and probe executeMethod required")
    batch_line = next((line for line in log_text.splitlines() if line.startswith("Batch mode:")), "")
    need(bool(re.fullmatch(r"Batch mode:\s+NO", batch_line)) and
         not re.search(r"(?m)^\s*(?:--)?batchmode\s*$", log_text),
         "Expected GUI-licensed non-batch Editor log")

    # Both raw snapshots must be linked from their observations and match the
    # source-authored binary plus JSON sidecar hashes before they are copied.
    raw_records = []
    for name, observation_name in zip(RAW_NAMES,
                                      ("pre_enqueue_input_observation", "post_apply_input_observation")):
        raw_path = raw_dir / name
        sidecar_path = raw_dir / (name + ".json")
        need(raw_path.is_file() and sidecar_path.is_file(), "Missing native raw snapshot or sidecar: " + name)
        sidecar = json.loads(sidecar_path.read_text(encoding="utf-8"))
        raw_ref = (snapshot.get(observation_name) or {}).get("raw_blocker_occupancy_snapshot") or {}
        need(sidecar.get("producer") == "native" and sidecar.get("observation") == observation_name and
             sidecar.get("native_session_id") == session_id and sidecar.get("raw_sha256") == sha(raw_path) and
             raw_ref.get("raw_sha256") == sha(raw_path) and raw_ref.get("raw_path") == str(raw_path),
             "Raw native blocker/occupancy sidecar does not bind to snapshot observation: " + name)
        raw_records.append({"name": name, "sha256": sha(raw_path),
                            "sidecar_sha256": sha(sidecar_path), "byte_length": raw_path.stat().st_size})

    snapshot_dest = output_dir / "snapshot.json"
    need(not snapshot_dest.exists(), "Refusing to overwrite native snapshot evidence")
    shutil.copyfile(snapshot_path, snapshot_dest)
    for name in RAW_NAMES:
        for source in (raw_dir / name, raw_dir / (name + ".json")):
            destination = output_dir / source.name
            need(not destination.exists(), "Refusing to overwrite native raw evidence: " + destination.name)
            shutil.copyfile(source, destination)
    log_dest = output_dir / "editor.log"
    if log_dest.exists():
        need(sha(log_dest) == sha(editor_log), "Evidence editor.log exists and differs from supplied wrapper log")
    else:
        shutil.copyfile(editor_log, log_dest)

    receipt = {
        "status": "Recorded", "wrapper_exit": 0,
        "snapshot_sha256": sha(snapshot_dest), "editor_log_sha256": sha(log_dest),
        "session_id": session_id, "source_pin_count": len(pins),
        "scope": RECEIPT_SCOPE, "diagnostic_scope": SNAPSHOT_SCOPE,
        "synthetic_input_scope": "One injected TargetPathOnly request through the production EnqueueTargetPathMoveOrder owner; not normal player input",
        "raw_snapshots": raw_records,
        "shutdown_intervention": "None", "mission_ready": False,
        "normal_input_playthrough": False, "player_readiness": False,
    }
    receipt_path = output_dir / "receipt.json"
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return receipt


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--snapshot", type=Path, required=True)
    parser.add_argument("--raw-dir", type=Path, required=True)
    parser.add_argument("--editor-log", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args()
    receipt = finalize(args.project_root, args.snapshot, args.raw_dir, args.editor_log,
                       args.output_dir, args.wrapper_exit)
    print(json.dumps({"status": receipt["status"], "session_id": receipt["session_id"],
                      "source_pin_count": receipt["source_pin_count"], "mission_ready": False}))


if __name__ == "__main__":
    main()
