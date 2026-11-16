#!/usr/bin/env python3
"""Join current S004 building identities to checked shared template captures."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_metadata import qualify as qualify_metadata
from finalize_s003_refreshed_prefab_capture import qualify as qualify_templates

FACTORY_DIR = ROOT / "Logs/SkirmishProduction/s004-current-factory-metadata-20261104-r1"
FACTORY, FACTORY_LOG = FACTORY_DIR / "data/building-definitions.json", FACTORY_DIR / "editor.log"
S3DIR = ROOT / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data"
S3META = S3DIR / "s003-native-metadata.json"
S3METARECEIPT = ROOT / "T Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-T3/receipt.json"
S3PREFAB = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
S3PREFABRECEIPT = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/receipt.json"
S3KEYS = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-keys-20261104-T1/receipt.json"
FOUR = ROOT / "Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3/data/prefab-reference-data.json"
FOURRECEIPT = ROOT / "T Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-T3/receipt.json"
FOURINVENTORY = ROOT / "T Logs/SkirmishProduction/s004-missing-four-focused-keys-20261103-T1/receipt.json"
COMBINED = ROOT / "T Logs/SkirmishProduction/s004-current-template-domains-20261104-T1/receipt.json"


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def read(path): return json.loads(Path(path).read_text())
def need(ok, message):
    if not ok: raise ValueError(message)


def identity(row): return tuple(row.get(name) for name in ("key", "path", "guid", "local_id"))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--wrapper-exit", required=True, type=int); parser.add_argument("--output", required=True)
    args = parser.parse_args(); output = Path(args.output).absolute()
    need(args.wrapper_exit == 0 and not output.exists(), "Successful wrapper and fresh output required")
    inputs = (FACTORY, FACTORY_LOG, S3META, S3METARECEIPT, S3PREFAB, S3PREFABRECEIPT, S3KEYS, FOUR, FOURRECEIPT, FOURINVENTORY, COMBINED)
    hashes = {str(path): sha(path) for path in inputs}
    factory = read(FACTORY); meta_receipt = read(S3METARECEIPT); templates_receipt = read(S3PREFABRECEIPT)
    need(re.findall(r"\[SkirmishS004OriginalFactoryBuildingDefinitions\] result=Passed structureEntries=20 distinctStructureIds=10 distinctVisualKeys=9 missionReady=0", FACTORY_LOG.read_text(errors="replace")) == ["[SkirmishS004OriginalFactoryBuildingDefinitions] result=Passed structureEntries=20 distinctStructureIds=10 distinctVisualKeys=9 missionReady=0"], "Exact factory marker required")
    need(factory.get("kind") == "S004OriginalFactoryBuildingDefinitions" and factory.get("setup_identity", {}).get("catalog_id") == "S004" and factory["setup_identity"].get("seed") == 104733 and factory["setup_identity"].get("setup_hash") == 1130726651, "Exact original factory setup required")
    for flag in ("scene_loaded", "gameplay_world_created", "mission_initialized", "native_visual_readiness", "mission_readiness"):
        need(factory.get(flag) is False, "Factory scope invalid: " + flag)
    pins = factory.get("source_pins"); need(isinstance(pins, dict) and len(pins) > 1000, "Factory pin closure required")
    for path, expected in pins.items():
        path = Path(path) if Path(path).is_absolute() else ROOT / path
        need(path.is_file() and sha(path) == expected, "Factory source changed: " + str(path))
    s3log = Path(meta_receipt["editor_log"]["path"]); compiler = Path(meta_receipt["source_compiler_receipt"]["path"])
    need(qualify_metadata(ROOT, S3META, s3log, compiler, 0) == meta_receipt, "S003 factory capture stale")
    s3plog = Path(templates_receipt["editor_log"]["path"])
    need(qualify_templates(ROOT, S3PREFAB, s3plog, 0) == templates_receipt, "S003 prefab capture stale")
    four_receipt = read(FOURRECEIPT); four = read(FOUR); s3 = read(S3PREFAB)
    need(four_receipt.get("status") == "PassedScopedSourceOnlyCapture" and four_receipt["input_sha256"][str(FOUR)] == sha(FOUR), "Checked four-root capture required")
    for path, expected in four["source_pins"].items(): need(Path(path).is_file() and sha(path) == expected, "Four-root source changed: " + path)
    combined = read(COMBINED)
    need(combined.get("status") == "PassedDisposableSourceImportControl" and combined["input_sha256"][str(S3PREFAB)] == sha(S3PREFAB) and combined["input_sha256"][str(FOUR)] == sha(FOUR) and combined.get("reference_domains_checked") is True, "Checked combined importer required")
    s3meta = read(S3META)["context"]["metadata"]
    buildings = {row["prefab"]["key"]: row for row in s3meta["buildings"]}
    need(len(buildings) == len(s3meta["buildings"]), "Duplicate shared building identity")
    joined = []
    for row in factory["factory_definitions"]:
        key = row["prefab"]["key"]; need(key in buildings and identity(row["prefab"]) == identity(buildings[key]["prefab"]), "Building prefab identity mismatch: " + key)
        footprint = buildings[key]["fields"]["FootprintCells"]["fields"]
        need(int(footprint["UnityEngine.Vector2Int::m_X"]["value"]) == row["runtime_footprint_cells"]["x"] and int(footprint["UnityEngine.Vector2Int::m_Y"]["value"]) == row["runtime_footprint_cells"]["y"], "Building footprint mismatch: " + key)
        joined.append(key)
    need(len(joined) == 9 and len(set(joined)) == 9, "Nine distinct S004 building definitions required")
    shared = {row["key"]: row for row in read(S3KEYS)["ordered_prefab_identities"]}
    fourids = {row["key"]: row for row in read(FOURINVENTORY)["source_assets"]}
    slots = []; unique = set()
    for key in joined:
        for slot in buildings[key]["fields"]["ProductionSlots"]["items"]:
            ref = slot["fields"]["SpawnUnitPrefab"]; unit = ref["key"]
            if unit in shared:
                expected = shared[unit]; need(unit in s3["opening_prefabs"], "Shared prefab missing from capture")
                expected_identity = identity(expected)
            else:
                need(unit in fourids and unit in four["opening_prefabs"], "Production prefab missing from captures: " + unit)
                expected = fourids[unit]; expected_identity = (expected["key"], str(Path(expected["path"]).relative_to(ROOT)), expected["guid"], expected["local_id"])
            need(identity(ref) == expected_identity, "Production prefab identity mismatch: " + unit)
            slots.append({"building": key, "unit": unit, "capture": "shared_s003" if unit in shared else "s004_four"}); unique.add(unit)
    need(len(slots) == 27 and len(unique) == 20, "Production slot identity domain changed")
    need({str(path): sha(path) for path in inputs} == hashes, "Evidence changed during join")
    result = {"status": "PassedSourceOnlyIdentityJoin", "factory_wrapper_exit_code": args.wrapper_exit, "factory_marker_present": True,
              "factory_source_pins_checked": len(pins), "building_identities_and_footprints_matched": len(joined),
              "shared_factory_production_slots_joined": len(slots), "distinct_unit_identities": len(unique), "slots": slots,
              "input_sha256": hashes, "s004_factory_production_payload_parity_proven": False,
              "mission_world_created": False, "runtime_admission": False, "source_fit_certified": False, "mission_ready": False}
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("x") as stream: json.dump(result, stream, indent=2); stream.write("\n")
    print(json.dumps({k: result[k] for k in ("status", "building_identities_and_footprints_matched", "shared_factory_production_slots_joined", "distinct_unit_identities")}))


if __name__ == "__main__": main()
