#!/usr/bin/env python3
"""Derive the original rifle authoring projection for the refreshed S003 capture."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify
from run_unit_move_authoring_source_host import create_source_input


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    capture = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
    log = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
    receipt = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
    if qualify(ROOT, capture, log, 0) != json.loads(receipt.read_bytes()):
        raise ValueError("Fresh checked prefab capture required")
    output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    path, value = create_source_input(ROOT, output, json.loads(capture.read_bytes()))
    result = {"status": "PassedSourceOnlyProjectionInput", "input": {"path": str(path.resolve()),
              "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}, "capture_sha256": hashlib.sha256(capture.read_bytes()).hexdigest(),
              "source_pin_count": len(value["source_pins"]), "movement_comparison_executed": False,
              "runtime_admission": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(result, indent=2) + "\n"); print(json.dumps(result))


if __name__ == "__main__": main()
