import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("unit_catalog_receipt", Path(__file__).with_name("finalize_s073_unit_catalog.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class UnitCatalogReceiptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.out = self.root / "out"
        self.out.mkdir()
        (self.out / "editor.log").write_text("[S073UnitCatalogExport] result=Passed entries=3 path=fixture\n")
        names = ["Unit_Chr_Soldier_Male_02_Alt_04", "Unit_Veh_Truck_Tray", "Unit_Veh_Truck_Tanker"]
        rows, pins = [], {}
        for i, name in enumerate(names):
            asset = "Assets/" + name + ".prefab"
            for path in (asset, asset + ".meta"):
                target = self.root / path
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_text(path)
                pins[path] = hashlib.sha256(target.read_bytes()).hexdigest()
            rows.append({"registry_order": i, "prefab_name": name, "display_name": name, "asset_path": asset,
                         "guid": str(i) * 32, "local_file_id": i + 1, "can_request": True, "is_vehicle": i > 0,
                         "displayed_price": 20, "credits_cost": 100, "materials_cost": 20})
        self.data = {"kind": "S073ConfiguredUnitCatalogExport", "schema_version": 1, "mission": "S073",
                     "mission_world_loaded": False, "native_readiness": False, "configured_building_count": 6,
                     "preset_asset_path": "Assets/Game/Resources/SkirmishIndustrialBasin.asset",
                     "building_placement_source_path": "Assets/Game/Configs/Skirmish/IndustrialBasin/Construction.asset",
                     "unit_registry_source_path": "Assets/Game/Configs/Skirmish/UnitRegistry.asset",
                     "source_dependencies_sha256": pins, "entries": rows}
        self.write()

    def write(self):
        (self.out / "unit-catalog.json").write_text(json.dumps(self.data))

    def tearDown(self):
        self.tmp.cleanup()

    def test_source_only_capture_qualifies(self):
        self.assertFalse(module.qualify(self.root, self.out, 0)["mission_ready"])

    def test_failed_wrapper_or_stale_source_rejected(self):
        with self.assertRaises(ValueError):
            module.qualify(self.root, self.out, 1)
        (self.root / self.data["entries"][0]["asset_path"]).write_text("changed")
        with self.assertRaisesRegex(ValueError, "source changed"):
            module.qualify(self.root, self.out, 0)

    def test_duplicate_identity_and_missing_marker_rejected(self):
        self.data["entries"][1]["guid"] = self.data["entries"][0]["guid"]
        self.data["entries"][1]["local_file_id"] = self.data["entries"][0]["local_file_id"]
        self.write()
        with self.assertRaises(ValueError):
            module.qualify(self.root, self.out, 0)
        (self.out / "editor.log").write_text("no marker\n")
        with self.assertRaises(ValueError):
            module.qualify(self.root, self.out, 0)


if __name__ == "__main__":
    unittest.main()
