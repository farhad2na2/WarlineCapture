"""Project exact qualified immutable native facts; never initialize a World."""
import argparse,copy,hashlib,json,re
from pathlib import Path
NATIVE_SHA='06ff2cf07f069988a04f9e1740cbc9d29461207014b566fe3f2845c205e7be17'
QUALIFIED={NATIVE_SHA:('NativeS026MetadataQualification',None),'6a55787a770c82623030907518e87343fad72b5c35d91ae293a5199128ba986a':('NativeS026LogicalBoundMetadataQualification','a6f7e1040b97c5daf76f86a9a81fcb48101863698c5f1593a2f6b00d1e70bf18'),'da1383237b7ec9bb0607596be496edc40d6a1cd9c15c12901d7dbee9731b5f37':('NativeS026LogicalBoundMetadataQualification','d0604a13ee34f00c19beb4b03be02850d5aa765592e4e33c0be3688dacd60443')}
S049_DIRECT_SHA='5b1852e889ff1e19e9dd5cfdeedc157d253990dc0d5963c82fe78abbd2cce040'
QUALIFIED['213b73cc917d53436aa9799ef3492f5e0dd48609f0f41d200fc0e03fbb807a37']=('NativeS026LogicalBoundMetadataQualification','13e8ac9bde1836ae6dc3d01e11cebb301c5f02f7eee6dbba0e293c6e1bf9af78')
S049_METADATA_EPOCH_SHA='1423ad3b5e87cd5ac6f6fc5a2f7eee87e20b0410f0a7bc2df4a2b66a13198132'
S002_TWO_SEED_SHA='50a86f262e48c07808a3e52e2efb1b8bf8c744c00f1fd1c4fbf01b2e0c0502f1'
S002_PRIOR_TWO_SEED_SHA='d2c9714d8f8a49cab62de59fe57dceff9cf6ab24b7d176608850a2bfcfd20383'
S002_REFRESHED_TWO_SEED_SHA='1e5ed14ff414435899f771414a6cf2f7a774bd621449a183ec1f6ae2e5a98c3c'
S002_CONTEXTS=((104731,3657809016),(1,3589425631))
S002_NEGATIVE_CONTROLS=['prefab-guid','prefab-local-id','provider-order','requestability','definition-field','recipe-quantity','unit-metadata','consumed-context']

def project_s002(native_path,bridge_path,compare_path,output,raw,bridge_raw):
 n=json.loads(raw);old_raw=Path(compare_path).read_bytes()
 native_sha=hashlib.sha256(raw).hexdigest()
 prior_sha=S002_TWO_SEED_SHA if native_sha==S002_REFRESHED_TWO_SEED_SHA else S002_PRIOR_TWO_SEED_SHA
 if hashlib.sha256(old_raw).hexdigest()!=prior_sha:raise ValueError('Exact prior S002 two-seed capture required')
 old=json.loads(old_raw)
 fields={'contexts','difficulty','full_mission_initialized','kind','metadata_equal','mission','negative_controls','opening_placement_transferred','public_admission_changed','runtime_state_transferred','scope','size','source_pins'}
 if set(n)!=fields or n.get('kind')!='NativeTwoSeedMetadataQualification' or n.get('scope')!='OriginalCompilerProvidersAndMetadataFactoriesOnly' or n.get('mission')!='S002' or n.get('size')!='Standard' or n.get('difficulty')!='Regular' or n.get('metadata_equal') is not True or any(n.get(k) is not False for k in ('full_mission_initialized','opening_placement_transferred','runtime_state_transferred','public_admission_changed')) or n.get('negative_controls')!=S002_NEGATIVE_CONTROLS:
  raise ValueError('Exact S002 metadata-only capture and negative-control scope required')
 if len(n['contexts'])!=2 or [(c.get('seed'),c.get('setup_hash')) for c in n['contexts']]!=list(S002_CONTEXTS):raise ValueError('Exact ordered S002 compiler contexts required')
 for context in n['contexts']:
  if context.get('catalog_id')!='S002' or context.get('size')!='Standard' or context.get('difficulty')!='Regular' or context.get('wrong_context_hash_rejected') is not True or context.get('wrong_target_seed_rejected') is not True or not isinstance(context.get('metadata_key_sha256'),str) or not re.fullmatch('[0-9a-f]{64}',context['metadata_key_sha256']):raise ValueError('Exact S002 native context and negative checks required')
 if not all(context.get('metadata')==n['contexts'][0]['metadata'] for context in n['contexts']):raise ValueError('S002 native factory metadata differs across seed contexts')
 if set(old)!=fields or old.get('kind')!='NativeTwoSeedMetadataQualification' or [(c.get('seed'),c.get('setup_hash')) for c in old.get('contexts',[])]!=list(S002_CONTEXTS) or any(old.get(k)!=n.get(k) for k in ('kind','scope','mission','size','difficulty','metadata_equal','negative_controls','full_mission_initialized','opening_placement_transferred','runtime_state_transferred','public_admission_changed')) or any(c.get('metadata')!=nc.get('metadata') for c,nc in zip(old['contexts'],n['contexts'])):
  raise ValueError('New S002 capture does not preserve prior native metadata semantics and provenance')
 root=Path(__file__).resolve().parents[4];pins=n['source_pins']
 expected_pin_count=8782 if native_sha==S002_REFRESHED_TWO_SEED_SHA else 8670
 if not isinstance(pins,dict) or len(pins)!=expected_pin_count:raise ValueError('Exact complete S002 current source-pin census required')
 for rel,digest in pins.items():
  q=Path(rel).resolve(strict=True)
  if not q.is_relative_to(root) or not re.fullmatch('[0-9a-f]{64}',digest):raise ValueError('Canonical S002 source pin required')
  if not q.is_file() or q.is_symlink() or hashlib.sha256(q.read_bytes()).hexdigest()!=digest:raise ValueError('Current S002 native capture input changed: '+rel)
 bridge=Path(bridge_path);text=bridge_raw.decode();body=re.search(r'FactorySources=\{(.*?)\};',text,re.S)
 if not body:raise ValueError('Current metadata bridge FactorySources required')
 rels=['Assets/Game/Scripts/'+x for x in re.findall(r'"([^"]+)"',body.group(1))]
 if len(rels)<1 or any(p not in pins or p+'.meta' not in pins for p in rels):raise ValueError('Current S002 source bridge is not covered by the capture')
 out=Path(output).absolute()
 if out.exists():raise ValueError('Fresh isolated projection directory required')
 out.mkdir(parents=True);index=[]
 for c in n['contexts']:
  folder=out/('Standard-Regular-'+str(c['seed']));folder.mkdir();m=c['metadata'];origin={k:copy.deepcopy(c[k]) for k in ['catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256']};origin.update(kind=n['kind'],artifact_sha256=native_sha)
  common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':c['catalog_id'],'setup_hash':c['setup_hash'],'seed':c['seed'],'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
  def write(name,value):
   f=folder/name;f.write_text(json.dumps(value,indent=2)+'\n');return {'path':str(f),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()}
  d=copy.deepcopy(common);d.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']} for r in m['buildings']],source_pins=pins);dd=write('building-definitions.json',d)
  u=copy.deepcopy(common);u.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=pins,transports=m['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key']) for r in m['units']],ui_catalog_runtime_policy_sampled=False);ud=write('unit-production-metadata.json',u)
  configured=copy.deepcopy(common);configured.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=dd,source_pins={k:pins[k] for rel in rels for k in (rel,rel+'.meta')},rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']} for r in m['buildings']]);sd=write('configured-metadata.json',configured)
  index.append({'selection':{k:c[k] for k in ['catalog_id','size','difficulty','seed','setup_hash']},'metadata_compatibility':{'path':str(Path(native_path).resolve()),'sha256':native_sha},'building_definitions':dd,'unit_production_metadata':ud,'building_request_metadata':sd,'building_count':len(m['buildings']),'unit_count':len(m['units'])})
 result={'kind':'NativeQualifiedImmutableMetadataProjectionIndex','native_artifact_sha256':native_sha,'prior_native_artifact_sha256':prior_sha,'metadata_semantics_equal_to_prior':True,'contexts':index,'runtime_admission':False,'positions_or_state_transferred':False};(out/'projection-index.json').write_text(json.dumps(result,indent=2)+'\n')
 if native_path.read_bytes()!=raw or bridge.read_bytes()!=bridge_raw or Path(compare_path).read_bytes()!=old_raw:raise ValueError('S002 projection input changed during projection')
 return result
def project_s049(native_path,bridge_path,output,raw):
 n=json.loads(raw); rows=n.get('missions',[])
 if n.get('catalogId')!='S049' or n.get('result')!='CollectedUnqualified' or n.get('cleanupVerified') is not True or len(rows)!=1 or rows[0].get('catalogId')!='S049':raise ValueError('Exact cleaned-up S049 native report required')
 m=rows[0]['measurement']; cap=m['s049NativeMetadataContext']; c=cap['context']; pins=cap['source_pins']
 if cap.get('kind')!='NativeS049DirectMetadataContext' or cap.get('same_capture_pins_rechecked') is not True or any(cap.get(k) is not False for k in ('mission_initialized','runtime_state_transferred','native_launch_provider_selected')) or (c['catalog_id'],c['size'],c['difficulty'],c['seed'],c['setup_hash'])!=('S049','Standard','Regular',104734,971644661) or c.get('wrong_context_hash_rejected') is not True or c.get('wrong_target_seed_rejected') is not True:raise ValueError('Exact S049 factory context and scope required')
 if m.get('normalThreeSizeCompilerPassed') is not True or m.get('proposedLayoutSemanticSha256')!='9e94e1a6be531a77829fa04e8672b660c3a31e7f479a51653f1c89c92a9649cb' or len(m.get('portableBinding',{}).get('fields',{}).get('Variants',[]))!=3:raise ValueError('Exact S049 V2 compiler and portable binding required')
 bridge_raw=Path(bridge_path).read_bytes();body=re.search(r'FactorySources=\{(.*?)\};',bridge_raw.decode(),re.S).group(1);rels=['Assets/Game/Scripts/'+x for x in re.findall(r'"([^"]+)"',body)];project_root=native_path.parents[3]
 def collect_assets(x):
  if isinstance(x,dict):
   if x.get('codec')=='source-asset-reference-v1' and isinstance(x.get('path'),str):yield x['path']
   for v in x.values():yield from collect_assets(v)
  elif isinstance(x,list):
   for v in x:yield from collect_assets(v)
 owner_body=re.search(r'OwnerDefinitions=\{(.*?)\};',bridge_raw.decode(),re.S).group(1);owners=['Assets/Game/Scripts/'+x for x in re.findall(r'"([^"]+)"',owner_body)];asset_paths=set(collect_assets(c['metadata']));required={p for rel in rels+owners for p in (rel,rel+'.meta')}|{p for rel in asset_paths for p in (rel,rel+'.meta')}|{'Packages/manifest.json','Packages/packages-lock.json'}
 if not asset_paths or any(path not in pins or not (project_root/path).is_file() or hashlib.sha256((project_root/path).read_bytes()).hexdigest()!=pins[path] for path in required):raise ValueError('S049 native factory/provider pinned dependency mismatch')
 # Candidate-only authoring files are retained separately after the project was restored.
 saved=Path(__file__).resolve().parents[4]/'Logs/private-candidates/s049-bundle-reconstruction-20261028-r1'
 variants={'Assets/Game/Scripts/Editor/Skirmish/SkirmishGroundSourceFitSurvey.RefinementOutput.cs':'RefinementOutput.v2-pinned-metadata-candidate.cs.txt','Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs':'S026LogicalBinding.s049-pinned-candidate.cs.txt','Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs':'TwoSeedMetadata.s049-pinned-candidate.cs.txt'}
 if any(hashlib.sha256((saved/name).read_bytes()).hexdigest()!=pins.get(path) for path,name in variants.items()):raise ValueError('S049 captured authoring source mismatch')
 out=Path(output).absolute()
 if out.exists():raise ValueError('Fresh isolated projection directory required')
 out.mkdir(parents=True); payload={'kind':'NativeS049DirectMetadataProjection','authority':'OriginalNativeMetadataFactories_NotMissionState','native_artifact_sha256':S049_DIRECT_SHA,'context':c,'source_pins':{p:pins[p] for p in sorted(required)},'authoring_source_pins':{p:pins[p] for p in variants},'runtime_admission':False,'mission_ready':False,'native_launch_provider_selected':False}
 (out/'s049-direct-metadata.json').write_text(json.dumps(payload,indent=2)+'\n')
 def write(name,value):
  f=out/name;f.write_text(json.dumps(value,indent=2)+'\n');return {'path':str(f),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()}
 origin={k:copy.deepcopy(c[k]) for k in ['catalog_id','size','difficulty','seed','setup_hash','metadata_key_sha256']};origin.update(kind='NativeS049DirectMetadataContext',artifact_sha256=S049_DIRECT_SHA)
 common={'schema_version':2,'authority':'OriginalNativeMetadataFactories_NotMissionState','catalog_id':c['catalog_id'],'setup_hash':c['setup_hash'],'seed':c['seed'],'native_origin':origin,'unsupported_values':[],'full_mission_initialized':False,'native_visual_readiness':False}
 meta=c['metadata'];d=copy.deepcopy(common);d.update(kind='NativeQualifiedSkirmishBuildingDefinitions',definitions=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']} for r in meta['buildings']],source_pins=pins);dd=write('building-definitions.json',d)
 u=copy.deepcopy(common);u.update(kind='NativeQualifiedSkirmishUnitProductionMetadata',baked_actor_footprint_substituted=False,runtime_camera_basis_exported=False,source_pins=pins,transports=meta['transports'],units=[dict(copy.deepcopy(r),key=r['prefab']['key']) for r in meta['units']],ui_catalog_runtime_policy_sampled=False);ud=write('unit-production-metadata.json',u)
 configured=copy.deepcopy(common);configured.update(kind='NativeQualifiedConfiguredBuildingMetadata',reference=dd,source_pins={k:pins[k] for rel in rels for k in (rel,rel+'.meta')},rows=[{'definition_index':r['index'],'key':r['prefab']['key'],'prefab':r['prefab'],'display_name':r['configured_entry']['fields']['DisplayName']['value'],'can_request':r['configured_entry']['fields']['CanRequest']['value'],'construction_catalog_index':None if r['construction_index']<0 else r['construction_index'],'scene_catalog_index':None if r['scene_index']<0 else r['scene_index']} for r in meta['buildings']]);sd=write('configured-metadata.json',configured)
 write('projection-index.json',{'kind':'NativeS049DirectImmutableMetadataProjectionIndex','native_artifact_sha256':S049_DIRECT_SHA,'building_definitions':dd,'unit_production_metadata':ud,'building_request_metadata':sd,'runtime_admission':False,'native_launch_provider_selected':False})
 if native_path.read_bytes()!=raw or Path(bridge_path).read_bytes()!=bridge_raw:raise ValueError('Input changed during S049 projection')
 return {'contexts':[{'selection':{'catalog_id':'S049','size':'Standard','difficulty':'Regular','seed':104734,'setup_hash':971644661}}],'runtime_admission':False}
def project(native_path,bridge_path,output,compare_native=None):
 native_path=Path(native_path).resolve(strict=True);raw=native_path.read_bytes()
 native_sha=hashlib.sha256(raw).hexdigest()
 if native_sha==S049_METADATA_EPOCH_SHA:
  # Native-11 includes its own complete pinned source closure and prior-epoch
  # parity evidence, so this additive metadata projection does not need the
  # older bridge-subset input used by the retained direct-capture format.
  import importlib.util
  epoch_path=Path(__file__).with_name('project_s049_metadata_epoch.py')
  spec=importlib.util.spec_from_file_location('s049_metadata_epoch_projection',epoch_path)
  module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
  return module.project(native_path,output,Path(__file__).resolve().parents[4])
 if native_sha==S049_DIRECT_SHA:
  if bridge_path is None:raise ValueError('S049 direct projection requires its metadata bridge source')
  return project_s049(native_path,bridge_path,output,raw)
 if native_sha in (S002_TWO_SEED_SHA,S002_REFRESHED_TWO_SEED_SHA):
  if compare_native is None:raise ValueError('Prior S002 native capture required for semantic comparison')
  if bridge_path is None:raise ValueError('S002 projection requires its metadata bridge source')
  return project_s002(native_path,bridge_path,compare_native,output,raw,Path(bridge_path).read_bytes())
 if native_sha not in QUALIFIED:raise ValueError('Exact passed native artifact required')
 if bridge_path is None:raise ValueError('Qualified projection requires its metadata bridge source')
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
 p=argparse.ArgumentParser();p.add_argument('--native',required=True);p.add_argument('--bridge');p.add_argument('--compare-native');p.add_argument('--output',required=True);a=p.parse_args();r=project(a.native,a.bridge,a.output,a.compare_native);print('Projected exact contexts='+str(len(r['contexts'])))
