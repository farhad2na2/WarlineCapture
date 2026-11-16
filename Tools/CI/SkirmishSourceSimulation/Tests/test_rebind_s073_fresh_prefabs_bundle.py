import copy, hashlib, importlib.util, json, unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
P=Path(__file__).with_name("rebind_s073_fresh_prefabs_bundle.py")
spec=importlib.util.spec_from_file_location("s073_rebind_fresh",P);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
def load(path):return json.loads((ROOT/path).read_text())
class FreshRebindTests(unittest.TestCase):
 def setUp(self):
  base={"kind":"S073LegacyStartupBundle","schema_version":1,"mission":"S073","seed":104729}
  roots={"one":{"id":1}}; old={"initial_spawn":{"InitialMaterials":220},"preset_values":{"seed":104729},"captured_unit_prefab_roots":roots}
  config={**copy.deepcopy(old),"kind":"S073LegacyInitialSpawnExternalData","mission":"S073","world_initialized":False,"mission_outcome_derived":False,"native_visual_readiness":False,"source_artifacts":{"prefab_capture_sha256":"prefab","name_readback_sha256":"names"}}
  common={"kind":"AuthoredSkirmishPrefabReferenceData","authority":"ActualSourceAssetsAndNativeBakers_NotMissionState","full_mission_initialized":False,"fresh_world_allocator_exported":False,"native_visual_readiness":False}
  prefab={**common,"opening_prefabs":roots,"requested_prefab_keys":["a","b","c","d"]}
  production={**common,"requested_prefab_keys":["a","b","c","d","e","f","g","h","i","j","k","l"]}
  names={"kind":"S073LegacyPrefabSourceNameReadback","prefab_capture_sha256":"prefab","mission_world_allocated":False,"native_visual_readiness":False}
  pr={"validation_status":"Passed","wrapper_exit_code":0,"required_marker_present":True,"capture_sha256":"prefab","mission_ready":False}
  nr={"validation_status":"Passed","wrapper_exit_code":0,"required_marker_present":True,"name_readback_sha256":"names","mission_ready":False}
  rr={"kind":"S073ProductionPrefabCaptureReceipt","validation_status":"Passed","capture_wrapper_exit_code":0,"capture_required_marker_present":True,"capture_sha256":"production","key_count":12,"mission_ready":False}
  self.values=[base,old,config,prefab,names,production,pr,nr,rr,{"prefab_capture":"prefab","source_identity":"names","production_prefabs":"production"}]
 def validate(self):module.validate_values(*self.values)
 def test_valid_fixture_lineage(self):self.validate()
 def test_changed_starting_materials_rejected(self):
  self.values[2]["initial_spawn"]["InitialMaterials"]+=1
  with self.assertRaises(ValueError):self.validate()
 def test_mismatched_names_rejected(self):
  self.values[4]["prefab_capture_sha256"]="0"*64
  with self.assertRaises(ValueError):self.validate()
 def test_unchecked_production_capture_rejected(self):
  self.values[8]["capture_required_marker_present"]=False
  with self.assertRaises(ValueError):self.validate()
 def test_claimed_native_readiness_rejected(self):
  self.values[3]["native_visual_readiness"]=True
  with self.assertRaises(ValueError):self.validate()
if __name__=="__main__":unittest.main()
