"""Finite source execution admission; never changes catalog playability."""
import json,hashlib,re,stat,importlib.util,sys,subprocess,tempfile
import skirmish_owned_source as owned
from pathlib import Path
PATH=Path(__file__).resolve().parent/'SkirmishSourceSimulation/admitted_profiles.json'
MODULE_SHA=owned.module_pin(__name__,__file__)
PROFILE_RAW=PATH.read_bytes()
PROFILE_SHA=hashlib.sha256(PROFILE_RAW).hexdigest()
_DERIVED=None
def bind_s002_derived_seed(path):
 global _DERIVED
 if _DERIVED is not None:raise ValueError("derived seed already bound")
 _DERIVED=owned.require("s002_derived_bootstrap").load(path)
 return _DERIVED[1]
def derived():
 if _DERIVED is None:return None
 value,descriptor=owned.require("s002_derived_bootstrap").load(_DERIVED[1]["path"])
 if (value,descriptor)!=_DERIVED:raise ValueError("derived descriptor changed")
 return _DERIVED

def _pairs(rows):
 d={}
 for k,v in rows:
  if k in d:raise ValueError('Duplicate profile key: '+k)
  d[k]=v
 return d
def _nonfinite(value):raise ValueError('Non-finite configuration JSON constant: '+value)
def _validate_s003_current_qualification_shape(q):
 fields={'schema_version','kind','scope','compiler_context','native_context_qualified','source_fit_certified','runtime_admission','mission_ready'}
 if type(q)is not dict or set(q)!=fields or type(q.get('schema_version'))is not int or q['schema_version']!=1 or q.get('kind')!='ExperimentalS003CurrentSourceQualificationDescriptor' or q.get('scope')!='DescriptorOnly_CurrentCompilerAndFactoryData_SourceOnly' or any(q.get(k)is not False for k in ('native_context_qualified','source_fit_certified','runtime_admission','mission_ready')):raise ValueError('Exact descriptor-only current S003 qualification required')
 return q['compiler_context']
def _validate_s003_current_target(context):
 target={'catalog_id':'S003','definition_id':'skirmish.s003','seed':104732,'size':'Standard','difficulty':'Regular','setup_hash':2394754492,'layout_binding_fingerprint':'','mission_logical_binding_fingerprint':''}
 if type(context)is not dict or context.get('kind')!='OriginalS003CurrentSourceOnlyFactoryContext' or context.get('authority')!='CurrentAuthoredS003Graph_OriginalCompilerAndFactoryData_DescriptorOnly' or context.get('target_context')!=target or any(context.get(k)is not False for k in ('runtime_admission','mission_ready','native_context_qualified','source_fit_certified','full_mission_initialized','mission_world_created','runtime_state_transferred')):raise ValueError('Exact current S003 target and false source-only scope required')
 return target

def _s003_projection_directory(root,context):
 root=Path(root).resolve(strict=True)
 descriptor=context.get('factory_projection_receipt')if type(context)is dict else None
 if type(descriptor)is not dict or set(descriptor)!={'path','sha256'}or type(descriptor.get('path'))is not str or type(descriptor.get('sha256'))is not str or not re.fullmatch('[0-9a-f]{64}',descriptor['sha256']):raise ValueError('Exact context-bound current S003 projection receipt required')
 path=Path(descriptor['path'])
 if not path.is_absolute()or path.name!='projection-receipt.json':raise ValueError('Canonical context-bound S003 projection receipt path required')
 info=path.lstat();resolved=path.resolve(strict=True)
 if not resolved.is_relative_to(root)or str(path)!=str(resolved)or not stat.S_ISREG(info.st_mode)or info.st_size<=0 or info.st_size>4*1024*1024 or any(parent.is_symlink()for parent in[path,*path.parents]):raise ValueError('Bounded regular in-repository S003 projection receipt required')
 raw=path.read_bytes()
 if hashlib.sha256(raw).hexdigest()!=descriptor['sha256']:raise ValueError('Context-bound current S003 projection receipt changed')
 projection=json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 target={'catalog_id':'S003','difficulty':'Regular','size':'Standard','seed':104732,'setup_hash':2394754492}
 if projection.get('kind')!='S003CurrentFactoryMetadataProjectionReceipt'or projection.get('status')!='PassedSourceOnlyProjection'or projection.get('target')!=target:raise ValueError('Exact current S003 projection receipt target required')
 projection_capture=projection.get('capture')if type(projection.get('capture'))is dict else {}
 if projection_capture.get('path')!=context.get('native_factory_capture',{}).get('path')or projection_capture.get('sha256')!=context.get('native_factory_capture',{}).get('sha256')or projection_capture.get('receipt_path')!=context.get('native_factory_capture_receipt',{}).get('path')or projection_capture.get('receipt_sha256')!=context.get('native_factory_capture_receipt',{}).get('sha256')or projection.get('source_graph')!=context.get('source_graph'):raise ValueError('Current S003 projection does not bind context capture/graph')
 if projection.get('artifacts')!=context.get('factory_projection_artifacts'):raise ValueError('Current S003 projection artifacts differ from context')
 for field in ('runtime_admission','mission_ready','native_context_qualified','source_fit_certified'):
  if projection.get(field)is not False:raise ValueError('Current S003 projection scope promotion rejected: '+field)
 epoch=projection.get('current_compiler_epoch')
 if type(epoch)is not dict or epoch.get('compiler_receipt_path')!=context.get('compiler_receipt',{}).get('path') or epoch.get('compiler_receipt_sha256')!=context.get('compiler_receipt',{}).get('sha256') or epoch.get('compiler_audit_path')!=context.get('compiler_post_run_pin_audit',{}).get('path') or epoch.get('compiler_audit_sha256')!=context.get('compiler_post_run_pin_audit',{}).get('sha256') or epoch.get('build_path')!=context.get('source_build_receipt',{}).get('path') or epoch.get('build_sha256')!=context.get('source_build_receipt',{}).get('sha256') or epoch.get('graph_closure_path')!=context.get('compiler_graph',{}).get('path') or epoch.get('graph_closure_sha256')!=context.get('compiler_graph',{}).get('sha256'):
  raise ValueError('Current S003 projection compiler epoch differs from context')
 return path.parent

def _s003_current_qualification(path):
 root=Path(__file__).resolve().parents[2]
 qpath=Path(path)
 if not qpath.is_absolute()or qpath.name!='s003-current-source-qualification-descriptor.json':raise ValueError('Absolute current S003 qualification descriptor path required')
 output_dir=qpath.parent.resolve(strict=True)
 if not output_dir.is_relative_to(root)or qpath.is_symlink()or str(qpath)!=str(qpath.resolve(strict=True)):raise ValueError('Current S003 descriptor must be a canonical repository file')
 def read(p,want,limit):
  p=Path(p);info=p.lstat()
  resolved=p.resolve(strict=True)
  if not resolved.is_relative_to(root)or not stat.S_ISREG(info.st_mode)or any(x.is_symlink()for x in[p,*p.parents])or not 0<info.st_size<=limit:raise ValueError('Bounded regular in-repository current S003 evidence required')
  raw=p.read_bytes()
  if want is not None and hashlib.sha256(raw).hexdigest()!=want:raise ValueError('Current S003 qualification evidence changed')
  return json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 q=read(qpath,None,1024*1024)
 qualification_sha=hashlib.sha256(qpath.read_bytes()).hexdigest()
 cdesc=_validate_s003_current_qualification_shape(q)
 context_path=output_dir/'s003-current-source-only-context.json'
 if type(cdesc)is not dict or set(cdesc)!={'path','sha256'}or cdesc.get('path')!=str(context_path)or type(cdesc.get('sha256'))is not str or not re.fullmatch('[0-9a-f]{64}',cdesc['sha256']):raise ValueError('Exact current S003 compiler-context descriptor required')
 context=read(context_path,cdesc['sha256'],4*1024*1024)
 builder_path=root/'Tools/CI/SkirmishSourceSimulation/Recipes/build_s003_current_source_qualification_descriptor.py'
 spec=importlib.util.spec_from_file_location('s003_current_descriptor_gate_builder',builder_path)
 if spec is None or spec.loader is None:raise ValueError('Current S003 qualification evidence builder unavailable')
 builder=importlib.util.module_from_spec(spec);spec.loader.exec_module(builder)
 projection_dir=_s003_projection_directory(root,context)
 evidence=builder.validate_evidence(root,projection_dir)
 expected_context,expected_qualification=builder.construct_descriptors(evidence)
 expected_qualification['compiler_context']=cdesc
 if context!=expected_context or q!=expected_qualification:raise ValueError('Current S003 descriptor does not equal fresh exact evidence reconstruction')
 receipt_path=context_path.parent/'build-receipt.json'
 receipt=read(receipt_path,None,1024*1024)
 if receipt.get('kind')!='S003CurrentSourceQualificationDescriptorBuildReceipt' or receipt.get('status')!='PassedDescriptorOnly' or receipt.get('context')!=cdesc or receipt.get('qualification_descriptor')!={'path':str(qpath),'sha256':qualification_sha} or receipt.get('compiler_receipt_sha256')!=context.get('compiler_receipt',{}).get('sha256') or receipt.get('native_capture_sha256')!=context.get('native_factory_capture',{}).get('sha256') or any(receipt.get(k)is not False for k in ('runtime_admission','native_context_qualified','source_fit_certified','mission_ready')):raise ValueError('Current S003 descriptor build receipt scope or binding changed')
 target=_validate_s003_current_target(context)
 pins={}
 def add_pin(name,digest):
  key=str(Path(name).resolve(strict=True))
  if key in pins and pins[key]!=digest:raise ValueError('Conflicting current S003 qualification pin: '+key)
  pins[key]=digest
 def collect(value):
  if type(value)is dict:
   if set(value)=={'path','sha256'} and type(value['path'])is str and type(value['sha256'])is str:add_pin(value['path'],value['sha256'])
   else:
    for item in value.values():collect(item)
  elif type(value)is list:
   for item in value:collect(item)
 collect(context)
 for name,digest in evidence['source_pins'].items():add_pin(builder.pinned_path(root,name),digest)
 for name,digest in evidence['build_pins'].items():add_pin(builder.pinned_path(root,name),digest)
 return target,pins,context
def _direct_s049_target(c):
 fields={'schema_version','kind','authority','target_context','compiler_evidence','metadata_projection','source_graph','source_root','authoring_sources','world_allocated','source_fit_certified','mission_ready','runtime_loader_admitted'}
 if type(c)is not dict or set(c)!=fields or c['schema_version']!=1 or c['kind']!='OriginalS049DirectNativeFactoryContext' or c['authority']!='OriginalCompilerAndNativeFactoryFacts_SourceOnly' or any(c[k] is not False for k in ('world_allocated','source_fit_certified','mission_ready','runtime_loader_admitted')):raise ValueError('Exact direct S049 context required')
 def read(d,expected,limit):
  if type(d)is not dict or set(d)!={'path','sha256'} or d['sha256']!=expected or not Path(d['path']).is_absolute():raise ValueError('Exact S049 evidence descriptor required')
  f=Path(d['path']);info=f.lstat()
  if not stat.S_ISREG(info.st_mode) or any(p.is_symlink() for p in [f,*f.parents]) or not 0<info.st_size<=limit:raise ValueError('Bounded regular S049 evidence required')
  raw=f.read_bytes()
  if hashlib.sha256(raw).hexdigest()!=expected:raise ValueError('S049 evidence changed')
  return json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 evidence=read(c['compiler_evidence'],'d22dfae0c0280acd62fa0019b850c43f121696a6e63c8eaaabf738cd81eb76e9',64*1024*1024);projection=read(c['metadata_projection'],'c8b588fd4708b1ccc94dd2d2c4d44ffa89a8510adeffc0d73421773fbe5edaec',4*1024*1024);read(c['source_graph'],c['source_graph'].get('sha256') if isinstance(c['source_graph'],dict) and c['source_graph'].get('sha256') in {'07dfc3ca2556190d46dec8693e73e30afa7c4daae678a61a6c2158e65e214d46','26ede0965d504ce712ef15ceb8e10d5ba505adf75c58af4779dcc7955feff843','a643f41cfaf68ba267dd321915bc1040cbe89de55c8b64851f1811357d66ccfc'} else '',16*1024*1024)
 target=c['target_context'];measurement=evidence['missions'][0]['measurement'];native=projection['context']
 if evidence['catalogId']!='S049' or evidence['result']!='CollectedUnqualified' or evidence['cleanupVerified'] is not True or target!=measurement['s049CompilerTarget'] or (target['catalog_id'],target['size'],target['difficulty'],target['seed'],target['setup_hash'])!=('S049','Standard','Regular',104734,971644661) or projection['kind']!='NativeS049DirectMetadataProjection' or projection['native_artifact_sha256']!='5b1852e889ff1e19e9dd5cfdeedc157d253990dc0d5963c82fe78abbd2cce040' or projection['runtime_admission'] is not False or projection['mission_ready'] is not False or projection['native_launch_provider_selected'] is not False or any(native[k]!=target[k] for k in ('catalog_id','size','difficulty','seed','setup_hash')):raise ValueError('S049 native and compiler identity differs')
 root=Path(c['source_root']);
 if not root.is_absolute() or not root.is_dir() or any(p.is_symlink() for p in [root,*root.parents]):raise ValueError('Exact S049 source root required')
 for rel,digest in projection['source_pins'].items():
  f=root/rel
  if Path(rel).is_absolute() or '..' in Path(rel).parts or not f.is_file() or f.is_symlink() or hashlib.sha256(f.read_bytes()).hexdigest()!=digest:raise ValueError('S049 factory/provider source changed: '+rel)
 if set(c['authoring_sources'])!=set(projection['authoring_source_pins']):raise ValueError('Exact S049 authoring source coverage required')
 for rel,d in c['authoring_sources'].items():
  if type(d)is not dict or set(d)!={'path','sha256'} or d['sha256']!=projection['authoring_source_pins'][rel] or not Path(d['path']).is_absolute() or hashlib.sha256(Path(d['path']).read_bytes()).hexdigest()!=d['sha256']:raise ValueError('S049 captured authoring source changed')
 return target
def _s049_metadata_epoch_target(context):
 fields={'schema_version','kind','authority','target_context','prior_compiler_context','native_metadata_receipt','metadata_epoch','projected_artifacts','source_root','world_allocated','source_fit_certified','mission_ready','runtime_loader_admitted'}
 if type(context)is not dict or set(context)!=fields or context['schema_version']!=1 or context['kind']!='OriginalS049NativeMetadataEpochContext' or context['authority']!='CurrentS049CompilerAndNativeMetadataEpoch_SourceOnly' or any(context[k] is not False for k in ('world_allocated','source_fit_certified','mission_ready','runtime_loader_admitted')):raise ValueError('Exact additive S049 metadata epoch context required')
 root=Path(context['source_root'])
 if not root.is_absolute() or root.resolve(strict=True)!=Path(__file__).resolve().parents[2] or any(p.is_symlink() for p in [root,*root.parents]):raise ValueError('Exact current S049 source root required')
 epoch_root=root/'Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Projection-17'
 def read(d,limit,expected=None):
  if type(d)is not dict or set(d)!={'path','sha256'} or type(d['path'])is not str or not Path(d['path']).is_absolute() or type(d['sha256'])is not str or not re.fullmatch('[0-9a-f]{64}',d['sha256']) or expected is not None and d['sha256']!=expected:raise ValueError('Exact S049 metadata epoch descriptor required')
  f=Path(d['path']);info=f.lstat()
  if not stat.S_ISREG(info.st_mode) or any(p.is_symlink() for p in [f,*f.parents]) or not 0<info.st_size<=limit:raise ValueError('Bounded regular S049 metadata epoch input required')
  raw=f.read_bytes()
  if hashlib.sha256(raw).hexdigest()!=d['sha256']:raise ValueError('S049 metadata epoch input changed: '+str(f))
  return f,raw,json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 prior_path,prior_raw,prior=read(context['prior_compiler_context'],4*1024*1024,'4e0599e7f0ac194e5d175fbbde567dd4cc4c70be006f90ed7a21988d5af9a045')
 if prior_path!=root/'Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r6/compiler-context.json':raise ValueError('S049 prior compiler context path changed')
 target=_direct_s049_target(prior)
 if context['target_context']!=target:raise ValueError('New S049 epoch compiler target differs from preserved r6 context')
 receipt_path,receipt_raw,receipt=read(context['native_metadata_receipt'],64*1024*1024,'1423ad3b5e87cd5ac6f6fc5a2f7eee87e20b0410f0a7bc2df4a2b66a13198132')
 expected_receipt=root/'Logs/SkirmishProduction/Offline/S049MetadataOnlyRecapture_20261102/Native-11/s049-metadata.json'
 if receipt_path!=expected_receipt or set(receipt)!={'kind','scope','context','historical_metadata_parity','campaign_source','candidate_asset_origin','physical_map_created_or_changed','mission_initialized','runtime_state_transferred','native_launch_provider_selected','public_admission_changed','source_pins'} or receipt.get('kind')!='NativeS049MetadataOnlyRecapture' or receipt.get('scope')!='CurrentCompilerAndOriginalMetadataFactoriesOnly' or any(receipt.get(k)is not False for k in ('physical_map_created_or_changed','mission_initialized','runtime_state_transferred','native_launch_provider_selected','public_admission_changed')):raise ValueError('Exact current S049 metadata-only receipt required')
 native=receipt['context'];identity=('S049','Standard','Regular',104734,971644661)
 if (native.get('catalog_id'),native.get('size'),native.get('difficulty'),native.get('seed'),native.get('setup_hash'))!=identity or target.get('operation_map_id')!=receipt['campaign_source'].get('operation_map_id') or target.get('source_content_hash')!=receipt['campaign_source'].get('content_hash') or target.get('source_fingerprint')!=receipt['campaign_source'].get('mission_source_fingerprint') or any(target.get(k)!=native.get(k) for k in ('layout_binding_fingerprint','mission_logical_binding_fingerprint')):raise ValueError('S049 metadata receipt identity/bindings differ from original compiler')
 parity=receipt['historical_metadata_parity']
 if parity.get('artifact_sha256')!='c8b588fd4708b1ccc94dd2d2c4d44ffa89a8510adeffc0d73421773fbe5edaec' or parity.get('native_artifact_sha256')!='5b1852e889ff1e19e9dd5cfdeedc157d253990dc0d5963c82fe78abbd2cce040' or any(parity.get(k)is not True for k in ('metadata_equal','metadata_digest_equal','selected_domain_equal')):raise ValueError('S049 prior native metadata epoch parity required')
 pins=receipt['source_pins']
 if type(pins)is not dict or len(pins)!=8681:raise ValueError('Exact S049 current receipt source-pin closure required')
 relative={}
 for name,digest in pins.items():
  p=Path(name);p=root/p if not p.is_absolute() else p
  if p.is_symlink() or '..' in p.parts or not re.fullmatch('[0-9a-f]{64}',digest):raise ValueError('Canonical S049 pinned input required: '+name)
  resolved=p.resolve(strict=True)
  if (resolved!=root and root not in resolved.parents) or hashlib.sha256(resolved.read_bytes()).hexdigest()!=digest:raise ValueError('S049 current source pin mismatch: '+name)
  rel=resolved.relative_to(root).as_posix()
  if rel in relative:raise ValueError('Duplicate canonical S049 source pin: '+rel)
  relative[rel]=digest
 epoch_path,epoch_raw,epoch=read(context['metadata_epoch'],4*1024*1024)
 if epoch_path!=epoch_root/'metadata-epoch.json':raise ValueError('S049 Native-11 epoch manifest path changed')
 if epoch.get('kind')!='S049NativeMetadataEpoch' or epoch.get('epoch_id')!='Native-11' or epoch.get('projection_id')!='Projection-17' or epoch.get('receipt_sha256')!=hashlib.sha256(receipt_raw).hexdigest() or epoch.get('prior_epoch',{}).get('sha256')!='c8b588fd4708b1ccc94dd2d2c4d44ffa89a8510adeffc0d73421773fbe5edaec' or epoch.get('source_pin_count')!=len(relative) or epoch.get('runtime_admission')is not False or epoch.get('mission_ready')is not False:raise ValueError('S049 Native-11/Projection-17 epoch manifest differs from receipt/prior epoch')
 lineage=epoch.get('source_lineage',{})
 if lineage!={'initialization_path':str(root/'Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r8/partial-initialization.json'),'initialization_sha256':'662d00b896e5a1b9cca36f420ff1e60b9b1de6525a76f55f633b624c8bcdd5d9','data_bundle_path':str(root/'Logs/private-candidates/s049-bundle-reconstruction-20261028-r1/s049-partial-bundle-r8/partial-data-bundle.json'),'data_bundle_sha256':'ded092c82bfc8db209dbc33dfc50e8a23606eaabfaa633a759dcd29c6fd39224','source_epoch':'OriginalCompilerDerivedNativeFactoryContext/v1','world_profile':{'identity_backend':'FreshLogicalEntitiesV1','system_initialization':'OriginalOnCreate','future_native_inputs':False}}:raise ValueError('Exact preserved r8 initialization/bundle provenance required')
 r8_init_path=Path(lineage['initialization_path']);r8_bundle_path=Path(lineage['data_bundle_path'])
 if hashlib.sha256(r8_init_path.read_bytes()).hexdigest()!=lineage['initialization_sha256'] or hashlib.sha256(r8_bundle_path.read_bytes()).hexdigest()!=lineage['data_bundle_sha256']:raise ValueError('Preserved S049 r8 initialization or bundle changed')
 r8_init=json.loads(r8_init_path.read_bytes(),object_pairs_hook=_pairs,parse_constant=_nonfinite)
 if r8_init.get('source_epoch')!=lineage['source_epoch'] or r8_init.get('world_profile')!=lineage['world_profile'] or r8_init.get('metadata_compiler_context')!=context['prior_compiler_context']:raise ValueError('S049 r8 source profile/compiler provenance differs')
 closure=hashlib.sha256(json.dumps(relative,sort_keys=True,separators=(',',':')).encode()).hexdigest()
 if epoch.get('source_pin_closure_sha256')!=closure:raise ValueError('S049 Native-11 source-pin closure digest differs')
 expected_tooling={str(Path(__file__).resolve()),str(root/'Tools/CI/SkirmishSourceSimulation/Recipes/project_native_qualified_metadata.py'),str(root/'Tools/CI/SkirmishSourceSimulation/Recipes/project_s049_metadata_epoch.py'),str(root/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.cs'),str(root/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceNativeMetadataEpoch.cs')}
 tooling=epoch.get('projector_inputs')
 if type(tooling)is not dict or set(tooling)!=expected_tooling:raise ValueError('Exact S049 epoch/projection/runtime tooling pins required')
 for name,digest in tooling.items():
  f=Path(name)
  if f.is_symlink() or any(p.is_symlink() for p in [f,*f.parents]) or not f.is_file() or hashlib.sha256(f.read_bytes()).hexdigest()!=digest:raise ValueError('S049 epoch tooling changed: '+name)
 expected_artifacts={'building_definitions':'NativeQualifiedSkirmishBuildingDefinitions','unit_production_metadata':'NativeQualifiedSkirmishUnitProductionMetadata','building_request_metadata':'NativeQualifiedConfiguredBuildingMetadata'}
 if type(context['projected_artifacts'])is not dict or set(context['projected_artifacts'])!=set(expected_artifacts):raise ValueError('Exact S049 projected metadata artifact set required')
 for key,kind in expected_artifacts.items():
  artifact_path,_,artifact=read(context['projected_artifacts'][key],8*1024*1024)
  expected_path=epoch_root/{'building_definitions':'building-definitions.json','unit_production_metadata':'unit-production-metadata.json','building_request_metadata':'configured-metadata.json'}[key]
  if artifact_path!=expected_path:raise ValueError('S049 projected metadata artifact path changed: '+key)
  if artifact.get('kind')!=kind or artifact.get('native_origin',{}).get('artifact_sha256')!=hashlib.sha256(receipt_raw).hexdigest() or artifact.get('native_origin',{}).get('previous_projection_sha256')!=parity['artifact_sha256'] or artifact.get('source_pins')!=relative or artifact.get('full_mission_initialized')is not False or artifact.get('native_visual_readiness')is not False:raise ValueError('S049 projected metadata artifact is not bound to Native-11 epoch: '+key)
  if key=='building_definitions':
   expected=[{'key':r['prefab']['key'],'prefab':r['prefab'],'fields':r['fields']} for r in native['metadata']['buildings']]
   if artifact.get('definitions')!=expected:raise ValueError('S049 building projection differs from native metadata')
  elif key=='unit_production_metadata':
   if artifact.get('transports')!=native['metadata']['transports'] or [x['key'] for x in artifact.get('units',[])]!=[x['prefab']['key'] for x in native['metadata']['units']]:raise ValueError('S049 unit projection differs from native metadata')
  elif len(artifact.get('rows',[]))!=len(native['metadata']['buildings']):raise ValueError('S049 configured projection domain differs from native metadata')
 return target
def _validated_context_target(context):
 if isinstance(context,dict) and context.get('kind')=='OriginalS049DirectNativeFactoryContext':return _direct_s049_target(context)
 if isinstance(context,dict) and context.get('kind')=='OriginalS049NativeMetadataEpochContext':return _s049_metadata_epoch_target(context)
 fields={'schema_version','kind','authority','target_context','native_factory_reference','native_origin','derived_metadata','derived_metadata_key_sha256','unchanged_native_sections_sha256','section_hash_format','native_origin_descriptor','current_factory_source_root','consumed_input_pins','checks','world_allocated','identities_allocated','native_S050_capture','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted'}
 legacy=type(context)is dict and type(context.get('schema_version'))is int and context['schema_version']==1 and context.get('kind')=='OriginalCompilerDerivedNativeFactoryContext' and context.get('authority')=='ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050'
 s003_derived=type(context)is dict and type(context.get('schema_version'))is int and context['schema_version']==2 and context.get('kind')=='OriginalS003DerivedNativeFactoryContext' and context.get('authority')=='ActualCompilerContext_HistoricalS003FactoryFacts_SourceOnly'
 if type(context)is not dict or set(context)!=fields or not (legacy or s003_derived) or context['section_hash_format']!='NewtonsoftCompactUtf8Sha256'or type(context['identities_allocated'])is not int or context['identities_allocated']!=0:raise ValueError('Exact versioned compiler context required')
 for flag in ['world_allocated','native_S050_capture','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted']:
  if context[flag]is not False:raise ValueError('Explicit honest false compiler-context flag required: '+flag)
 checks={'original_all_role_overlay_fields_raw_equal','original_provider_selection_equal','original_native_origin68_and_assets_exact','current_factory67_package_exact','factory_keyed_coverage_and_order_projected' if s003_derived else 'factory_sections_unchanged'}
 if type(context['checks'])is not dict or set(context['checks'])!=checks or any(v is not True for v in context['checks'].values()):raise ValueError('Exact compiler/factory check declarations required')
 t=context['target_context'];keys={'catalog_id','definition_id','content_version','seed','size','difficulty','setup_hash','operation_map_id','source_content_hash','source_fingerprint','layout_fingerprint','layout_binding_fingerprint','mission_logical_binding_fingerprint'}
 if type(t)is not dict or set(t)!=keys or any(type(t[k])is not str for k in keys-{'seed','content_version','setup_hash'})or any(type(t[k])is not int for k in ['seed','content_version','setup_hash']):raise ValueError('Exact compiler target fields/types required')
 if not re.fullmatch(r'S[0-9]{3}',t['catalog_id'])or not 1<=int(t['catalog_id'][1:])<=120 or t['definition_id']!='skirmish.'+t['catalog_id'].lower()or not -2147483648<=t['seed']<=2147483647 or not 1<=t['content_version']<=2147483647 or not 0<=t['setup_hash']<=4294967295 or t['size']not in{'Standard','War','LargeWar'}or t['difficulty'] not in {'Recruit','Regular','Veteran','Commander'}or not 0<len(t['operation_map_id'])<=256 or any(not re.fullmatch('[0-9a-f]{64}',t[k])for k in ['source_content_hash','source_fingerprint','layout_fingerprint','layout_binding_fingerprint','mission_logical_binding_fingerprint']):raise ValueError('Finite canonical compiler target required')
 return t
def _validated_s003_context(c):
 required={'schema_version','kind','target_context','native_capture','source_graph','source_root','authoring_evidence_root','runtime_admission','mission_ready','fixed_wing_capability_qualified'}
 if type(c)is not dict or set(c)!=required or c['schema_version']!=1 or c['kind']!='OriginalS003SourceOnlyFactoryContext' or any(c[k] is not False for k in ('runtime_admission','mission_ready','fixed_wing_capability_qualified')):raise ValueError('Exact source-only S003 context required')
 target={'catalog_id':'S003','definition_id':'skirmish.s003','seed':104732,'size':'Standard','difficulty':'Regular','setup_hash':2394754492,'layout_binding_fingerprint':'','mission_logical_binding_fingerprint':''}
 if c['target_context']!=target:raise ValueError('Actual S003 compiler identity required')
 def read(d,expected,limit):
  if type(d)is not dict or set(d)!={'path','sha256'} or d['sha256']!=expected or not Path(d['path']).is_absolute():raise ValueError('Exact S003 evidence descriptor required')
  p=Path(d['path']);info=p.lstat()
  if not stat.S_ISREG(info.st_mode) or any(x.is_symlink() for x in [p,*p.parents]) or not 0<info.st_size<=limit:raise ValueError('Bounded S003 evidence required')
  raw=p.read_bytes()
  if hashlib.sha256(raw).hexdigest()!=expected:raise ValueError('S003 evidence changed')
  return json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 capture=read(c['native_capture'],'335284549b2ebb6b48c00acaa31cd796c056a9960db975396cb6628997690927',64*1024*1024)
 graph=read(c['source_graph'],'cde53fd9acfae52ed638f76d42ce803d7ce6ccfd641717e996116f48f266b634',16*1024*1024)
 native=capture['context']
 if capture.get('kind')!='NativeS003MetadataDiagnostic' or any(capture.get(k)is not False for k in ('native_readiness','full_mission_initialized','runtime_state_transferred')) or (native.get('catalog_id'),native.get('seed'),native.get('setup_hash'))!=('S003',104732,2394754492) or native.get('wrong_context_hash_rejected')is not True or native.get('wrong_target_seed_rejected')is not True:raise ValueError('S003 captured source context mismatch')
 root=Path(c['source_root']);evidence=Path(c['authoring_evidence_root']);prefix='Assets/Game/Scripts/Editor/Skirmish/'
 overrides={prefix+'SkirmishBakedActorSnapshotExporter.S003Metadata.cs':evidence/'SkirmishBakedActorSnapshotExporter.S003Metadata.cs',prefix+'SkirmishBakedActorSnapshotExporter.S003Metadata.cs.meta':evidence/'s003-metadata-helper-r3.cs.meta',prefix+'SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs':evidence/'SkirmishBakedActorSnapshotExporter.TwoSeedMetadata.cs'}
 verified={}
 for rel,want in capture['source_pins'].items():
  q=Path(rel)
  if q.is_absolute():
   try:q=q.relative_to(Path(c['source_root']))
   except ValueError:raise ValueError('S003 source pin escapes root')
  if '..' in q.parts:raise ValueError('S003 source pin escapes root')
  p=overrides.get(rel,root/q);info=p.lstat()
  if not stat.S_ISREG(info.st_mode) or any(x.is_symlink() for x in [p,*p.parents]) or info.st_size>64*1024*1024 or hashlib.sha256(p.read_bytes()).hexdigest()!=want:raise ValueError('S003 source pin mismatch: '+rel)
  if str(p) in verified and verified[str(p)]!=want:raise ValueError('Conflicting S003 source pin')
  verified[str(p)]=want
 for rel,want in graph['source_pins'].items():
  historical=Path(rel)
  if not historical.is_absolute() or '..' in historical.parts:raise ValueError('Canonical S003 graph pin required')
  try:relative=historical.relative_to(Path(c['source_root']))
  except ValueError:raise ValueError('S003 graph pin escapes source root')
  p=root/relative;info=p.lstat()
  if not stat.S_ISREG(info.st_mode) or any(x.is_symlink() for x in [p,*p.parents]) or hashlib.sha256(p.read_bytes()).hexdigest()!=want:raise ValueError('S003 graph pin mismatch: '+rel)
  if str(p) in verified and verified[str(p)]!=want:raise ValueError('Conflicting S003 graph pin')
  verified[str(p)]=want
 return target,verified
_EXTERNAL=None
def bind_configuration_qualification(path):
 global _EXTERNAL
 if _EXTERNAL is not None:raise ValueError('Explicit configuration qualification binds once')
 f=Path(path)
 def read(d,limit):
  if type(d)is not dict or set(d)!={'path','sha256'}or type(d['path'])is not str or not Path(d['path']).is_absolute()or type(d['sha256'])is not str or not re.fullmatch('[0-9a-f]{64}',d['sha256']):raise ValueError('Exact absolute qualification descriptor required')
  q=Path(d['path']);info=q.lstat()
  if not stat.S_ISREG(info.st_mode)or any(x.is_symlink()for x in[q,*q.parents])or not 0<info.st_size<=limit:raise ValueError('Bounded canonical qualification file required')
  raw=q.read_bytes()
  if hashlib.sha256(raw).hexdigest()!=d['sha256']:raise ValueError('Qualification input changed')
  return json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 if not f.is_absolute():raise ValueError('Absolute qualification path required')
 descriptor={'path':str(f),'sha256':hashlib.sha256(f.read_bytes()).hexdigest()};q=read(descriptor,1024*1024)
 fields={'schema_version','kind','runtime_backend','compiler_context','scope','native_context_qualified','mission_ready'}
 if type(q)is dict and q.get('kind')=='S003IndependentCurrentSourceEvidencePackage':
  root=Path(__file__).resolve().parents[2];expected={'schema_version','kind','scope','source_chain_audit','independent_prior_audit','source_chain_auditor','prefab_movement_fixture','builder','target','runtime_admission','native_context_qualified','source_fit_certified','mission_ready'}
  target={'catalog_id':'S003','size':'Standard','difficulty':'Regular','seed':104732,'setup_hash':2394754492}
  if set(q)!=expected or q['schema_version']!=1 or q['scope']!='SourceEvidenceOnly_NoRuntimeAdmission' or q['target']!=target or any(q.get(k)is not False for k in ('runtime_admission','native_context_qualified','source_fit_certified','mission_ready')):raise ValueError('Exact independent S003 source-only package required')
  if not f.resolve(strict=True).is_relative_to((root/'T Logs/SkirmishProduction').resolve(strict=True)):raise ValueError('Repository-owned independent S003 evidence required')
  auditor=root/'Tools/CI/SkirmishSourceSimulation/Recipes/audit_s003_independent_source_chain.py';builder=root/'Tools/CI/SkirmishSourceSimulation/Recipes/build_s003_independent_qualification.py'
  for name,path in [('source_chain_auditor',auditor),('builder',builder)]:
   d=q[name]
   if type(d)is not dict or set(d)!={'path','sha256'} or d['path']!=str(path) or hashlib.sha256(path.read_bytes()).hexdigest()!=d['sha256']:raise ValueError('Exact current independent S003 evidence owner required')
  audit=read(q['source_chain_audit'],16*1024*1024);prior=read(q['independent_prior_audit'],16*1024*1024);fixture=read(q['prefab_movement_fixture'],16*1024*1024)
  if audit!=prior or audit.get('kind')!='S003IndependentSourceEvidenceChainAudit' or audit.get('status')!='PassedSourceOnlyEvidence' or audit.get('setup_hash')!=2394754492 or any(audit.get(k)is not False for k in ('runtime_admission','native_context_qualified','source_fit_certified','mission_ready')):raise ValueError('Reproducible independent S003 source evidence required')
  if fixture.get('status')!='PassedDisposableS003ImporterFixture' or fixture.get('movement_source_projection_checked') is not True or fixture.get('mission_ready') is not False or not fixture.get('compiled_importer','').endswith('/305_GeneratedReferenceTemplateImporter.s003.cs'):raise ValueError('Actual S003 source importer fixture required')
  for path,digest in fixture.get('source_pins',{}).items():
   if not Path(path).is_file() or hashlib.sha256(Path(path).read_bytes()).hexdigest()!=digest:raise ValueError('Independent S003 importer fixture source changed')
  with tempfile.TemporaryDirectory(prefix='s003-source-audit-') as tmp:
   regenerated=Path(tmp)/'audit.json';cmd=[sys.executable,'-B',str(auditor),'--output',str(regenerated),'--compiler',audit['compiler']['path'],'--source-build',audit['source_build']['path']]
   result=subprocess.run(cmd,capture_output=True,text=True,timeout=600)
   if result.returncode or json.loads(regenerated.read_bytes(),object_pairs_hook=_pairs,parse_constant=_nonfinite)!=audit:raise ValueError('Independent S003 source evidence reconstruction failed')
  pins={descriptor['path']:descriptor['sha256']}
  def add(value):
   if type(value)is dict:
    if set(value)=={'path','sha256'} and type(value['path'])is str and type(value['sha256'])is str:
     p=Path(value['path']);raw=p.read_bytes()
     if hashlib.sha256(raw).hexdigest()!=value['sha256']:raise ValueError('Independent S003 artifact changed')
     pins[str(p.resolve(strict=True))]=value['sha256']
    else:
     for item in value.values():add(item)
   elif type(value)is list:
    for item in value:add(item)
  add(q);add(audit)
  for section in ('runtime_graph','native_capture','source_build'):
   content=read(audit[section],64*1024*1024)
   for name,digest in content.get('source_pins',content.get('source_and_reference_pins',{})).items():
    p=Path(name);pins[str((p if p.is_absolute() else root/p).resolve(strict=True))]=digest
  pins.update(fixture['source_pins'])
  row={'mission_id':'S003','difficulty_id':2,'size_id':1,'seeds':[104732],'runtime_backend':'TypedGroundS026V1','compiler_context':descriptor}
  _EXTERNAL={'descriptor':descriptor,'row':row,'pins':pins,'s003_independent_descriptor':True};return
 if type(q)is dict and q.get('kind')=='S003RefreshedSourceOnlyQualificationEvidence':
  root=Path(__file__).resolve().parents[2]
  actual=f.resolve(strict=True)
  evidence_root=(root/'T Logs/SkirmishProduction').resolve(strict=True)
  if not actual.is_relative_to(evidence_root):raise ValueError('Repository-owned S003 evidence required')
  audit_descriptor=q.get('evidence_audit')
  audit=read(audit_descriptor,16*1024*1024)
  audit_path=Path(audit_descriptor['path'])
  if not audit_path.resolve(strict=True).is_relative_to(evidence_root):raise ValueError('Repository-owned S003 audit required')
  recipe=root/'Tools/CI/SkirmishSourceSimulation/Recipes'
  sys.path.insert(0,str(recipe))
  try:
   from build_s003_refreshed_qualification import construct
   if q!=construct(root,audit,audit_path):raise ValueError('Refreshed S003 evidence reconstruction mismatch')
  finally:sys.path.pop(0)
  pins={descriptor['path']:descriptor['sha256']}
  for section in ('runtime_graph','native_capture','source_build'):
   item=q[section]; pins[item['path']]=item['sha256']
   contents=read(item,64*1024*1024)
   for name,digest in contents.get('source_pins',contents.get('source_and_reference_pins',{})).items():
    key=str((Path(name) if Path(name).is_absolute() else root/name).resolve(strict=True))
    if key in pins and pins[key]!=digest:raise ValueError('Conflicting refreshed S003 source pin')
    pins[key]=digest
  for name in ('compiler','capture_receipt','projection_receipt','evidence_audit','evidence_auditor','descriptor_builder'):
   item=q[name];read(item,16*1024*1024) if name not in ('evidence_auditor','descriptor_builder') else None
   if hashlib.sha256(Path(item['path']).read_bytes()).hexdigest()!=item['sha256']:raise ValueError('Refreshed S003 evidence changed')
   pins[item['path']]=item['sha256']
  for item in q['factory_artifacts'].values():
   read(item,16*1024*1024);pins[item['path']]=item['sha256']
  row={'mission_id':'S003','difficulty_id':2,'size_id':1,'seeds':[104732],'runtime_backend':'TypedGroundS026V1','compiler_context':descriptor}
  _EXTERNAL={'descriptor':descriptor,'row':row,'pins':pins,'s003_refreshed_descriptor':True};return
 if type(q)is dict and q.get('kind')=='ExperimentalS003CurrentSourceQualificationDescriptor':
  target,pins,_=_s003_current_qualification(f)
  row={'mission_id':'S003','difficulty_id':2,'size_id':1,'seeds':[104732],'runtime_backend':'TypedGroundS026V1','compiler_context':q['compiler_context']}
  _EXTERNAL={'descriptor':descriptor,'row':row,'pins':pins,'s003_current_descriptor':True};return
 if type(q)is dict and q.get('kind')=='ExperimentalS003SourceQualification':
  if set(q)!=fields or q['schema_version']!=1 or q['runtime_backend']!='TypedGroundS026V1' or q['scope']!='OriginalS003CompilerAndFactories_SourceOnly' or q['native_context_qualified'] is not False or q['mission_ready'] is not False:raise ValueError('Exact S003 source qualification required')
  context=read(q['compiler_context'],4*1024*1024);_,s003pins=_validated_s003_context(context)
  row={'mission_id':'S003','difficulty_id':2,'size_id':1,'seeds':[104732],'runtime_backend':q['runtime_backend'],'compiler_context':q['compiler_context']}
  for name,digest in {descriptor['path']:descriptor['sha256'],q['compiler_context']['path']:q['compiler_context']['sha256'],context['native_capture']['path']:context['native_capture']['sha256'],context['source_graph']['path']:context['source_graph']['sha256']}.items():
   if name in s003pins and s003pins[name]!=digest:raise ValueError('Conflicting S003 qualification pin')
   s003pins[name]=digest
  _EXTERNAL={'descriptor':descriptor,'row':row,'pins':s003pins};return
 if type(q)is not dict or set(q)!=fields or type(q['schema_version'])is not int or q['schema_version']!=1 or q['kind']!='ExperimentalGroundConfigurationQualification'or q['runtime_backend']!='TypedGroundS026V1'or q['scope']!='OriginalCompilerAndQualifiedFactoryInputs_SourceOnly'or q['native_context_qualified']is not False or q['mission_ready']is not False:raise ValueError('Exact experimental source qualification required')
 context=read(q['compiler_context'],4*1024*1024);t=_validated_context_target(context)
 if type(context.get('schema_version'))is not int or not ((context['schema_version']==1 and context.get('kind') in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS049DirectNativeFactoryContext','OriginalS049NativeMetadataEpochContext')) or (context['schema_version']==2 and context.get('kind')=='OriginalS003DerivedNativeFactoryContext')) or (context.get('kind') in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS003DerivedNativeFactoryContext') and context.get('native_S050_capture') is not False) or type(t.get('catalog_id'))is not str or not re.fullmatch(r'S[0-9]{3}',t['catalog_id'])or not 1<=int(t['catalog_id'][1:])<=120 or t.get('definition_id')!='skirmish.'+t['catalog_id'].lower()or t.get('size')not in{'Standard','War','LargeWar'}or t.get('difficulty') not in {'Recruit','Regular','Veteran','Commander'}or type(t.get('seed'))is not int or not -2147483648<=t['seed']<=2147483647:raise ValueError('Finite original compiler target required')
 if context.get('kind')=='OriginalS003DerivedNativeFactoryContext':
  # The current derived template and disposable-import evidence covers this exact target only.
  covered={'catalog_id':'S075','definition_id':'skirmish.s075','content_version':2,'seed':104804,'size':'Standard','difficulty':'Regular','setup_hash':1280564676,'operation_map_id':'opmap.skirmish.industrial_basin','source_content_hash':'cbbaa8366cbfb5e0ad9bc6bf70a98b48503df10e3d0c5c45757295af841a7a6d','source_fingerprint':'3f329ea016436f3104e7f69e975d871f842381e0b5654ff3f483256e786c3c96','layout_fingerprint':'0777cae578e8f75106fd2f55478c8688498d3473ef8691113fd56c41031dc18a','layout_binding_fingerprint':'cdf0f891866cbf3be71b819e8f9db5d59337514dd0c22ee8a1aef765e01eda6c','mission_logical_binding_fingerprint':'a032ea05c372d21dbba6b7cee6b7f866e4bff81353378d74ccd5bebb2aa4e8bb'}
  if t!=covered:raise ValueError('S003-derived source evidence covers only the exact S075 target')
 row={'mission_id':t['catalog_id'],'difficulty_id':{'Recruit':1,'Regular':2,'Veteran':3,'Commander':4}[t['difficulty']],'size_id':{'Standard':1,'War':2,'LargeWar':3}[t['size']],'seeds':[t['seed']],'runtime_backend':q['runtime_backend'],'compiler_context':q['compiler_context']}
 _EXTERNAL={'descriptor':descriptor,'row':row,'pins':{descriptor['path']:descriptor['sha256'],q['compiler_context']['path']:q['compiler_context']['sha256']},'s003_derived_factory':context.get('kind')=='OriginalS003DerivedNativeFactoryContext'}
def configuration_qualification():return None if _EXTERNAL is None else dict(_EXTERNAL['descriptor'])
def uses_s003_derived_factory(mission):
 return _EXTERNAL is not None and _EXTERNAL.get('s003_derived_factory') is True and _EXTERNAL['row']['mission_id']==mission
def require_initial_qualification_pair(initial):
 has_context='metadata_compiler_context'in initial;has_qualification='configuration_qualification'in initial
 if has_context!=has_qualification:raise ValueError('Configuration qualification and compiler context must be present together')
 if not has_context and configuration_qualification()is not None:raise ValueError('Orphan externally bound configuration qualification')
 if has_context and(type(initial['metadata_compiler_context'])is not dict or type(initial['configuration_qualification'])is not dict):raise ValueError('Exact configuration qualification/context descriptors required')
 if has_context and _EXTERNAL is not None:
  row=_EXTERNAL['row'];selection=initial.get('selection')
  if initial['configuration_qualification']!=_EXTERNAL['descriptor'] or initial['metadata_compiler_context']!=row['compiler_context'] or type(selection)is not dict or initial.get('mission_id')!=row['mission_id'] or type(selection.get('difficulty_id'))is not int or selection['difficulty_id']!=row['difficulty_id'] or type(selection.get('size_id'))is not int or selection['size_id']!=row['size_id'] or type(selection.get('seed'))is not int or selection['seed'] not in row['seeds']:
   raise ValueError('Initialization differs from explicitly bound source context')
def runtime_configuration_mode(mission):return 'ExternalQualifiedGroundV1'if runtime_backend(mission)=='TypedGroundS026V1'else'LegacyS073BundleV1'if runtime_backend(mission)=='LegacyS073OwnerV1'else'LegacyBoundProfilesV1'
def load_profiles():
 owned.pins();raw=PATH.read_bytes()
 if raw!=PROFILE_RAW:raise ValueError('Admission JSON changed since owned module execution')
 d=json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 if set(d)!={'schema_version','numeric_runtime','profiles'} or type(d['schema_version'])is not int or d['schema_version']!=1 or d['numeric_runtime']!='InstalledMono':raise ValueError('Unsupported admission schema/runtime')
 if type(d['profiles'])is not list or not 0<len(d['profiles'])<=16:raise ValueError('Finite profiles required')
 if _EXTERNAL is not None:
  external=dict(_EXTERNAL['row'])
  # An explicit source-only S026 context may target its already admitted
  # tuple. Select that exact context for this invocation; never add another
  # seed or change the ordinary profile when no context was supplied.
  matches=[i for i,r in enumerate(d['profiles']) if (r['mission_id'],r['difficulty_id'],r['size_id'])==(external['mission_id'],external['difficulty_id'],external['size_id'])]
  if matches and external['mission_id']=='S026' and len(matches)==1 and external['seeds']==d['profiles'][matches[0]]['seeds']:
   d['profiles'][matches[0]]=external
  else:d['profiles'].append(external)
 seen=set()
 for r in d['profiles']:
  required={'mission_id','difficulty_id','size_id','seeds'}
  if type(r)is not dict:raise ValueError('Exact profile record required')
  if set(r)==required:
    if r['mission_id']not in ('S002','S026') or type(r['difficulty_id'])is not int or r['difficulty_id']!=2 or type(r['size_id'])is not int or r['size_id']not in ((1,)if r['mission_id']=='S002'else(1,2,3)):raise ValueError('Unqualified mission selection')
    if type(r['seeds'])is not list or not r['seeds'] or any(type(x)is not int or x not in ((104731,1)if r['mission_id']=='S002'else(104755,)) for x in r['seeds']) or len(set(r['seeds']))!=len(r['seeds']):raise ValueError('Unqualified seed')
  elif set(r)==required|{'runtime_backend','scope'}:
   if (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'],r['runtime_backend'],r['scope'])!=('S073',2,1,[104729],'LegacyS073OwnerV1','LegacyStartupAndQueueOnly'):raise ValueError('Exact bounded S073 legacy-owner profile required')
  elif set(r)==required|{'runtime_backend','compiler_context'}:
   if r['runtime_backend']!='TypedGroundS026V1' or type(r['mission_id'])is not str or not re.fullmatch(r'S[0-9]{3}',r['mission_id']):raise ValueError('Unsupported existing source family')
   descriptor=r['compiler_context']
   if type(descriptor)is not dict or set(descriptor)!={'path','sha256'} or type(descriptor['path'])is not str or not Path(descriptor['path']).is_absolute() or type(descriptor['sha256'])is not str or not re.fullmatch('[0-9a-f]{64}',descriptor['sha256']):raise ValueError('Exact reviewed compiler-context descriptor required')
   path=Path(descriptor['path']);info=path.lstat()
   if not stat.S_ISREG(info.st_mode)or not 0<info.st_size<=4*1024*1024:raise ValueError('Bounded regular compiler context required')
   raw_context=path.read_bytes()
   if hashlib.sha256(raw_context).hexdigest()!=descriptor['sha256']:raise ValueError('Compiler context changed')
   context=json.loads(raw_context,object_pairs_hook=_pairs,parse_constant=_nonfinite)
   if context.get('kind')=='S003IndependentCurrentSourceEvidencePackage':
    if _EXTERNAL is None or _EXTERNAL.get('s003_independent_descriptor') is not True or descriptor!=_EXTERNAL['descriptor'] or (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'])!=('S003',2,1,[104732]):raise ValueError('Independent S003 source-only descriptor must be explicitly bound')
    key=(r['mission_id'],r['difficulty_id'],r['size_id'])
    if key in seen:raise ValueError('Duplicate profile')
    seen.add(key)
    continue
   if context.get('kind')=='S003RefreshedSourceOnlyQualificationEvidence':
    if _EXTERNAL is None or _EXTERNAL.get('s003_refreshed_descriptor') is not True or descriptor!=_EXTERNAL['descriptor'] or (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'])!=('S003',2,1,[104732]):raise ValueError('Refreshed S003 source-only descriptor must be explicitly bound')
    key=(r['mission_id'],r['difficulty_id'],r['size_id'])
    if key in seen:raise ValueError('Duplicate profile')
    seen.add(key)
    continue
   if context.get('kind')=='OriginalS003SourceOnlyFactoryContext':
    _validated_s003_context(context)
    if (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'])!=('S003',2,1,[104732]):raise ValueError('S003 profile identity mismatch')
    key=(r['mission_id'],r['difficulty_id'],r['size_id'])
    if key in seen:raise ValueError('Duplicate profile')
    seen.add(key)
    continue
   if context.get('kind')=='OriginalS003CurrentSourceOnlyFactoryContext':
    if _EXTERNAL is None or _EXTERNAL.get('s003_current_descriptor') is not True:raise ValueError('Current S003 descriptor must be explicitly bound')
    target,_,current=_s003_current_qualification(_EXTERNAL['descriptor']['path'])
    if context!=current or target.get('catalog_id')!='S003' or (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'])!=('S003',2,1,[104732]):raise ValueError('Current S003 profile differs from the exact bound descriptor')
    key=(r['mission_id'],r['difficulty_id'],r['size_id'])
    if key in seen:raise ValueError('Duplicate profile')
    seen.add(key)
    continue
   target=_validated_context_target(context)
   if context.get('kind')=='OriginalS003DerivedNativeFactoryContext' and (_EXTERNAL is None or _EXTERNAL.get('s003_derived_factory') is not True or descriptor!=_EXTERNAL['row']['compiler_context'] or r!=_EXTERNAL['row']):raise ValueError('S003-derived target must be explicitly bound to its exact source qualification')
   if not ((context.get('schema_version')==1 and context.get('kind') in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS049DirectNativeFactoryContext','OriginalS049NativeMetadataEpochContext')) or (context.get('schema_version')==2 and context.get('kind')=='OriginalS003DerivedNativeFactoryContext')) or (context.get('kind') in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS003DerivedNativeFactoryContext') and context.get('native_S050_capture') is not False) or target.get('catalog_id')!=r['mission_id']or type(r['difficulty_id'])is not int or type(r['size_id'])is not int or r['difficulty_id']not in(1,2,3,4) or r['size_id']not in(1,2,3) or target.get('size')!={1:'Standard',2:'War',3:'LargeWar'}[r['size_id']] or target.get('difficulty')!={1:'Recruit',2:'Regular',3:'Veteran',4:'Commander'}[r['difficulty_id']] or r['seeds']!=[target.get('seed')]or type(target.get('seed'))is not int:raise ValueError('Profile differs from actual finite compiler context')
  else:raise ValueError('Exact profile keys required')
  key=(r['mission_id'],r['difficulty_id'],r['size_id'])
  if key in seen:raise ValueError('Duplicate profile')
  seen.add(key)
 pins={str(PATH):PROFILE_SHA,str(Path(__file__).resolve()):MODULE_SHA}
 for r in d['profiles']:
  if 'compiler_context'in r:pins[str(Path(r['compiler_context']['path']).resolve())]=r['compiler_context']['sha256']
 if _EXTERNAL is not None:pins.update(_EXTERNAL['pins'])
 return d,pins
def runtime_backend(mission):
 d,_=load_profiles();rows=[r for r in d['profiles']if r['mission_id']==mission]
 if not rows:raise ValueError('No reviewed source profile')
 kinds={r.get('runtime_backend',{'S002':'LegacyGroundS002V1','S026':'TypedGroundS026V1'}.get(mission))for r in rows}
 if len(kinds)!=1 or not kinds<= {'LegacyGroundS002V1','TypedGroundS026V1','LegacyS073OwnerV1'}:raise ValueError('Unsupported/ambiguous existing backend family')
 return next(iter(kinds))
def require_profile(mission,seed,difficulty_id=2,size_id=1):
 d,pins=load_profiles()
 bound=derived()
 if bound is not None and (mission,seed,difficulty_id,size_id)==("S002",bound[0]["seed"],2,1):pins[bound[1]["path"]]=bound[1]["sha256"];return pins
 if type(seed)is not int or type(difficulty_id)is not int or type(size_id)is not int or not any((mission,difficulty_id,size_id)==(r['mission_id'],r['difficulty_id'],r['size_id']) and seed in r['seeds'] for r in d['profiles']):raise ValueError('Unqualified source execution profile')
 return pins
def generate_guard(compiled_mission="S002"):
 d,pins=load_profiles();clauses=[]
 selected=[r for r in d['profiles']if r['mission_id']==compiled_mission]
 if not selected:raise ValueError('No qualified source profile')
 backends={r.get('runtime_backend',{'S002':'LegacyGroundS002V1','S026':'TypedGroundS026V1'}.get(r['mission_id']))for r in selected}
 if len(backends)!=1 or not backends<= {'LegacyGroundS002V1','TypedGroundS026V1','LegacyS073OwnerV1'}:raise ValueError('Ambiguous/unsupported existing backend family')
 backend=next(iter(backends))
 bound=derived() if compiled_mission=="S002" else None
 for r in d['profiles']:
  if (backend=='TypedGroundS026V1' and r['mission_id'] not in ('S026',compiled_mission)) or (backend!='TypedGroundS026V1' and r['mission_id']!=compiled_mission):continue
  clauses.append('(mission=='+json.dumps(r['mission_id'])+' && difficulty=='+str(r['difficulty_id'])+' && size=='+str(r['size_id'])+' && ('+' || '.join('seed=='+str(s) for s in r['seeds'])+'))')
 if bound:clauses.append('(mission==\"S002\" && difficulty==2 && size==1)')
 if PATH.read_bytes()!=PROFILE_RAW:raise ValueError('Admission JSON changed during guard generation')
 owned.pins()
 preflight='internal static Newtonsoft.Json.Linq.JObject Preflight(Newtonsoft.Json.Linq.JObject initial,Newtonsoft.Json.Linq.JObject run){'
 if backend=='TypedGroundS026V1':
  preflight+='using var roads=new Game.Runtime.SourceFreshInitialRoadOwner(run);roads.Preflight();var compiled=Game.Runtime.AutonomousWorld.ReadCompiledInitialization(initial);var freshInputBytes=Game.Runtime.AutonomousWorld.Descriptor(run["initialization"],4*1024*1024);var freshAuthority=Game.Runtime.FreshSourceStorageLifetimeAuthority.FromValidatedSource(initial,(string)run["initialization"]["sha256"],freshInputBytes,compiled.definition,compiled.setup,compiled.manifest,compiled.matrix);var selectedRuntimeFacts=Game.Runtime.SourceDeclaredRuntimeInputs.Preflight(initial,run,compiled.graph,compiled.assets,compiled.definition,compiled.setup,compiled.canonicalCatalog);var compatibility=Game.Runtime.SourceMetadataCompatibility.Load(initial,compiled.setup,(string)run["source_repository_root"],(string)run["repository_root"]);compatibility.VerifyUnchanged();Newtonsoft.Json.Linq.JObject expectedCaptureGraph=null;if(compiled.setup.CatalogId=="S003"&&initial["metadata_compiler_context"]!=null){var validatedTemplateContext=Game.Runtime.SourceConfigurationQualification.Context(initial);expectedCaptureGraph=validatedTemplateContext["source_graph"]as Newtonsoft.Json.Linq.JObject;if(expectedCaptureGraph==null)throw new System.IO.InvalidDataException("Validated S003 capture graph required for template preflight");}string binaryDirectory;Newtonsoft.Json.Linq.JObject templateDomain;if(compiled.setup.CatalogId=="S074")templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,(string)run["source_repository_root"],initial);else{var inactive=initial["placement_domain"]?["s026_inactive_diagnostic"] as Newtonsoft.Json.Linq.JObject;templateDomain=inactive!=null?Game.Runtime.GeneratedReferenceTemplateView.ReadS026Diagnostic((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],(string)inactive["source_root"],out binaryDirectory):Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,expectedCaptureGraph);}var units=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Game.Runtime.AutonomousWorld.Descriptor(initial["unit_production_metadata"],2*1024*1024)));var keys=new System.Collections.Generic.List<string>();foreach(var unit in(Newtonsoft.Json.Linq.JArray)units["units"])keys.Add((string)unit["key"]);var templatePreflight=Game.Runtime.GeneratedReferenceTemplateImporter.Preflight(templateDomain,binaryDirectory,keys);if((compiled.setup.CatalogId=="S003"?((int)templatePreflight["roots"]!=19||(int)templatePreflight["entities"]!=647||(int)templatePreflight["decoded_components"]!=3965||(int)templatePreflight["omitted_presentation_components"]!=26):((int)templatePreflight["roots"]!=16||(int)templatePreflight["entities"]!=528||(int)templatePreflight["decoded_components"]!=3250||(compiled.setup.CatalogId=="S074"&&((int)templatePreflight["omitted_presentation_components"]!=20||(int)templatePreflight["unsupported_unconsumed_values"]!=23))))||(bool)templatePreflight["world_allocated"]||(int)templatePreflight["entity_identities_allocated"]!=0)throw new System.IO.InvalidDataException("Qualified paired template read domain changed");Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));Game.Configs.SourceEnglishCommandText.Bind(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024));return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["compiler_hash"]=Game.Configs.SkirmishSetupCompiler.ComputeHash(compiled.setup,compiled.setup.Seed),["mission"]=compiled.setup.CatalogId,["metadata_source_and_current_origins_checked"]=true,["fresh_storage_authority_created_unclaimed"]=freshAuthority!=null,["fresh_storage_input_sha256"]=run["initialization"]["sha256"],["paired_template_preflight"]=templatePreflight,["selected_runtime_map_preflight"]=selectedRuntimeFacts};}'
 else:preflight+='return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["scope"]="S002 profile/descriptor boundary; existing source-smoke-data owns native input qualification"};}'

 if backend=='TypedGroundS026V1' and _EXTERNAL is not None and _EXTERNAL.get('s003_derived_factory') is True and _EXTERNAL['row']['mission_id']==compiled_mission:
  start=preflight.index('Newtonsoft.Json.Linq.JObject expectedCaptureGraph=null;')
  end=preflight.index('var units=',start)
  derived_dispatch='var validatedTemplateContext=Game.Runtime.SourceConfigurationQualification.Context(initial);if((string)validatedTemplateContext["kind"]!="OriginalS003DerivedNativeFactoryContext")throw new System.IO.InvalidDataException("Exact S003-derived source context required");var historicalCapture=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Game.Runtime.AutonomousWorld.Descriptor(validatedTemplateContext["native_factory_reference"],64*1024*1024)));var expectedCaptureGraph=historicalCapture["source_graph"] as Newtonsoft.Json.Linq.JObject;if(expectedCaptureGraph==null)throw new System.IO.InvalidDataException("Pinned historical template graph required");string binaryDirectory;var templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,expectedCaptureGraph,initial,compiled.setup,validatedTemplateContext,(string)run["source_repository_root"]);'
  preflight=preflight[:start]+derived_dispatch+preflight[end:]
  count_anchor='compiled.setup.CatalogId=="S003"?((int)templatePreflight["roots"]!=19'
  if preflight.count(count_anchor)!=1:raise ValueError('Exact historical template count gate required')
  preflight=preflight.replace(count_anchor,'true?((int)templatePreflight["roots"]!=19',1)

 if compiled_mission=='S026':
  if preflight.count('Game.Runtime.GeneratedReferenceTemplateView.ReadS026Diagnostic')!=1 or preflight.count('Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory,expectedCaptureGraph)')!=1:
   raise ValueError('Exact S026 inactive/default template dispatch required')
  original_death='Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));'
  diagnostic_death='if(initial["placement_domain"]?["s026_inactive_diagnostic"] is Newtonsoft.Json.Linq.JObject)Game.Runtime.SourceNativeDeathReadModel.LoadS026Diagnostic((Newtonsoft.Json.Linq.JObject)initial["death_mechanical_inputs"]);else Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));'
  if preflight.count(original_death)!=1:raise ValueError('Exact source death preflight anchor required')
  preflight=preflight.replace(original_death,diagnostic_death)
 if compiled_mission=='S074':
  original_death='Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));'
  diagnostic_death='if(SourceSimulationProgram.IsNoCameraDiagnostic(run,initial))Game.Runtime.SourceNativeDeathReadModel.LoadS074NoCameraDiagnostic((Newtonsoft.Json.Linq.JObject)initial["death_mechanical_inputs"]);else Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));'
  if preflight.count(original_death)!=1:raise ValueError('Exact S074 no-camera death preflight anchor required')
  preflight=preflight.replace(original_death,diagnostic_death)
 runtime_require='if((initial.Property(\"configuration_qualification\")!=null)!=(initial.Property(\"metadata_compiler_context\")!=null))throw new System.IO.InvalidDataException(\"Configuration qualification and compiler context must be present together\");if(initial[\"metadata_compiler_context\"]!=null){Game.Runtime.SourceConfigurationQualification.RequireInitialization(initial,JsonInteger(selection[\"seed\"],\"selection.seed\"),JsonInteger(selection[\"difficulty_id\"],\"selection.difficulty_id\"),JsonInteger(selection[\"size_id\"],\"selection.size_id\"));return;}'if backend=='TypedGroundS026V1'else'if(initial.Property(\"configuration_qualification\")!=null||initial.Property(\"metadata_compiler_context\")!=null)throw new System.IO.InvalidDataException(\"External configuration requires supported ground backend\");'
 if bound:runtime_require+=owned.require("s002_derived_bootstrap").generated_guard(*bound)
 return 'using System; internal static class SourceAdmittedProfiles { internal static int JsonInteger(Newtonsoft.Json.Linq.JToken value,string name){if(value==null||value.Type!=Newtonsoft.Json.Linq.JTokenType.Integer)throw new System.IO.InvalidDataException("Integer JSON field required: "+name);long n=(long)value;if(n<int.MinValue||n>int.MaxValue)throw new System.IO.InvalidDataException("Int32 JSON field required: "+name);return(int)n;} internal static void RequireInitialization(Newtonsoft.Json.Linq.JObject initial){if(initial==null||JsonInteger(initial["schema_version"],"initialization.schema_version")!=1||initial["kind"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["kind"]!="GeneratedMissionInitialization"||initial["authority"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["authority"]!="AuthoredConfigAndBakedCampaignMap"||initial["mission_id"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String)throw new System.IO.InvalidDataException("Exact authored initialization envelope required");var selection=initial["selection"]as Newtonsoft.Json.Linq.JObject;if(selection==null||selection.Count!=3||selection["seed"]==null||selection["size_id"]==null||selection["difficulty_id"]==null)throw new System.IO.InvalidDataException("Exact finite selection required");'+runtime_require+'Require((string)initial["mission_id"],JsonInteger(selection["seed"],"selection.seed"),JsonInteger(selection["difficulty_id"],"selection.difficulty_id"),JsonInteger(selection["size_id"],"selection.size_id"));} '+preflight+' internal const string RuntimeConfigurationMode='+json.dumps('ExternalQualifiedGroundV1'if backend=='TypedGroundS026V1'else'LegacyBoundProfilesV1')+'; internal const string Backend='+json.dumps(backend)+'; internal static void RequireBackend(string value){if(value!=Backend)throw new NotSupportedException("Compiled runtime backend mismatch");} internal static void Require(string mission,int seed,int difficulty=2,int size=1) { if(!('+' || '.join(clauses)+')) throw new NotSupportedException("Unqualified source execution profile"); } }\n',pins
