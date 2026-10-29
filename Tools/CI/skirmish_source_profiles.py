"""Finite source execution admission; never changes catalog playability."""
import json,hashlib,re,stat
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
def _validated_context_target(context):
 if isinstance(context,dict) and context.get('kind')=='OriginalS049DirectNativeFactoryContext':return _direct_s049_target(context)
 fields={'schema_version','kind','authority','target_context','native_factory_reference','native_origin','derived_metadata','derived_metadata_key_sha256','unchanged_native_sections_sha256','section_hash_format','native_origin_descriptor','current_factory_source_root','consumed_input_pins','checks','world_allocated','identities_allocated','native_S050_capture','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted'}
 if type(context)is not dict or set(context)!=fields or type(context['schema_version'])is not int or context['schema_version']!=1 or context['kind']!='OriginalCompilerDerivedNativeFactoryContext'or context['authority']!='ActualSourceCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050'or context['section_hash_format']!='NewtonsoftCompactUtf8Sha256'or type(context['identities_allocated'])is not int or context['identities_allocated']!=0:raise ValueError('Exact versioned compiler context required')
 for flag in ['world_allocated','native_S050_capture','source_fit_certified','active_mission_water_qualified','mission_ready','runtime_loader_admitted']:
  if context[flag]is not False:raise ValueError('Explicit honest false compiler-context flag required: '+flag)
 checks={'original_all_role_overlay_fields_raw_equal','original_provider_selection_equal','original_native_origin68_and_assets_exact','current_factory67_package_exact','factory_sections_unchanged'}
 if type(context['checks'])is not dict or set(context['checks'])!=checks or any(v is not True for v in context['checks'].values()):raise ValueError('Exact compiler/factory check declarations required')
 t=context['target_context'];keys={'catalog_id','definition_id','content_version','seed','size','difficulty','setup_hash','operation_map_id','source_content_hash','source_fingerprint','layout_fingerprint','layout_binding_fingerprint','mission_logical_binding_fingerprint'}
 if type(t)is not dict or set(t)!=keys or any(type(t[k])is not str for k in keys-{'seed','content_version','setup_hash'})or any(type(t[k])is not int for k in ['seed','content_version','setup_hash']):raise ValueError('Exact compiler target fields/types required')
 if not re.fullmatch(r'S[0-9]{3}',t['catalog_id'])or not 1<=int(t['catalog_id'][1:])<=120 or t['definition_id']!='skirmish.'+t['catalog_id'].lower()or not -2147483648<=t['seed']<=2147483647 or not 1<=t['content_version']<=2147483647 or not 0<=t['setup_hash']<=4294967295 or t['size']not in{'Standard','War','LargeWar'}or t['difficulty']!='Regular'or not 0<len(t['operation_map_id'])<=256 or any(not re.fullmatch('[0-9a-f]{64}',t[k])for k in ['source_content_hash','source_fingerprint','layout_fingerprint','layout_binding_fingerprint','mission_logical_binding_fingerprint']):raise ValueError('Finite canonical compiler target required')
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
 if type(context.get('schema_version'))is not int or context['schema_version']!=1 or context.get('kind') not in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS049DirectNativeFactoryContext') or (context.get('kind')=='OriginalCompilerDerivedNativeFactoryContext' and context.get('native_S050_capture') is not False) or type(t.get('catalog_id'))is not str or not re.fullmatch(r'S[0-9]{3}',t['catalog_id'])or not 1<=int(t['catalog_id'][1:])<=120 or t.get('definition_id')!='skirmish.'+t['catalog_id'].lower()or t.get('size')not in{'Standard','War','LargeWar'}or t.get('difficulty')!='Regular'or type(t.get('seed'))is not int or not -2147483648<=t['seed']<=2147483647:raise ValueError('Finite original compiler target required')
 row={'mission_id':t['catalog_id'],'difficulty_id':2,'size_id':{'Standard':1,'War':2,'LargeWar':3}[t['size']],'seeds':[t['seed']],'runtime_backend':q['runtime_backend'],'compiler_context':q['compiler_context']}
 _EXTERNAL={'descriptor':descriptor,'row':row,'pins':{descriptor['path']:descriptor['sha256'],q['compiler_context']['path']:q['compiler_context']['sha256']}}
def configuration_qualification():return None if _EXTERNAL is None else dict(_EXTERNAL['descriptor'])
def require_initial_qualification_pair(initial):
 has_context='metadata_compiler_context'in initial;has_qualification='configuration_qualification'in initial
 if has_context!=has_qualification:raise ValueError('Configuration qualification and compiler context must be present together')
 if not has_context and configuration_qualification()is not None:raise ValueError('Orphan externally bound configuration qualification')
 if has_context and(type(initial['metadata_compiler_context'])is not dict or type(initial['configuration_qualification'])is not dict):raise ValueError('Exact configuration qualification/context descriptors required')
def runtime_configuration_mode(mission):return 'ExternalQualifiedGroundV1'if runtime_backend(mission)=='TypedGroundS026V1'else'LegacyBoundProfilesV1'
def load_profiles():
 owned.pins();raw=PATH.read_bytes()
 if raw!=PROFILE_RAW:raise ValueError('Admission JSON changed since owned module execution')
 d=json.loads(raw,object_pairs_hook=_pairs,parse_constant=_nonfinite)
 if set(d)!={'schema_version','numeric_runtime','profiles'} or type(d['schema_version'])is not int or d['schema_version']!=1 or d['numeric_runtime']!='InstalledMono':raise ValueError('Unsupported admission schema/runtime')
 if type(d['profiles'])is not list or not 0<len(d['profiles'])<=16:raise ValueError('Finite profiles required')
 if _EXTERNAL is not None:d['profiles'].append(dict(_EXTERNAL['row']))
 seen=set()
 for r in d['profiles']:
  required={'mission_id','difficulty_id','size_id','seeds'}
  if type(r)is not dict:raise ValueError('Exact profile record required')
  if set(r)==required:
    if r['mission_id']not in ('S002','S026') or type(r['difficulty_id'])is not int or r['difficulty_id']!=2 or type(r['size_id'])is not int or r['size_id']not in ((1,)if r['mission_id']=='S002'else(1,2,3)):raise ValueError('Unqualified mission selection')
    if type(r['seeds'])is not list or not r['seeds'] or any(type(x)is not int or x not in ((104731,1)if r['mission_id']=='S002'else(104755,)) for x in r['seeds']) or len(set(r['seeds']))!=len(r['seeds']):raise ValueError('Unqualified seed')
  elif set(r)==required|{'runtime_backend','compiler_context'}:
   if r['runtime_backend']!='TypedGroundS026V1' or type(r['mission_id'])is not str or not re.fullmatch(r'S[0-9]{3}',r['mission_id']):raise ValueError('Unsupported existing source family')
   descriptor=r['compiler_context']
   if type(descriptor)is not dict or set(descriptor)!={'path','sha256'} or type(descriptor['path'])is not str or not Path(descriptor['path']).is_absolute() or type(descriptor['sha256'])is not str or not re.fullmatch('[0-9a-f]{64}',descriptor['sha256']):raise ValueError('Exact reviewed compiler-context descriptor required')
   path=Path(descriptor['path']);info=path.lstat()
   if not stat.S_ISREG(info.st_mode)or not 0<info.st_size<=4*1024*1024:raise ValueError('Bounded regular compiler context required')
   raw_context=path.read_bytes()
   if hashlib.sha256(raw_context).hexdigest()!=descriptor['sha256']:raise ValueError('Compiler context changed')
   context=json.loads(raw_context,object_pairs_hook=_pairs,parse_constant=_nonfinite)
   if context.get('kind')=='OriginalS003SourceOnlyFactoryContext':
    _validated_s003_context(context)
    if (r['mission_id'],r['difficulty_id'],r['size_id'],r['seeds'])!=('S003',2,1,[104732]):raise ValueError('S003 profile identity mismatch')
    key=(r['mission_id'],r['difficulty_id'],r['size_id'])
    if key in seen:raise ValueError('Duplicate profile')
    seen.add(key)
    continue
   target=_validated_context_target(context)
   if context.get('schema_version')!=1 or context.get('kind') not in ('OriginalCompilerDerivedNativeFactoryContext','OriginalS049DirectNativeFactoryContext') or (context.get('kind')=='OriginalCompilerDerivedNativeFactoryContext' and context.get('native_S050_capture') is not False) or target.get('catalog_id')!=r['mission_id']or type(r['difficulty_id'])is not int or type(r['size_id'])is not int or r['difficulty_id']!=2 or r['size_id']not in(1,2,3) or target.get('size')!={1:'Standard',2:'War',3:'LargeWar'}[r['size_id']] or target.get('difficulty')!='Regular' or r['seeds']!=[target.get('seed')]or type(target.get('seed'))is not int:raise ValueError('Profile differs from actual finite compiler context')
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
 if len(kinds)!=1 or not kinds<= {'LegacyGroundS002V1','TypedGroundS026V1'}:raise ValueError('Unsupported/ambiguous existing backend family')
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
 if len(backends)!=1 or not backends<= {'LegacyGroundS002V1','TypedGroundS026V1'}:raise ValueError('Ambiguous/unsupported existing backend family')
 backend=next(iter(backends))
 bound=derived() if compiled_mission=="S002" else None
 for r in d['profiles']:
  if (backend=='TypedGroundS026V1'and(r.get('runtime_backend')=='TypedGroundS026V1'or r['mission_id']!='S026'))or(backend!='TypedGroundS026V1'and r['mission_id']!=compiled_mission):continue
  clauses.append('(mission=='+json.dumps(r['mission_id'])+' && difficulty=='+str(r['difficulty_id'])+' && size=='+str(r['size_id'])+' && ('+' || '.join('seed=='+str(s) for s in r['seeds'])+'))')
 if bound:clauses.append('(mission==\"S002\" && difficulty==2 && size==1)')
 if PATH.read_bytes()!=PROFILE_RAW:raise ValueError('Admission JSON changed during guard generation')
 owned.pins()
 preflight='internal static Newtonsoft.Json.Linq.JObject Preflight(Newtonsoft.Json.Linq.JObject initial,Newtonsoft.Json.Linq.JObject run){'
 if backend=='TypedGroundS026V1':
  preflight+='using var roads=new Game.Runtime.SourceFreshInitialRoadOwner(run);roads.Preflight();var compiled=Game.Runtime.AutonomousWorld.ReadCompiledInitialization(initial);var freshInputBytes=Game.Runtime.AutonomousWorld.Descriptor(run["initialization"],4*1024*1024);var freshAuthority=Game.Runtime.FreshSourceStorageLifetimeAuthority.FromValidatedSource(initial,(string)run["initialization"]["sha256"],freshInputBytes,compiled.definition,compiled.setup,compiled.manifest,compiled.matrix);var selectedRuntimeFacts=Game.Runtime.SourceDeclaredRuntimeInputs.Preflight(initial,run,compiled.graph,compiled.assets,compiled.definition,compiled.setup,compiled.canonicalCatalog);var compatibility=Game.Runtime.SourceMetadataCompatibility.Load(initial,compiled.setup,(string)run["source_repository_root"],(string)run["repository_root"]);compatibility.VerifyUnchanged();string binaryDirectory;var templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory);var units=Newtonsoft.Json.Linq.JObject.Parse(System.Text.Encoding.UTF8.GetString(Game.Runtime.AutonomousWorld.Descriptor(initial["unit_production_metadata"],2*1024*1024)));var keys=new System.Collections.Generic.List<string>();foreach(var unit in(Newtonsoft.Json.Linq.JArray)units["units"])keys.Add((string)unit["key"]);var templatePreflight=Game.Runtime.GeneratedReferenceTemplateImporter.Preflight(templateDomain,binaryDirectory,keys);if((compiled.setup.CatalogId=="S003"?((int)templatePreflight["roots"]!=19||(int)templatePreflight["entities"]!=647||(int)templatePreflight["decoded_components"]!=3965||(int)templatePreflight["omitted_presentation_components"]!=26):((int)templatePreflight["roots"]!=16||(int)templatePreflight["entities"]!=528||(int)templatePreflight["decoded_components"]!=3250))||(bool)templatePreflight["world_allocated"]||(int)templatePreflight["entity_identities_allocated"]!=0)throw new System.IO.InvalidDataException("Qualified paired template read domain changed");Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));Game.Configs.SourceEnglishCommandText.Bind(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024));return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["compiler_hash"]=Game.Configs.SkirmishSetupCompiler.ComputeHash(compiled.setup,compiled.setup.Seed),["mission"]=compiled.setup.CatalogId,["metadata_source_and_current_origins_checked"]=true,["fresh_storage_authority_created_unclaimed"]=freshAuthority!=null,["fresh_storage_input_sha256"]=run["initialization"]["sha256"],["paired_template_preflight"]=templatePreflight,["selected_runtime_map_preflight"]=selectedRuntimeFacts};}'
  # S026 diagnostic uses the same preflight owners with exact capture identities; qualified paths stay unchanged.
  preflight=preflight.replace('string binaryDirectory;var templateDomain=Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory);','var diagnostic=initial["placement_domain"]?["s026_inactive_diagnostic"] as Newtonsoft.Json.Linq.JObject;string binaryDirectory;var templateDomain=diagnostic==null?Game.Runtime.GeneratedReferenceTemplateView.Read((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],out binaryDirectory):Game.Runtime.GeneratedReferenceTemplateView.ReadS026Diagnostic((Newtonsoft.Json.Linq.JObject)initial["reference_templates"],(string)diagnostic["source_root"],out binaryDirectory);')
  preflight=preflight.replace('Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));Game.Configs.SourceEnglishCommandText.Bind(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024));','if(diagnostic==null){Game.Runtime.SourceNativeDeathReadModel.LoadAtBoundary(Game.Runtime.AutonomousWorld.Descriptor(initial["death_mechanical_inputs"],1024*1024));Game.Configs.SourceEnglishCommandText.Bind(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024));}else{if(compiled.setup.CatalogId!="S026")throw new System.IO.InvalidDataException("S026 diagnostic identity required");Game.Runtime.SourceNativeDeathReadModel.LoadS026Diagnostic((Newtonsoft.Json.Linq.JObject)initial["death_mechanical_inputs"]);Game.Configs.SourceEnglishCommandText.BindS026Diagnostic(Game.Runtime.AutonomousWorld.Descriptor(initial["command_text_data"],4*1024*1024),(string)run["repository_root"]);}')
 else:preflight+='return new Newtonsoft.Json.Linq.JObject{["status"]="Passed",["world_allocated"]=false,["entity_ids_allocated"]=0,["scope"]="S002 profile/descriptor boundary; existing source-smoke-data owns native input qualification"};}'

 runtime_require='if((initial.Property(\"configuration_qualification\")!=null)!=(initial.Property(\"metadata_compiler_context\")!=null))throw new System.IO.InvalidDataException(\"Configuration qualification and compiler context must be present together\");if(initial[\"metadata_compiler_context\"]!=null){Game.Runtime.SourceConfigurationQualification.RequireInitialization(initial,JsonInteger(selection[\"seed\"],\"selection.seed\"),JsonInteger(selection[\"difficulty_id\"],\"selection.difficulty_id\"),JsonInteger(selection[\"size_id\"],\"selection.size_id\"));return;}'if backend=='TypedGroundS026V1'else'if(initial.Property(\"configuration_qualification\")!=null||initial.Property(\"metadata_compiler_context\")!=null)throw new System.IO.InvalidDataException(\"External configuration requires supported ground backend\");'
 if bound:runtime_require+=owned.require("s002_derived_bootstrap").generated_guard(*bound)
 return 'using System; internal static class SourceAdmittedProfiles { internal static int JsonInteger(Newtonsoft.Json.Linq.JToken value,string name){if(value==null||value.Type!=Newtonsoft.Json.Linq.JTokenType.Integer)throw new System.IO.InvalidDataException("Integer JSON field required: "+name);long n=(long)value;if(n<int.MinValue||n>int.MaxValue)throw new System.IO.InvalidDataException("Int32 JSON field required: "+name);return(int)n;} internal static void RequireInitialization(Newtonsoft.Json.Linq.JObject initial){if(initial==null||JsonInteger(initial["schema_version"],"initialization.schema_version")!=1||initial["kind"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["kind"]!="GeneratedMissionInitialization"||initial["authority"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String||(string)initial["authority"]!="AuthoredConfigAndBakedCampaignMap"||initial["mission_id"]?.Type!=Newtonsoft.Json.Linq.JTokenType.String)throw new System.IO.InvalidDataException("Exact authored initialization envelope required");var selection=initial["selection"]as Newtonsoft.Json.Linq.JObject;if(selection==null||selection.Count!=3||selection["seed"]==null||selection["size_id"]==null||selection["difficulty_id"]==null)throw new System.IO.InvalidDataException("Exact finite selection required");'+runtime_require+'Require((string)initial["mission_id"],JsonInteger(selection["seed"],"selection.seed"),JsonInteger(selection["difficulty_id"],"selection.difficulty_id"),JsonInteger(selection["size_id"],"selection.size_id"));} '+preflight+' internal const string RuntimeConfigurationMode='+json.dumps('ExternalQualifiedGroundV1'if backend=='TypedGroundS026V1'else'LegacyBoundProfilesV1')+'; internal const string Backend='+json.dumps(backend)+'; internal static void RequireBackend(string value){if(value!=Backend)throw new NotSupportedException("Compiled runtime backend mismatch");} internal static void Require(string mission,int seed,int difficulty=2,int size=1) { if(!('+' || '.join(clauses)+')) throw new NotSupportedException("Unqualified source execution profile"); } }\n',pins
