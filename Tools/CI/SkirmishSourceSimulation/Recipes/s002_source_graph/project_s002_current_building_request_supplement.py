#!/usr/bin/env python3
"""Project current S002 original-provider metadata into the schema 1 supplement."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
PROVIDER_MARKER = "[SkirmishTwoSeedMetadata] result=Passed contexts=2 negatives=8 scope=MetadataOnly"
DEFINITIONS_MARKER = "[SkirmishAuthoredBuildingDefinitions] result=Passed definitions=8 unsupported=0 fullMission=0"
SCOPE = "CurrentOriginalS002ConfiguredProvidersAndRequestability_MetadataOnly"
HEX = re.compile(r"^[0-9a-f]{64}$")


class ProjectionError(ValueError):
    pass


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ProjectionError(message)


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def read_json(path: Path) -> tuple[dict, bytes]:
    raw = path.read_bytes()
    value = json.loads(raw.decode("utf-8"))
    require(isinstance(value, dict), f"JSON object required: {path}")
    return value, raw


def verify_capture(receipt_path: Path, log_path: Path, artifact_path: Path,
                   expected_scope: str, expected_marker: str,
                   expected_artifact_kind: str) -> tuple[dict, dict, bytes]:
    receipt, receipt_bytes = read_json(receipt_path)
    artifact, artifact_bytes = read_json(artifact_path)
    log_bytes = log_path.read_bytes()
    require(receipt.get("status") == "Passed" and receipt.get("wrapper_exit_code") == 0
            and receipt.get("marker_count") == 1, f"Capture receipt did not pass: {receipt_path}")
    require(receipt.get("scope") == expected_scope, f"Unexpected capture scope: {receipt_path}")
    require(receipt.get("mission_ready") is False, f"Capture readiness must remain false: {receipt_path}")
    require(receipt.get("artifact_sha256") == sha256(artifact_bytes),
            f"Capture artifact hash differs from receipt: {artifact_path}")
    require(receipt.get("log_sha256") == sha256(log_bytes),
            f"Capture log hash differs from receipt: {log_path}")
    require(log_bytes.count(expected_marker.encode("utf-8")) == 1,
            f"Exact capture pass marker required once: {log_path}")
    require(artifact.get("kind") == expected_artifact_kind,
            f"Unexpected capture artifact kind: {artifact_path}")
    # Retain exact receipt bytes as a provenance input, even though only its parsed
    # fields are consumed here.
    _ = receipt_bytes
    return receipt, artifact, artifact_bytes


def _normalized_source_path(root: Path, source: str) -> tuple[str, Path]:
    p = Path(source)
    if p.is_absolute():
        resolved = p.resolve(strict=True)
        try:
            relative = resolved.relative_to(root.resolve())
        except ValueError as exc:
            raise ProjectionError(f"Source pin escapes repository: {source}") from exc
    else:
        require("\\" not in source and all(part not in ("", ".", "..") for part in source.split("/")),
                f"Noncanonical source pin: {source}")
        relative = Path(source)
        resolved = (root / relative).resolve(strict=True)
        try:
            resolved.relative_to(root.resolve())
        except ValueError as exc:
            raise ProjectionError(f"Source pin escapes repository: {source}") from exc
    return relative.as_posix(), resolved


def verify_source_pins(root: Path, *pin_maps: dict, expected_counts: tuple[int, ...]) -> dict[str, str]:
    require(len(pin_maps) == len(expected_counts), "Capture source pin count declarations differ")
    merged: dict[str, str] = {}
    for pins, expected_count in zip(pin_maps, expected_counts):
        require(isinstance(pins, dict) and len(pins) == expected_count,
                "Capture source pin count differs from its receipt")
        observed: dict[str, str] = {}
        for source, digest in pins.items():
            require(isinstance(source, str) and isinstance(digest, str) and HEX.fullmatch(digest),
                    "Malformed capture source pin")
            relative, path = _normalized_source_path(root, source)
            require(sha256(path.read_bytes()) == digest, f"Stale current source pin: {relative}")
            if relative in observed:
                require(observed[relative] == digest, f"Conflicting duplicate source pin: {relative}")
            observed[relative] = digest
        for relative, digest in observed.items():
            if relative in merged:
                require(merged[relative] == digest, f"Capture source pin conflict: {relative}")
            merged[relative] = digest
    # The schema 1 consumer accepts project Assets pins and routes code pins to
    # the source root, data pins to the asset-data root. ProjectSettings and
    # package/build inputs are validated above but are not consumer dependency
    # rows, which only describe original configured-provider assets/code.
    asset_pins = {path: digest for path, digest in merged.items() if path.startswith("Assets/")}
    require(asset_pins, "Current project asset pins required")
    return dict(sorted(asset_pins.items()))


def identity_key(prefab: object, context: str) -> str:
    require(isinstance(prefab, dict), f"Asset identity object required: {context}")
    require(prefab.get("codec") == "source-asset-reference-v1", f"Source identity codec required: {context}")
    key, path, guid, local_id = (prefab.get(k) for k in ("key", "path", "guid", "local_id"))
    require(isinstance(key, str) and key and isinstance(path, str) and path.startswith("Assets/"),
            f"Current source prefab path/name required: {context}")
    require(isinstance(guid, str) and re.fullmatch(r"[0-9a-f]{32}", guid) is not None,
            f"Resolved source GUID required: {context}")
    require(type(local_id) is int and local_id > 0, f"Resolved source local ID required: {context}")
    return f"{guid}:{local_id}"


def project(provider: dict, definitions: dict, provider_receipt: dict, definitions_receipt: dict,
            provider_path: Path, definitions_path: Path, source_pins: dict[str, str]) -> dict:
    require(provider.get("kind") == "NativeTwoSeedMetadataQualification"
            and provider.get("mission") == "S002" and provider.get("size") == "Standard"
            and provider.get("difficulty") == "Regular" and provider.get("metadata_equal") is True
            and provider.get("scope") == "OriginalCompilerProvidersAndMetadataFactoriesOnly",
            "Exact original S002 two-seed metadata qualification required")
    for flag in ("full_mission_initialized", "opening_placement_transferred", "runtime_state_transferred", "public_admission_changed"):
        require(provider.get(flag) is False, f"Two-seed scope flag must remain false: {flag}")
    contexts = provider.get("contexts")
    require(isinstance(contexts, list) and len(contexts) == 2, "Both original seed contexts required")
    chosen = [c for c in contexts if type(c.get("seed")) is int and c["seed"] == 104731]
    require(len(chosen) == 1, "Unique exact S002 seed 104731 context required")
    context = chosen[0]
    require(context.get("catalog_id") == "S002" and context.get("difficulty") == "Regular"
            and context.get("size") == "Standard" and context.get("wrong_context_hash_rejected") is True
            and context.get("wrong_target_seed_rejected") is True,
            "Compiled S002 context identity/negative controls required")
    metadata = context.get("metadata")
    require(isinstance(metadata, dict) and context.get("metadata_key_sha256")
            == next(c.get("metadata_key_sha256") for c in contexts if c.get("seed") == 1),
            "Seed-independent original metadata digest required")
    building_rows = metadata.get("buildings")
    providers = metadata.get("providers")
    require(isinstance(building_rows, list) and len(building_rows) == 8 and isinstance(providers, dict),
            "Complete current S002 configured building domain required")
    baseline, scene = providers.get("baseline"), providers.get("scene")
    require(isinstance(baseline, list) and isinstance(scene, list), "Original provider lists required")

    require(definitions.get("schema_version") == 1 and definitions.get("kind") == "AuthoredSkirmishBuildingDefinitions"
            and definitions.get("authority") == "OriginalConfiguredPrefabMetadataFactory_NotMissionState"
            and definitions.get("catalog_id") == "S002" and definitions.get("seed") == 104731
            and definitions.get("setup_hash") == context.get("setup_hash")
            and definitions.get("full_mission_initialized") is False
            and definitions.get("native_visual_readiness") is False
            and definitions.get("unsupported_values") == [],
            "Current passed original building definitions artifact required")
    requested = definitions.get("requested_keys")
    def_rows = definitions.get("definitions")
    require(isinstance(requested, list) and len(requested) == 8 and len(set(requested)) == 8
            and isinstance(def_rows, list) and len(def_rows) == 8,
            "Exact bounded eight-definition S002 input required")
    require([r.get("key") for r in def_rows] == requested, "Definition rows must preserve authored request order")

    native_by_key: dict[str, dict] = {}
    identity_by_key: dict[str, str] = {}
    for row in building_rows:
        require(isinstance(row, dict) and isinstance(row.get("prefab"), dict), "Malformed original native building row")
        key = row["prefab"].get("key")
        require(isinstance(key, str) and key not in native_by_key, "Duplicate native building key")
        native_by_key[key] = row
        identity_by_key[key] = identity_key(row["prefab"], key)
    require(set(native_by_key) == set(requested), "Definition domain differs from original compiler provider domain")

    rows = []
    baseline_indices = []
    for definition in def_rows:
        key = definition.get("key")
        require(key in native_by_key, f"Unknown building definition: {key}")
        native = native_by_key[key]
        prefab = definition.get("prefab")
        require(prefab == native.get("prefab") and identity_key(prefab, key) == identity_by_key[key],
                f"Current definition prefab identity differs from original provider: {key}")
        native_index = native.get("index")
        require(type(native_index) is int and native_index >= 0, f"Original definition index missing: {key}")
        require(type(native.get("construction_index")) is int and type(native.get("scene_index")) is int,
                f"Original provider indices must be observed integers: {key}")
        construction = native["construction_index"]
        scene_index = native["scene_index"]
        require(construction >= -1 and scene_index >= -1, f"Invalid negative provider index: {key}")
        if construction >= 0:
            require(construction < len(baseline) and baseline[construction] == prefab,
                    f"Construction provider order/identity mismatch: {key}")
            baseline_indices.append(construction)
        else:
            require(not any(x.get("prefab", {}).get("key") == key for x in baseline),
                    f"Construction provider omission contradicts source list: {key}")
        if scene_index >= 0:
            require(scene_index < len(scene) and scene[scene_index] == prefab,
                    f"Scene provider order/identity mismatch: {key}")
        else:
            require(not any(x.get("prefab", {}).get("key") == key for x in scene),
                    f"Scene provider omission contradicts source list: {key}")

        entry = native.get("configured_entry")
        fields = entry.get("fields") if isinstance(entry, dict) else None
        require(isinstance(fields, dict), f"Original configured entry missing: {key}")
        display = fields.get("DisplayName")
        can_request = fields.get("CanRequest")
        entry_prefab = fields.get("Prefab")
        require(isinstance(display, dict) and display.get("codec") == "utf16-v1"
                and isinstance(display.get("value"), str), f"Original display name unsupported: {key}")
        require(isinstance(can_request, dict) and can_request.get("codec") == "bool-v1"
                and type(can_request.get("value")) is bool, f"Original requestability unsupported: {key}")
        require(entry_prefab == prefab, f"Configured entry prefab identity mismatch: {key}")
        rows.append({
            "definition_index": native_index,
            "key": key,
            "prefab": prefab,
            "display_name": display["value"],
            "can_request": can_request["value"],
            "construction_catalog_index": construction if construction >= 0 else None,
            "scene_catalog_index": scene_index if scene_index >= 0 else None,
        })
    require(sorted(baseline_indices) == list(range(len(baseline))),
            "Every baseline provider slot must be represented exactly and contiguously")
    require(len({r["definition_index"] for r in rows}) == len(rows), "Duplicate original definition indices")

    return {
        "schema_version": 1,
        "kind": "CurrentSourceBuildingRequestMetadataSupplement",
        "scope": SCOPE,
        "reference": {"path": str(definitions_path.resolve()), "sha256": sha256(definitions_path.read_bytes())},
        "source_pins": source_pins,
        "rows": rows,
        "mission_ready": False,
        "full_mission_initialized": False,
        "native_visual_readiness": False,
        "runtime_state_transferred": False,
        "provider_capture": {
            "path": str(provider_path.resolve()),
            "sha256": sha256(provider_path.read_bytes()),
            "seed": 104731,
            "setup_hash": context["setup_hash"],
            "scope": provider_receipt["scope"],
        },
        "definition_capture": {
            "path": str(definitions_path.resolve()),
            "sha256": sha256(definitions_path.read_bytes()),
            "scope": definitions_receipt["scope"],
        },
    }


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=ROOT)
    parser.add_argument("--provider", type=Path, required=True)
    parser.add_argument("--provider-receipt", type=Path, required=True)
    parser.add_argument("--provider-log", type=Path, required=True)
    parser.add_argument("--definitions", type=Path, required=True)
    parser.add_argument("--definitions-receipt", type=Path, required=True)
    parser.add_argument("--definitions-log", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)

    provider_receipt, provider, provider_bytes = verify_capture(
        args.provider_receipt, args.provider_log, args.provider,
        "OriginalCompilerProvidersAndMetadataFactoriesOnly", PROVIDER_MARKER,
        "NativeTwoSeedMetadataQualification")
    definitions_receipt, definitions, definitions_bytes = verify_capture(
        args.definitions_receipt, args.definitions_log, args.definitions,
        "OriginalConfiguredPrefabMetadataFactory_NotMissionState", DEFINITIONS_MARKER,
        "AuthoredSkirmishBuildingDefinitions")
    require(provider_receipt.get("source_pins_checked") == len(provider.get("source_pins", {})),
            "Provider source pin count differs from receipt")
    require(definitions_receipt.get("source_pins_checked") == len(definitions.get("source_pins", {})),
            "Definition source pin count differs from receipt")
    pins = verify_source_pins(args.repo, provider["source_pins"], definitions["source_pins"],
                              expected_counts=(provider_receipt["source_pins_checked"],
                                               definitions_receipt["source_pins_checked"]))
    result = project(provider, definitions, provider_receipt, definitions_receipt,
                     args.provider, args.definitions, pins)
    # Detect source/artifact drift during projection, not just at initial read.
    require(args.provider.read_bytes() == provider_bytes and args.definitions.read_bytes() == definitions_bytes,
            "Capture artifact changed during projection")
    for relative, digest in pins.items():
        require(sha256((args.repo / relative).read_bytes()) == digest,
                f"Current source changed during projection: {relative}")
    out_bytes = (json.dumps(result, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    if args.output.exists():
        raise ProjectionError(f"Refusing to overwrite output: {args.output}")
    receipt_path = args.output.with_name("receipt.json")
    if args.output == receipt_path or receipt_path.exists():
        raise ProjectionError(f"Refusing to overwrite receipt: {receipt_path}")
    with args.output.open("xb") as stream:
        stream.write(out_bytes)
    receipt = {
        "status": "Passed",
        "scope": SCOPE,
        "wrapper_exit_code": 0,
        "provider_receipt_sha256": sha256(args.provider_receipt.read_bytes()),
        "provider_artifact_sha256": sha256(provider_bytes),
        "provider_log_sha256": sha256(args.provider_log.read_bytes()),
        "definitions_receipt_sha256": sha256(args.definitions_receipt.read_bytes()),
        "definitions_artifact_sha256": sha256(definitions_bytes),
        "definitions_log_sha256": sha256(args.definitions_log.read_bytes()),
        "projector_sha256": sha256(Path(__file__).read_bytes()),
        "source_pins_checked": len(provider["source_pins"]) + len(definitions["source_pins"]),
        "consumer_source_pins_emitted": len(pins),
        "artifact_sha256": sha256(out_bytes),
        "ordered_key_identity_join": [
            {"key": row["key"], "guid": row["prefab"]["guid"], "local_id": row["prefab"]["local_id"]}
            for row in result["rows"]
        ],
        "ordered_key_identity_join_kind": "schema1_rows_order_key_plus_source_prefab_guid_local_id",
        "ordered_key_identity_join_sha256": sha256(json.dumps(
            [{"key": row["key"], "guid": row["prefab"]["guid"], "local_id": row["prefab"]["local_id"]}
             for row in result["rows"]], separators=(",", ":"), ensure_ascii=False).encode("utf-8")),
        "mission_ready": False,
        "full_mission_initialized": False,
        "native_visual_readiness": False,
        "runtime_state_transferred": False,
    }
    with receipt_path.open("x", encoding="utf-8") as stream:
        stream.write(json.dumps(receipt, indent=2) + "\n")
    print(json.dumps({"status": "Passed", "rows": len(result["rows"]),
                      "source_pins": len(pins), "mission_ready": False,
                      "artifact_sha256": receipt["artifact_sha256"]}))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ProjectionError, OSError, json.JSONDecodeError, KeyError, TypeError, ValueError) as exc:
        print(f"projection rejected: {exc}", file=sys.stderr)
        raise SystemExit(2)
