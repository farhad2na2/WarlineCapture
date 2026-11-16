import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

MODULE = Path(__file__).with_name("finalize_s073_catalog_metadata.py")
spec = importlib.util.spec_from_file_location("catalog_receipt", MODULE)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CatalogReceiptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.out = self.root / "out"
        self.out.mkdir()
        marker_path = (self.out / "catalog.json").resolve()
        (self.out / "editor.log").write_text(
            "COMMAND LINE ARGUMENTS:\n"
            "Game.Editor.S073ConfiguredCatalogMetadataExport.RunS073ConfiguredCatalogMetadataExport\n"
            f"[S073CatalogMetadataExport] result=Passed entries=6 footprints=6 path={marker_path}\n")
        names = ["Building_Barrack", "Building_GuardTower", "Building_Ammunition_Depot",
                 "Building_OilPump", "Building_Refinery", "Building_Fuel_Bladder"]
        rows, pins = [], {}
        for i, name in enumerate(names):
            asset = "Assets/" + name + ".prefab"
            for path in (asset, asset + ".meta"):
                target = self.root / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(path)
                pins[path] = hashlib.sha256(target.read_bytes()).hexdigest()
            rows.append({"prefab_name": name, "asset_path": asset, "guid": str(i) * 32,
                         "local_file_id": i + 1, "effective_can_request": True,
                         "effective_price": 10, "effective_materials_cost": 5})
            rows[-1]["effective_footprint_cells"] = {"x": i + 1, "y": i + 2}
        required_sources = sorted(module.REQUIRED_NATIVE_METADATA_SOURCES | {
            module.PRESET_PATH, module.PRESET_PATH + ".meta",
            module.CONSTRUCTION_PATH, module.CONSTRUCTION_PATH + ".meta",
        })
        for path in required_sources:
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(path)
            pins[path] = hashlib.sha256(target.read_bytes()).hexdigest()
        self.catalog = {"kind": "S073ConfiguredCatalogMetadataExport", "schema_version": 1,
                        "mission": "S073", "mission_world_loaded": False, "native_readiness": False,
                        "footprint_authority": "OriginalBuildingDefinitionMetadata.FootprintCells",
                        "preset_asset_path": module.PRESET_PATH,
                        "building_placement_source_path": module.CONSTRUCTION_PATH,
                        "source_dependencies_sha256": pins, "entries": rows}
        (self.out / "catalog.json").write_text(json.dumps(self.catalog))

    def tearDown(self):
        self.tmp.cleanup()

    def test_qualified_source_only_capture(self):
        self.assertFalse(module.qualify(self.root, self.out, 0)["mission_ready"])

    def test_wrapper_failure_and_stale_source_fail_closed(self):
        with self.assertRaises(ValueError):
            module.qualify(self.root, self.out, 1)
        (self.root / self.catalog["entries"][0]["asset_path"]).write_text("changed")
        with self.assertRaisesRegex(ValueError, "Capture source changed"):
            module.qualify(self.root, self.out, 0)

    def test_missing_marker_fails_closed(self):
        (self.out / "editor.log").write_text("no marker\n")
        with self.assertRaises(ValueError):
            module.qualify(self.root, self.out, 0)


if __name__ == "__main__":
    unittest.main()
