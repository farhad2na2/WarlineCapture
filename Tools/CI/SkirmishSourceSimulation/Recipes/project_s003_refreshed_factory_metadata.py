#!/usr/bin/env python3
"""Project the checked refreshed S003 factory capture; grants no runtime admission."""
import argparse
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify
from project_s003_current_factory_metadata import project_payload, read_factory_sources

CAPTURE_SHA = "7d039dd9a09fdff34e88682be17ce648b14dc926637915ec5748b243d1de2433"


def write(path, value):
    raw = (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode()
    with path.open("xb") as stream:
        stream.write(raw)
    return {"path": str(path.resolve()), "sha256": hashlib.sha256(raw).hexdigest()}


def project(root, output):
    root = root.resolve(strict=True)
    capture = root / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
    log = root / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/editor.log"
    compiler = root / "T Logs/SkirmishProduction/s003-current-original-compiler-20261104-T1/receipt.json"
    receipt_path = root / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
    qualified = qualify(root, capture, log, compiler, 0)
    if qualified != json.loads(receipt_path.read_bytes()) or qualified["capture"]["sha256"] != CAPTURE_SHA:
        raise ValueError("Exact freshly revalidated capture receipt required")
    value = json.loads(capture.read_bytes())
    payload = project_payload(value, CAPTURE_SHA, value["source_pins"], read_factory_sources(root), CAPTURE_SHA)
    output = output.absolute()
    output.mkdir(parents=True, exist_ok=False)
    definitions = write(output / "building-definitions.json", payload["building-definitions.json"])
    payload["configured-metadata.json"]["reference"] = definitions
    artifacts = {"building-definitions.json": definitions}
    for name in ("unit-production-metadata.json", "configured-metadata.json"):
        artifacts[name] = write(output / name, payload[name])
    receipt = {"kind": "S003RefreshedFactoryMetadataProjection", "status": "PassedSourceOnlyProjection",
               "capture": qualified["capture"], "capture_receipt": {"path": str(receipt_path),
               "sha256": hashlib.sha256(receipt_path.read_bytes()).hexdigest()}, "artifacts": artifacts,
               "counts": {"buildings": len(payload["building-definitions.json"]["definitions"]),
                          "units": len(payload["unit-production-metadata.json"]["units"]),
                          "transports": len(payload["unit-production-metadata.json"]["transports"])},
               "native_context_qualified": False, "source_fit_certified": False,
               "runtime_admission": False, "mission_ready": False}
    write(output / "projection-receipt.json", receipt)
    return receipt


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    print(json.dumps(project(Path(args.repo), Path(args.output))))


if __name__ == "__main__":
    main()
