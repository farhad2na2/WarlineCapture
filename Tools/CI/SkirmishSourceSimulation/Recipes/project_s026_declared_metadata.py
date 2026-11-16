"""Project one exact native S026 authoring context; never initialize a mission."""
import argparse, copy, hashlib, json, re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
NATIVE_SHA='0c8c592b13e04ab49194dd076a287cbdd6db8ca486d06b8930907df97f2b6dbb'
BINDING_SHA='d44197811ecac5ccc94357bf6fc974abdd3beb3c9eb3c73337f88c32e4018e70'
CANDIDATE_SHA='0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61'
def sha(raw):return hashlib.sha256(raw).hexdigest()

def project(native_path,binding_path,bridge_path,output):
 native_path=Path(native_path).resolve(strict=True);binding_path=Path(binding_path).resolve(strict=True);bridge_path=Path(bridge_path).resolve(strict=True)
 native_raw=native_path.read_bytes();binding_raw=binding_path.read_bytes();bridge_raw=bridge_path.read_bytes()
 if sha(native_raw)!=NATIVE_SHA or sha(binding_raw)!=BINDING_SHA:raise ValueError('Exact captured single-context bytes required')
 n=json.loads(native_raw);b=json.loads(binding_raw);c=n.get('context',{});rows=b.get('provider_contexts',[])
 if n.get('kind')!='NativeS026DeclaredMetadataContext' or b.get('kind')!='NativeAuthoredS026DeclaredLogicalBindingReference' or n.get('logical_binding_reference_sha256')!=BINDING_SHA or b.get('candidate_sha256')!=CANDIDATE_SHA or any(n.get(k) is not False for k in ('full_mission_initialized','runtime_state_transferred','public_admission_changed')) or any(b.get(k) is not False for k in ('mission_initialized','positions_or_state_transferred','public_admission_changed','native_visual_acceptance')) or b.get('authoring_world_created') is not True or b.get('wrong_declared_tuple_rejected') is not True:raise ValueError('Exact declared authoring-only scope required')
 if (c.get('catalog_id'),c.get('size'),c.get('difficulty'),c.get('seed'),c.get('setup_hash'))!=('S026','Standard','Regular',104755,116236341) or c.get('wrong_context_hash_rejected') is not True or c.get('wrong_target_seed_rejected') is not True or len(rows)!=1 or (rows[0].get('size'),rows[0].get('difficulty'),rows[0].get('setup_hash'),rows[0].get('actual_provider_passed'),rows[0].get('definition_instance_shared'))!=('Standard','Regular',116236341,True,True):raise ValueError('Exact original compiler/provider context required')
 m=c.get('metadata',{});domain=c.get('selected_native_domain',{});buildings=m.get('buildings',[]);units=m.get('units',[])
 if len(buildings)!=9 or len(units)!=16 or domain.get('building_count')!=len(buildings) or domain.get('unit_count')!=len(units) or domain.get('all_recipe_producers_and_units_in_selected_domain') is not True:raise ValueError('Exact original factory domain required')
 pins=n.get('source_pins');
 if not isinstance(pins,dict) or len(pins)!=8790 or pins!=b.get('source_pins'):raise ValueError('Exact same-capture source pin census required')
 def verify():
  for name,digest in pins.items():
   p=Path(name);p=(p if p.is_absolute() else ROOT/p).resolve(strict=True)
   if not p.is_relative_to(ROOT) or not p.is_file() or sha(p.read_bytes())!=digest:raise ValueError('Captured input changed: '+name)
  if native_path.read_bytes()!=native_raw or binding_path.read_bytes()!=binding_raw or bridge_path.read_bytes()!=bridge_raw:raise ValueError('Projection input changed')
 verify();body=re.search(r'FactorySources=\{(.*?)\};',bridge_raw.decode(),re.S)
 if not body:raise ValueError('Current source metadata factory closure required')
 rels=['Assets/Game/Scripts/'+s for s in re.findall(r'"([^"]+)"',body.group(1))]
 if not rels or any(k not in pins for rel in rels for k in (rel,rel+'.meta')):raise ValueError('Captured factory source closure incomplete')
 out=Path(output).absolute()
 if out.exists() or not out.is_relative_to(ROOT):raise ValueError('Fresh repository projection directory required')
 out.mkdir(parents=True);origin={k:copy.deepcopy(c[k]) for k in ('catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256')};origin.update(kind=n['kind'],artifact_sha256=NATIVE_SHA)
 common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':'S026','setup_hash':c['setup_hash'],'seed':c['seed'],'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
 def write(name,value):
  path=out/name;path.write_text(json.dumps(value,indent=2)+'\n');return {'path':str(path),'sha256':sha(path.read_bytes())}
 defs=copy.deepcopy(common);defs.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']} for r in buildings],source_pins=pins);dd=write('building-definitions.json',defs)
 unit=copy.deepcopy(common);unit.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=pins,transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key']) for r in units],ui_catalog_runtime_policy_sampled=False);ud=write('unit-production-metadata.json',unit)
 configured=copy.deepcopy(common);configured.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=dd,source_pins={k:pins[k] for rel in rels for k in (rel,rel+'.meta')},rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']} for r in buildings]);sd=write('configured-metadata.json',configured)
 result={'kind':'S026DeclaredImmutableMetadataProjectionIndex','native_artifact_sha256':NATIVE_SHA,'logical_binding_sha256':BINDING_SHA,'selection':{k:c[k] for k in ('catalog_id','size','difficulty','seed','setup_hash')},'metadata_compatibility':{'path':str(native_path),'sha256':NATIVE_SHA},'mission_logical_binding':{'path':str(binding_path),'sha256':BINDING_SHA},'building_definitions':dd,'unit_production_metadata':ud,'building_request_metadata':sd,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False};write('projection-index.json',result);verify();return result

if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--native',required=True);p.add_argument('--binding',required=True);p.add_argument('--bridge',required=True);p.add_argument('--output',required=True);a=p.parse_args();r=project(a.native,a.binding,a.bridge,a.output);print('Projected exact declared contexts=1 runtime_admission=False')
