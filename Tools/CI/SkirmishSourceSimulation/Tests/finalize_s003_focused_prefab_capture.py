#!/usr/bin/env python3
"""Finalize the S003 source-template diagnostic capture without qualifying replay/readiness."""
import argparse
import hashlib
import json
import re
from pathlib import Path


KEYS_REL = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/prefab-keys.json")
KEYS_SHA = "38bd886005c62bc121d165e1119a3f8876b456eddaa0ef873a1a5681aa66539b"
DESCRIPTOR_RECEIPT_REL = Path("T Logs/SkirmishProduction/s003-current-prefab-key-descriptor-20261103-r1/receipt.json")
DESCRIPTOR_RECEIPT_SHA = "c676b2a726ea8134f72189d37e46ecb552e34eec8524b7b033c2464540eee181"
METADATA_REL = Path("Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata.json")
METADATA_SHA = "2f857f965acd8743339ee73cf032ddf54e4ba7f07f278f4cde50d1e1876e91d3"
METADATA_RECEIPT_REL = Path("Logs/SkirmishProduction/s003-current-metadata-capture-20261103-r1/data/s003-native-metadata-receipt.json")
METADATA_RECEIPT_SHA = "aff49e834d5f2c74fe6b5a8c9666fdfeb297fb18b624ed81b5d5f4348db494f0"
CAPTURE_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/data/prefab-reference-data.json")
LOG_REL = Path("Logs/SkirmishProduction/s003-current-focused-prefab-capture-20261103-r1/editor.log")
CAPTURE_METHOD = "Game.Editor.SkirmishBakedActorSnapshotExporter.RunS003FocusedPrefabReferenceData"
MARKER_PREFIX = "[SkirmishS003FocusedPrefabReferenceData] result=Collected prefabs=19 entities="
MARKER_SUFFIX = " utilityWorld=True missionWorld=False replayReady=False missionReady=False"
GRAPH_SHA = "a472522efd4a3d2115377e3a9dc422811785c02d1a75a023bd9e2cea986dadcb"
RUN6_RECEIPT_SHA = "3d518a67a576bf223d66fb88adf181fc893bfb890200d902948d4a1a36b13609"
RUN6_AUDIT_SHA = "52b5e97a494dc4adf5e5159e65fbe5921f57ad90b730012c5d0698f820abeda9"
RUN6_GRAPH_SHA = "150cb503bb95dcdb1371e2c52c54764308b9bf1ad5e6305ae7ba3a68bf39cc5b"
SETUP_HASH = 2394754492
UNIT_COUNT = 19
PRESENTATION_FIELDS = {
    ("Game.Components.UnitAttackImpactVfxReference", "Prefab"),
    ("Game.Components.UnitMuzzleFlashVfxReference", "Prefab"),
    *(("Game.Components.AirMissileLauncherVfxReferenceComponent", field) for field in (
        "MissileVisualPrefab", "LaunchFlashPrefab", "LaunchSmokePrefab", "MissileTrailPrefab",
        "AirburstExplosionPrefab", "AirTargetImpactPrefab", "InterceptExplosionPrefab")),
}
ENTITY_ID = re.compile(r"(?:0|[1-9][0-9]*):(?:0|[1-9][0-9]*)\Z")


def sha_bytes(data):
    return hashlib.sha256(data).hexdigest()


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON property: " + key)
        result[key] = value
    return result


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=unique_object,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError("Invalid JSON constant: " + value)))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def validate_input_bundle(repo):
    paths = {"keys": repo / KEYS_REL, "descriptor_receipt": repo / DESCRIPTOR_RECEIPT_REL,
             "metadata": repo / METADATA_REL, "metadata_receipt": repo / METADATA_RECEIPT_REL}
    expected = {"keys": KEYS_SHA, "descriptor_receipt": DESCRIPTOR_RECEIPT_SHA,
                "metadata": METADATA_SHA, "metadata_receipt": METADATA_RECEIPT_SHA}
    raw = {}
    for name, path in paths.items():
        data = path.read_bytes()
        require(sha_bytes(data) == expected[name], "Exact current S003 input changed: " + str(path))
        raw[name] = data
    keys = read_json(paths["keys"])
    descriptor = read_json(paths["descriptor_receipt"])
    metadata = read_json(paths["metadata"])
    capture_receipt = read_json(paths["metadata_receipt"])
    require(isinstance(keys, list) and len(keys) == UNIT_COUNT and
            all(isinstance(key, str) and key and key == key.strip() for key in keys) and
            len(set(k.casefold() for k in keys)) == UNIT_COUNT,
            "Exact ordered unique 19-key current descriptor required")
    context = metadata.get("context", {})
    native = context.get("metadata", {})
    unit_rows = native.get("units", [])
    digest = sha_bytes(json.dumps(native, separators=(",", ":"), ensure_ascii=False).encode("utf-8"))
    require(descriptor.get("kind") == "S003FocusedPrefabKeyDescriptorReceipt" and
            descriptor.get("status") == "PassedSourceOnly" and
            descriptor.get("descriptor", {}).get("sha256") == KEYS_SHA and
            descriptor.get("descriptor", {}).get("keys_in_metadata_order") == keys and
            descriptor.get("capture", {}).get("sha256") == METADATA_SHA and
            descriptor.get("capture", {}).get("receipt_sha256") == METADATA_RECEIPT_SHA and
            descriptor.get("source_graph", {}).get("sha256") == GRAPH_SHA and
            descriptor.get("run6_compiler_provenance", {}).get("receipt_sha256") == RUN6_RECEIPT_SHA and
            descriptor.get("run6_compiler_provenance", {}).get("post_run_audit_sha256") == RUN6_AUDIT_SHA and
            descriptor.get("run6_compiler_provenance", {}).get("source_graph_sha256") == RUN6_GRAPH_SHA,
            "Exact S003 key descriptor/capture/compiler provenance binding required")
    require(context.get("catalog_id") == "S003" and context.get("size") == "Standard" and
            context.get("difficulty") == "Regular" and context.get("seed") == 104732 and
            context.get("setup_hash") == SETUP_HASH and context.get("metadata_key_sha256") == digest and
            len(unit_rows) == UNIT_COUNT and [row.get("prefab", {}).get("key") for row in unit_rows] == keys,
            "Current S003 Standard/Regular seed 104732 metadata order required")
    identity_rows = descriptor.get("prefab_identities")
    metadata_pins = metadata.get("source_pins", {})
    require(isinstance(identity_rows, list) and len(identity_rows) == UNIT_COUNT and
            isinstance(metadata_pins, dict), "Exact 19 current prefab identities and source pins required")
    normalized_metadata_pins = {}
    for raw_path, expected in metadata_pins.items():
        candidate = Path(raw_path)
        candidate = candidate if candidate.is_absolute() else repo / candidate
        normalized_metadata_pins[candidate.resolve(strict=True)] = expected
    for index, (identity, unit) in enumerate(zip(identity_rows, unit_rows)):
        prefab = unit.get("prefab", {})
        path = identity.get("path")
        require(identity.get("index") == index and identity.get("key") == keys[index] and
                identity.get("key") == prefab.get("key") and identity.get("guid") == prefab.get("guid") and
                identity.get("local_id") == prefab.get("local_id") and path == prefab.get("path") and
                isinstance(path, str) and path.startswith("Assets/Game/Prefabs/") and path.endswith(".prefab") and
                ".." not in Path(path).parts,
                "Current metadata and key descriptor source identity differ at unit " + str(index))
        asset_path = (repo / path).resolve(strict=True)
        meta_path = Path(str(asset_path) + ".meta").resolve(strict=True)
        require(asset_path.is_relative_to(repo) and meta_path.is_relative_to(repo),
                "Prefab identity source escapes repository")
        asset_sha, meta_sha = identity.get("prefab_sha256"), identity.get("meta_sha256")
        require(isinstance(asset_sha, str) and isinstance(meta_sha, str) and
                normalized_metadata_pins.get(asset_path) == asset_sha and
                normalized_metadata_pins.get(meta_path) == meta_sha and
                sha_bytes(asset_path.read_bytes()) == asset_sha and sha_bytes(meta_path.read_bytes()) == meta_sha,
                "Current prefab or .meta source pin mismatch: " + path)
        guid_rows = re.findall(rb"(?m)^guid:\s*([0-9a-f]{32})\s*$", meta_path.read_bytes())
        require(guid_rows == [str(identity.get("guid")).encode("ascii")],
                "Current prefab .meta GUID differs from descriptor identity: " + path)
    require(metadata.get("utility_world_created") is True and metadata.get("utility_world_disposed") is True and
            metadata.get("mission_world_created") is False and metadata.get("native_context_qualified") is False and
            metadata.get("source_fit_certified") is False and metadata.get("mission_ready") is False and
            capture_receipt.get("kind") == "S003OriginalCompilerFactoryMetadataCaptureReceipt" and
            capture_receipt.get("validation_status") == "Passed" and capture_receipt.get("wrapper_exit_code") == 0 and
            capture_receipt.get("capture_sha256") == METADATA_SHA and capture_receipt.get("graph_sha256") == GRAPH_SHA and
            capture_receipt.get("run6_receipt_sha256") == RUN6_RECEIPT_SHA and
            capture_receipt.get("run6_post_run_audit_sha256") == RUN6_AUDIT_SHA and
            capture_receipt.get("run6_graph_sha256") == RUN6_GRAPH_SHA and
            capture_receipt.get("utility_world_created") is True and
            capture_receipt.get("utility_world_disposed") is True and
            capture_receipt.get("mission_world_created") is False and
            capture_receipt.get("mission_ready") is False,
            "Data-only native metadata receipt and honest World/readiness flags required")
    return paths, raw, keys, descriptor, metadata, capture_receipt


def _entity_id(value):
    if not isinstance(value, str) or not ENTITY_ID.fullmatch(value):
        return False
    index, version = (int(part) for part in value.split(":"))
    return ((index == 0) == (version == 0))


def classify_unsupported(rows, entity_rows):
    require(isinstance(rows, list), "Raw unsupported_values array required")
    entities = {row["id"]: row for row in entity_rows}
    seen = set()
    classified = []
    for row in rows:
        require(isinstance(row, dict), "Unsupported value rows must be objects")
        path = row.get("path")
        require(isinstance(path, str), "Unsupported value path required")
        parts = path.split("/")
        require(len(parts) == 3 and _entity_id(parts[0]) and parts[0] in entities and
                isinstance(row.get("reason"), str) and row["reason"] and
                isinstance(row.get("type"), str) and row["type"],
                "Canonical unsupported entity/component/field identity required")
        require(path not in seen, "Duplicate unsupported path")
        seen.add(path)
        component = parts[1]
        component_row = entities[parts[0]]["components"].get(component)
        require(isinstance(component_row, dict) and component_row.get("present") is True,
                "Unsupported path must identify a present component")
        is_object_ref = (row["reason"] == "unity_object_ref_requires_bound_source_identity" and
                         row["type"].startswith("Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject,"))
        is_presentation = is_object_ref and (component, parts[2]) in PRESENTATION_FIELDS
        classified.append({"path": path,
                           "classification": "unresolved_presentation_object_reference" if is_presentation
                           else "unclassified_unsupported_consumer_blocker",
                           "reason": row["reason"], "type": row["type"],
                           "raw_row_sha256": sha_bytes(json.dumps(row, sort_keys=True, separators=(",", ":"),
                                                          ensure_ascii=False).encode("utf-8"))})
    return classified


def _entity_refs(value):
    if isinstance(value, dict):
        if value.get("codec") == "entity-v1":
            yield value.get("id")
        for child in value.values():
            yield from _entity_refs(child)
    elif isinstance(value, list):
        for child in value:
            yield from _entity_refs(child)


def validate_wrapper_binding(log, capture_path, input_paths, audit):
    marker_lines = [line for line in log.splitlines() if line.startswith(MARKER_PREFIX) and line.endswith(MARKER_SUFFIX)]
    require(len(marker_lines) == 1, "Exact unique S003 Collected wrapper marker required")
    match = re.fullmatch(re.escape(MARKER_PREFIX) + r"([1-9][0-9]*) unsupported=([0-9]+)" +
                         re.escape(MARKER_SUFFIX), marker_lines[0])
    require(match is not None and int(match.group(1)) == audit["entity_count"] and
            int(match.group(2)) == audit["unsupported_count"], "Wrapper marker counts differ from raw artifact")
    require(CAPTURE_METHOD in log and str(capture_path.parent) in log and
            all(str(path.resolve()) in log or str(path) in log for path in input_paths),
            "Editor log does not bind exact method, output and input bundle")


def validate_wrapper_exit(wrapper_exit):
    require(wrapper_exit == 0, "Checked Unity wrapper exit code must be zero")


def audit_document(capture, expected_keys):
    require(capture.get("schema_version") == 1 and
            capture.get("kind") == "AuthoredSkirmishPrefabReferenceData" and
            capture.get("authority") == "ActualSourceAssetsAndNativeBakers_NotMissionState" and
            capture.get("mission_id") == "S003" and
            capture.get("source_scenes") == ["Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity"] and
            capture.get("requested_prefab_keys") == expected_keys and
            capture.get("requested_prefab_keys_sha256") == KEYS_SHA and
            capture.get("diagnostic_status") == "CollectedSourceOnly" and
            capture.get("unsupported_values_preserved_verbatim") is True and
            capture.get("replay_ready") is False and capture.get("fresh_world_allocator_exported") is False and
            capture.get("full_mission_initialized") is False and capture.get("utility_world_created") is True and
            capture.get("utility_world_disposed") is True and capture.get("mission_world_created") is False and
            capture.get("native_visual_readiness") is False and capture.get("source_fit_certified") is False and
            capture.get("native_context_qualified") is False and capture.get("mission_ready") is False and
            capture.get("geometry_exported") is False,
            "Exact S003 source-only diagnostic scope and false readiness fields required")
    require(re.fullmatch(r"[0-9a-f]{32}", capture.get("template_namespace_id", "")) is not None,
            "One valid captured template namespace required")
    roots = capture.get("opening_prefabs")
    entities = capture.get("entities")
    require(isinstance(roots, dict) and list(roots.keys()) == expected_keys and isinstance(entities, list) and entities,
            "Exact ordered 19-root map and nonempty component closure required")
    entity_ids = [row.get("id") if isinstance(row, dict) else None for row in entities]
    require(len(set(entity_ids)) == len(entity_ids) and all(_entity_id(value) for value in entity_ids),
            "Unique canonical entity IDs required")
    by_id = {row["id"]: row for row in entities}
    root_ids = list(roots.values())
    require(all(_entity_id(value) and value in by_id for value in root_ids) and len(set(root_ids)) == UNIT_COUNT,
            "Exact 19 unique root Entity references required")
    for row in entities:
        require(isinstance(row.get("entity_enabled"), bool) and isinstance(row.get("components"), dict),
                "Entity enabled flag and component map required")
        for component, data in row["components"].items():
            require(isinstance(component, str) and isinstance(data, dict) and data.get("present") is True and
                    isinstance(data.get("buffer"), bool) and "value" in data and
                    (data.get("enabled") is None or isinstance(data.get("enabled"), bool)),
                    "Typed component closure row malformed")
    dangling = []
    for row in entities:
        for ref in _entity_refs(row["components"]):
            if not _entity_id(ref) or (ref != "0:0" and ref not in by_id):
                dangling.append(ref)
    require(not dangling, "Template closure contains a dangling entity reference")
    unsupported = capture.get("unsupported_values")
    classified = classify_unsupported(unsupported, entities)
    field_counts = {}
    for row in unsupported:
        parts = row["path"].split("/")
        field = parts[1] + "/" + parts[2]
        field_counts[field] = field_counts.get(field, 0) + 1
    return {"entity_count": len(entities), "root_count": len(roots), "unsupported_count": len(unsupported),
            "unsupported_classifications": classified,
            "presentation_reference_count": sum(item["classification"] == "unresolved_presentation_object_reference"
                                                   for item in classified),
            "unclassified_unsupported_count": sum(item["classification"] == "unclassified_unsupported_consumer_blocker"
                                                    for item in classified),
            "null_entity_reference_count": sum(ref == "0:0" for row in entities
                                               for ref in _entity_refs(row["components"])),
            "unsupported_field_counts": dict(sorted(field_counts.items())),
            "component_type_count": len({name for row in entities for name in row["components"]})}


def verify_pins(capture, repo, minimum_count):
    pins = capture.get("source_pins")
    require(isinstance(pins, dict) and len(pins) >= minimum_count, "Complete current source pin map required")
    seen = {}
    for raw_path, expected in pins.items():
        lexical = Path(raw_path)
        path = lexical if lexical.is_absolute() else repo / lexical
        resolved = path.resolve(strict=True)
        require(resolved.is_relative_to(repo), "Source pin escapes repository: " + raw_path)
        require(len(expected) == 64 and all(c in "0123456789abcdef" for c in expected),
                "Canonical SHA256 required for source pin: " + raw_path)
        if resolved in seen:
            require(seen[resolved] == expected, "Duplicate source pin aliases disagree: " + raw_path)
        seen[resolved] = expected
        require(sha_bytes(resolved.read_bytes()) == expected, "Current source pin drift: " + raw_path)
    return len(pins), seen


def validate(repo_path, capture_path, log_path, wrapper_exit):
    repo = Path(repo_path).resolve(strict=True)
    capture_path = Path(capture_path).resolve(strict=True)
    log_path = Path(log_path).resolve(strict=True)
    validate_wrapper_exit(wrapper_exit)
    require(capture_path == (repo / CAPTURE_REL).resolve(), "Exact designated S003 capture path required")
    require(log_path == (repo / LOG_REL).resolve(), "Exact designated S003 Editor log path required")
    paths, input_raw, expected_keys, descriptor, metadata, metadata_receipt = validate_input_bundle(repo)
    capture_raw = capture_path.read_bytes()
    capture = json.loads(capture_raw.decode("utf-8", errors="strict"), object_pairs_hook=unique_object)
    audit = audit_document(capture, expected_keys)
    if capture.get("requested_prefab_keys_sha256") != KEYS_SHA:
        raise ValueError("Exact key descriptor hash binding required")
    capture_pins = metadata.get("source_pins", {})
    raw_pins = capture.get("source_pins", {})
    normalized_raw_pins = {}
    for raw_path, expected in raw_pins.items():
        candidate = Path(raw_path)
        candidate = candidate if candidate.is_absolute() else repo / candidate
        normalized_raw_pins[candidate.resolve(strict=True)] = expected
    for raw_path, expected in capture_pins.items():
        candidate = Path(raw_path)
        candidate = candidate if candidate.is_absolute() else repo / candidate
        require(normalized_raw_pins.get(candidate.resolve(strict=True)) == expected,
                "Focused capture omits or alters native capture source pin: " + raw_path)
    for name, input_path in paths.items():
        require(normalized_raw_pins.get(input_path.resolve(strict=True)) == sha_bytes(input_raw[name]),
                "Raw artifact does not pin exact input file: " + name)
    pin_count, normalized_pins = verify_pins(capture, repo, len(capture_pins))
    log_raw = log_path.read_bytes()
    log = log_raw.decode("utf-8", errors="strict")
    validate_wrapper_binding(log, capture_path, paths.values(), audit)
    require(capture_path.read_bytes() == capture_raw and log_path.read_bytes() == log_raw,
            "Capture/log changed during finalization")
    for name, input_path in paths.items():
        require(input_path.read_bytes() == input_raw[name], "Pinned S003 input changed during finalization: " + name)
    for path, expected in normalized_pins.items():
        require(sha_bytes(path.read_bytes()) == expected, "Source pin changed during finalization: " + str(path))
    require(metadata_receipt.get("utility_world_created") is True and
            metadata_receipt.get("utility_world_disposed") is True,
            "Original compiler utility World must be honestly disclosed")
    return {
        "schema_version": 1,
        "kind": "S003FocusedPrefabCaptureReceipt",
        "validation_status": "PassedDiagnosticSourceOnly",
        "wrapper_exit_code": 0,
        "required_marker_count": 1,
        "capture_path": str(capture_path),
        "capture_sha256": sha_bytes(capture_raw),
        "editor_log_path": str(log_path),
        "editor_log_sha256": sha_bytes(log_raw),
        "key_descriptor_sha256": KEYS_SHA,
        "key_descriptor_receipt_sha256": DESCRIPTOR_RECEIPT_SHA,
        "native_metadata_sha256": METADATA_SHA,
        "native_metadata_receipt_sha256": METADATA_RECEIPT_SHA,
        "graph_sha256": GRAPH_SHA,
        "run6_compiler_receipt_sha256": RUN6_RECEIPT_SHA,
        "run6_post_run_audit_sha256": RUN6_AUDIT_SHA,
        "run6_graph_sha256": RUN6_GRAPH_SHA,
        "seed": 104732,
        "setup_hash": SETUP_HASH,
        "source_pin_count": pin_count,
        **audit,
        "utility_world_created": True,
        "utility_world_disposed": True,
        "mission_world_created": False,
        "unsupported_values_preserved_verbatim": True,
        "replay_ready": False,
        "native_context_qualified": False,
        "source_fit_certified": False,
        "mission_ready": False,
        "scope": "19-root original source/baker template closure diagnostic only; unresolved presentation references are classified without resolution and every unsupported value blocks any consumer that requires it; no replay, mission-world, source-fit, or readiness authority.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--capture", required=True, type=Path)
    parser.add_argument("--capture-log", required=True, type=Path)
    parser.add_argument("--capture-wrapper-exit", required=True, type=int)
    parser.add_argument("--receipt", required=True, type=Path)
    args = parser.parse_args()
    receipt_path = args.receipt.resolve()
    require(not receipt_path.exists(), "Fresh output receipt path required")
    receipt = validate(args.repo, args.capture, args.capture_log, args.capture_wrapper_exit)
    receipt_path.parent.mkdir(parents=True, exist_ok=True)
    receipt_path.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(json.dumps(receipt, indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
