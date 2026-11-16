#!/usr/bin/env python3
"""Classify captured S073 storage/production components without importing them."""
import argparse
import hashlib
import json
from pathlib import Path


CAPTURE_SHA256 = "1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7"
CAPTURE_KIND = "S073IsolatedStreamedSourceWorldCapture"
SOURCE_PINS = {
    "Assets/Game/Scripts/Components/OperationMapBuildingComponents.cs": "752a6819258eeffa7d144e3672ffdbff2d0ca4b284abc6b9cee6c96438dab292",
    "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs": "b81c797aad5a0256fb331d772aa445c52d2337ce47617863f51b80f1011498d5",
    "Assets/Game/Scripts/Components/OperationMapEntityPresentationComponents.cs": "5f0c21d4d5882ec395aa5ddf28dfa981756832a82e7e1788baddba938bdb2e4c",
    "Assets/Game/Scripts/Authorings/OperationMapBuildingAuthoring.cs": "28ca030be8b358b4f31a943ee405d112b0ed009dc84308d107f5d44e56498c35",
    "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.cs": "713468bac94cf43de5c994664206448723e13db9d8fc41d31ae8a5e1b4c9a272",
    "Assets/Game/Scripts/Systems/BuildingProductionRequestSystemHelper.Scheduler.cs": "d2879a41f2c5801980c64c215b042033e1cec398dbf9ca0885a38e1f706c3868",
    "Assets/Game/Scripts/Systems/BuildingResourceStorageReservationSystemHelper.cs": "6f586e8a9f5334624a82a4518a94577097d22b143c7c13c09b72bc5924ba87b5",
    "Assets/Game/Scripts/Systems/BuildingRuntimeEntityCompositionSystemHelper.cs": "d07d42abbddeaba089bfee3061614a685ea08e66b4d16aa03fd572d63cf3002b",
}

STORAGE = "Game.Components.BuildingResourceStorageComponent"
QUEUE = "Game.Components.OperationMapBuildingProductionQueueComponent"
REQUESTS = "Game.Components.OperationMapBuildingUnitProductionRequest"
PRODUCTION_PREFABS = "Game.Components.OperationMapBuildingProductionPrefab"
STORAGE_AUTHORED = {
    "OilStorageCapacity", "FuelStorageCapacity", "OilBarrelsPerDay", "FuelBarrelsPerDay",
    "RuntimeBuildingId", "OwnerFactionId",
}
STORAGE_MUTABLE = {
    "StoredOilBarrels", "StoredFuelBarrels", "ReservedOilInboundBarrels",
    "ReservedOilOutboundBarrels", "ReservedFuelInboundBarrels",
    "ReservedFuelOutboundBarrels", "CivilianFuelReserveBarrels",
    "CivilianFuelReserveBarrels", "ReceivedHaulerFuelBarrels", "Version",
}
REQUEST_FIELDS = {
    "RequestId", "ProductionIndex", "UnitPrefab", "UnitSourceKey", "QueuedAt",
    "ReadyAt", "RemainingQuantity", "Status",
}


class AuditError(ValueError):
    pass


def require(ok, message):
    if not ok:
        raise AuditError(message)


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def field_map(component, expected_type):
    require(component.get("present") is True and component.get("buffer") is False,
            f"Expected scalar component {expected_type}")
    value = component.get("value")
    require(isinstance(value, dict) and value.get("codec") == "struct-fields-v1" and
            value.get("type") == expected_type and isinstance(value.get("fields"), dict),
            f"Expected exact struct-fields envelope for {expected_type}")
    result = {}
    for full_name, item in value["fields"].items():
        require(full_name.startswith(expected_type + "::"), f"Unqualified field in {expected_type}")
        name = full_name.split("::", 1)[1]
        require(name not in result, f"Duplicate field {expected_type}::{name}")
        result[name] = item
    return result


def integer(field, system_type):
    require(field.get("codec") == "integer-v1" and field.get("type") == system_type and
            isinstance(field.get("value"), str) and field["value"].lstrip("-").isdigit(),
            f"Expected {system_type} integer")
    return int(field["value"])


def scalar_bits(field):
    require(field.get("codec") == "f32-bits-v1" and isinstance(field.get("bits"), str) and
            len(field["bits"]) == 8 and all(c in "0123456789abcdef" for c in field["bits"]),
            "Expected exact f32 bit encoding")
    return field["bits"]


def audit(capture_path, receipt_path, repo_root):
    capture_path = Path(capture_path)
    receipt = json.loads(Path(receipt_path).read_text())
    require(sha256(capture_path) == CAPTURE_SHA256 == receipt.get("capture_sha256"),
            "Capture differs from retained S073 source receipt")
    require(receipt.get("kind") == "S073StreamedSourceCaptureReceipt" and
            receipt.get("wrapper_exit_code") == 0 and receipt.get("required_marker_present") is True and
            receipt.get("closure_complete") is False and receipt.get("mission_ready") is False,
            "Successful scoped pregame capture receipt required")
    repo_root = Path(repo_root)
    for relative, expected in SOURCE_PINS.items():
        path = repo_root / relative
        require(path.is_file(), f"Missing pinned source semantics: {relative}")
        actual = sha256(path)
        if expected:
            require(actual == expected, f"Reviewed source drift: {relative}")

    data = json.loads(capture_path.read_text())
    require(data.get("kind") == CAPTURE_KIND and data.get("scope") ==
            "IsolatedPreGameSource_NotLiveSessionOrReadiness" and
            data.get("scene_sha256") == receipt.get("scene_sha256") and
            data.get("closure_complete") is False and data.get("live_s073_session_observed") is False and
            data.get("physical_source_validated") is False and data.get("scheduler_admitted") is False and
            data.get("mission_ready") is False,
            "Exact unqualified pregame capture required")
    rows = data.get("entities")
    roots = data.get("root_entity_ids")
    require(isinstance(rows, list) and len(rows) == 17261 and isinstance(roots, list) and
            len(roots) == 4931 and len(set(roots)) == 4931, "Captured entity/root census changed")
    by_id = {row.get("id"): row for row in rows}
    require(len(by_id) == len(rows) and set(roots) <= set(by_id), "Unique captured source identities required")

    storage_count = queue_count = request_buffer_count = request_item_count = 0
    storage_field_count = {name: 0 for name in sorted(STORAGE_AUTHORED | STORAGE_MUTABLE)}
    queue_last_request_ids = {}
    request_buffer_lengths = {}
    production_prefab_lengths = {}
    storage_runtime_nonzero = {name: 0 for name in sorted(STORAGE_MUTABLE)}
    for entity_id in roots:
        components = by_id[entity_id].get("components", {})
        if STORAGE in components:
            storage_count += 1
            fields = field_map(components[STORAGE], STORAGE)
            require(set(fields) == STORAGE_AUTHORED | STORAGE_MUTABLE,
                    f"Unknown/missing storage field at {entity_id}")
            for name in STORAGE_AUTHORED:
                storage_field_count[name] += 1
            for name in STORAGE_MUTABLE:
                storage_field_count[name] += 1
                field = fields[name]
                if name == "Version":
                    value = integer(field, "System.UInt32")
                    if value != 1:
                        storage_runtime_nonzero[name] += 1
                else:
                    value = scalar_bits(field)
                    if value not in (0, "00000000"):
                        storage_runtime_nonzero[name] += 1
            # Authored storage inputs are represented by the exact source authoring fields;
            # owner/id are bake-time identity. Runtime ledger values remain mutable.
            for name in ("OilStorageCapacity", "FuelStorageCapacity"):
                integer(fields[name], "System.Int32")
            for name in ("RuntimeBuildingId",):
                require(integer(fields[name], "System.Int32") >= 0, "Invalid source runtime building id")
            integer(fields["OwnerFactionId"], "System.Byte")
            for name in ("OilBarrelsPerDay", "FuelBarrelsPerDay"):
                scalar_bits(fields[name])

        if QUEUE in components or REQUESTS in components:
            require(QUEUE in components and REQUESTS in components and PRODUCTION_PREFABS in components,
                    f"Authored production definition, queue and request buffer must be paired at {entity_id}")
            authored = components[PRODUCTION_PREFABS]
            require(authored.get("present") is True and authored.get("buffer") is True,
                    f"Authored production prefab buffer required at {entity_id}")
            authored_envelope = authored.get("value")
            require(isinstance(authored_envelope, dict) and authored_envelope.get("codec") == "template-buffer-values-v1" and
                    authored_envelope.get("element_type") == PRODUCTION_PREFABS and
                    isinstance(authored_envelope.get("items"), list) and authored_envelope["items"],
                    f"Nonempty authored production definitions required at {entity_id}")
            production_prefab_lengths[str(len(authored_envelope["items"]))] = production_prefab_lengths.get(
                str(len(authored_envelope["items"])), 0) + 1
            for authored_item in authored_envelope["items"]:
                require(isinstance(authored_item, dict) and authored_item.get("codec") == "struct-fields-v1" and
                        authored_item.get("type") == PRODUCTION_PREFABS and isinstance(authored_item.get("fields"), dict),
                        f"Production definition row shape changed at {entity_id}")
                authored_fields = authored_item["fields"]
                expected_authored_fields = {PRODUCTION_PREFABS + "::" + name for name in
                                            ("ProductionIndex", "Prefab", "SourceKey", "Quantity")}
                require(set(authored_fields) == expected_authored_fields,
                        f"Unknown/missing authored production field at {entity_id}")
                prefab = authored_fields[PRODUCTION_PREFABS + "::Prefab"]
                require(prefab.get("codec") == "entity-v1" and prefab.get("id") in by_id,
                        f"Authored production prefab reference is not bound inside capture at {entity_id}")
            queue_count += 1
            qfields = field_map(components[QUEUE], QUEUE)
            require(set(qfields) == {"LastRequestId"}, f"Unknown queue field at {entity_id}")
            last_id = integer(qfields["LastRequestId"], "System.Int32")
            queue_last_request_ids[str(last_id)] = queue_last_request_ids.get(str(last_id), 0) + 1
            request_component = components[REQUESTS]
            require(request_component.get("present") is True and request_component.get("buffer") is True,
                    f"Request buffer envelope required at {entity_id}")
            envelope = request_component.get("value")
            require(isinstance(envelope, dict) and envelope.get("codec") == "template-buffer-values-v1" and
                    envelope.get("element_type") == REQUESTS and isinstance(envelope.get("items"), list),
                    f"Request buffer encoding changed at {entity_id}")
            items = envelope["items"]
            request_buffer_count += 1
            request_item_count += len(items)
            request_buffer_lengths[str(len(items))] = request_buffer_lengths.get(str(len(items)), 0) + 1
            for item in items:
                require(isinstance(item, dict) and item.get("codec") == "struct-fields-v1" and
                        item.get("type") == REQUESTS and isinstance(item.get("fields"), dict),
                        f"Request item must have exact struct-fields encoding at {entity_id}")
                actual_names = {name.split("::", 1)[-1] for name in item["fields"]}
                require(all(name.startswith(REQUESTS + "::") for name in item["fields"]) and
                        actual_names == REQUEST_FIELDS, f"Unknown/missing request field at {entity_id}")
                raise AuditError(f"Captured mutable production request present at {entity_id}; reject state copy")

    require(storage_count == 43 and queue_count == request_buffer_count == 21,
            "Captured authority component census changed")
    require(request_item_count == 0 and queue_last_request_ids == {"0": 21} and
            request_buffer_lengths == {"0": 21}, "Captured production queue is no longer empty/default")
    return {
        "schema_version": 1,
        "kind": "S073CapturedCampaignComponentAuthorityAudit",
        "status": "Passed_ClassificationOnly_NoImport",
        "capture_sha256": CAPTURE_SHA256,
        "source_pins": SOURCE_PINS,
        "counts": {
            "captured_roots": len(roots),
            "storage_components": storage_count,
            "production_queue_components": queue_count,
            "paired_request_buffers": request_buffer_count,
            "serialized_production_requests": request_item_count,
            "storage_field_occurrences": storage_field_count,
            "storage_nondefault_mutable_fields": storage_runtime_nonzero,
            "queue_last_request_id_distribution": queue_last_request_ids,
            "request_buffer_length_distribution": request_buffer_lengths,
            "authored_production_prefab_length_distribution": production_prefab_lengths,
        },
        "decisions": {
            "storage_definition_or_bake_fields": sorted(STORAGE_AUTHORED),
            "storage_mutable_fields": sorted(STORAGE_MUTABLE),
            "storage_action": "DoNotCopyComponent; capacities/rates are definition-derived, identity is bake-bound, ledger is runtime-owned. Regenerate only from selected mission-owned definitions and fresh runtime owners.",
            "production_queue_action": "ProductionPrefab buffer is authoring-derived; captured queue ID=0/request buffer empty is default only. DoNotCopyCapturedQueueState; regenerate an empty queue only from a selected mission-owned production definition.",
            "serialized_requests_action": "RejectImport; any nonempty request is mutable queued Campaign state until separately authorized and provenance-qualified.",
            "unknown_or_changed_fields_action": "RejectBeforeImport",
        },
        "scope": "Pinned declarations, authoring/runtime writers and exact isolated pregame capture; no ECS allocation, no scene load, no closure or readiness claim.",
        "campaign_runtime_state_copied": False,
        "import_admitted": False,
        "mission_ready": False,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--capture", type=Path, required=True)
    parser.add_argument("--receipt", type=Path, required=True)
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = audit(args.capture, args.receipt, args.repo)
    with args.output.open("x") as stream:
        json.dump(result, stream, indent=2)
        stream.write("\n")
    print(json.dumps({"status": result["status"], "counts": result["counts"],
                      "decisions": result["decisions"]}, separators=(",", ":")))


if __name__ == "__main__":
    main()
