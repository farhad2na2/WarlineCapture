#!/usr/bin/env python3
"""Read-only source inventory for the pinned S004 Standard compiled opening forces."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import tempfile

HERE = Path(__file__).resolve().parent
BASE = HERE / "inventory_s004_source_graph.py"
spec = importlib.util.spec_from_file_location("s004_source_inventory", BASE)
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def need(ok, message):
    if not ok: raise ValueError(message)

def find_guid(assets, guid, cache):
    if guid not in cache:
        proc = subprocess.run(["rg", "-l", "--glob", "*.meta", f"^guid: {guid}$", str(assets)],
                              capture_output=True, text=True, timeout=120)
        need(proc.returncode in (0, 1), "GUID lookup failed: " + proc.stderr.strip())
        found = []
        for raw_path in sorted(proc.stdout.splitlines()):
            meta = Path(raw_path); data = meta.read_bytes()
            need(len(data) <= base.MAX_FILE and re.findall(rb"^guid: ([0-9a-f]{32})$", data, re.M) == [guid.encode()],
                 "non-exact GUID meta returned: " + raw_path)
            found.append(str(meta.relative_to(assets.parent)))
        cache[guid] = found
    return cache[guid]

def config_meta_guid(meta_path):
    """Return the one exact Unity GUID declared by a source config's .meta."""
    need(meta_path.is_file() and not meta_path.is_symlink(), "source config .meta missing or symlink: " + str(meta_path))
    raw = meta_path.read_bytes()
    need(0 < len(raw) <= base.MAX_FILE, "source config .meta size unsupported: " + str(meta_path))
    matches = re.findall(rb"^guid: ([0-9a-f]{32})$", raw, re.M)
    need(len(matches) == 1, "source config .meta must declare exactly one lowercase GUID: " + str(meta_path))
    return matches[0].decode("ascii")

def validate_config_meta_binding(meta_path, expected_guid, expected_sha256):
    actual_guid = config_meta_guid(meta_path)
    need(actual_guid == expected_guid, "source config GUID changed: " + str(meta_path))
    need(sha(meta_path) == expected_sha256, "source config .meta hash changed: " + str(meta_path))
    return actual_guid

def controls():
    rows = []
    with tempfile.TemporaryDirectory(prefix="s004-opening-prefab-controls-") as temp:
        root = Path(temp)
        prefab = root / "sample.prefab"
        prefab.write_bytes(b"%YAML 1.1\n--- !u!1 &101\nGameObject: {}\n")
        need(base.validate_file_id(prefab, 101)[0] == "proven", "matching prefab anchor control failed")
        rows.append({"case": "matching-prefab-anchor", "result": "ProvenAsExpected"})
        need(base.validate_file_id(prefab, 202)[0] == "unresolved", "unknown prefab local ID control failed")
        rows.append({"case": "unknown-prefab-local-id", "result": "UnresolvedAsExpected"})
        unsupported = root / "sample.fbx"; unsupported.write_bytes(b"binary")
        need(base.validate_file_id(unsupported, 101)[0] == "unresolved", "unsupported target control failed")
        rows.append({"case": "unsupported-prefab-target-format", "result": "UnresolvedAsExpected"})
        meta = root / "sample.asset.meta"
        meta.write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\n")
        pinned_hash = sha(meta)
        need(validate_config_meta_binding(meta, "a" * 32, pinned_hash) == "a" * 32, "exact config meta GUID control failed")
        rows.append({"case": "exact-config-meta-guid", "result": "AcceptedAsExpected"})
        try: validate_config_meta_binding(meta, "b" * 32, pinned_hash)
        except ValueError: rows.append({"case": "mutated-config-meta-guid", "result": "RejectedAsExpected"})
        else: raise ValueError("wrong expected config meta GUID control unexpectedly accepted")
        meta.write_text("fileFormatVersion: 2\nguid: " + "b" * 32 + "\n")
        try: validate_config_meta_binding(meta, "a" * 32, pinned_hash)
        except ValueError: rows.append({"case": "changed-config-meta-bytes", "result": "RejectedAsExpected"})
        else: raise ValueError("changed config meta bytes control unexpectedly accepted")
        meta.write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\nguid: " + "b" * 32 + "\n")
        try: config_meta_guid(meta)
        except ValueError: rows.append({"case": "duplicate-config-meta-guid", "result": "RejectedAsExpected"})
        else: raise ValueError("duplicate config meta GUID control unexpectedly accepted")
    return rows

def run(repo: Path, export: Path, output: Path, prior_inventory: Path):
    repo, export, output = repo.resolve(), export.resolve(), output.absolute()
    need(not output.exists(), "fresh output directory required")
    receipt_path, setup_path = export / "receipt.json", export / "compiled-setup.json"
    prior_inventory = prior_inventory.resolve()
    need(prior_inventory.is_file() and not prior_inventory.is_symlink(), "historical inventory missing or symlink")
    prior = json.loads(prior_inventory.read_text())
    need(prior.get("kind") == "S004StandardOpeningForcePrefabSourceInventory", "unsupported historical inventory")
    prior_setup_sha = prior.get("source_export", {}).get("compiled_setup_sha256")
    receipt = json.loads(receipt_path.read_text())
    need(receipt.get("kind") == "S004StandardSetupSourceExportReceipt" and receipt.get("status") == "Passed",
         "pinned passed S004 source export receipt required")
    need(receipt.get("compiled_setup", {}).get("path") == str(setup_path), "receipt compiled setup path mismatch")
    need(sha(setup_path) == receipt["compiled_setup"].get("sha256"), "compiled setup pin mismatch")
    need(prior_setup_sha == sha(setup_path), "historical inventory does not bind this exact compiled setup")
    setup = json.loads(setup_path.read_text())
    need(setup.get("kind") == "S004StandardCompiledSetupSourceExport" and setup.get("status") == "Passed",
         "supported compiled setup required")
    need(setup.get("identity", {}).get("size_id") == "Standard", "unsupported setup size")
    compiled = setup.get("compiled_setup")
    need(isinstance(compiled, dict) and setup.get("identity", {}).get("size_id") == "Standard", "unsupported compiled setup payload")
    forces = compiled.get("Forces")
    need(isinstance(forces, list) and 0 < len(forces) <= 128, "missing/oversized force list")
    roots, pins, unresolved, cache = [], {str(receipt_path): sha(receipt_path), str(setup_path): sha(setup_path),
                                        str(prior_inventory): sha(prior_inventory)}, [], {}
    reader = base.READER
    pins[str(reader)] = sha(reader)
    for i, force in enumerate(forces):
        need(isinstance(force, dict), f"unsupported force row {i}")
        path_value = force.get("SourceConfigPath")
        need(isinstance(path_value, str) and path_value.startswith("Assets/"), f"missing/unsupported SourceConfigPath at force {i}")
        source = (repo / path_value).resolve()
        need(source.is_relative_to(repo / "Assets") and source.is_file() and not source.is_symlink(), f"unsafe/missing source config: {path_value}")
        raw = source.read_bytes(); need(0 < len(raw) <= base.MAX_FILE, f"source config size unsupported: {path_value}")
        pins[str(source)] = sha(source)
        source_meta = Path(str(source) + ".meta")
        source_guid = config_meta_guid(source_meta)
        source_meta_hash = sha(source_meta)
        validate_config_meta_binding(source_meta, source_guid, source_meta_hash)
        config_guid_paths = find_guid(repo / "Assets", source_guid, cache)
        need(config_guid_paths == [str(source_meta.relative_to(repo))],
             f"source config GUID missing, ambiguous, or points elsewhere: {path_value}")
        pins[str(source_meta)] = sha(source_meta)
        doc = base.parse(reader, [source]).get(str(source))
        need(isinstance(doc, dict) and isinstance(doc.get("MonoBehaviour"), dict), f"unsupported config YAML: {path_value}")
        refs = base.refs(doc["MonoBehaviour"], "MonoBehaviour")
        # The config's actual prefab slots are serialized fields ending in Prefab.
        prefab_refs = [r for r in refs if re.search(r"Prefab(?:\]|$)", r["field"], re.I)]
        resolved = []
        for ref in refs:
            is_prefab = re.search(r"Prefab(?:\]|$)", ref["field"], re.I) is not None
            if not is_prefab:
                resolved.append({**ref, "resolution_scope": "serialized-direct-reference-only",
                                 "fileID_validation": "not_applicable"})
                continue
            hits = find_guid(repo / "Assets", ref["guid"], cache)
            need(len(hits) == 1, f"prefab GUID missing or ambiguous in {path_value}: {ref['guid']}")
            meta = repo / hits[0]; pins[str(meta)] = sha(meta)
            target = base.target_for_meta(repo / "Assets", hits[0]); traw = target.read_bytes()
            need(0 < len(traw) <= base.MAX_FILE, f"prefab target unsupported size: {target}")
            pins[str(target)] = sha(target)
            validation, reason = base.validate_file_id(target, ref["fileID"])
            need(target.suffix == ".prefab", f"prefab-named slot does not resolve to .prefab: {target}")
            row = {**ref, "target": str(target.relative_to(repo)), "target_sha256": pins[str(target)], "fileID_validation": validation,
                   "fileID_validation_reason": reason}
            if validation != "proven":
                unresolved.append({"source_config_path": path_value, "force_index": i, **row})
            resolved.append(row)
        prefab_set = {r["field"] for r in prefab_refs}
        prefab_rows = [r for r in resolved if r["field"] in prefab_set]
        need(len(prefab_rows) == len(prefab_refs), f"prefab reference resolution incomplete: {path_value}")
        roots.append({"force_index": i, "faction_id": force.get("FactionId"), "role_id": force.get("RoleId"),
                      "runtime_prefab_key": force.get("RuntimePrefabKey"), "source_config_path": path_value,
                      "source_config_guid": source_guid,
                      "source_config_meta_path": str(source_meta.relative_to(repo)),
                      "source_config_meta_sha256": source_meta_hash,
                      "quantity": force.get("Quantity"), "supply_cost": force.get("SupplyCost"),
                      "prefab_references": prefab_rows, "all_direct_references": resolved,
                      "prefab_reference_count": len(prefab_rows)})
    need(all(isinstance(x.get("quantity"), int) and x["quantity"] > 0 for x in roots), "invalid force quantity")
    # Verify the input pins again before writing; no Unity/runtime hydration is performed.
    for path, digest in pins.items(): need(Path(path).is_file() and sha(Path(path)) == digest, f"source drift during inventory: {path}")
    result = {"schema_version": 1, "kind": "S004StandardOpeningForcePrefabSourceInventory",
              "status": "InventoryComplete" if not unresolved else "InventoryPartialUnresolvedPrefabLocalIds",
              "source_export": {"receipt": str(receipt_path.relative_to(repo)), "receipt_sha256": pins[str(receipt_path)],
                                "compiled_setup": str(setup_path.relative_to(repo)), "compiled_setup_sha256": pins[str(setup_path)]},
              "historical_inventory": {"path": str(prior_inventory.relative_to(repo)), "sha256": sha(prior_inventory),
                                       "role": "provenance-only; current qualification is bound to source_export above"},
              "scope": "Ordered compiled Standard opening-force rows and direct serialized source-config references; no Unity, hydration, instantiation, or readiness claim.",
              "force_row_count": len(roots), "total_quantity": sum(x["quantity"] for x in roots),
              "unique_source_config_count": len({x["source_config_path"] for x in roots}), "forces": roots,
              "unresolved_prefab_local_ids": unresolved,
              "negative_controls": controls(),
              "missing_baked_runtime_data": ["instantiated runtime component state", "resolved runtime asset/object contents beyond serialized references", "runtime initialization and spawn results", "runtime production/action behavior", "physical source fit and mission readiness"],
              "source_pins": pins, "provenance": "Pinned compiled export SourceConfigPath values parsed with the existing strict selected_operation_map YAML reader; GUID and local fileID proof uses the existing checked-in source graph resolver."}
    output.mkdir(parents=True)
    target = output / "inventory.json"; target.write_text(json.dumps(result, indent=2) + "\n")
    return {"status": result["status"], "force_rows": len(roots), "quantity": result["total_quantity"], "output": str(target), "sha256": sha(target)}

def main():
    p = argparse.ArgumentParser(description=__doc__); p.add_argument("--repo", type=Path, required=True); p.add_argument("--export", type=Path, required=True); p.add_argument("--output", type=Path, required=True); p.add_argument("--prior-inventory", type=Path, required=True)
    a = p.parse_args()
    try: print(json.dumps(run(a.repo, a.export, a.output, a.prior_inventory), indent=2))
    except Exception as e: raise SystemExit("S004 opening force prefab inventory failed closed: " + str(e))
if __name__ == "__main__": main()
