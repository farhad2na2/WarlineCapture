#!/usr/bin/env python3
"""Verify narrow joins in an actual S004 source-only AirSelf transaction."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def find(value, key):
    result = []
    if isinstance(value, dict):
        for k, v in value.items():
            if k == key: result.append(v)
            result.extend(find(v, key))
    elif isinstance(value, list):
        for v in value: result.extend(find(v, key))
    return result


def verify(directory):
    receipt_path, requests_path = directory / "receipt.json", directory / "requests.json"
    receipt, requests = json.loads(receipt_path.read_bytes()), json.loads(requests_path.read_bytes())
    if receipt.get("requests_sha256") != sha(requests_path) or receipt.get("mission_ready") is not False or \
       receipt.get("physical_source_validated") is not False or receipt.get("runtime_admission") is not False or \
       receipt.get("airself_delivery_scope") != "SingleTransport_SourceDiagnostic_NoNativeOrRenderedParity":
        raise ValueError("Exact unqualified source diagnostic receipt and payload required")
    probes = find(requests, "source_semantic_aircraft_probe")
    ticks = [row for row in find(requests, "source_production_ticks") if isinstance(row, dict) and "delivery_events" in row]
    if len(probes) != 1 or len(ticks) != 1:
        raise ValueError("Exactly one source request and owner-tick record required")
    probe, tick = probes[0], ticks[0]
    if probe.get("scope") != "ExplicitSourceOnlySemanticCommand_NotAriaOrNativeInput" or probe.get("accepted") is not True:
        raise ValueError("Actual explicitly accepted source request required")
    before, paid, after = (probe[k] for k in ("source_transaction_before_request", "source_transaction_after_request", "source_transaction_after_owner_ticks"))
    if len({before["session_id"], paid["session_id"], after["session_id"]}) != 1:
        raise ValueError("Same-world session required")
    ident = probe["receipt_id"]
    if not isinstance(ident, int) or ident <= 0 or paid["receipt_id"] != ident or after["receipt_id"] != ident:
        raise ValueError("Exact actual receipt identity required")
    if len(paid["receipts"]) != 1 or len(after["receipts"]) != 1:
        raise ValueError("Exactly one matching reservation at both readbacks required")
    reserved, live = paid["receipts"][0], after["receipts"][0]
    if reserved["id"] != ident or live["id"] != ident or reserved["phase"] != "Reserved" or live["phase"] != "Live" or \
       reserved["member_count"] != 1 or reserved["delivered_members"] != 0 or live["delivered_members"] != 1 or \
       reserved["producer_id"] != live["producer_id"] or reserved["materials_paid"] != live["materials_paid"] or \
       reserved["supply_cost"] != live["supply_cost"]:
        raise ValueError("Actual reservation did not reach matching single-member live state")
    if before["materials"] - paid["materials"] != reserved["materials_paid"] or before["fuel"] != paid["fuel"] or \
       paid["air_reserved"] - before["air_reserved"] != 1 or paid["supply_reserved"] - before["supply_reserved"] != reserved["supply_cost"] or \
       after["air_reserved"] != before["air_reserved"] or after["supply_reserved"] != before["supply_reserved"] or \
       after["air_live"] - before["air_live"] != 1 or after["supply_live"] - before["supply_live"] != reserved["supply_cost"]:
        raise ValueError("Same-world payment/reservation/live deltas differ")
    if len(paid["producer_queue"]) != 1 or paid["producer_queue"][0]["receipt_id"] != ident or after["producer_queue"]:
        raise ValueError("Producer queue transition differs")
    if len(after["owned_members"]) != 1 or len(after["producer_readmodel"]) != 1 or after["airself_active_count"] != 0:
        raise ValueError("Single adopted member/readmodel and completed transport required")
    member, model = after["owned_members"][0], after["producer_readmodel"][0]
    if (member["entity_index"], member["entity_version"]) != (model["unit_index"], model["unit_version"]) or \
       member["source_key"] != model["source_key"] or model["producer_id"] != reserved["producer_id"]:
        raise ValueError("Adopted entity and producer readmodel differ")
    events = [e for e in tick["delivery_events"] if e.get("receipt_id") == ident]
    if [e["kind"] for e in events] != ["queue_accepted", "airself_launch", "unit_spawned_and_placed"]:
        raise ValueError("Exact observed event order required")
    launch, spawn = events[1:]
    if not launch["placement_observed"] or not spawn["placement_observed"] or \
       (launch["cell_x"], launch["cell_y"]) != (spawn["cell_x"], spawn["cell_y"]) or \
       (spawn["entity_index"], spawn["entity_version"]) != (member["entity_index"], member["entity_version"]):
        raise ValueError("Observed launch, spawn and adopted member do not join")
    return {"status": "PassedNarrowSourceTransaction", "input_sha256": {str(receipt_path): sha(receipt_path), str(requests_path): sha(requests_path)},
        "session_id": before["session_id"], "receipt_id": ident, "materials_paid": reserved["materials_paid"],
        "air_reserved_then_live": 1, "supply_reserved_then_live": reserved["supply_cost"],
        "member": [member["entity_index"], member["entity_version"]], "spawn_cell": [spawn["cell_x"], spawn["cell_y"]],
        "production_slot_index": model["slot_index"], "slot_assignment_verified": model["slot_index"] >= 0,
        "fuel_after_request": paid["fuel"], "fuel_after_ticks": after["fuel"], "later_fuel_change_attributed": False,
        "scope": "SingleReceipt_SourceOwnerSubset_NotFullMissionConservation", "native_parity": False, "mission_ready": False}


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("directory", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.directory)
    args.output.write_text(json.dumps(result, indent=2) + "\n")
    print("[S004AirSelfTransaction] result=Passed scope=SingleReceiptSourceDiagnostic")
