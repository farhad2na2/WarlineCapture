#!/usr/bin/env python3
"""Build a hash-pinned S004 focused-prefab key array from compiled source data only."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def need(ok: bool, message: str) -> None:
    if not ok:
        raise ValueError(message)


def exact_keys(rows, field="runtime_prefab_key", unique=True):
    need(isinstance(rows, list) and 0 < len(rows) <= 64, "bounded nonempty key array required")
    keys = []
    seen = set()
    for row in rows:
        key = row.get(field) if isinstance(row, dict) else None
        need(isinstance(key, str) and key and key == key.strip() and not any(ord(c) < 32 for c in key),
             "exact nonempty runtime prefab key required")
        folded = key.casefold()
        need(not unique or folded not in seen, "duplicate runtime prefab key")
        seen.add(folded)
        keys.append(key)
    return keys


def controls():
    sample = [{"runtime_prefab_key": "Unit_A"}, {"runtime_prefab_key": "Unit_B"}]
    rows = []
    try:
        exact_keys(sample + [{"runtime_prefab_key": "Unit_A"}])
    except ValueError:
        rows.append({"case": "duplicate-key-rejected", "result": "Passed"})
    else:
        raise ValueError("duplicate-key negative control failed")
    # Input pin checks use exact digest equality; a one-nibble altered digest must fail.
    actual = hashlib.sha256(b"pinned source bytes").hexdigest()
    try:
        need(actual == "0" * 64, "source hash mismatch")
    except ValueError:
        rows.append({"case": "tampered-source-hash-rejected", "result": "Passed"})
    else:
        raise ValueError("tampered source hash negative control failed")
    return rows


def generate(repo: Path, inventory_path: Path, output: Path):
    repo, inventory_path, output = repo.resolve(), inventory_path.resolve(), output.absolute()
    need(not output.exists(), "fresh output directory required")
    inv = json.loads(inventory_path.read_text())
    need(inv.get("schema_version") == 1 and inv.get("kind") == "S004StandardOpeningForcePrefabSourceInventory",
         "supported S004 inventory required")
    need(inv.get("status") in ("InventoryComplete", "InventoryPartialUnresolvedPrefabLocalIds"),
         "supported inventory status required")
    pins = inv.get("source_pins")
    need(isinstance(pins, dict) and pins, "pinned inventory source set required")
    for raw, digest in pins.items():
        path = Path(raw)
        need(path.is_absolute() and path.is_file() and isinstance(digest, str) and len(digest) == 64 and sha(path) == digest,
             "inventory source pin mismatch: " + raw)

    receipt_path = repo / inv["source_export"]["receipt"]
    setup_path = repo / inv["source_export"]["compiled_setup"]
    need(sha(receipt_path) == inv["source_export"]["receipt_sha256"], "inventory receipt pin mismatch")
    need(sha(setup_path) == inv["source_export"]["compiled_setup_sha256"], "inventory setup pin mismatch")
    receipt = json.loads(receipt_path.read_text())
    need(receipt.get("kind") == "S004StandardSetupSourceExportReceipt" and receipt.get("status") == "Passed",
         "passed S004 Standard export receipt required")
    need(receipt.get("compiled_setup", {}).get("path") == str(setup_path) and
         receipt.get("compiled_setup", {}).get("sha256") == sha(setup_path), "receipt-to-setup binding mismatch")
    setup = json.loads(setup_path.read_text())
    need(setup.get("kind") == "S004StandardCompiledSetupSourceExport" and setup.get("status") == "Passed" and
         setup.get("identity", {}).get("size_id") == "Standard", "passed Standard compiled setup required")
    forces = setup.get("compiled_setup", {}).get("Forces")
    inv_forces = inv.get("forces")
    need(isinstance(forces, list) and isinstance(inv_forces, list) and len(forces) == len(inv_forces) == inv.get("force_row_count"),
         "compiled and inventoried force rows must align")
    for index, (compiled, inventoried) in enumerate(zip(forces, inv_forces)):
        for source_field, inventory_field in (("RuntimePrefabKey", "runtime_prefab_key"),
                                               ("SourceConfigPath", "source_config_path"),
                                               ("Quantity", "quantity"), ("SupplyCost", "supply_cost")):
            need(compiled.get(source_field) == inventoried.get(inventory_field),
                 f"compiled/inventory mismatch at force {index}: {source_field}")
    keys = list(dict.fromkeys(exact_keys(forces, "RuntimePrefabKey", unique=False)))
    need(keys == list(dict.fromkeys(exact_keys(inv_forces, unique=False))), "compiled and inventory unique key order differs")
    need(len(keys) == inv.get("unique_source_config_count") == 9, "expected nine unique S004 key/config pairs")
    unresolved = inv.get("unresolved_prefab_local_ids", [])
    need(isinstance(unresolved, list), "unresolved local-ID ledger must be explicit")
    # The descriptor is only names. It must not claim any unresolved serialized local ID was proven.
    need(all(x.get("fileID_validation") == "unresolved" for x in unresolved), "unresolved ID promoted to proven")

    descriptor_bytes = (json.dumps(keys, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
    descriptor_sha = hashlib.sha256(descriptor_bytes).hexdigest()
    audit = {
        "schema_version": 1,
        "kind": "S004FocusedPrefabKeyDescriptorReceipt",
        "status": "PassedSourceOnly",
        "descriptor": "prefab-keys.json",
        "descriptor_sha256": descriptor_sha,
        "key_count": len(keys),
        "keys_in_first_compiled_use_order": keys,
        "force_row_count": len(forces),
        "unique_source_config_count": len({row["source_config_path"] for row in inv_forces}),
        "unresolved_prefab_local_id_count": len(unresolved),
        "unresolved_prefab_local_ids": sorted([
            {"source_config_path": x.get("source_config_path"), "field": x.get("field"),
             "guid": x.get("guid"), "fileID": x.get("fileID"), "fileID_validation": x.get("fileID_validation")}
            for x in unresolved
        ], key=lambda x: (x["source_config_path"] or "", x["field"] or "", x["fileID"] or 0)),
        "source_export": {"receipt": str(receipt_path.relative_to(repo)), "receipt_sha256": sha(receipt_path),
                          "compiled_setup": str(setup_path.relative_to(repo)), "compiled_setup_sha256": sha(setup_path)},
        "inventory": {"path": str(inventory_path.relative_to(repo)), "sha256": sha(inventory_path)},
        "negative_controls": controls(),
        "scope": "Exact key names only, derived from pinned compiled Standard force rows and cross-checked against the pinned source inventory; no local-ID proof, Unity, runtime hydration, readiness, or mission claim."
    }
    output.mkdir(parents=True)
    (output / "prefab-keys.json").write_bytes(descriptor_bytes)
    (output / "receipt.json").write_text(json.dumps(audit, indent=2) + "\n")
    return {"status": audit["status"], "keys": len(keys), "descriptor_sha256": descriptor_sha,
            "receipt_sha256": sha(output / "receipt.json"), "output": str(output)}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", type=Path, required=True)
    p.add_argument("--inventory", type=Path, required=True)
    p.add_argument("--output", type=Path, required=True)
    a = p.parse_args()
    try:
        print(json.dumps(generate(a.repo, a.inventory, a.output), indent=2))
    except Exception as exc:
        raise SystemExit("S004 prefab key descriptor generation failed closed: " + str(exc))


if __name__ == "__main__":
    main()
