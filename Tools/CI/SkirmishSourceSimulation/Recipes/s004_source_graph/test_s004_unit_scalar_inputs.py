import importlib.util
import shutil
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
SPEC = importlib.util.spec_from_file_location("s004_scalar_inputs", HERE / "read_s004_unit_scalar_inputs.py")
MODULE = importlib.util.module_from_spec(SPEC); SPEC.loader.exec_module(MODULE)
REAL_ROOT = MODULE.ROOT

class S004ScalarInputControls(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="s004-scalar-control-")
        self.root = Path(self.temp.name)
        names = [MODULE.BASE, MODULE.BASE+".meta", MODULE.VEHICLE, MODULE.VEHICLE+".meta", MODULE.AUTHORING, MODULE.AUTHORING+".meta", MODULE.POLICY, MODULE.OWNER, MODULE.DEFINITION_OWNER, MODULE.MATERIAL_POLICY,
                 "Assets/Game/Scripts/Configs/GameplayConfigModels.cs", "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs", "Assets/Game/Scripts/Configs/Prefabs/UnitGridAuthoringPrefabConfigAsset.cs.meta",
                 "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/read_s004_unit_scalar_inputs.py"]
        for key, config in MODULE.CONFIGS.items():
            prefab = "Assets/Game/Prefabs/Vehicles/"+key+".prefab"
            names.extend((prefab, prefab+".meta", config, config+".meta"))
        for name in names:
            dest = self.root/name; dest.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(REAL_ROOT/name,dest)
        MODULE.ROOT = self.root

    def tearDown(self):
        MODULE.ROOT = REAL_ROOT
        self.temp.cleanup()

    def test_current_serialized_inputs(self):
        value = MODULE.read()
        self.assertEqual(3, len(value["rows"]))
        self.assertFalse(value["mission_ready"])

    def test_changed_config_scalar_rejected(self):
        path = self.root / MODULE.CONFIGS["Unit_Veh_APC_Heavy"]
        value = path.read_text(); self.assertEqual(1, value.count("productionDurationSeconds: 5"))
        path.write_text(value.replace("productionDurationSeconds: 5", "productionDurationSeconds: 6"))
        with self.assertRaisesRegex(ValueError, "scalar difference"): MODULE.read()

    def test_wrong_prefab_config_binding_rejected(self):
        path = self.root / "Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab"
        value = path.read_text(); old = "1a9b9d82830054e60bb0318f90cbfa91"; self.assertEqual(1, value.count(old))
        path.write_text(value.replace(old, "0" * 32))
        with self.assertRaisesRegex(ValueError, "config binding"): MODULE.read()

    def test_duplicate_scalar_rejected(self):
        path = self.root / MODULE.CONFIGS["Unit_Veh_Truck_Canopy"]
        path.write_text(path.read_text() + "\n  productionDurationSeconds: 5\n")
        with self.assertRaisesRegex(ValueError, "unique"): MODULE.read()

    def test_price_disagreement_rejected(self):
        path = self.root / MODULE.CONFIGS["Unit_Veh_APC_Heavy"]
        value = path.read_text(); self.assertEqual(1, value.count("  price: 45000"))
        path.write_text(value.replace("  price: 45000", "  price: 45001"))
        with self.assertRaisesRegex(ValueError, "matching source price"): MODULE.read()

    def test_unsupported_quoted_description_rejected(self):
        path = self.root / MODULE.CONFIGS["Unit_Veh_Truck_Canopy"]
        value = path.read_text(); self.assertIn("  description: Carries", value)
        path.write_text(value.replace("  description: Carries", '  description: "Carries'))
        with self.assertRaisesRegex(ValueError, "Unsupported serialized plain text"): MODULE.read()

if __name__ == "__main__": unittest.main()
