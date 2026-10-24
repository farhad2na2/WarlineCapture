#!/usr/bin/env python3
"""Build/run the unchanged production ARIA planner Step without Unity Editor.
Source files are copied/extracted into a separate immutable build directory. No
runtime source edit, accepted command, device touch or world step is performed.
"""
import argparse,hashlib,json,os,pathlib,shutil,subprocess,sys,tempfile
MAX_INPUT=16*1024*1024;MAX_OUTPUT=64*1024*1024
HERE=pathlib.Path(__file__).resolve();HOST=HERE.parent/'SkirmishAriaReplay'
def digest(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def no_duplicates(pairs):
 d={}
 for k,v in pairs:
  if k in d:raise ValueError('Duplicate JSON field: '+k)
  d[k]=v
 return d
def read_json(path):
 p=pathlib.Path(path);size=p.stat().st_size
 if size>MAX_INPUT:raise ValueError('Input exceeds16MiB')
 raw=p.read_bytes()
 if len(raw)!=size:raise ValueError('Input changed during read')
 return json.loads(raw,object_pairs_hook=no_duplicates,parse_constant=lambda s:(_ for _ in ()).throw(ValueError('Nonfinite JSON constant '+s)))
def emit_new(path,data):
 p=pathlib.Path(path);raw=(json.dumps(data,indent=2,allow_nan=False)+'\n').encode()
 if len(raw)>MAX_OUTPUT:raise ValueError('Output exceeds64MiB')
 with p.open('xb')as f:f.write(raw)
def method_span(s,signature):
 a=s.index(signature);j=s.index('{',a);depth=1;k=j+1
 # Production methods here have balanced braces in their comments/strings; final
 # extraction proof compares every remaining byte, not a rewritten method parser.
 while depth:
  depth+=(s[k]=='{')-(s[k]=='}');k+=1
 return a,k
CONTRACTS=['AriaPlayContracts.cs','AriaSkirmishContracts.cs','AriaObjectiveTouchContracts.cs','AriaObjectiveSupportContracts.cs','AriaAircraftServiceContracts.cs','AriaPublicScanContracts.cs','AriaPublicBoardingContracts.cs','AriaOffensiveAirContracts.cs']
PARTIALS=['AriaSkirmishPlanSystem.cs','AriaSkirmishPlanSystem.AircraftService.cs','AriaSkirmishPlanSystem.Objective.cs','AriaSkirmishPlanSystem.ObjectiveSupport.cs','AriaSkirmishPlanSystem.OffensiveAir.cs','AriaSkirmishPlanSystem.PublicBoarding.cs','AriaSkirmishPlanSystem.PublicScan.cs']
def build(args):
 repo=pathlib.Path(args.repo).resolve();out=pathlib.Path(args.build_dir).absolute()
 if out.exists():raise ValueError('Build directory must be new: '+str(out))
 src={}
 for name in PARTIALS:src['Assets/Game/Scripts/UI/Shell/Ecs/'+name]=(repo/'Assets/Game/Scripts/UI/Shell/Ecs'/name).read_bytes()
 for name in CONTRACTS:src['Assets/Game/Scripts/UI/Contracts/'+name]=(repo/'Assets/Game/Scripts/UI/Contracts'/name).read_bytes()
 for name in ['AriaPlayComponents.cs','AriaSkirmishComponents.cs']:src['Assets/Game/Scripts/UI/Shell/Ecs/Contracts/'+name]=(repo/'Assets/Game/Scripts/UI/Shell/Ecs/Contracts'/name).read_bytes()
 for name in ['Assets/Game/Scripts/UI/Contracts/IUiSkirmishGateway.cs','Assets/Game/Scripts/Editor/AriaSkirmishPlanValidation.cs','Assets/Game/Scripts/UI/Contracts/MatchHudSelectionPanelModels.cs']:src[name]=(repo/name).read_bytes()
 if args.engine_dir:engine=pathlib.Path(args.engine_dir).resolve()
 else:
  version=(repo/'ProjectSettings/ProjectVersion.txt').read_text().split('m_EditorVersion: ',1)[1].splitlines()[0].strip()
  engine=pathlib.Path('/Applications/Unity/Hub/Editor')/version/'Unity.app/Contents/Resources/Scripting/Managed/UnityEngine'
  if not(engine/'UnityEngine.CoreModule.dll').is_file():raise ValueError('Version-matched managed runtime absent; pass --engine-dir explicitly')
 dlls=[engine/'UnityEngine.CoreModule.dll',engine/'UnityEngine.SharedInternalsModule.dll']
 if not all(f.is_file()for f in dlls):raise ValueError('Actual Unity managed assemblies required')
 dotnet=pathlib.Path(shutil.which('dotnet')or'').resolve()
 if not dotnet.is_file():raise ValueError('Installed dotnet required')
 version=subprocess.check_output([str(dotnet),'--version'],text=True).strip();tfm='net8.0';runtime_lines=subprocess.check_output([str(dotnet),'--list-runtimes'],text=True).splitlines();runtime_line=next(x for x in reversed(runtime_lines)if x.startswith('Microsoft.NETCore.App 8.'));runtime_version=runtime_line.split()[1];runtime_root=pathlib.Path(runtime_line.split('[',1)[1].rstrip(']'))/runtime_version
 out.mkdir(parents=True);(out/'source').mkdir();(out/'original').mkdir();proof=[]
 for rel,raw in src.items():
  s=raw.decode();name=pathlib.Path(rel).name;(out/'original'/name).write_bytes(raw);mode='whole_file';details={}
  if name=='AriaSkirmishPlanSystem.cs':
   a,k=method_span(s,'public void OnUpdate(ref SystemState state)');start=s.rfind('        [BurstCompile]',0,a)
   assert start>=0 and s[start:a].strip()=='[BurstCompile]'
   removed=s[start:k];s=s[:start]+s[k:];mode='OnUpdate_only_removed';details=dict(removed_sha256=hashlib.sha256(removed.encode()).hexdigest(),start_byte=len(raw[:start]),removed_bytes=len(removed.encode()),step_and_all_helpers_verbatim=True)
  elif name=='IUiSkirmishGateway.cs':
   a,k=method_span(s,'public struct UiExpandedPrecisionAttackResult');s='namespace Game.UI.Contracts{\n'+s[a:k]+'\n}\n';mode='one_exact_contract_declaration'
  elif name=='MatchHudSelectionPanelModels.cs':
   a,k=method_span(s,'public readonly struct UiEntityHandle');s='namespace Game.UI.Contracts{\n'+s[a:k]+'\n}\n';mode='one_exact_value_contract_declaration'
  elif name=='AriaSkirmishPlanValidation.cs':
   bodies=[]
   for signature in ['public static void RunPublicPrecisionAttackValidation()']:
    a,k=method_span(s,signature);bodies.append(s[a:k])
   s='using System;using Game.UI.Contracts;using Game.UI.Shell.Ecs;using Game.UI.Shell.Contracts.Ecs;namespace Game.Editor{public static class AriaSkirmishPlanValidation{\n'+'\n'.join(bodies)+'\n}}\n';mode='one_exact_existing_validation_method'
  target=out/'source'/name;target.write_text(s);proof.append(dict(source=rel,source_sha256=hashlib.sha256(raw).hexdigest(),output=name,output_sha256=digest(target),mode=mode,**details))
 for f in HOST.glob('*.cs'):shutil.copyfile(f,out/'source'/f.name)
 # No packages or network restore sources; installed SDK reference packs only.
 (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
 (out/'global.json').write_text(json.dumps({'sdk':{'version':version,'rollForward':'disable'}}))
 refs=''.join('<Reference Include="'+f.stem+'"><HintPath>'+str(f)+'</HintPath><Private>true</Private></Reference>'for f in dlls)
 (out/'AriaReplay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>'+tfm+'</TargetFramework><RuntimeFrameworkVersion>'+runtime_version+'</RuntimeFrameworkVersion><RollForward>Disable</RollForward><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit><UseAppHost>false</UseAppHost></PropertyGroup><ItemGroup><Compile Include="source/*.cs"/>'+refs+'</ItemGroup></Project>')
 for rel,raw in src.items():
  if(repo/rel).read_bytes()!=raw:raise ValueError('Production source changed during extraction: '+rel)
 cmd=[str(dotnet),'build',str(out/'AriaReplay.csproj'),'-c','Release','--nologo','--configfile',str(out/'NuGet.Config')]
 result=subprocess.run(cmd,cwd=out,capture_output=True,text=True,timeout=120);(out/'build.log').write_text(result.stdout+result.stderr)
 if result.returncode:raise ValueError('Build failed; '+str(out/'build.log')+'\n'+result.stdout[-8000:])
 for rel,raw in src.items():
  if(repo/rel).read_bytes()!=raw:raise ValueError('Production source changed during build: '+rel)
 runtime=out/'bin/Release'/tfm
 collections_sources=list((repo/'Library/PackageCache').glob('com.unity.collections*/Unity.Collections/FixedList.gen.cs'));extra_pins=collections_sources if len(collections_sources)==1 else [];extra_pins+=[dotnet.parent/'sdk'/version/'Roslyn/bincore/csc.dll'];files=[repo/rel for rel in src]+list(HOST.glob('*.cs'))+list((out/'source').glob('*.cs'))+list((out/'original').glob('*.cs'))+list(runtime.glob('*'))+[out/'AriaReplay.csproj',out/'global.json',out/'NuGet.Config',HERE,dotnet]+dlls+[runtime_root/x for x in ['System.Private.CoreLib.dll','libcoreclr.dylib','libclrjit.dylib','libhostpolicy.dylib','coreclr.dll','clrjit.dll','hostpolicy.dll']]+extra_pins
 manifest=dict(schema_version=1,scope='planner-step-only',repo=str(repo),engine_dir=str(engine),dotnet=str(dotnet),sdk_version=version,runtime_version=subprocess.check_output([str(dotnet),'--list-runtimes'],text=True),host=str(runtime/'AriaReplay.dll'),source_extraction=proof,pins={str(p):digest(p)for p in files if p.is_file()},command=cmd,limits=dict(input_bytes=MAX_INPUT,output_bytes=MAX_OUTPUT,frames=1024),limitations=['No ECS OnUpdate lifecycle, AriaPlayDecision/Input systems, hardware input, command execution, gameplay acceptance, world advancement or native parity.','Session fields change only by original planner or explicit caller feedback; receipt annotation never changes Actions.','Actual Unity managed vectors/math; bounded copy-on-write uint FixedList adapter, not native layout/Burst/safety implementation.'])
 emit_new(out/'manifest.json',manifest);print(json.dumps(dict(result='Built',build_dir=str(out),manifest_sha256=digest(out/'manifest.json'))))
def invoke(args,mode,input_data=None):
 p=pathlib.Path(args.build_dir).resolve();manifest=read_json(p/'manifest.json')
 for name,wanted in manifest['pins'].items():
  if digest(name)!=wanted:raise ValueError('Pinned build/source/runtime changed: '+name)
 data=b''if input_data is None else json.dumps(input_data,allow_nan=False).encode()
 if len(data)>MAX_INPUT:raise ValueError('Normalized input exceeds16MiB')
 result=subprocess.run([manifest['dotnet'],manifest['host'],mode],input=data,capture_output=True,timeout=120)
 if len(result.stdout)>MAX_OUTPUT:raise ValueError('Host output exceeds64MiB')
 if result.returncode:raise ValueError(result.stderr.decode(errors='replace').strip())
 for name,wanted in manifest['pins'].items():
  if digest(name)!=wanted:raise ValueError('Pinned input changed during replay: '+name)
 answer=json.loads(result.stdout,object_pairs_hook=no_duplicates);
 if mode=='self-test':answer['validation_log']=result.stderr.decode(errors='replace').splitlines()

 if mode!='template':answer['build_manifest_sha256']=digest(p/'manifest.json')
 if input_data is not None:answer['input_sha256']=digest(args.input)
 return answer

def main():
 parser=argparse.ArgumentParser(description=__doc__);subs=parser.add_subparsers(dest='command',required=True)
 b=subs.add_parser('build');b.add_argument('--repo',default=str(HERE.parents[2]));b.add_argument('--build-dir',required=True);b.add_argument('--engine-dir')
 for name in ['template','run','self-test']:
  p=subs.add_parser(name);p.add_argument('--build-dir',required=True);p.add_argument('--output',required=True)
  if name=='run':p.add_argument('--input',required=True)
 args=parser.parse_args()
 try:
  if args.command=='build':build(args);return 0
  if pathlib.Path(args.output).exists():raise ValueError('Output exists; refuse overwrite: '+args.output)
  data=read_json(args.input)if args.command=='run'else None
  if data is not None and pathlib.Path(args.input).resolve()==pathlib.Path(args.output).resolve():raise ValueError('Input/output alias')
  answer=invoke(args,args.command,data);emit_new(args.output,answer);print(json.dumps(dict(result='Passed',command=args.command,output=str(pathlib.Path(args.output).absolute()),sha256=digest(args.output))))
  return 0
 except(Exception)as e:print('ARIA replay rejected: '+str(e),file=sys.stderr);return 2
if __name__=='__main__':sys.exit(main())
