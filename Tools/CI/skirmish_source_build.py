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
 receipt={'schema_version':1,'kind':'SkirmishSourceSimulationBuild','mission':mission,'seed':seed,'repository_root':str(repo),'status':'Passed'if r.returncode==0 else'Failed','compile_exit':r.returncode,'compile_wall_seconds':time.monotonic()-start,'source_count':len(sources),'source_and_reference_pins':pins,'source_manifest':manifest_pin,'source_resolution':str(out/'source/resolution.json'),'placement_build':str(out/'placement/build.json'),'compiler_response':pin(rsp),'runtime_selection':{k:str(tools[k])for k in ['contents','script','engine','mono','newtonsoft']},'numeric_runtime':'InstalledMono --optimize=-float32','native_or_player_launched':False,'scope':'Current source plus explicit finite engine/data adapters. No native UI/render, seed-only determinism or mission outcome claim from compilation.'}
 if r.returncode==0:receipt['executable']=pin(code)
 write(out/'build.json',receipt);need(r.returncode==0,'Current source compilation failed; see '+str(out/'compile.log'));return receipt

def prepare_mono_config(tools,out):
 installed=tools['script']/'MonoBleedingEdge/etc/mono/config';raw=installed.read_bytes();text=raw.decode('utf-8');helper=tools['contents']/'Frameworks/libMonoPosixHelper.dylib';need(helper.is_file(),'Actual installed MonoPosixHelper required')
 text=text.replace('target="libMonoPosixHelper.dylib"','target="'+str(helper)+'"');text=text.replace('target="MonoPosixHelper"','target="'+str(helper)+'"');path=out/'mono-config.xml';path.write_text(text);return path,{str(installed):sha(raw),str(helper):sha(helper.read_bytes())}
def run(repo,build_receipt,bundle,output,max_ticks=None,timeout=1800,data_repo=None):
 repo=Path(repo).resolve(strict=True);data_repo=Path(data_repo).resolve(strict=True)if data_repo is not None else repo;out=Path(output).absolute();need(math.isfinite(timeout)and timeout>0,'Finite positive owned-process timeout required');need(not out.exists(),'Fresh isolated run output required');out.mkdir(parents=True);receipt,build_pin=read_json_pinned(build_receipt);inputs,bundle_pin=read_json_pinned(bundle);need(receipt.get('status')=='Passed','Qualified supported build required');need(str(repo)==receipt.get('repository_root'),'Run repository differs from built source root');verify(receipt['source_and_reference_pins']);exe=receipt['executable'];need(sha(Path(exe['path']).read_bytes())==exe['sha256'],'Built image changed')
 require_profile(receipt.get('mission'),receipt.get('seed'));tools={k:Path(v).resolve(strict=True)for k,v in receipt['runtime_selection'].items()};need(set(tools)=={'contents','script','engine','mono','newtonsoft'},'Build-recorded runtime paths required');need(str(tools['mono'])in receipt['source_and_reference_pins']and str(tools['newtonsoft'])in receipt['source_and_reference_pins'],'Unbound runtime selection');config,extra=prepare_mono_config(tools,out);need(inputs.get('schema_version')==1 and inputs.get('mission')==receipt.get('mission')and inputs.get('seed')==receipt.get('seed'),f"Explicit matching {receipt.get('mission')}/{receipt.get('seed')} data bundle required")
 initial,initial_pin=read_json_pinned(inputs['initialization']['path']);need(initial_pin['sha256']==inputs['initialization']['sha256'],'Initialization pin mismatch');selection=initial['selection'];need(initial.get('mission_id')==inputs['mission']and selection.get('seed')==inputs['seed'],'Initialization identity mismatch');require_profile(initial['mission_id'],selection['seed'],selection['difficulty_id'],selection['size_id'])
 request={'schema_version':1,'kind':'SkirmishSourceSimulationRun','mission':inputs['mission'],'seed':inputs['seed'],'repository_root':str(data_repo),'source_repository_root':str(repo),'output_directory':str(out),'runtime_config':pin(config)}
 request['mission_scene']=pin(data_repo/'Assets/Game/Scenes/Match.unity');request['camera_profile']=pin(data_repo/'Assets/Game/Resources/SkirmishMissionViewProfile_S002.asset')
 for key in ['initialization','unit_metadata','static_clearance','camera_values','camera_motion']:
  row=inputs[key];p=Path(row['path']).resolve(strict=True);need(sha(p.read_bytes())==row['sha256'],'Bundle input changed: '+key);request[key]={'path':str(p),'sha256':row['sha256']}
 if max_ticks is not None:need(0<max_ticks<=72000,'Bounded max_ticks required');request['max_ticks']=max_ticks
 write(out/'run-request.json',request);env=os.environ.copy();env['MONO_PATH']=os.pathsep.join([str(repo/'Library/ScriptAssemblies'),str(tools['engine']),str(tools['newtonsoft'].parent),str(Path(receipt['placement_build']).parent)]);pins=dict(receipt['source_and_reference_pins']);merge_pins(pins,owned.pins());merge_pins(pins,extra);merge_pins(pins,{build_pin['path']:build_pin['sha256'],bundle_pin['path']:bundle_pin['sha256'],exe['path']:exe['sha256']});merge_pins(pins,{v['path']:v['sha256']for k,v in request.items()if isinstance(v,dict)and'path'in v});verify(pins)
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
 need((out/'runtime-before-world.json').is_file()and(out/'runtime-after-outcome.json').is_file(),'Actual loaded-runtime manifests missing');write(out/'run-receipt.json',result);return result
