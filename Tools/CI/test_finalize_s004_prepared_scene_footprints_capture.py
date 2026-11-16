#!/usr/bin/env python3
"""Focused negative controls for S004 authored scene footprint finalization."""
from __future__ import annotations

import copy
import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

MODULE_PATH = Path(__file__).with_name("finalize_s004_prepared_scene_footprints_capture.py")
spec = importlib.util.spec_from_file_location("s004_scene_footprint_finalizer", MODULE_PATH)
finalizer = importlib.util.module_from_spec(spec)
assert spec and spec.loader
spec.loader.exec_module(finalizer)


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class PreparedFootprintFinalizerControls(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve()
        self.capture = self.root / "Logs/SkirmishProduction/capture"
        self.data = self.capture / "data"
        self.data.mkdir(parents=True)
        self.log, self.raw_path = self.capture / "editor.log", self.data / finalizer.RAW_NAME
        self.inventory_path = self.root / "inventory.json"
        self.authors_path = self.root / "authorings.json"
        self.mesh_dir = self.root / "mesh-capture"
        self.mesh_dir.mkdir()
        exporter = self.root / finalizer.EXPORTER
        exporter.parent.mkdir(parents=True)
        exporter.write_text("CloseAndRestore(ref ownedScene, setup);\nWriteFreshDirectory(outputDir, report);\n"+
                            "EditorSceneManager.RestoreSceneManagerSetup(setup); VerifySetup(setup);\n"+
                            "ownedScene.isDirty; EditorSceneManager.CloseScene(ownedScene, true);\n")
        scene_path = self.root / finalizer.SCENE
        scene_path.parent.mkdir(parents=True, exist_ok=True)
        scene_path.write_text("prepared scene bytes")
        (scene_path.with_name(scene_path.name + ".meta")).write_text("guid: scene\n")
        mesh_asset = self.root / "Assets/Mesh.asset"
        mesh_asset.parent.mkdir(parents=True, exist_ok=True)
        mesh_asset.write_text("mesh asset bytes")
        mesh_meta = mesh_asset.with_name(mesh_asset.name + ".meta")
        mesh_meta.write_text("guid: " + "a" * 32 + "\n")
        binding = self.root / "Assets/RuntimeBinding.unity"
        map_asset = self.root / "Assets/Map.asset"
        binding.write_text("binding")
        map_asset.write_text("map")
        self.inventory = {"kind": "S004PreparedEntitiesVisualDependencyInventory", "status": "PassedInventoryOnly",
                          "input_scene": {"path": str(scene_path), "sha256": sha(scene_path)}}
        self.inventory_path.write_text(json.dumps(self.inventory))
        self.authors = []
        self.raw = {"kind": "S004PreparedSceneAuthoredVisualFootprints", "schema_version": 1,
                    "status": "PassedSourceTraversal",
                    "scope": "EditModeAdditiveSceneReadOnly_ExactProjectAuthoredFootprintOwnerAndBakerTraversal",
                    "scene_path": finalizer.SCENE, "scene_sha256": sha(scene_path),
                    "inventory_path": str(self.inventory_path), "inventory_sha256": sha(self.inventory_path),
                    "scene_loaded_additively": True, "scene_mutated_or_saved": False,
                    "source_pin_rehash_before_after": True, "gameplay_world_created": False,
                    "baker_executed": False, "scene_fit_claim": False, "mission_readiness": False,
                    "visual_readiness": False, "grid": {"guid": "c" * 32, "local_file_id": 1,
                        "origin": [0, 0, 0], "cell_size": 1.0, "width": 1200, "height": 500},
                    "building_count": 344, "buildings": [], "source_pins": {}}
        self.mesh_guid = "a" * 32
        for i in range(344):
            stable = "densecity." + str(i).zfill(3)
            self.authors.append({"placement_index": i, "stable_id": stable, "operation_map_id": finalizer.MAP_ID,
                                 "source_global_object_id": "", "faction_id": 0, "blocker_policy": "1",
                                 "origin_cell": {"x": str(i), "y": "0"},
                                 "serialized_generated_footprint_cells": {"x": "1", "y": "1"}})
            matrix = [1,0,0,0, 0,1,0,0, 0,0,1,0, float(i)+.5,.5,.5,1]
            meshrow = {"renderer_path": "Visual/Mesh", "mesh_name": "FixtureMesh", "mesh_asset_path": "Assets/Mesh.asset",
                       "mesh_guid": self.mesh_guid, "mesh_local_file_id": 4300000,
                       "mesh_local_bounds": {"center": [0,0,0], "size": [1,1,1]},
                       "renderer_local_to_world": matrix}
            self.raw["buildings"].append({"stable_id": stable, "operation_map_id": finalizer.MAP_ID,
                "placement_index": i, "source_global_object_id": "", "faction_id": 0, "blocker_policy": 1,
                "serialized_origin_cell": [i,0], "serialized_generated_footprint_cells": [1,1],
                "baker_input_footprint_cells": [1,1], "calculated_origin_cell": [i,0],
                "calculated_footprint_cells": [1,1], "world_visual_bounds": {
                    "min": [i,0,0], "max": [i+1,1,1], "center": [i+.5,.5,.5], "size": [1,1,1]},
                "mesh_renderer_inputs": [meshrow], "visual_traversal":
                "EnabledActiveInHierarchyMeshRenderer+MeshFilter.sharedMesh.bounds.TransformPoint(8Corners)",
                "baker_executed": False})
        self.authors_path.write_text(json.dumps(self.authors))
        pins = [scene_path, scene_path.with_name(scene_path.name + ".meta"), mesh_asset, mesh_meta, binding, map_asset,
                exporter, self.inventory_path, self.authors_path]
        self.raw["source_pins"] = {str(p): sha(p) for p in pins}
        self.raw_path.write_text(json.dumps(self.raw))
        mesh_raw = {"kind": "S004PreparedVisualMeshBoundsCapture", "status": "Passed",
                    "input_inventory_sha256": sha(self.inventory_path), "unique_mesh_count": 1,
                    "meshes": [{"guid": self.mesh_guid, "local_file_id": 4300000,
                        "asset_path": "Assets/Mesh.asset", "bounds_center": [0,0,0], "bounds_size": [1,1,1]}]}
        self.mesh_raw_path = self.mesh_dir / "mesh-bounds.json"
        self.mesh_raw_path.write_text(json.dumps(mesh_raw))
        self.mesh_log = self.mesh_dir / "editor.log"
        self.mesh_log.write_text("Batch mode:              NO\n-executeMethod\nGame.Editor.S004PreparedVisualMeshBoundsCapture.CaptureInventoryMeshBounds\n"
            "Exiting batchmode successfully now!\nBatchmode quit successfully invoked\n"
            "[S004PreparedVisualMeshBoundsCapture] result=Passed mesh_count=1 output=x\n")
        self.mesh_receipt = {"schema": "s004-prepared-mesh-bounds-capture-receipt-v1",
            "rawExportSha256": sha(self.mesh_raw_path), "captureLogSha256": sha(self.mesh_log),
            "inventorySha256": sha(self.inventory_path), "uniqueMeshCount": 1,
            "exactInventoryIdentityCoverage": True, "finiteBounds": True, "wrapper": {"exitCode": 0}}
        (self.mesh_dir / "capture-receipt.json").write_text(json.dumps(self.mesh_receipt))
        self.log.write_text("Batch mode:              NO\n-executeMethod\n" + finalizer.METHOD + "\n" +
            str(self.root) + "\nExiting batchmode successfully now!\nBatchmode quit successfully invoked\n"+
            finalizer.MARKER + " output=" + str(self.data) + "\n")

    def tearDown(self):
        self.temp.cleanup()

    def verify(self, raw=None, wrapper_exit=0, mesh_raw=None):
        raw_path = self.raw_path
        if raw is not None:
            raw_path = self.data / "mutated.json"
            raw_path.write_text(json.dumps(raw))
        if mesh_raw is not None:
            path = self.mesh_dir / "mesh-bounds.json"
            path.write_text(json.dumps(mesh_raw))
            receipt_path = self.mesh_dir / "capture-receipt.json"
            receipt = json.loads(receipt_path.read_text())
            receipt["rawExportSha256"] = sha(path)
            receipt_path.write_text(json.dumps(receipt))
        return finalizer.verify(self.log, raw_path, self.inventory_path, self.authors_path,
                                self.root, wrapper_exit, self.mesh_dir)

    def test_accepts_full_identity_and_bounds_provenance(self):
        receipt = self.verify()
        self.assertEqual(receipt["buildingCount"], 344)
        self.assertTrue(receipt["sceneSetupRestored"])
        self.assertTrue(receipt["meshBoundsProvenanceVerified"])
        self.assertFalse(receipt["bakerExecuted"])

    def test_nonzero_wrapper_exit_rejected(self):
        with self.assertRaisesRegex(finalizer.FinalizeError, "exactly zero"):
            self.verify(wrapper_exit=1)

    def test_duplicate_stable_id_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["buildings"][1]["stable_id"] = raw["buildings"][0]["stable_id"]
        with self.assertRaisesRegex(finalizer.FinalizeError, "duplicate stable ID"):
            self.verify(raw)

    def test_serialized_origin_mismatch_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["buildings"][0]["serialized_origin_cell"] = [99,0]
        with self.assertRaisesRegex(finalizer.FinalizeError, "serialized origin/footprint differs"):
            self.verify(raw)

    def test_incorrect_world_bounds_provenance_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["buildings"][0]["world_visual_bounds"]["max"][0] += .1
        with self.assertRaisesRegex(finalizer.FinalizeError, "world visual bounds"):
            self.verify(raw)

    def test_qualified_mesh_bounds_mismatch_rejected(self):
        mesh = json.loads(self.mesh_raw_path.read_text())
        mesh["meshes"][0]["bounds_size"] = [2,2,2]
        with self.assertRaisesRegex(finalizer.FinalizeError, "mesh local bounds differ"):
            self.verify(mesh_raw=mesh)

    def test_readiness_claim_rejected(self):
        raw = copy.deepcopy(self.raw)
        raw["visual_readiness"] = True
        with self.assertRaisesRegex(finalizer.FinalizeError, "forbidden claim"):
            self.verify(raw)

    def test_stale_source_pin_rejected(self):
        raw = copy.deepcopy(self.raw)
        path = next(iter(raw["source_pins"]))
        raw["source_pins"][path] = "0" * 64
        with self.assertRaisesRegex(finalizer.FinalizeError, "source pin differs"):
            self.verify(raw)


if __name__ == "__main__":
    unittest.main()
