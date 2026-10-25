"""Project exact qualified immutable native facts; never initialize a World."""
import argparse,copy,hashlib,json,re
from pathlib import Path
NATIVE_SHA='06ff2cf07f069988a04f9e1740cbc9d29461207014b566fe3f2845c205e7be17'
QUALIFIED={NATIVE_SHA:('NativeS026MetadataQualification',None),'6a55787a770c82623030907518e87343fad72b5c35d91ae293a5199128ba986a':('NativeS026LogicalBoundMetadataQualification','a6f7e1040b97c5daf76f86a9a81fcb48101863698c5f1593a2f6b00d1e70bf18'),'da1383237b7ec9bb0607596be496edc40d6a1cd9c15c12901d7dbee9731b5f37':('NativeS026LogicalBoundMetadataQualification','d0604a13ee34f00c19beb4b03be02850d5aa765592e4e33c0be3688dacd60443')}
def project(native_path,bridge_path,output):
 native_path=Path(native_path).resolve(strict=True);raw=native_path.read_bytes()
 native_sha=hashlib.sha256(raw).hexdigest()
 if native_sha not in QUALIFIED:raise ValueError('Exact passed native artifact required')
 expected_kind,expected_logical=QUALIFIED[native_sha]
 n=json.loads(raw)
 if n['kind']!=expected_kind or len(n['contexts'])!=12 or n['full_mission_initialized'] or n['runtime_state_transferred'] or n['public_admission_changed'] or (n.get('logical_binding_reference_sha256')!=expected_logical if expected_logical is not None else n['opening_placement_transferred']):raise ValueError('Exact metadata-only native context domain required')
 bridge_raw=Path(bridge_path).read_bytes();body=re.search(r'FactorySources=\{(.*?)\};',bridge_raw.decode(),re.S).group(1);rels=['Assets/Game/Scripts/'+s for s in re.findall(r'"([^"]+)"',body)];subset={k:n['source_pins'][k]for rel in rels for k in [rel,rel+'.meta']}
 out=Path(output).absolute()
 if out.exists():raise ValueError('Fresh isolated projection directory required')
 out.mkdir(parents=True);rows=[]
 def write(path,value):
  path.write_text(json.dumps(value,indent=2)+'\n');return {'path':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
 for c in n['contexts']:
  m=c['metadata'];folder=out/(c['size']+'-'+c['difficulty']);folder.mkdir();origin={k:copy.deepcopy(c[k])for k in ['catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256']};origin.update(kind=n['kind'],artifact_sha256=native_sha)
  common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':c['catalog_id'],'setup_hash':c['setup_hash'],'seed':c['seed'],'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
  d=copy.deepcopy(common);d.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']}for r in m['buildings']],source_pins=n['source_pins']);dd=write(folder/'building-definitions.json',d)
  u=copy.deepcopy(common);u.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=n['source_pins'],transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key'])for r in m['units']],ui_catalog_runtime_policy_sampled=False);ud=write(folder/'unit-production-metadata.json',u)
  s=copy.deepcopy(common);s.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=dd,source_pins=subset,rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']}for r in m['buildings']]);sd=write(folder/'configured-metadata.json',s)
  rows.append({'selection':{k:c[k]for k in ['catalog_id','size','difficulty','seed','setup_hash']},'metadata_compatibility':{'path':str(native_path),'sha256':native_sha},'building_definitions':dd,'unit_production_metadata':ud,'building_request_metadata':sd,'building_count':len(m['buildings']),'unit_count':len(m['units'])})
 result={'kind':'NativeQualifiedImmutableMetadataProjectionIndex','native_artifact_sha256':native_sha,'contexts':rows,'runtime_admission':False,'positions_or_state_transferred':False};write(out/'projection-index.json',result)
 if native_path.read_bytes()!=raw or Path(bridge_path).read_bytes()!=bridge_raw:raise ValueError('Input changed during projection')
 return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--native',required=True);p.add_argument('--bridge',required=True);p.add_argument('--output',required=True);a=p.parse_args();r=project(a.native,a.bridge,a.output);print('Projected exact contexts='+str(len(r['contexts'])))
