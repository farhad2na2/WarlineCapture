#!/usr/bin/env python3
"""Exercise fail-closed 10m S074 source-owner bundle joins without mutating inputs."""
import argparse, copy, hashlib, importlib.util, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
DEFAULT_BUNDLE=ROOT/'T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-source-bundle-20261114-r1'
HELPER=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/validate_s074_ten_metre_player_vehicle_owner_inputs.py'

def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def load_module():
 spec=importlib.util.spec_from_file_location('s074_ten_metre_owner_validation_test',HELPER)
 module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--bundle-dir',type=Path,default=DEFAULT_BUNDLE);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
 bundle_dir=a.bundle_dir.resolve(strict=True);out=a.output.resolve()
 if not out.is_relative_to(ROOT/'T Logs') or out.exists():raise ValueError('Fresh repository T Logs test output required')
 out.mkdir(parents=True);initial=json.loads((bundle_dir/'initialization.json').read_bytes());bundle=json.loads((bundle_dir/'data-bundle.json').read_bytes());validator=load_module();passed=validator.validate_s074_ten_metre_owner_inputs(initial,bundle,ROOT)
 if not passed:raise ValueError('Positive source-owner admission did not return verified pins')
 results=[]
 def reject(name,mutated_initial,mutated_bundle=bundle):
  try:validator.validate_s074_ten_metre_owner_inputs(mutated_initial,mutated_bundle,ROOT)
  except (ValueError,KeyError,TypeError) as error:results.append({'case':name,'rejected':True,'reason':str(error)})
  else:raise ValueError('Negative case was admitted: '+name)
 bad=copy.deepcopy(initial);bad['typed_mission_configuration']['layout']['sha256']='0'*64;reject('stale_layout_descriptor',bad)
 bad=copy.deepcopy(initial);bad['setup_matrix']['sha256']='0'*64;reject('stale_matrix_descriptor',bad)
 bad=copy.deepcopy(initial);bad['mission_logical_binding']['sha256']='0'*64;reject('stale_binding_descriptor',bad)
 admission_desc=bundle['s074_source_owner_admission'];admission=json.loads(Path(admission_desc['path']).read_bytes());admission['status']='ForgedPassedStatus';forged=out/'forged-status-admission.json';forged.write_text(json.dumps(admission,indent=2,sort_keys=True)+'\n')
 bad_bundle=copy.deepcopy(bundle);bad_bundle['s074_source_owner_admission']={'path':str(forged.resolve()),'sha256':sha(forged)};reject('forged_admission_status',initial,bad_bundle)
 report={'kind':'S074TenMetreSourceOwnerAdmissionNegativeControls','status':'Passed','positive_case':'passed with current exact descriptors','negative_cases':results,'target':{'setup_hash':3241215135,'layout_fingerprint':'1af7a7962ec0f6e424ccce542295df14ba74a02e3c5262789ef608c68f3a6ffe'},'admission_receipt':admission_desc,'world_allocated':False,'runtime_admission':False,'source_fit_certified':False,'native_context_qualified':False,'mission_ready':False}
 (out/'negative-checks.json').write_text(json.dumps(report,indent=2,sort_keys=True)+'\n');print(json.dumps({'status':'Passed','negative_cases':len(results),'output':str(out/'negative-checks.json')}))
if __name__=='__main__':main()
