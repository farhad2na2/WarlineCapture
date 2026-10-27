"""One-entry source-only S002 build/run plumbing. Never launches Unity or a player."""
from __future__ import annotations
import hashlib,json,os,platform,re,shutil,subprocess,sys,time,importlib.util,math,types
from pathlib import Path
from skirmish_source_manifest import resolve_manifest,need,sha
import skirmish_owned_source as owned
from skirmish_source_profiles import require_profile # admitted first by owned.load_cli
sys.dont_write_bytecode=True

def pin(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();return {'path':str(p),'sha256':sha(raw),'bytes':len(raw)}
def verify(pins):
 for name,expected in pins.items():need(sha(Path(name).read_bytes())==expected,'Input changed: '+name)
def merge_pins(pins,other):
 verify(pins)
 for name,expected in other.items():
  key=str(Path(name).resolve(strict=True))
  need(key not in pins or pins[key]==expected,'Conflicting source/runtime epoch: '+key)
  need(sha(Path(key).read_bytes())==expected,'Input changed before merge: '+key)
  pins[key]=expected
def merge_file_pins(pins,paths):
 added={}
 for path in paths:
  entry=pin(path);key=entry['path'];expected=entry['sha256']
  need(key not in added or added[key]==expected,'Conflicting captured source/runtime epoch: '+key)
  added[key]=expected
 merge_pins(pins,added)
def read_json_pinned(path):
 p=Path(path).resolve(strict=True);raw=p.read_bytes();return json.loads(raw),{'path':str(p),'sha256':sha(raw),'bytes':len(raw)}
def write(path,value):
 p=Path(path);temporary=p.with_name(p.name+'.tmp');temporary.write_text(json.dumps(value,indent=2)+'\n');os.replace(temporary,p)
def runtime_paths(repo):
 need(platform.system()=='Darwin','Only the qualified installed macOS Mono numeric lane is currently supported')
 version=re.search(r'm_EditorVersion:\s*(\S+)',(repo/'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
 contents=Path('/Applications/Unity/Hub/Editor')/version/'Unity.app/Contents';script=contents/'Resources/Scripting';engine=script/'Managed/UnityEngine';mono=script/'MonoBleedingEdge/bin/mono'
 dot=Path(shutil.which('dotnet')or'').resolve(strict=True);sdks=list((dot.parent/'sdk').glob('*/Roslyn/bincore/csc.dll'));need(bool(sdks),'Installed Roslyn compiler required')
 def key(p):return tuple(int(x)for x in p.parents[2].name.split('.')if x.isdigit())
 csc=sorted(sdks,key=key)[-1];newtons=list((repo/'Library/PackageCache').glob('com.unity.nuget.newtonsoft-json@*/Runtime/Newtonsoft.Json.dll'));need(len(newtons)==1,'One resolved current Newtonsoft assembly required')
 refs=[repo/'Library/ScriptAssemblies/Unity.Addressables.dll',repo/'Library/ScriptAssemblies/Unity.ResourceManager.dll',script/'NetStandard/ref/2.1.0/netstandard.dll',script/'NetStandard/compat/2.1.0/shims/netfx/mscorlib.dll',engine/'UnityEngine.MathematicsModule.dll',engine/'UnityEngine.SharedInternalsModule.dll',newtons[0]]
 core=engine/'UnityEngine.CoreModule.dll'
 for p in refs+[core,mono,dot,csc]:need(p.is_file(),'Required installed build/runtime file missing: '+str(p))
 return {'contents':contents,'script':script,'engine':engine,'mono':mono,'dotnet':dot,'csc':csc,'refs':refs,'core':core,'newtonsoft':newtons[0]}
def _generate_navigation_identity(tool,inputs,output,pins):
 # Execute exactly the source bytes hashed/pinned here; never import cached .pyc.
 tool=Path(tool).resolve(strict=True);raw=tool.read_bytes();digest=sha(raw)
 merge_pins(pins,{str(tool):digest});need(tool.read_bytes()==raw,'Identity generator changed before execution')
 module=types.ModuleType('current_navigation_facts_identity');module.__file__=str(tool)
 exec(compile(raw,str(tool),'exec'),module.__dict__)
 need(tool.read_bytes()==raw,'Identity generator changed during execution')
 result=module.generate(inputs,output)
 need(tool.read_bytes()==raw,'Identity generator changed during generation');verify(pins)
 return result

def build(repo,package,manifest,output,mission='S002',seed=104731):
 repo=Path(repo).resolve(strict=True);package=Path(package).resolve(strict=True);out=Path(output).absolute();need(not out.exists(),'Fresh isolated build output required');out.mkdir(parents=True)
 result=resolve_manifest(repo,package,manifest,out/'source',mission,seed)
 need(not result['unresolved'],'Unmaterialized source recipes: '+', '.join(x['id']for x in result['unresolved']))
 tools=runtime_paths(repo);placement_root=package/'Tools/CI/SkirmishSourceSimulation/Placement';module=owned.load_placement(placement_root);placement=module.build(repo,tools['engine'],out/'placement')
 pins=dict(result['pins']);merge_pins(pins,owned.pins());verify(pins);merge_pins(pins,{x['path']:x['sha256']for x in result['resolved']});merge_pins(pins,placement['source_and_reference_pins']);code=out/'SourceSimulation.exe';sources=[Path(x['path'])for x in result['resolved']]
 identity_tool=package/'Tools/CI/generate_navigation_facts_identity.py';identity_output=out/'source/generated/SourceNavigationFactsIdentity.cs';identity_inputs=out/'navigation-facts-sources.rsp'
 identity_inputs.write_text('\n'.join(json.dumps(str(x))for x in sources)+'\n')
 _generate_navigation_identity(identity_tool,identity_inputs,identity_output,pins);sources.append(identity_output)
 for identity_input in [identity_tool,identity_inputs,identity_output,identity_output.with_suffix('.json')]:
  identity_pin=pin(identity_input);merge_pins(pins,{identity_pin['path']:identity_pin['sha256']})
 merge_file_pins(pins,sources+[tools['mono'],tools['dotnet'],tools['csc'],tools['core']]+tools['refs']+[Path(placement['dll']),Path(__file__),repo/'ProjectSettings/ProjectVersion.txt',tools['script']/'MonoBleedingEdge/etc/mono/config',tools['contents']/'Frameworks/libMonoPosixHelper.dylib'])
 data,manifest_pin=read_json_pinned(manifest);merge_pins(pins,{manifest_pin['path']:manifest_pin['sha256']});defines=data.get('compiler_defines',[]);need(all(re.fullmatch(r'[A-Za-z_]\w*',x)and not x.startswith('UNITY_EDITOR')for x in defines),'Explicit non-Editor compiler profile required')
 args=['-nologo','-unsafe+','-nostdlib+','-langversion:latest','-target:exe','-main:SourceSimulationProgram','-out:'+str(code)]+(['-define:'+','.join(defines)]if defines else[])+['-r:'+str(p)for p in tools['refs']]+['-r:ActualCore='+str(tools['core']),'-r:'+placement['dll']]+[str(p)for p in sources]
 rsp=out/'compile.rsp';rsp.write_text('\n'.join(json.dumps(x)for x in args)+'\n');verify(pins);start=time.monotonic();r=subprocess.run([str(tools['dotnet']),str(tools['csc']),'@'+str(rsp)],capture_output=True,text=True,timeout=120);(out/'compile.log').write_text(r.stdout+r.stderr);verify(pins)
 receipt={'runtime_configuration_mode':owned.require('skirmish_source_profiles').runtime_configuration_mode(mission),'runtime_backend':result['runtime_backend'],'schema_version':1,'kind':'SkirmishSourceSimulationBuild','mission':mission,'seed':seed,'repository_root':str(repo),'status':'Passed'if r.returncode==0 else'Failed','compile_exit':r.returncode,'compile_wall_seconds':time.monotonic()-start,'source_count':len(sources),'source_and_reference_pins':pins,'source_manifest':manifest_pin,'source_resolution':str(out/'source/resolution.json'),'placement_build':str(out/'placement/build.json'),'compiler_response':pin(rsp),'runtime_selection':{k:str(tools[k])for k in ['contents','script','engine','mono','newtonsoft']},'numeric_runtime':'InstalledMono --optimize=-float32','native_or_player_launched':False,'scope':'Current source plus explicit finite engine/data adapters. No native UI/render, seed-only determinism or mission outcome claim from compilation.'}
 if owned.require('skirmish_source_profiles').runtime_backend(mission)=='TypedGroundS026V1':
  readset=out/'source/compiled-transform-readset.json';sidecar=Path(str(code)+'.transform-readset.json');sidecar.write_bytes(readset.read_bytes());receipt['transform_readset']=pin(sidecar);merge_file_pins(pins,[readset,sidecar])
 if r.returncode==0:receipt['executable']=pin(code)
 write(out/'build.json',receipt);need(r.returncode==0,'Current source compilation failed; see '+str(out/'compile.log'));return receipt

def prepare_mono_config(tools,out):
 installed=tools['script']/'MonoBleedingEdge/etc/mono/config';raw=installed.read_bytes();text=raw.decode('utf-8');helper=tools['contents']/'Frameworks/libMonoPosixHelper.dylib';need(helper.is_file(),'Actual installed MonoPosixHelper required')
 text=text.replace('target="libMonoPosixHelper.dylib"','target="'+str(helper)+'"');text=text.replace('target="MonoPosixHelper"','target="'+str(helper)+'"');path=out/'mono-config.xml';path.write_text(text);return path,{str(installed):sha(raw),str(helper):sha(helper.read_bytes())}
def metadata_origin_pins(initial,repo,receipt):
 descriptor=initial.get('metadata_source_origin')
 if descriptor is None:return {}
 def exact_descriptor(value,maximum=1024*1024):
  need(isinstance(value,dict)and set(value)=={'path','sha256'}and all(isinstance(value[k],str)for k in value),'Exact native origin descriptor required')
  path=Path(value['path']);need(path.is_absolute()and path.is_file()and not path.is_symlink(),'Regular absolute origin descriptor required')
  need(0<path.stat().st_size<=maximum,'Bounded origin descriptor required');return path
 path=exact_descriptor(descriptor);origin_pin=pin(path);origin=json.loads(path.read_bytes(),object_pairs_hook=owned.require('skirmish_source_syntax').no_duplicates);need(origin_pin['sha256']==descriptor['sha256'],'Metadata origin descriptor changed')
 need(set(origin)=={'schema_version','kind','native_metadata','source_root','data_root','input_pins'}and type(origin['schema_version'])is int and origin['schema_version']==1 and origin['kind']=='QualifiedNativeMetadataOrigin','Exact native metadata origin schema required')
 need(origin['native_metadata']==initial.get('metadata_compatibility'),'Native origin artifact binding differs')
 native_path=exact_descriptor(origin['native_metadata'],64*1024*1024);native,native_pin=read_json_pinned(native_path);need(native_pin['sha256']==origin['native_metadata']['sha256'],'Native origin artifact changed')
 roots=[]
 for name in ['source_root','data_root']:
  root=Path(origin[name]);need(root.is_absolute()and root.is_dir(),'Explicit native origin root required')
  need(all(not x.is_symlink()for x in [root,*root.parents]),'Symlink origin root forbidden');roots.append(root.resolve(strict=True))
 pins={origin_pin['path']:origin_pin['sha256'],native_pin['path']:native_pin['sha256']};declared=origin['input_pins'];need(isinstance(declared,dict)and 68<=len(declared)<=8192,'Bounded origin file closure required')
 native_pins=native['source_pins']
 for name,expected in declared.items():
  f=Path(name);need(f.is_absolute()and f.is_file()and not f.is_symlink(),'Regular native origin input required');canonical=f.resolve(strict=True);matches=[str(canonical.relative_to(root))for root in roots if canonical.is_relative_to(root)];need(matches and all(native_pins.get(rel)==expected for rel in matches),'Origin file differs from captured native pin');entry=pin(f);need(entry['sha256']==expected,'Native origin input changed');pins[entry['path']]=expected
 response=receipt['compiler_response'];response_path=Path(response['path']);need(pin(response_path)['sha256']==response['sha256'],'Actual compiler response changed');arguments=[json.loads(line)for line in response_path.read_text().splitlines()if line.strip()];reader_paths=[name for name in arguments if isinstance(name,str)and Path(name).is_absolute()and Path(name).name=='SourceMetadataCompatibility.cs'];need(len(reader_paths)==1,'One actual compiled metadata reader required');reader=pin(Path(reader_paths[0]));need(receipt['source_and_reference_pins'].get(reader['path'])==reader['sha256'],'Compiled metadata reader receipt pin differs');pins[reader['path']]=reader['sha256'];pins[str(response_path)]=response['sha256'];text=Path(reader_paths[0]).read_text()
 def literals(name):
  matches=re.findall(r'\b'+name+r'\s*=\s*\{([^}]+)\}',text);need(len(matches)==1,'One literal metadata dependency list required');body=matches[0];values=re.findall(r'"([^"\n]+)"',body);need(not re.sub(r'"[^"\n]+"|[\s,]','',body),'Only literal dependency paths admitted');return values
 factories=literals('FactorySources');owners=literals('OwnerDefinitions');need(len(factories)==27 and len(owners)==6,'Exact current metadata consumer domain required')
 expected_origin={}
 def consume(root,rel,expected):
  f=root/rel;entry=pin(f);need(entry['sha256']==expected,'Metadata dependency changed: '+str(f));pins[entry['path']]=expected
 for suffix in factories+owners:
  rel='Assets/Game/Scripts/'+suffix
  for item in [rel,rel+'.meta']:
   expected=native_pins[item];expected_origin[str(roots[0]/item)]=expected
   current_expected=expected
   if item=='Assets/Game/Scripts/Composition/Game.Composition.asmdef':
    current=pin(repo/item)['sha256'];need(current==expected or(current=='8f41d8cd74821639e2628fb77ad32fdd89db88111af3af800ebb16d109753cfe'and expected=='b6db33e0e266aea3c4f63a70677b92a70188f101ad149b91560f897e466d3c6b'),'Unqualified current Composition packaging definition');current_expected=current
   consume(repo,item,current_expected)
 for rel in ['Packages/manifest.json','Packages/packages-lock.json']:
  expected_origin[str(roots[0]/rel)]=native_pins[rel];consume(repo,rel,native_pins[rel])
 for rel,expected in native_pins.items():
  if rel.startswith('Assets/')and not rel.startswith('Assets/Game/Scripts/')and not rel.endswith(('.cs','.cs.meta')):expected_origin[str(roots[1]/rel)]=expected
 need(declared==expected_origin,'Native origin consumed closure mismatch')
 return pins

def derived_projection_header_pins(initial):
 # Shape-only fast failure for supplied supported headers. C# owns provenance equality,
 # the native epoch and every factory/semantic/hash qualification.
 if initial.get('metadata_compiler_context')is None:return {}
 kinds={'building_definitions':'CompilerContextDerivedSkirmishBuildingDefinitions',
        'unit_production_metadata':'CompilerContextDerivedSkirmishUnitProductionMetadata',
        'building_request_metadata':'CompilerContextDerivedConfiguredBuildingMetadata'}
 pins={}
 for name,kind in kinds.items():
  if name not in initial:continue
  row=initial[name];header,pin=read_json_pinned(row['path'])
  need(pin['sha256']==row['sha256'],'Input changed: initialization.'+name)
  pins[pin['path']]=pin['sha256']
  if not isinstance(header,dict)or type(header.get('schema_version'))is not int or header['schema_version']!=3 or header.get('kind')!=kind or header.get('authority')!='OriginalCompilerContext_ImmutableNativeFactoryFacts_NotNativeS050':continue
  location='$.'+name+'.native_origin';origin=header.get('native_origin')
  need(isinstance(origin,dict),location+': required object in supplied derived projection')
  need(type(origin.get('kind'))is str and bool(origin['kind'].strip()),location+'.kind: required nonempty string in supplied derived projection')
  need(type(origin.get('artifact_sha256'))is str and re.fullmatch(r'[0-9a-f]{64}',origin['artifact_sha256'])is not None,location+'.artifact_sha256: required lowercase SHA256 in supplied derived projection')
 return pins

def runtime_input_pins(initial,inputs,repo,receipt,data_repo):
 if owned.require('skirmish_source_profiles').runtime_backend(inputs['mission'])!='TypedGroundS026V1':return {}
 pins={}
 def descriptor(row,maximum=256*1024*1024):
  need(isinstance(row,dict)and isinstance(row.get('path'),str)and isinstance(row.get('sha256'),str),'Required S026 input descriptor')
  f=Path(row['path']);need(f.is_absolute()and f.is_file()and f.stat().st_size<=maximum,'Bounded absolute S026 input required');entry=pin(f);need(entry['sha256']==row['sha256'],'S026 input changed: '+str(f));pins[entry['path']]=entry['sha256'];return f
 runtime=inputs.get('runtime_data_inputs');need(isinstance(runtime,dict)and set(runtime)=={'schema_version','kind','data_root','data_files','source_files'}and type(runtime['schema_version'])is int and runtime['schema_version']==1 and runtime['kind']=='CurrentRoadSceneRuntimeInputs','Exact current road input contract required')
 need(Path(runtime['data_root']).is_absolute()and Path(runtime['data_root']).resolve(strict=True)==data_repo,'Road data root differs from actual selected runtime root')
 datafiles={'Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset','Assets/Game/Configs/Scene/Game_RoadBuild_Config.asset.meta','Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset','Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset.meta'}
 sourcefiles={'Assets/Game/Scripts/Configs/Scene/RoadBuildSystemSceneConfigAsset.cs.meta','Assets/Game/Scripts/Configs/Scene/RuntimeCitySpawnerSystemSceneConfigAsset.cs.meta'}
 need(set(runtime['data_files'])==datafiles and set(runtime['source_files'])==sourcefiles,'Exact road runtime/current-source closure required')
 for root,rows in [(data_repo,runtime['data_files']),(repo,runtime['source_files'])]:
  for rel,digest in rows.items():descriptor({'path':str(root/rel),'sha256':digest},4*1024*1024)
 for key in ['source_config_graph','setup_matrix','canonical_battle_catalog','selected_operation_map','building_definitions','building_placement_config','reference_templates','navigation_reference','resource_preset','unit_production_metadata','building_request_metadata','metadata_compatibility','engineering_camera_profile','camera_engine_adapter','operation_map_rotation_reference','mission_logical_binding','death_mechanical_inputs','command_text_data']:descriptor(initial[key])
 if initial.get('metadata_compiler_context')is not None:
  descriptor(initial['metadata_compiler_context']);descriptor(initial['configuration_qualification'])
  need(initial['configuration_qualification']==owned.require('skirmish_source_profiles').configuration_qualification(),'Explicit active configuration qualification differs')
  qualification,_=read_json_pinned(initial['configuration_qualification']['path']);need(qualification.get('compiler_context')==initial['metadata_compiler_context'],'Qualified compiler descriptor differs')
 rotation,_=read_json_pinned(initial['operation_map_rotation_reference']['path'])
 if rotation.get('kind')=='SourceQualifiedOperationMapRotationReferenceSet':
  descriptor(rotation['base_reference']);descriptor(rotation['supplementary_reference'])
 for key in ['definition','grid_config','surface_data']:descriptor(initial['static_map'][key])
 if initial.get('metadata_compiler_context')is not None:
  descriptor(initial['placement_domain']['original_owner_masks'])
  fixed,_=read_json_pinned(initial['navigation_reference']['path']);need(fixed.get('kind')=='OriginalCapturedFixedMapCompilerContext'and fixed.get('metadata_compiler_context')==initial['metadata_compiler_context']and fixed.get('configuration_qualification')==initial['configuration_qualification'],'Explicit same compiler/fixed-map data binding required');native_path=descriptor(fixed['native_readback']);native,_=read_json_pinned(native_path);need(native.get('result')=='Collected'and native.get('worldDisposed')is True,'Actual disposed native source read required')
  masks,_=read_json_pinned(initial['placement_domain']['original_owner_masks']['path'])
  capture=initial['placement_domain']['captured_asset_origin'];need(set(capture)=={'source_root','source_definition_path','source_definition_sha256'}and capture['source_definition_sha256']==initial['static_map']['definition']['sha256'],'Explicit captured asset origin identity required');capture_root=Path(capture['source_root']);need(capture_root.is_absolute()and capture_root.is_dir()and all(not p.is_symlink()for p in[capture_root,*capture_root.parents]),'Canonical captured asset source root required')
  for root in [native,masks]:
   for path,digest in root['readPins'].items():
    if path.startswith('Assets/'):
     relative=Path(path);need(all(part not in {'','..','.'}for part in relative.parts),'Canonical captured asset relative path required');path=str(capture_root/relative)
    elif path.startswith('Packages/'):
     locator=root['packageAssetLocators'][path];need(locator['logicalPath']==path and locator['sha256']==digest,'Registered package locator differs');path=locator['resolvedPhysicalPath']
    if any(segment in path for segment in ['/Assets/','/Packages/','/Library/PackageCache/'])and not path.endswith(('.cs','.cs.meta','.asmdef','.asmdef.meta','.dll','.pdb')):descriptor({'path':path,'sha256':digest},512*1024*1024)
  camera,_=read_json_pinned(initial['engineering_camera_profile']['path']);descriptor(camera['native_function_profile'])
 else:
  for key in ['packed','water_reference','geometry_reference']:descriptor(initial['placement_domain'][key])
 descriptor(initial['typed_mission_configuration']['layout'])
 graph,_=read_json_pinned(initial['source_config_graph']['path'])
 for f,digest in graph['source_pins'].items():descriptor({'path':f,'sha256':digest},4*1024*1024)
 row=initial['reference_templates']
 for stem in ['source_reference','projection_manifest']:descriptor({'path':row[stem+'_path'],'sha256':row[stem+'_sha256']})
 need(initial['unit_production_metadata']==inputs['unit_metadata'],'S026 unit metadata descriptor differs')
 text,_=read_json_pinned(initial['command_text_data']['path']);source=inputs['command_text_sources'];need(set(source)=={'captured_root','current_root','input_pins','artifact_sha256'}and source['artifact_sha256']==initial['command_text_data']['sha256'],'Exact original text source manifest required')
 captured=Path(source['captured_root']);current=Path(source['current_root']);need(captured.is_absolute()and current.is_absolute()and current.is_dir(),'Explicit text source roots required')
 expected={str(current/Path(name).relative_to(captured)):digest for name,digest in text['source_pins'].items()};need(source['input_pins']==expected,'Original text source closure differs')
 for f,digest in expected.items():descriptor({'path':f,'sha256':digest})
 return pins

def run(repo,build_receipt,bundle,output,max_ticks=None,timeout=1800,data_repo=None,prepare_only=False):
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo is not None else repo;out=Path(output).absolute();need(math.isfinite(timeout)and timeout>0,'Finite positive owned-process timeout required');need(not out.exists(),'Fresh isolated run output required');out.mkdir(parents=True);receipt,build_pin=read_json_pinned(build_receipt);inputs,bundle_pin=read_json_pinned(bundle);need(receipt.get('status')=='Passed','Qualified supported build required');need(str(repo)==receipt.get('repository_root'),'Run repository differs from built source root');verify(receipt['source_and_reference_pins']);exe=receipt['executable'];need(sha(Path(exe['path']).read_bytes())==exe['sha256'],'Built image changed')
 generic_host=owned.require('run_skirmish_source_batch').is_external_build(receipt)
 if not generic_host:require_profile(receipt.get('mission'),receipt.get('seed'))
 tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Build-recorded runtime paths required');need(str(tools['mono'])in receipt['source_and_reference_pins']and str(tools['newtonsoft'])in receipt['source_and_reference_pins'],'Unbound runtime selection');config,extra=prepare_mono_config(tools,out);need(inputs.get('schema_version')==1 and (generic_host or(inputs.get('mission')==receipt.get('mission')and inputs.get('seed')==receipt.get('seed'))),f"Explicit matching {receipt.get('mission')}/{receipt.get('seed')} data bundle required")
 initial,initial_pin=read_json_pinned(inputs['initialization']['path']);need(initial_pin['sha256']==inputs['initialization']['sha256'],'Initialization pin mismatch');selection=initial['selection'];owned.require('skirmish_source_profiles').require_initial_qualification_pair(initial);need(initial.get('mission_id')==inputs['mission']and selection.get('seed')==inputs['seed'],'Initialization identity mismatch');require_profile(initial['mission_id'],selection['seed'],selection['difficulty_id'],selection['size_id'])
 expected_backend=owned.require('skirmish_source_profiles').runtime_backend(inputs['mission'])
 owned.require('run_skirmish_source_batch').require_build_target(receipt,inputs['mission'],inputs['seed'],expected_backend)
 need(inputs.get('runtime_backend', 'LegacyGroundS002V1')==expected_backend,'Bundle/build runtime backend mismatch')
 origin_pins=derived_projection_header_pins(initial)
 merge_pins(origin_pins,metadata_origin_pins(initial,repo,receipt))
 merge_pins(origin_pins,runtime_input_pins(initial,inputs,repo,receipt,data_repo))
 request={'runtime_backend':expected_backend,'schema_version':1,'kind':'SkirmishSourceSimulationRun','mission':inputs['mission'],'seed':inputs['seed'],'repository_root':str(data_repo),'source_repository_root':str(repo),'output_directory':str(out),'runtime_config':pin(config)}
 request['mission_scene']=pin(data_repo/'Assets/Game/Scenes/Match.unity');request['camera_profile']=inputs['camera_profile']if expected_backend=='TypedGroundS026V1'else pin(data_repo/'Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset')
 if expected_backend=='TypedGroundS026V1':need(initial.get('engineering_camera_profile')==request['camera_profile'],'S026 current engineering camera descriptor differs')
 for key in ['initialization','unit_metadata','static_clearance','camera_values','camera_motion']:
  row=inputs[key];p=Path(row['path']).resolve(strict=True);need(sha(p.read_bytes())==row['sha256'],'Bundle input changed: '+key);request[key]={'path':str(p),'sha256':row['sha256']}
 if max_ticks is not None:need(0<max_ticks<=72000,'Bounded max_ticks required');request['max_ticks']=max_ticks
 write(out/'run-request.json',request);env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);pins=dict(receipt['source_and_reference_pins']);merge_pins(pins,owned.pins());merge_pins(pins,extra);merge_pins(pins,origin_pins);merge_pins(pins,{build_pin['path']:build_pin['sha256'],bundle_pin['path']:bundle_pin['sha256'],exe['path']:exe['sha256']});merge_pins(pins,{v['path']:v['sha256']for k,v in request.items()if isinstance(v,dict)and'path'in v});verify(pins)
 if prepare_only:
  write(out/'input-pins.json',pins);prepared={'status':'PreparedOnly','world_allocated':False,'run_request':pin(out/'run-request.json'),'input_pins':pin(out/'input-pins.json'),'runtime_backend':expected_backend,'build_receipt':build_pin,'data_bundle':bundle_pin};write(out/'prepared-run.json',prepared);return prepared
 start=time.monotonic();returncode=None;timed_out=False
 with (out/'process.log').open('w')as log:
  process=subprocess.Popen([str(tools['mono']),'--config',str(config),'--optimize=-float32',exe['path'],str(out/'run-request.json')],env=env,stdout=log,stderr=subprocess.STDOUT)
  try:returncode=process.wait(timeout=timeout)
  except subprocess.TimeoutExpired:timed_out=True
  finally:
   if process.poll()is None:
    process.terminate()
    try:process.wait(timeout=5)
    except subprocess.TimeoutExpired:process.kill();process.wait(timeout=5)
  returncode=process.returncode
 verify(pins);raw=json.loads((out/'result.json').read_text())if(out/'result.json').is_file()else None
 result={'schema_version':1,'kind':'SkirmishSourceSimulationRunReceipt','mission':inputs['mission'],'seed':inputs['seed'],'source_repository_root':str(repo),'data_repository_root':str(data_repo),'process_exit':returncode,'timed_out':timed_out,'wall_seconds':time.monotonic()-start,'build_receipt':build_pin,'data_bundle':bundle_pin,'input_and_runtime_pins':pins,'source_result':raw,'status':'SourceOutcomeObserved'if not timed_out and returncode==0 and raw and raw.get('status')=='SourceTerminal'and raw.get('source_outcome')in {'Victory','Defeat'}else'BoundedPrefix'if not timed_out and returncode==0 and raw and raw.get('status')=='BoundedPrefix'and max_ticks is not None else'Failed','scope':'Source gameplay outcome or explicit bounded prefix; native normal-input/UI/render/result-save/return acceptance is separate. Real wall-duration path budgeting is preserved; fixed seed does not establish deterministic scheduling.'}
 result['repository_data_scope']='CURRENT_REPOSITORY_ASSET_SNAPSHOT_PREWORLD_AND_POSTRUN; not historical asset equality'
 result['runtime_identity_scope']='Common before/after loaded files and MVIDs unchanged; late-only modules listed with post-load hashes, not pre-load equality'
 result.update(full_outcome_observed=result['status']=='SourceOutcomeObserved',expected_outcome='Victory',validation_passed=result['status']=='SourceOutcomeObserved'and raw.get('source_outcome')=='Victory',diagnostic_prefix_completed=result['status']=='BoundedPrefix',native_acceptance=False)
 missing_manifests=[name for name in ['runtime-before-world.json','runtime-after-outcome.json']if not(out/name).is_file()]
 if missing_manifests:
  result.update(status='Failed',full_outcome_observed=False,validation_passed=False,diagnostic_prefix_completed=False)
  result['runtime_manifest_failure']={'reason':'Actual loaded-runtime manifests missing','missing_files':missing_manifests}
  write(out/'run-receipt.json',result)
 need((out/'runtime-before-world.json').is_file()and(out/'runtime-after-outcome.json').is_file(),'Actual loaded-runtime manifests missing');write(out/'run-receipt.json',result);return result
