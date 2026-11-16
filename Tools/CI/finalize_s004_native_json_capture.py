#!/usr/bin/env python3
"""Offline finalizer for the observed S004 native JSON Editor capture."""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path

CAPTURE = "Logs/SkirmishProduction/s004-native-json-capture-20261103-r1/editor.log"
OUT_DIR = "Logs/SkirmishProduction/s004-native-json-capture-20261103-r1"
MARKER = "[S004OriginalLayoutNativeJson] result=Passed compilerCapture=Completed gameplay=NotRun world=NotCreated authoredInputsUnchanged=1 assemblyUnchanged=1"
PINS = {
    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset": "c2ffbdb5b7ddef82fdc89dfc9a115e0fd2036e81617d6a887288460b6b9a2dde",
    "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset": "c3b55adc48e1fe8ff5d4845510bc005f821af1c9c46eaf7f5c13210a3d5a2c9d",
    "Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs": "607099cbc3b702b55d4117486cc653c68bf5491e1411eadc6e7ff9dc0e15a166",
    "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupCompiler.cs": "1a89bebbb6194cb98887ec1f549e8fc8a6b2b5598a01880293032fd03799e225",
    "Assets/Game/Scripts/Configs/Skirmish/SkirmishSetupMatrixTable.cs": "50a15854409d02f2ea70f25d97b74b1cbda03fe5414c3373a5e6ad7806483dcc",
    "Assets/Game/Scripts/Skirmish/Contracts/SkirmishScenarioContracts.cs": "08420f35bea3da3abc2be02b6d2f5944443a20847082a9929ab2a44af88277c7",
    "Assets/Game/Scripts/Editor/Skirmish/S004OriginalLayoutNativeJsonCapture.cs": "77ba04b9033ff0a7dacb3b72b8595c1a1a149b3d7cb0e5f6492b82decb64cf89",
}
MATRIX_PATH = "Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv"
ASSEMBLIES = {
    "Game.Configs": ("Library/ScriptAssemblies/Game.Configs.dll", "assemblyPath", "assemblySha256"),
    "Game.Editor": ("Library/ScriptAssemblies/Game.Editor.dll", "captureAssemblyPath", "captureAssemblySha256"),
}
SIZES = {
    "Standard": ("1130726651", "True"),
    "War": ("2349248971", "False"),
    "LargeWar": ("762134471", "False"),
}
SYNTHETICS = {"null-empty", "signed-zero-floats-enums-nested-arrays"}


class FinalizeError(ValueError):
    pass


def need(condition: bool, message: str) -> None:
    if not condition:
        raise FinalizeError(message)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def fields(line: str) -> dict[str, str]:
    return dict(re.findall(r"([A-Za-z][A-Za-z0-9]*)=([^\s]+)", line))


def parse_json(value: str, label: str):
    try:
        return json.loads(value)
    except json.JSONDecodeError as exc:
        raise FinalizeError(label + " is not valid JSON") from exc


def verify(log_path: Path, root: Path, wrapper_exit: int) -> dict:
    need(wrapper_exit == 0, "wrapper exit must be exactly zero")
    raw = log_path.read_bytes()
    log = raw.decode("utf-8", errors="strict")
    lines = log.splitlines()
    need(sum(line.startswith(MARKER) for line in lines) == 1, "expected exactly one final pass marker")
    marker_line = next(line for line in lines if line.startswith(MARKER))
    need(marker_line == MARKER, "final pass marker has unexpected trailing data")

    authored = [line for line in lines if line.startswith("[S004OriginalLayoutNativeJson] authored asset=")]
    need(len(authored) == 1, "expected one authored S004 identity record")
    ident = fields(authored[0])
    need(ident.get("asset") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishScenario_S004.asset" and
         ident.get("assetGuid") == "2b7e4c91a0d64f3e8a5c1d9b6e0f4732" and
         ident.get("layoutAsset") == "Assets/Game/Configs/SkirmishExpansion/Scenarios/S004/SkirmishLayout_S004.asset" and
         ident.get("layoutGuid") == "5e9a2d74c1b84f60a3e7d8c0b5f19463" and
         ident.get("layout") == "layout.skirmish.db.ba" and ident.get("format") == "NativeJsonV1",
         "authored asset/layout identity is not the pinned S004 source")
    need(ident.get("fingerprint") == "e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00",
         "S004 layout semantic fingerprint differs")
    actual_json = parse_json(authored[0].split(" json=", 1)[1], "authored S004 JSON")
    need(actual_json.get("Id") == "layout.skirmish.db.ba" and actual_json.get("OperationMapId") == "opmap.skirmish.desert_base_01" and
         len(actual_json.get("Anchors", [])) == 24 and len(actual_json.get("Routes", [])) == 3 and len(actual_json.get("Pads", [])) == 18,
         "authored S004 native JSON identity/content shape differs")

    pin_lines = [line for line in lines if line.startswith("[S004OriginalLayoutNativeJson] sourcePin ")]
    observed = {}
    for line in pin_lines:
        row = fields(line)
        need(row.get("path") not in observed, "duplicate source pin")
        observed[row.get("path", "")] = row.get("sha256", "")
    need(observed == PINS, "source pin set differs from required seven inputs")
    for rel, digest in PINS.items():
        p = root / rel
        need(p.is_file() and sha(p) == digest, "source pin is missing or stale: " + rel)

    matrix_lines = [line for line in lines if line.startswith("[S004OriginalLayoutNativeJson] compilerInput ")]
    need(len(matrix_lines) == 1, "expected exactly one compiler matrix input record")
    matrix = fields(matrix_lines[0])
    matrix_file = root / MATRIX_PATH
    need(matrix.get("matrixPath") == MATRIX_PATH and matrix.get("matrixGuid") == "d9cfc4d22cff453cbba2ae4e9d815738" and
         matrix.get("matrixSha256") == "b28cfe04f72ab0161dea86813fc12a9a61c5d46361fc9299839394ee91a2cf97" and
         matrix_file.is_file() and sha(matrix_file) == matrix.get("matrixSha256"), "matrix source pin is stale or mismatched")

    for name, (rel, path_key, hash_key) in ASSEMBLIES.items():
        need(ident.get("assembly" if name == "Game.Configs" else "captureAssembly") == name, "assembly identity mismatch: " + name)
        path = ident.get(path_key, "")
        need(Path(path).name == Path(rel).name and (root / rel).is_file() and sha(root / rel) == ident.get(hash_key),
             "captured assembly file hash mismatch: " + name)
    need(ident.get("assemblyMvid") == "265a07e9-e8db-441c-b8ac-58a87facde7c" and
         ident.get("captureAssemblyMvid") == "a5a11039-f456-4fe1-b619-fe0aab614700", "assembly MVID mismatch")

    synth_lines = [line for line in lines if line.startswith("[S004OriginalLayoutNativeJson] synthetic=")]
    need(len(synth_lines) == 2, "expected exactly two synthetic JSON outputs")
    synthetic = {}
    for line in synth_lines:
        head, payload = line.split(" json=", 1)
        name = fields(head).get("synthetic")
        need(name not in synthetic, "duplicate synthetic output")
        synthetic[name] = parse_json(payload, "synthetic JSON")
    need(set(synthetic) == SYNTHETICS, "synthetic output labels differ")
    empty, signed = synthetic["null-empty"], synthetic["signed-zero-floats-enums-nested-arrays"]
    need(empty.get("Anchors") == [] and empty.get("Routes") == [] and empty.get("Pads") == [], "null/empty synthetic arrays did not serialize as empty arrays")
    need(signed.get("OriginX") == 0 and signed.get("OriginZ") == 0 and signed.get("Anchors", [{}])[0].get("NormalizedU") == 0 and
         signed.get("Routes", [{}])[0].get("Kind") == 2 and signed.get("Routes", [{}])[0].get("WaypointAnchorIds") == ["a", "b"] and
         signed.get("Pads", [{}])[0].get("FactionId") == 2, "signed-zero/enum/nested-array synthetic JSON contract failed")

    compiler_lines = [line for line in lines if line.startswith("[S004OriginalLayoutNativeJson] compiler size=")]
    need(len(compiler_lines) == 3, "expected exactly three per-size compiler records")
    compiler = {}
    for line in compiler_lines:
        row = fields(line)
        size = row.get("size")
        need(size not in compiler, "duplicate compiler size record")
        need(row.get("difficulty") == "Regular" and row.get("seed") == "104733" and row.get("outcome") == "accepted", "compiler record outcome/input mismatch")
        compiler[size] = (row.get("setupHash"), row.get("measuredLayoutBound"))
    need(compiler == SIZES, "compiler size/hash/binding set differs")

    return {
        "schema": "s004-native-json-capture-receipt-v1",
        "evidenceClass": "OfflineFinalizationOfObservedEditorCapture",
        "captureLog": str(log_path),
        "captureLogSha256": hashlib.sha256(raw).hexdigest(),
        "wrapperExit": wrapper_exit,
        "finalPassMarkerCount": 1,
        "sourcePinsVerified": observed,
        "matrix": {"path": MATRIX_PATH, "guid": matrix["matrixGuid"], "sha256": matrix["matrixSha256"]},
        "assemblies": {name: {"path": ident[key], "sha256": ident[hash_key], "mvid": ident[mvid_key]} for name, key, hash_key, mvid_key in [
            ("Game.Configs", "assemblyPath", "assemblySha256", "assemblyMvid"),
            ("Game.Editor", "captureAssemblyPath", "captureAssemblySha256", "captureAssemblyMvid")]},
        "authoredS004": {"asset": ident["asset"], "assetGuid": ident["assetGuid"], "layoutAsset": ident["layoutAsset"],
                         "layoutGuid": ident["layoutGuid"], "layoutId": ident["layout"], "format": ident["format"],
                         "fingerprint": ident["fingerprint"], "json": actual_json},
        "syntheticLabels": sorted(synthetic),
        "compilerRows": [{"size": size, "difficulty": "Regular", "seed": 104733, "outcome": "accepted",
                           "setupHash": compiler[size][0], "measuredLayoutBound": compiler[size][1] == "True"} for size in SIZES],
        "gameplay": "NotRun",
        "world": "NotCreated",
        "readiness": "NotClaimed",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--log", type=Path)
    parser.add_argument("--wrapper-exit", type=int, required=True, help="observed wrapper exit status")
    parser.add_argument("--out-dir", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    log = args.log.resolve() if args.log else root / CAPTURE
    out_dir = args.out_dir.resolve() if args.out_dir else root / OUT_DIR
    try:
        receipt = verify(log, root, args.wrapper_exit)
        out_dir.mkdir(parents=True, exist_ok=True)
        (out_dir / "capture-source.json").write_text(json.dumps({"schema": receipt["schema"], "sourcePins": receipt["sourcePinsVerified"], "matrix": receipt["matrix"], "assemblies": receipt["assemblies"]}, indent=2, sort_keys=True) + "\n")
        (out_dir / "capture-receipt.json").write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n")
        print("[S004NativeJsonFinalizer] result=Passed compilerCapture=Completed gameplay=NotRun world=NotCreated readiness=NotClaimed")
        return 0
    except (OSError, UnicodeError, json.JSONDecodeError, FinalizeError) as exc:
        print("[S004NativeJsonFinalizer] result=Failed reason=" + str(exc), file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
