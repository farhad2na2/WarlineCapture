#!/usr/bin/env python3
"""Write partial source-only evidence for serialized prefab config target rows."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
RUNNER = HERE / "run_s002_current_setup_provider_projection.py"
GENERIC = HERE.parent / "s004_source_graph" / "generate_s004_typed_config_graph.py"
READER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/selected_operation_map/helpers/read_source_configs.rb"


def sha(raw: bytes) -> str:
    return hashlib.sha256(raw).hexdigest()


def capture_pins(paths: list[Path]) -> dict[str, str]:
    pins = {}
    for path in paths:
        need(path.is_file() and not path.is_symlink(), "regular pinned source required: " + str(path))
        pins[str(path.resolve(strict=True))] = sha(path.read_bytes())
    return pins


def verify_pins(pins: dict[str, str], phase: str) -> None:
    for raw, expected in pins.items():
        path = Path(raw)
        need(path.is_file() and not path.is_symlink() and sha(path.read_bytes()) == expected,
             phase + " source pin changed/missing: " + raw)


def run(prefab: Path, output: Path) -> dict:
    need(not output.exists(), "fresh inspection output directory required")
    prefab = prefab.resolve(strict=True)
    need(prefab.is_relative_to(ROOT / "Assets") and prefab.suffix == ".prefab",
         "inspected prefab must be a current in-Assets prefab")
    execution_pins = capture_pins([Path(__file__).resolve(), RUNNER.resolve(), GENERIC.resolve(), READER.resolve()])
    verify_pins(execution_pins, "pre-inspection")
    spec = importlib.util.spec_from_file_location("s002_provider_projection", RUNNER)
    module = importlib.util.module_from_spec(spec)
    need(spec is not None and spec.loader is not None, "provider projection module unavailable")
    spec.loader.exec_module(module)
    helper = module.load_module(module.GENERIC, "s002_partial_inspection_helpers")
    projection = module.inspect_prefab_config_targets(ROOT, prefab, ROOT / module.UNIT_OWNER, helper)
    projection["source_pins"].update(execution_pins)
    verify_pins(execution_pins, "post-inspection")
    output.mkdir(parents=True)
    projection_path = output / "partial-source-evidence.json"
    projection_path.write_text(json.dumps(projection, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    verify_pins(projection["source_pins"], "pre-receipt")
    receipt = {"kind": "S002PrefabConfigTargetPartialSourceEvidenceReceipt", "status": projection["status"],
               "prefab_path": projection["prefab_path"], "row_count": len(projection["rows"]),
               "evidence": {"path": str(projection_path), "sha256": sha(projection_path.read_bytes())},
               "runner": {"path": str(Path(__file__).resolve()), "sha256": sha(Path(__file__).read_bytes())},
               "projection_runner": {"path": str(RUNNER), "sha256": sha(RUNNER.read_bytes())},
               "source_pins": projection["source_pins"], "complete_provider_join": False,
               "factory_execution": False, "unity_importer_semantics": False,
               "source_fit_verified": False, "mission_ready": False}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    verify_pins(projection["source_pins"], "post-receipt")
    return receipt


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--prefab", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        print(json.dumps(run(args.prefab, args.output), indent=2))
        return 0
    except (ValueError, OSError, KeyError, TypeError) as error:
        print("S002 partial prefab config target inspection failed closed: " + str(error), file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
