#!/usr/bin/env python3
"""Validate the first S050 request for the compiler-selected physical exporter."""
import hashlib
import json
from pathlib import Path
import re
import tempfile

ROOT = Path(__file__).resolve().parents[4]
REQUEST = ROOT / "T Logs/SkirmishProduction/s050-physical-input-request-20261115-r1/request.json"
RECEIPT = REQUEST.with_name("request-receipt.json")
CAPTURE = ROOT / "Logs/SkirmishProduction/s050-navigation-source-capture-20261115-r1/navigation-reference.json"
CAPTURE_AUDIT = ROOT / "T Logs/SkirmishProduction/s050-navigation-source-capture-20261115-r1/capture-audit.json"
SOURCE = ROOT / "Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.PinnedPhysicalInputExport.cs"
LEGACY_SHA = "db13e20d91a3ab7396cfa7baf7c66213d8e763e1f2c37b267735d0f5d5f4dc4f"
EXPECTED_SHA = "192fb31f53cf01348650b7a71db429b6f721c251a721666315f53f98a9b5d350"


def sha(path):
    path = Path(path)
    if path.is_symlink():
        raise ValueError(f"symbolic link not allowed: {path}")
    path = path.resolve(strict=True)
    if not path.is_file():
        raise ValueError(f"regular file required: {path}")
    return hashlib.sha256(path.read_bytes()).hexdigest()


def no_duplicates(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON property: {key}")
        result[key] = value
    return result


request_bytes = REQUEST.read_bytes()
if hashlib.sha256(request_bytes).hexdigest() != EXPECTED_SHA:
    raise ValueError("S050 physical request digest changed")
request = json.loads(request_bytes, object_pairs_hook=no_duplicates)
expected_keys = {
    "schema_version", "kind", "source_path", "source_sha256", "grid_path", "grid_sha256",
    "surface_path", "surface_sha256", "operation_map_id", "source_identity", "content_hash",
    "frame", "selection", "source_fit_certified", "mission_ready",
}
if set(request) != expected_keys or type(request.get("schema_version")) is not int or request.get("schema_version") != 1 or request.get("kind") != "PinnedCampaignPhysicalInputExport":
    raise ValueError("exact physical input request schema required")
if request.get("operation_map_id") != "opmap.ch05.citywide_alert" or request.get("source_identity") != "6c258da69b63e5d391ad820c9c38a0df56859db7dc062e69a7f345d390c70fbe" or request.get("content_hash") != "5c9233dca1ecae738c3883dcac6eaed33da51596bc8bb87613881794005a463d":
    raise ValueError("Citywide source identity/content mismatch")
if request.get("frame") != {"width": 440, "height": 220, "cell_size": 1, "origin": [860, 0, 300]}:
    raise ValueError("exact Citywide source frame required")
if request.get("source_fit_certified") is not False or request.get("mission_ready") is not False:
    raise ValueError("request must retain non-readiness flags")
for path_key, digest_key in (("source_path", "source_sha256"), ("grid_path", "grid_sha256"), ("surface_path", "surface_sha256")):
    asset_path = ROOT / request[path_key]
    if sha(asset_path) != request[digest_key]:
        raise ValueError(f"source asset digest mismatch: {asset_path}")

selection = request["selection"]
if set(selection) != {"path", "sha256"} or sha(selection["path"]) != selection["sha256"]:
    raise ValueError("hash-pinned selection required")
selected = json.loads(Path(selection["path"]).read_bytes(), object_pairs_hook=no_duplicates)
if type(selected.get("schema_version")) is not int or selected.get("schema_version") != 1 or selected.get("status") != "ConfigurationCompiled" or selected.get("compiler_accepted") is not True or selected.get("catalog_id") != "S050" or selected.get("seed") != 104779 or selected.get("operation_map_id") != request["operation_map_id"] or selected.get("source_content_hash") != request["content_hash"]:
    raise ValueError("compiler selection does not match Citywide request")
for flag in ("world_allocated", "source_fit_verified", "mission_ready", "native_acceptance", "native_playable", "validation_passed"):
    if selected.get(flag) is not False:
        raise ValueError(f"selection must explicitly retain {flag}=false")

capture = json.loads(CAPTURE.read_bytes(), object_pairs_hook=no_duplicates)
audit = json.loads(CAPTURE_AUDIT.read_bytes(), object_pairs_hook=no_duplicates)
if capture.get("result") != "Collected" or capture.get("worldDisposed") is not True or capture.get("missionInitialized") is not False or capture.get("sourceFitCertified") is not False or capture.get("missionReady") is not False:
    raise ValueError("fresh source-only navigation capture required")
if audit.get("status") != "CapturedSourceRows" or audit.get("row_count") != 348 or audit.get("components_outside_current_importer_allowlist") != []:
    raise ValueError("verified fresh navigation audit required")
read_pins = capture.get("readPins")
if not isinstance(read_pins, dict) or len(read_pins) != audit.get("recorded_pins_verified"):
    raise ValueError("complete audited navigation pin set required")
for name, expected in read_pins.items():
    pinned_path = Path(name)
    if not pinned_path.is_absolute():
        pinned_path = ROOT / pinned_path
    if sha(pinned_path) != expected:
        raise ValueError(f"navigation source pin drift: {name}")
if capture.get("sourcePath") != request["source_path"] or capture.get("sourceSha256") != request["source_sha256"] or capture.get("operationMapId") != request["operation_map_id"] or capture.get("fixedMapReadDomain", {}).get("gridFrame") != request["frame"]:
    raise ValueError("navigation capture and physical request differ")

receipt = json.loads(RECEIPT.read_bytes(), object_pairs_hook=no_duplicates)
if receipt.get("request", {}).get("sha256") != EXPECTED_SHA or receipt.get("request", {}).get("bytes") != len(request_bytes) or receipt.get("source_fit_certified") is not False or receipt.get("mission_ready") is not False:
    raise ValueError("request receipt does not bind exact source-only request")
if receipt.get("fresh_navigation_read_pins_rechecked") != len(read_pins):
    raise ValueError("request receipt does not carry complete navigation pin audit")
source = SOURCE.read_text()
if f'const string ApprovedPhysicalInputSha="{LEGACY_SHA}";' not in source:
    raise ValueError("legacy S026 request pin changed")
if ("RunPinnedCompilerSelectedPhysicalSourceInputExport" not in source or
        "Hash-pinned compiler-selected map/content result required" not in source or
        "One duplicate-free compiler result document required" not in source or
        "IsSupportedSkirmishCatalogId(catalog)" not in source or "IsPositiveBoundedSeed(selected[\"seed\"])" not in source or
        "selected[\"schema_version\"]?.Type==JTokenType.Integer&&(long)selected[\"schema_version\"]==1" not in source or
        "S050FileHash(inputPath)==suppliedDigest" not in source or "S050FileHash(selectedPath)==selectedSha" not in source):
    raise ValueError("compiler-selected entrypoint authorization controls missing")


def exact_schema_one(value):
    return type(value) is int and value == 1


if not exact_schema_one(1):
    raise ValueError("integer schema version 1 rejected")
for value in (True, "1", 1.0, 0, 2, None):
    if exact_schema_one(value):
        raise ValueError(f"non-integer or unsupported schema version accepted: {value!r}")


def supported_catalog_id(value):
    return isinstance(value, str) and re.fullmatch(r"S[0-9]{3}", value) is not None and 1 <= int(value[1:]) <= 120


def positive_bounded_seed(value):
    return type(value) is int and 1 <= value <= 2**31 - 1


for value in ("S001", "S050", "S120"):
    if not supported_catalog_id(value):
        raise ValueError(f"valid catalog ID rejected: {value}")
for value in ("S000", "S121", "S999", "S50", "X050", "S０５０"):
    if supported_catalog_id(value):
        raise ValueError(f"invalid catalog ID accepted: {value}")
for value in (1, 104779, 2**31 - 1):
    if not positive_bounded_seed(value):
        raise ValueError(f"valid seed rejected: {value}")
for value in (0, -1, 2**31, "104779", True, 104779.0):
    if positive_bounded_seed(value):
        raise ValueError(f"invalid seed accepted: {value!r}")

with tempfile.TemporaryDirectory(prefix="s050-request-path-") as temp_dir:
    target = Path(temp_dir) / "target.json"
    target.write_text("{}")
    link = Path(temp_dir) / "linked-request.json"
    link.symlink_to(target)
    try:
        sha(link)
    except ValueError:
        pass
    else:
        raise ValueError("symbolic-link request path accepted")

print(json.dumps({
    "status": "PassedS050CompilerSelectedPhysicalRequest",
    "request_sha256": sha(REQUEST),
    "request_bytes": len(request_bytes),
    "selection_sha256": selection["sha256"],
    "source_assets_verified": 3,
    "navigation_read_pins_rechecked": len(read_pins),
    "fresh_navigation_rows": audit["row_count"],
    "unsupported_navigation_components": audit["components_outside_current_importer_allowlist"],
    "legacy_s026_pin_preserved": LEGACY_SHA,
    "source_fit_certified": False,
    "mission_ready": False,
}, indent=2))
