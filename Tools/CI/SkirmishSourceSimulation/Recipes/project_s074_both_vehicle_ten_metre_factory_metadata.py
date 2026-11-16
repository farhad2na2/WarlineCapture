#!/usr/bin/env python3
"""Run the original S074 factory exporter on the both-vehicle 10m source-owner setup.

The exporter is compiled as an isolated entrypoint from the pinned current S074
source build. Its only added behavior is the existing source-only immutable S026
factory-context export. No world/native capture or readiness is performed.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, subprocess, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
HOST=ROOT/'T Logs/SkirmishProduction/s074-ten-metre-player-vehicle-source-build-20261114-r2'
AUTHOR=ROOT/'T Logs/SkirmishProduction/s074-both-vehicle-ten-metre-authoring-20261114-r1'
BINDING_REPLAY=ROOT/'T Logs/SkirmishProduction/s074-both-vehicle-ten-metre-origin-binding-20261114-r1'
ORIGIN_NATIVE=ROOT/'Logs/SkirmishProduction/s026-current-factory-origin-20261107-r1/native-origin.json'
TEMPLATE_INITIALIZATION=ROOT/'T Logs/SkirmishProduction/s026-direct-layout-runtime-inputs-20261107-r1/initialization.json'
SOURCE=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMissionAuthoringTool.cs'
EXPORT=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.DeclaredExport.cs'

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def need(ok,msg):
    if not ok: raise ValueError(msg)
def load_module(name,path):
    spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def run():
    global HOST, BINDING_REPLAY
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output-dir',type=Path,required=True);parser.add_argument('--host-build',type=Path,default=HOST);parser.add_argument('--binding-replay',type=Path,default=BINDING_REPLAY);a=parser.parse_args()
    out=a.output_dir.resolve();need(out.is_relative_to(ROOT/'T Logs') and not out.exists(),'Fresh repository T Logs output required')
    HOST=a.host_build.resolve(strict=True);BINDING_REPLAY=a.binding_replay.resolve(strict=True)
    need(HOST.is_relative_to(ROOT) and HOST.is_dir() and BINDING_REPLAY.is_relative_to(ROOT) and BINDING_REPLAY.is_dir(),'Repository-owned host and binding directories required')
    host_build=json.loads((HOST/'build.json').read_bytes());author_result=json.loads((AUTHOR/'authoring-result.json').read_bytes());author_receipt=json.loads((AUTHOR/'authoring-receipt.json').read_bytes());request=json.loads((AUTHOR/'authoring-request.json').read_bytes())
    binding_receipt=json.loads((BINDING_REPLAY/'receipt.json').read_bytes());binding=json.loads((BINDING_REPLAY/'owner/mission-logical-binding.json').read_bytes());binding_sha=sha(BINDING_REPLAY/'owner/mission-logical-binding.json')
    need(binding_receipt.get('source_and_reference_pins',{}).get(str(HOST/'build.json'))==sha(HOST/'build.json'),'Binding replay must pin the selected host build')
    need(host_build.get('status')=='Passed' and host_build.get('mission')=='S074' and host_build.get('seed')==104803 and host_build.get('compile_exit')==0,'Passed current S074 original source owner build required')
    host_pins=host_build.get('source_and_reference_pins');need(isinstance(host_pins,dict) and host_pins,'Host pin closure required')
    for path,digest in host_pins.items(): need(Path(path).is_file() and sha(path)==digest,'Host source drift: '+path)
    setup_hash=binding_receipt.get('setup_hash')
    need(isinstance(setup_hash,int) and author_result.get('status')=='ConfigurationCompiled' and author_result.get('compiler_accepted') is True and author_result.get('catalog_id')=='S074' and author_result.get('seed')==104803 and author_result.get('resolved_setup',{}).get('setup_hash')==setup_hash and author_result.get('layout_fingerprint')==binding_receipt.get('layout_fingerprint') and author_result.get('world_allocated') is False and author_result.get('source_fit_verified') is False and author_result.get('mission_ready') is False,'Current S074 author result must match fresh bound setup')
    need(binding_receipt.get('status')=='PassedSourceOwnerCompile' and binding_receipt.get('setup_hash')==setup_hash and binding_receipt.get('mission_ready') is False and binding_receipt.get('native_context_qualified') is False and binding_receipt.get('source_fit_certified') is False and binding_receipt.get('camera_framing_observed') is False,'Provenance-complete both-vehicle 10m binding replay required')
    need(request.get('binding',{}).get('sha256')==binding_sha and request.get('binding',{}).get('sha256')==sha(BINDING_REPLAY/'owner/mission-logical-binding.json') and request.get('layout',{}).get('sha256')==sha(ROOT/'T Logs/SkirmishProduction/s074-provisional-both-vehicle-ten-metre-20261114-r1/layout.json'),'Author request must bind exact 10m raw binding and layout')
    need(author_receipt.get('process_exit')==0 and author_receipt.get('timed_out') is False and author_receipt.get('mission_ready') is False and author_receipt.get('source_fit_verified') is False,'Passed source-only bound author receipt required')
    native=json.loads(ORIGIN_NATIVE.read_bytes());need(native.get('kind')=='QualifiedNativeMetadataOrigin' and native.get('schema_version')==1,'Immutable S026 native origin descriptor required')
    host_runtime=host_build.get('runtime_selection',{});need(host_runtime.get('mono') and host_runtime.get('engine') and host_runtime.get('newtonsoft'),'Host runtime selection required')

    out.mkdir(parents=True);owner=out/'owner';owner.mkdir()
    generated=owner/'SourceMissionAuthoringTool.s074factory.cs'
    entrypoint=owner/'SourceMissionAuthoringToolMain.cs'
    original=SOURCE.read_text()
    anchor='if(compiled&&nativeBinding!=null&&(string)nativeBinding["kind"]=="NativeAuthoredS026DeclaredLogicalBindingReference")'
    need(original.count(anchor)==1,'Exact original author owner export insertion point required')
    injected=('if(compiled){var exported=Game.Runtime.SourceMetadataCompatibility.ExportDerivedContext(setup,definition,'+json.dumps(str(ROOT))+','+json.dumps(str(ORIGIN_NATIVE))+','+json.dumps(sha(ORIGIN_NATIVE))+');File.WriteAllText(Path.Combine(directory,"source-factory-context.json"),exported.ToString()+Environment.NewLine);}')
    generated.write_text(original.replace(anchor,injected+anchor))
    entrypoint.write_text('internal static class SourceMissionAuthoringToolMain{private static int Main(string[] args){if(SourceMissionAuthoringTool.TryRun(args,out int code))return code;System.Console.Error.WriteLine("Authoring-only exporter arguments required.");return 2;}}\n')

    argv=[json.loads(x) for x in (HOST/'compile.rsp').read_text().splitlines() if x.strip()]
    main='-main:SourceSimulationProgram';oldout='-out:'+str(HOST/'SourceSimulation.exe')
    need(argv.count(main)==1 and argv.count(oldout)==1 and argv.count(str(SOURCE))==1 and argv.count(str(EXPORT))==0,'Exact current S074 compiler response required')
    replacements={
      str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.cs'):str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceMetadataCompatibility.s003.cs'),
      str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.Derived.cs'):str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceMetadataCompatibility.Derived.s003.cs'),
      str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceConfigurationQualification.cs'):str(ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceConfigurationQualification.s003.cs')}
    for old in replacements: need(argv.count(old)==1 and Path(replacements[old]).is_file(),'Exact original exporter derived-owner substitution required: '+old)
    argv=[('-main:SourceMissionAuthoringToolMain' if x==main else '-out:'+str(owner/'SourceSimulation.export.exe') if x==oldout else str(generated) if x==str(SOURCE) else replacements.get(x,x)) for x in argv]
    argv.extend((str(EXPORT),str(entrypoint)));rsp=owner/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in argv))
    csc=next((Path(x) for x in host_pins if x.endswith('/Roslyn/bincore/csc.dll')),None);dotnet=next((Path(x) for x in host_pins if Path(x).name=='dotnet'),None)
    need(csc and dotnet and sha(csc)==host_pins[str(csc)] and sha(dotnet)==host_pins[str(dotnet)],'Pinned compiler runtime required')
    executable=owner/'SourceSimulation.export.exe'
    compiled=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True,timeout=300)
    (owner/'compile.log').write_text(compiled.stdout+compiled.stderr)
    need(compiled.returncode==0 and executable.is_file(),'Isolated original exporter compile failed; see owner/compile.log')

    # Copy the compiled request, making only its output path fresh and binding path explicit to r7.
    source_request=AUTHOR/'authoring-request.json';fresh_request=json.loads(source_request.read_bytes());before={k:v for k,v in fresh_request.items() if k not in ('binding','output_directory')}
    fresh_request['binding']={'path':str((BINDING_REPLAY/'owner/mission-logical-binding.json').resolve()),'sha256':binding_sha}
    fresh_request['output_directory']=str((out/'s074').resolve())
    need({k:v for k,v in fresh_request.items() if k not in ('binding','output_directory')}==before,'Unexpected authoring request mutation')
    s074=out/'s074';s074.mkdir()
    request_path=s074/'authoring-request.json';request_path.write_text(json.dumps(fresh_request,indent=2)+'\n')
    runtime_config=Path(fresh_request['runtime_config']['path']);need(runtime_config.is_file() and sha(runtime_config)==fresh_request['runtime_config']['sha256'],'Pinned author runtime config required')
    env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((host_runtime['engine'],str(Path(host_runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(HOST/'placement')))
    run=subprocess.run([host_runtime['mono'],'--config',str(runtime_config),'--optimize=-float32',str(executable),'--authoring-preflight',str(request_path),sha(request_path)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=600)
    process_log=s074/'process.log';process_log.write_text(run.stdout+run.stderr)
    result_path=s074/'authoring-result.json';context_path=s074/'source-factory-context.json'
    need(run.returncode==0 and result_path.is_file() and context_path.is_file(),'Original S074 source factory exporter failed; preserve owner/process evidence')
    exported_result=json.loads(result_path.read_bytes());context=json.loads(context_path.read_bytes());target=context.get('target_context',{})
    need(exported_result.get('compiler_accepted') is True and exported_result.get('resolved_setup',{}).get('setup_hash')==setup_hash and target.get('setup_hash')==setup_hash and target.get('catalog_id')=='S074' and target.get('seed')==104803 and target.get('size')=='Standard' and target.get('difficulty')=='Regular','Fresh original factory context differs from current bound author result')
    need(exported_result.get('resolved_setup')==author_result.get('resolved_setup') and exported_result.get('layout_fingerprint')==author_result.get('layout_fingerprint') and context.get('world_allocated') is False and context.get('identities_allocated')==0 and context.get('runtime_loader_admitted') is False and context.get('source_fit_certified') is False and context.get('active_mission_water_qualified') is False and context.get('mission_ready') is False,'Fresh context must reproduce compiled author facts and remain source-only')
    need(context.get('native_factory_reference')==json.loads((BINDING_REPLAY/'candidate-compiler-context.json').read_bytes()).get('native_factory_reference') if 'native_factory_reference' in json.loads((BINDING_REPLAY/'candidate-compiler-context.json').read_bytes()) else context.get('native_factory_reference')==json.loads(ORIGIN_NATIVE.read_bytes()).get('native_metadata'),'Native S026 metadata reference changed')

    # Use the checked shared-factory projection core; no metadata is authored here.
    projector=load_module('s074_checked_factory_projector',ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/project_s074_source_factory.py')
    source_inputs=dict(host_pins)
    projector_path=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/project_s074_source_factory.py'
    script_path=Path(__file__).resolve()
    for path in (HOST/'build.json',HOST/'compile.rsp',SOURCE,EXPORT,generated,entrypoint,rsp,executable,source_request,request_path,runtime_config,AUTHOR/'authoring-result.json',AUTHOR/'authoring-receipt.json',BINDING_REPLAY/'receipt.json',BINDING_REPLAY/'owner/mission-logical-binding.json',ORIGIN_NATIVE,context_path,result_path,process_log,projector_path,script_path,*[Path(x) for x in replacements.values()]): source_inputs[str(path.resolve())]=sha(path)
    replay_receipt={'kind':'S074OriginalFactoryContextDiagnostic','status':'PassedSourceFactoryContext','process_exit':0,'timed_out':False,'source_only':True,'world_allocated':False,'native_context_qualified':False,'source_fit_certified':False,'mission_ready':False,'source_inputs':source_inputs,'result_sha256':sha(result_path),'context_sha256':sha(context_path),'process_log_sha256':sha(process_log),'replay_evidence':{'bound_author_result_sha256':sha(AUTHOR/'authoring-result.json'),'binding_replay_receipt_sha256':sha(BINDING_REPLAY/'receipt.json'),'observed_setup_hash':target['setup_hash']},'scope':'Fresh original compiler/exporter on the exact both-vehicle 10m S074 candidate; immutable native S026 factory facts only; no world, S074 native capture, gameplay, fit or readiness.'}
    replay_receipt_path=out/'factory-context-replay-receipt.json';replay_receipt_path.write_text(json.dumps(replay_receipt,indent=2,sort_keys=True)+'\n')
    projection=projector._project_core(context_path,replay_receipt_path,TEMPLATE_INITIALIZATION,out/'projection',replay_setup_hash=target['setup_hash'])
    need(projection.get('status')=='ProjectedSourceOnly' and projection.get('target_context',{}).get('setup_hash')==setup_hash and projection.get('world_allocated') is False and projection.get('runtime_admission') is False and projection.get('source_fit_certified') is False and projection.get('mission_ready') is False,'Immutable S026 factory projection failed its source-only boundary')

    closure=dict(source_inputs)
    for name,digest in projection.get('inputs',{}).items():
        need(name not in closure or closure[name]==digest,'Projection input conflicts with exporter closure: '+name);closure[name]=digest
    final={'kind':'S074BothVehicleTenMetreOriginalFactoryMetadataProjection','status':'ProjectedSourceOnly','target_context':target,'bound_author_result':{'path':str(AUTHOR/'authoring-result.json'),'sha256':sha(AUTHOR/'authoring-result.json')},'binding_replay':{'path':str(BINDING_REPLAY/'receipt.json'),'sha256':sha(BINDING_REPLAY/'receipt.json')},'source_factory_context':{'path':str(context_path.resolve()),'sha256':sha(context_path)},'source_factory_replay_receipt':{'path':str(replay_receipt_path.resolve()),'sha256':sha(replay_receipt_path)},'projection_receipt':{'path':str((out/'projection/receipt.json').resolve()),'sha256':sha(out/'projection/receipt.json')},'factory_templates':{'path':str(TEMPLATE_INITIALIZATION.resolve()),'sha256':sha(TEMPLATE_INITIALIZATION),'scope':'Immutable S026 shared factory equality templates only; not an S074 initializer'},'immutable_native_origin':{'path':str(ORIGIN_NATIVE.resolve()),'sha256':sha(ORIGIN_NATIVE)},'source_and_reference_pins':closure,'world_allocated':False,'identities_allocated':0,'native_S050_capture':False,'native_context_qualified':False,'active_mission_water_qualified':False,'runtime_loader_admitted':False,'source_fit_certified':False,'mission_ready':False,'camera_framing_observed':False,'scope':'Original C# compiler/exporter projected immutable S026 shared factory facts for fresh S074 both-vehicle 10m candidate setup identity; source-only metadata projection, not readiness.'}
    receipt_path=out/'receipt.json';receipt_path.write_text(json.dumps(final,indent=2,sort_keys=True)+'\n')
    print(json.dumps({'status':final['status'],'setup_hash':target['setup_hash'],'context':final['source_factory_context'],'projection':final['projection_receipt'],'mission_ready':False}))

if __name__=='__main__':
    try:run()
    except Exception as e:
        print('[S074BothVehicleTenMetreFactoryProjection] status=Failed '+str(e),file=sys.stderr);raise
