#!/usr/bin/env python3
"""Focused negative controls for the S004 prepared mesh-bounds finalizer."""
from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("finalize_s004_prepared_mesh_bounds_capture.py")
spec = importlib.util.spec_from_file_location("s004_prepared_mesh_finalizer", MODULE_PATH)
finalizer = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(finalizer)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class PreparedMeshFinalizerControls(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.inventory_path = self.root / "inventory.json"
        self.raw_path = self.root / "mesh-bounds.json"
        self.log_path = self.root / "editor.log"
        self.exporter = self.root / finalizer.EXPORTER
        self.exporter.parent.mkdir(parents=True)
        self.exporter.write_text("capture exporter fixture")
        mesh_guid, prefab_guid = "a" * 32, "b" * 32
        mesh = self.root / "Assets/Mesh.asset"
        mesh.parent.mkdir(parents=True, exist_ok=True)
        mesh.write_text("fixture mesh")
        paths = {
            "scene": self.root / "Assets/PreparedEntities.unity",
            "binding": self.root / "Assets/RuntimeBinding.unity",
            "map": self.root / "Assets/Map.asset",
            "meta": self.root / "Assets/Mesh.asset.meta",
        }
        for path in paths.values():
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("fixture " + path.name)
        pins = {str(path): {"status": "pinned", "sha256": sha(path), "size_bytes": path.stat().st_size}
                for path in paths.values()}
        pins[str(mesh)] = {"status": "pinned", "sha256": sha(mesh), "size_bytes": mesh.stat().st_size}
        self.inventory = {
            "kind": "S004PreparedEntitiesVisualDependencyInventory", "status": "PassedInventoryOnly",
            "source_pins_before_after": "passed", "asset_and_meta_pins": pins,
            "input_scene": {"path": str(paths["scene"]), "sha256": sha(paths["scene"])},
            "source_map_binding_context": {
                "runtime_binding_scene": str(paths["binding"]), "runtime_binding_sha256": sha(paths["binding"]),
                "operation_map_asset": str(paths["map"]), "operation_map_sha256": sha(paths["map"])},
            "prefab_inventory": {prefab_guid: {"source_local_roots": [{"mesh_filters": [
                {"mesh": {"guid": mesh_guid, "file_id": "4300000"}}]}]}}
        }
        self.inventory_path.write_text(json.dumps(self.inventory))
        self.raw = {
            "kind": "S004PreparedVisualMeshBoundsCapture", "schema_version": 1, "status": "Passed",
            "capture_mode": "AssetDatabaseExactGuidLocalId_NoSceneLoad_NoGameObjects_NoSimulation",
            "input_inventory_path": str(self.inventory_path.resolve()),
            "input_inventory_sha256": sha(self.inventory_path), "unique_mesh_count": 1,
            "meshes": [{"guid": mesh_guid, "local_file_id": 4300000, "asset_path": str(mesh),
                        "asset_dependency_hash": "dependency-hash", "bounds_center": [0, 0, 0],
                        "bounds_size": [1, 2, 3]}]}
        self.raw_path.write_text(json.dumps(self.raw))
        self.log_path.write_text("COMMAND LINE ARGUMENTS:\n-executeMethod\n" + finalizer.METHOD +
                                 "\n" + str(self.root.resolve()) +
                                 "\nBatch mode:              NO\n" + finalizer.MARKER + " mesh_count=1 output=x\n")

    def tearDown(self):
        self.temp.cleanup()

    def verify(self, raw=None, inventory=None, exit_code=0):
        raw_file = self.raw_path
        inv_file = self.inventory_path
        if raw is not None:
            raw_file = self.root / "mutated-raw.json"
            raw_file.write_text(json.dumps(raw))
        if inventory is not None:
            inv_file = self.root / "mutated-inventory.json"
            inv_file.write_text(json.dumps(inventory))
            if raw is None:
                changed = copy.deepcopy(self.raw)
                changed["input_inventory_path"] = str(inv_file.resolve())
                changed["input_inventory_sha256"] = sha(inv_file)
                raw_file = self.root / "bound-mutated-raw.json"
                raw_file.write_text(json.dumps(changed))
        return finalizer.verify(self.log_path, raw_file, inv_file, self.root, exit_code)

    def test_accepts_exact_finite_inventory_mesh_coverage(self):
        receipt = self.verify()
        self.assertTrue(receipt["exactInventoryIdentityCoverage"])
        self.assertEqual(receipt["uniqueMeshCount"], 1)
        self.assertFalse(receipt["visualReadiness"])

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaisesRegex(finalizer.FinalizeError, "exactly zero"):
            self.verify(exit_code=1)

    def test_missing_mesh_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["meshes"] = []
        raw["unique_mesh_count"] = 0
        with self.assertRaisesRegex(finalizer.FinalizeError, "exact unique inventory"):
            self.verify(raw=raw)

    def test_duplicate_mesh_identity_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["meshes"].append(copy.deepcopy(raw["meshes"][0]))
        raw["unique_mesh_count"] = 2
        with self.assertRaisesRegex(finalizer.FinalizeError, "exact unique inventory"):
            self.verify(raw=raw)

    def test_nonfinite_bounds_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["meshes"][0]["bounds_center"][1] = float("nan")
        with self.assertRaisesRegex(finalizer.FinalizeError, "nonfinite"):
            self.verify(raw=raw)

    def test_stale_source_pin_rejected(self):
        inv = copy.deepcopy(self.inventory)
        inv["asset_and_meta_pins"][next(iter(inv["asset_and_meta_pins"]))]["sha256"] = "0" * 64
        with self.assertRaisesRegex(finalizer.FinalizeError, "source pin differs"):
            self.verify(inventory=inv)

    def test_wrong_capture_method_rejected(self):
        self.log_path.write_text("-executeMethod\nGame.Editor.Wrong\n" + str(self.root.resolve()) + "\n" +
                                 finalizer.MARKER + " mesh_count=1 output=x\n")
        with self.assertRaisesRegex(finalizer.FinalizeError, "method/project/mode"):
            self.verify()


if __name__ == "__main__":
    unittest.main()
