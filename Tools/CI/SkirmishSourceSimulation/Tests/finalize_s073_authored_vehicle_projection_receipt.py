#!/usr/bin/env python3
"""Bind a fresh isolated S073 authored vehicle capture to its wrapper log."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import shutil
from pathlib import Path


MARKER = "[S073AuthoredVehicleOccupancyProjection] result=Captured vehicles=22 sourceFit=0 ready=0"
METHOD = "Game.Editor.S073AuthoredVehicleOccupancyProjectionCapture.Run"
OUTPUT_ARG = "-s073AuthoredVehicleProjectionOutput"
SCOPE = "IsolatedPregameAuthoredScene_NotRuntimeCampaignStateOrReadiness"


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def load_projection_finalizer():
    path = Path(__file__).with_name("finalize_s073_authored_vehicle_occupancy_projection.py")
    spec = importlib.util.spec_from_file_location("s073_authored_vehicle_finalizer", path)
    if spec is None or spec.loader is None:
        raise ValueError("Could not load the existing source projection finalizer")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def finalize(project_root: Path, capture_path: Path, projection_path: Path,
             editor_log_path: Path, wrapper_exit: int) -> dict:
    root = project_root.resolve(strict=True)
    capture_path = capture_path.resolve(strict=True)
    projection_path = projection_path.resolve(strict=True)
    editor_log_path = editor_log_path.resolve(strict=True)
    if wrapper_exit != 0:
        raise ValueError("Unity wrapper exit must be zero")

    expected_projection = load_projection_finalizer().finalize(capture_path, root)
    projection = json.loads(projection_path.read_text(encoding="utf-8"))
    if projection != expected_projection:
        raise ValueError("Projection does not equal the current-source strict reduction of the captured inputs")

    log_text = editor_log_path.read_text(encoding="utf-8", errors="replace")
    if log_text.count(MARKER) != 1:
        raise ValueError("Exact captured-22 marker must occur exactly once in the Editor log")
    if log_text.count(METHOD) != 1:
        raise ValueError("Expected executeMethod must occur exactly once in the Editor log")
    if log_text.count(OUTPUT_ARG) != 1 or log_text.count(str(capture_path.parent)) != 1:
        raise ValueError("Editor command line must bind the output argument to this capture directory")
    batch_line = next((line for line in log_text.splitlines() if line.startswith("Batch mode:")), "")
    if not re.fullmatch(r"Batch mode:\s+NO", batch_line) or re.search(r"(?m)^\s*(?:--)?batchmode\s*$", log_text):
        raise ValueError("Expected GUI-licensed non-batch Editor log")
    if "-executeMethod\n" + METHOD not in log_text:
        raise ValueError("Editor log does not show the expected executeMethod command")

    evidence_dir = capture_path.parent
    required_paths = (evidence_dir / "authored-vehicle-occupancy-inputs.json", evidence_dir / "projection.json")
    if capture_path != required_paths[0] or projection_path != required_paths[1]:
        raise ValueError("Capture and projection must use the canonical filenames in one evidence directory")
    bound_log = evidence_dir / "editor.log"
    if bound_log.exists():
        raise FileExistsError("Refusing to replace existing evidence log: " + str(bound_log))
    shutil.copyfile(editor_log_path, bound_log)

    receipt = {
        "status": "Captured",
        "wrapper_exit": 0,
        "required_marker_present": True,
        "input_sha256": sha(capture_path),
        "projection_sha256": sha(projection_path),
        "editor_log_sha256": sha(bound_log),
        "scope": SCOPE,
        "mission_ready": False,
        "source_fit_validated": False,
        "source_runtime_validated": False,
        "scheduler_admitted": False,
    }
    receipt_path = evidence_dir / "capture-receipt.json"
    with receipt_path.open("x", encoding="utf-8") as stream:
        json.dump(receipt, stream, indent=2)
        stream.write("\n")
    return receipt


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--projection", type=Path, required=True)
    parser.add_argument("--editor-log", type=Path, required=True)
    parser.add_argument("--wrapper-exit", type=int, required=True)
    args = parser.parse_args()
    result = finalize(args.project_root, args.capture, args.projection, args.editor_log, args.wrapper_exit)
    print(json.dumps({"status": result["status"], "mission_ready": result["mission_ready"],
                      "input_sha256": result["input_sha256"], "editor_log_sha256": result["editor_log_sha256"]}))


if __name__ == "__main__":
    main()
