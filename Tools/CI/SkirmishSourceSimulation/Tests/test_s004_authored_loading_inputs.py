import importlib.util
from pathlib import Path
import unittest
R=Path(__file__).resolve().parents[4]; P=R/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/audit_s004_authored_loading_inputs.py"
spec=importlib.util.spec_from_file_location("s004_loading_audit",P);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
class Controls(unittest.TestCase):
 def setUp(self): self.values={k:(R/p).read_text() for k,p in module.FILES.items()}
 def test_current_serialized_inputs(self):
  value=module.audit(self.values);self.assertEqual(value["city_count"],0);self.assertEqual(value["building_startup_complete"],"unknown")
 def test_changed_scene_reference_rejected(self):
  self.values["scene_meta"]=self.values["scene_meta"].replace("915718fcf0cd7422db987b388cd0d925","015718fcf0cd7422db987b388cd0d925")
  with self.assertRaises(ValueError):module.audit(self.values)
 def test_nonnull_placement_rejected(self):
  self.values["scene"]=self.values["scene"].replace("  buildingPlacements: {fileID: 0}","  buildingPlacements: {fileID: 123}")
  with self.assertRaises(ValueError):module.audit(self.values)
 def test_duplicate_serialized_field_rejected(self):
  self.values["match"]+="\n  mapVehiclePlacementConfig: {fileID: 0}\n"
  with self.assertRaises(ValueError):module.audit(self.values)
if __name__=="__main__":unittest.main()
