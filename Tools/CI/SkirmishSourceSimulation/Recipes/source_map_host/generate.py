#!/usr/bin/env python3
"""Generate a source-only host overlay consuming a preflighted map package."""
import argparse,hashlib,json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
RUNTIME=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/SourceDerivedMapRuntime.cs'
CLEARANCE=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/527_GeneratedS002DropClearance.cs'
SOURCE_PROGRAM=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceSimulationProgram.cs'
STARTING_WORLD_SOURCE=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/307_GeneratedS002StartingWorld.lifetime.cs'
DELIVERY_BASIS_COMPONENT=ROOT/'Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionDeliveryBasisComponent.cs'
S074_NO_CAMERA_HOST=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/S026/000-000_AutonomousWorld.cs'
SOURCE_DECLARED_INPUTS=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/SourceDeclaredRuntimeInputs.cs'
GENERATOR=Path(__file__).resolve()
PROOF_SEED=ROOT/'T Logs/SkirmishProduction/source-map-host-overlay-20261107-r32'
ALIAS_SEAM_ROOT=ROOT/'T Logs/SkirmishProduction/s074-source-derived-worker-alias-overlay-20261107-r3'
ALIAS_SEAM_RECEIPT_SHA='9697b8934b86957fa82a50696189d19ea9d5fd66880bfe8a7e2f9b5bc1fc6d8e'
ALIAS_SEAM_RUNTIME_SHA='4323cca6f022f9876c6c3fba93cae612c0231c7d597b11f429bdfc93ae90ff40'
ALIAS_OUTPUTS={
    'S026_000_AutonomousWorld.cs':('AutonomousWorld.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_autonomous_world.py'),
    '527_GeneratedS002DropClearance.cs':('SourceDerivedClearance.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_source_clearance.py'),
}
STARTING_WORLD_OUTPUT=('StartingWorld.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_starting_world.py')
PROGRAM_OUTPUT=('SourceSimulationProgram.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_source_program_receipt.py')
DECLARED_OUTPUT=('SourceDeclaredRuntimeInputs.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_declared_runtime_inputs.py')
ALIAS_RUNTIME=('SourceDerivedMapRuntime.cs','Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate_source_derived_runtime.py')

def digest(data): return hashlib.sha256(data).hexdigest()
def once(text,old,new):
    if text.count(old)!=1: raise ValueError('Expected one exact host anchor: '+old[:120])
    return text.replace(old,new,1)

def live_owner_result_handoff():
    """Return diagnostics from the actual world owner; never synthesize install proof."""
    return ('var installedSourceMap=world.SourceDerivedMapReceipt;'
        'var startupReadback=JObject.Parse(File.ReadAllText(Path.Combine(dir,"publication-startup.json")));'
        'result["source_derived_map_live_owner_diagnostic"]=new JObject{'
        '["kind"]="SourceDerivedMapLiveOwnerBindingDiagnostic",'
        '["prepared_runtime_present"]=preparedSourceMap!=null,'
        '["world_install_receipt_present"]=installedSourceMap!=null,'
        '["descriptor"]=installedSourceMap==null?null:installedSourceMap["descriptor"]?.DeepClone(),'
        '["installed_water_sha256"]=installedSourceMap==null?null:installedSourceMap["installed_water_sha256"]?.DeepClone(),'
        '["installed_buildings_sha256"]=installedSourceMap==null?null:installedSourceMap["installed_buildings_sha256"]?.DeepClone(),'
        '["installed_roads_sha256"]=installedSourceMap==null?null:installedSourceMap["installed_roads_sha256"]?.DeepClone(),'
        '["installed_sidewalks_sha256"]=installedSourceMap==null?null:installedSourceMap["installed_sidewalks_sha256"]?.DeepClone(),'
        '["navigation_rows_sha256"]=installedSourceMap==null?null:installedSourceMap["navigation_rows_sha256"]?.DeepClone(),'
        '["installed_navigation_count"]=installedSourceMap==null?null:installedSourceMap["installed_navigation_count"]?.DeepClone(),'
        '["clearance_descriptor_sha256"]=installedSourceMap==null?null:installedSourceMap["clearance_descriptor_sha256"]?.DeepClone(),'
        '["source_only"]=true,["source_fit_certified"]=false,["mission_ready"]=false};'
        'result["source_owner_setup_readback"]=new JObject{'
        '["kind"]="ActualSourceOwnerSetupAndCadenceReadback",'
        '["catalog_id"]=world.Setup.CatalogId,["seed"]=world.Setup.Seed,'
        '["setup_hash"]=world.Setup.SetupHash,["operation_map_id"]=world.Setup.OperationMapId,'
        '["resource_preset_input"]=admitted["resource_preset"]?.DeepClone(),'
        '["source_owner_supply_day_seconds"]=world.SourceOwnerResourceSecondsPerDay,'
        '["source_owner_cadence_initialized"]=true,'
        '["publication_source_selection"]=startupReadback["source_selection"]?.DeepClone(),'
        '["publication_metadata_hash"]=startupReadback["metadata_hash"]?.DeepClone(),'
        '["initial_cadence_completions"]=startupReadback["initial_cadence_completions"]?.DeepClone(),'
        '["publication_order"]=startupReadback["order"]?.DeepClone(),'
        '["preset_cadence_value_readback"]=true,["source_only"]=true,["mission_ready"]=false};'
        'if(installedSourceMap!=null)result["source_derived_map"]=installedSourceMap;'
        'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);')

def strip_existing_source_map_host_fields(host):
    staged='        private SourceStagedLaunch stagedLaunch;'
    old=('        private SourceStagedLaunch stagedLaunch;\n        private SourceDerivedMapRuntime sourceDerivedMap;\n'
         '        internal JObject SourceDerivedMapReceipt { get; private set; }\n'
         '        internal void BindSourceDerivedMap(SourceDerivedMapRuntime value){if(sourceDerivedMap!=null)throw new InvalidOperationException("Source-derived map already bound");sourceDerivedMap=value;}')
    strict=('        private SourceStagedLaunch stagedLaunch;\n        private SourceDerivedMapRuntime sourceDerivedMap;\n'
         '        internal JObject SourceDerivedMapReceipt { get; private set; }\n'
         '        internal void BindSourceDerivedMap(SourceDerivedMapRuntime value){if(value==null||sourceDerivedMap!=null)throw new InvalidOperationException("Source-derived map binding must be nonnull and single-use");sourceDerivedMap=value;}')
    if host.count(strict)==1:
        return host.replace(strict,staged,1)
    if host.count(old)==1:
        return host.replace(old,staged,1)
    # Fresh staged S026 handoffs have the staged-launch field but no map overlay.
    # Accept that exact bare shape; reject partial or ambiguous prior overlays.
    if host.count(staged)==1 and all(token not in host for token in
        ('SourceDerivedMapRuntime','SourceDerivedMapReceipt','BindSourceDerivedMap','sourceDerivedMap')):
        return host
    raise ValueError('Expected exact prior source-map host fields or a bare staged S026 host')

def strip_existing_source_map_starting_world(world):
    pairs=(
        ('if(sourceDerivedMap!=null){AuthoredNavigationCount=GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation);}else if(inactive!=null){if(Setup.CatalogId!=', 'if(inactive!=null){if(Setup.CatalogId!='),
        ('if(sourceDerivedMap!=null)domain=sourceDerivedMap.Domain;else if(inactive!=null)domain=', 'if(inactive!=null)domain='),
        ('var waterMask=sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);', 'var waterMask=domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);'),
        (' if(sourceDerivedMap!=null)SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt(masks.AuthoredBuildings,roads,masks.Sidewalks,waterMask,AuthoredNavigationCount);\n if(StartingPipeline.SubmitOrBind(Session,Setup,out var reason)', ' if(StartingPipeline.SubmitOrBind(Session,Setup,out var reason)'),
    )
    present=[world.count(overlay) for overlay,_ in pairs]
    markers=('sourceDerivedMap','SourceDerivedMapReceipt')
    if present==[1]*len(pairs):
        for overlay,canonical in pairs:
            world=once(world,overlay,canonical)
        if any(marker in world for marker in markers):
            raise ValueError('Partial source-map StartingWorld overlay remains after exact strip')
        return world
    canonical_anchors=(pairs[0][1],pairs[1][1],pairs[2][1],pairs[3][1])
    if present==[0]*len(pairs) and not any(marker in world for marker in markers) and all(world.count(anchor)==1 for anchor in canonical_anchors):
        return world
    raise ValueError('Expected exact prior source-map StartingWorld overlay or exact bare staged S026 owner')

def validate_clearance_overlay_path(canonical_source, overlay_source, root):
    root=Path(root).resolve(strict=True)
    canonical=Path(canonical_source).resolve(strict=True)
    overlay=Path(overlay_source)
    if overlay.is_symlink() or not overlay.is_file():
        raise ValueError('Regular repository-owned source-map clearance overlay required')
    overlay=overlay.resolve(strict=True)
    if not canonical.is_relative_to(root) or not overlay.is_relative_to(root):
        raise ValueError('Canonical and overlay clearance sources must remain repository-owned')
    return canonical,overlay

def configure_proof_seed(seed_path):
    global PROOF_SEED
    seed=Path(seed_path).resolve(strict=True)
    if not seed.is_relative_to(ROOT) or seed.is_symlink() or not seed.is_dir():
        raise ValueError('Repository-owned current source-map proof seed required')
    receipt_path=seed/'generation-receipt.json'
    if receipt_path.is_symlink() or not receipt_path.is_file():
        raise ValueError('Regular source-map proof seed receipt required')
    receipt=json.loads(receipt_path.read_bytes())
    if receipt.get('status')!='GeneratedSourceOnly' or receipt.get('mission_ready') is not False:
        raise ValueError('Current source-only proof seed receipt required')
    inputs=receipt.get('inputs');outputs=receipt.get('outputs')
    if not isinstance(inputs,dict) or not isinstance(outputs,dict) or not inputs or not outputs:
        raise ValueError('Exact source-map proof seed input/output pins required')
    if inputs.get(str(GENERATOR.resolve()))!=digest(GENERATOR.read_bytes()):
        raise ValueError('Proof seed must bind the current source-map generator')
    required={'AutonomousWorld.cs','StartingWorld.cs','SourceSimulationProgram.cs','SourceDerivedClearance.cs'}
    names=set()
    for collection,label in ((inputs,'input'),(outputs,'output')):
        for path_text,expected in collection.items():
            path=Path(path_text)
            if not path.is_absolute() or path.resolve(strict=True)!=path or not path.is_relative_to(ROOT):
                raise ValueError(f'Canonical repository-owned proof seed {label} path required: {path_text}')
            if path.is_symlink() or not path.is_file() or digest(path.read_bytes())!=expected:
                raise ValueError(f'Current proof seed {label} pin changed: {path_text}')
            if label=='output': names.add(path.name)
    if not required.issubset(names):
        raise ValueError('Complete current source-map proof seed outputs required')
    PROOF_SEED=seed

def starting_world_seed_wrapper(seed_path):
    seed=Path(seed_path).resolve(strict=True)
    if not seed.is_relative_to(ROOT):
        raise ValueError('Repository-owned source-map proof seed required')
    relative=seed.relative_to(ROOT).as_posix()
    return '''from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

def generate(repo, output):
    root=Path(repo).resolve(strict=True)
    seed=root/'''+repr(relative)+'''
    helper=root/"Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py"
    spec=spec_from_file_location("source_map_host_generator", helper)
    module=module_from_spec(spec); spec.loader.exec_module(module)
    module.configure_proof_seed(seed)
    return module.generate_starting_world_output(root, output, Path(__file__))
'''

def seed_output_wrapper(seed_path, output_name):
    seed=Path(seed_path).resolve(strict=True)
    if not seed.is_relative_to(ROOT) or output_name not in {item[0] for item in ALIAS_OUTPUTS.values()}:
        raise ValueError('Repository-owned source-map alias seed and exact output required')
    relative=seed.relative_to(ROOT).as_posix()
    return '''from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path

def generate(repo, output):
    root=Path(repo).resolve(strict=True)
    seed=root/'''+repr(relative)+'''
    helper=root/"Tools/CI/SkirmishSourceSimulation/Recipes/source_map_host/generate.py"
    spec=spec_from_file_location("source_map_host_generator", helper)
    module=module_from_spec(spec); spec.loader.exec_module(module)
    module.configure_proof_seed(seed)
    return module.generate_seed_output(root, output, '''+repr(output_name)+''', Path(__file__))
'''

def merge_s074_no_camera_host(host, current_host):
    declaration='        private bool sourceS074DeathReadsetQualified;\n        internal bool S074DeathReadsetQualified=>sourceS074DeathReadsetQualified;\n        internal string[] SourceQualifiedPrefabKeys=>(sourceQualifiedPrefabKeys??Array.Empty<string>()).ToArray();\n'
    if current_host.count(declaration)!=1: raise ValueError('Current canonical host no-camera declaration shape changed')
    new_signature='        internal void Initialize(string path, string sha256, float fixedDeltaTime, string metadataSourceRepo=null, string metadataDataRepo=null, bool s074NoCameraDiagnostic=false)'
    if declaration in host:
        if host.count(declaration)!=1 or host.count(new_signature)!=1: raise ValueError('Existing generated S074 no-camera host shape changed')
        block_start='            var deathDescriptor=request["death_mechanical_inputs"] as JObject;\n'
        block_end='            Entities.LogicalAllocatorAdmitted = true;\n'
        hs=host.index(block_start);he=host.index(block_end,hs)
        cs=current_host.index(block_start);ce=current_host.index(block_end,cs)
        stage='            InitializationStage = "ConstructingBakedCampaignMap";\n'
        expected=current_host[cs:ce].replace(stage,'',1)
        existing=host[hs:he].replace(stage,'',1)
        if existing==expected:
            return host
        if existing!=expected:
            s050_selector='            var s050DeathDiagnostic=Setup.CatalogId=="S050"&&deathDescriptor?.Count==1?deathDescriptor["s050_source_diagnostic"] as JObject:null;\n'
            s050_dispatch='                : s050DeathDiagnostic!=null\n                    ? SourceNativeDeathReadModel.LoadS050SourceDiagnostic(s050DeathDiagnostic)\n'
            comparable=expected.replace(s050_selector,'',1).replace(s050_dispatch,'',1)
            if comparable!=existing: raise ValueError('Existing generated S074 death-readset branch differs outside the exact S050 source-diagnostic dispatch')
            host=host[:hs]+expected+stage+host[he:]
        return host
    host=once(host,'        internal float FixedDeltaTime { get; private set; }\n','        internal float FixedDeltaTime { get; private set; }\n'+declaration)
    old_signature='        internal void Initialize(string path, string sha256, float fixedDeltaTime, string metadataSourceRepo=null, string metadataDataRepo=null)'
    if current_host.count(new_signature)!=1: raise ValueError('Current canonical host no-camera Initialize signature changed')
    host=once(host,old_signature,new_signature)
    block_start='            var deathDescriptor=request["death_mechanical_inputs"] as JObject;\n'
    block_end='            InitializationStage = "ConstructingBakedCampaignMap";\n'
    source_start=current_host.index(block_start)
    source_end=current_host.index(block_end,source_start)
    replacement=current_host[source_start:source_end]
    old='            var deathDescriptor=request["death_mechanical_inputs"] as JObject;\n            var inactiveDiagnostic=request["placement_domain"]?["s026_inactive_diagnostic"] as JObject;\n            var deathInputs=deathDescriptor?.Count==5 && Setup.CatalogId=="S026" && inactiveDiagnostic!=null\n                ? SourceNativeDeathReadModel.LoadS026Diagnostic(deathDescriptor)\n                : SourceNativeDeathReadModel.LoadAtBoundary(Descriptor(request["death_mechanical_inputs"],1024*1024));\n            SourceNativeDeathBinding.BindAtBoundary(Entities,deathInputs);\n            sourceQualifiedPrefabKeys=deathInputs.QualifiedPrefabKeys;\n            InitializationStage = "ConstructingBakedCampaignMap";\n'
    if replacement.count('SourceNativeDeathReadModel.LoadS074NoCameraDiagnostic')!=1 or replacement.count('sourceS074DeathReadsetQualified=s074NoCameraDiagnostic;')!=1:
        raise ValueError('Current canonical host death readset branch changed')
    host=once(host,old,replacement)
    return host

def generate_seed_output(repo, output, output_name, wrapper_path):
    repo=Path(repo).resolve(strict=True); seed=PROOF_SEED.resolve(strict=True)
    if not seed.is_relative_to(repo): raise ValueError('Experimental proof seed must remain inside the repository')
    receipt_path=seed/'generation-receipt.json';receipt=json.loads(receipt_path.read_text())
    if receipt.get('status')!='GeneratedSourceOnly' or receipt.get('mission_ready') is not False: raise ValueError('Current source-only proof seed receipt required')
    source_pins=dict(receipt.get('inputs',{}));output_pins=dict(receipt.get('outputs',{}))
    if not source_pins or output_name not in {Path(path).name for path in output_pins}: raise ValueError('Proof seed output not declared')
    for path,expected in {**source_pins,**output_pins}.items():
        candidate=Path(path)
        if not candidate.is_absolute() or candidate.is_symlink() or not candidate.is_file() or digest(candidate.read_bytes())!=expected: raise ValueError('Experimental proof seed pin changed: '+path)
    source_pins[str(receipt_path)]=digest(receipt_path.read_bytes())
    wrapper_path=Path(wrapper_path).resolve(strict=True)
    source_pins[str(wrapper_path)]=digest(wrapper_path.read_bytes())
    source_pins[str(GENERATOR)]=digest(GENERATOR.read_bytes())
    seed_output=next(Path(path) for path in output_pins if Path(path).name==output_name)
    output.mkdir(parents=True,exist_ok=True)
    (output/output_name).write_bytes(seed_output.read_bytes())
    return {'experimental_only':True,'mission_ready':False,'native_or_player_readiness':False,
        'scope':'Source-map alias copied byte-for-byte from a verified generated source-only overlay receipt.',
        'source_pins':source_pins,'outputs':{output_name:output_pins[str(seed_output)]}}

def proof_seed_inputs():
    seed=PROOF_SEED.resolve(strict=True);receipt_path=seed/'generation-receipt.json';receipt=json.loads(receipt_path.read_text())
    paths=set(receipt.get('inputs',{}))|set(receipt.get('outputs',{}))|{str(receipt_path)}
    return sorted(str(Path(path).resolve(strict=True).relative_to(ROOT)) for path in paths)

def generate_logical_alias_runtime(repo, output, wrapper_path):
    repo=Path(repo).resolve(strict=True); output=Path(output);receipt_path=ALIAS_SEAM_ROOT/'worker-seam-receipt.json'
    def add_pin(path,sha):
        actual=Path(path)
        if actual.is_symlink() or not actual.is_file():raise ValueError('Alias proof dependency must be a regular non-symlink file: '+str(actual))
        actual=actual.resolve(strict=True)
        if not actual.is_relative_to(repo) or digest(actual.read_bytes())!=sha:raise ValueError('Alias proof dependency escaped repository or changed: '+str(actual))
        pins[str(actual)]=sha
    if not receipt_path.is_file() or digest(receipt_path.read_bytes())!=ALIAS_SEAM_RECEIPT_SHA:raise ValueError('Retained S074 alias seam receipt pin changed')
    receipt=json.loads(receipt_path.read_text())
    if receipt.get('status')!='RoslynCompilePassed;OriginalValidatorReplayPassed;WorkerPrepareAliasPassed;TamperRejected' or receipt.get('kind')!='S074ExperimentalSourceDerivedLogicalAliasWorkerSeam':raise ValueError('Validated retained S074 alias seam required')
    runtime_path=ALIAS_SEAM_ROOT/'SourceDerivedMapRuntime.cs';build_path=ALIAS_SEAM_ROOT/'build.json'
    runtime_sha=digest(runtime_path.read_bytes());build_sha=digest(build_path.read_bytes())
    if runtime_sha!=ALIAS_SEAM_RUNTIME_SHA or build_sha!=(receipt.get('fresh_original_join') or {}).get('source_build',{}).get('sha256'):raise ValueError('Retained S074 alias runtime/build pin changed')
    if (receipt.get('overlay_behavior') or {}).get('native_claims') is not False or (receipt.get('overlay_behavior') or {}).get('world') is None:raise ValueError('Retained alias seam scope changed')
    selected=receipt.get('selected_alias') or {}
    if selected.get('logical_map_id')!='opmap.skirmish.industrial_basin' or selected.get('physical_map_id')!='opmap.skirmish.desert_base_01':raise ValueError('Unexpected retained logical-to-physical map join')
    code=runtime_path.read_text()
    if code.count('ValidateLogicalJoin')<2 or code.count('VerifyLogicalSelection')!=1 or 'IsLogicalAlias' not in code:raise ValueError('Retained alias runtime owner shape changed')
    pins={}
    add_pin(receipt_path,ALIAS_SEAM_RECEIPT_SHA);add_pin(runtime_path,runtime_sha);add_pin(build_path,build_sha)
    join=(receipt.get('fresh_original_join') or {});probe=(receipt.get('worker_probe') or {});package=(receipt.get('source_package') or {});graph=(receipt.get('source_graph') or {})
    preflight=join.get('package_preflight') or {}
    for path,sha in ((join.get('path'),join.get('sha256')),(preflight.get('path'),preflight.get('sha256'))):
        if path and sha:
            actual=Path(path)
            add_pin(actual,sha)
    for path,sha in ((package.get('path'),package.get('sha256')),(graph.get('path'),graph.get('sha256')),
                     ((join.get('compiler_response') or {}).get('path'),(join.get('compiler_response') or {}).get('sha256')),
                     ((receipt.get('base_build') or {}).get('path'),(receipt.get('base_build') or {}).get('sha256')),
                     (probe.get('source'),probe.get('source_sha256')),(probe.get('executable'),probe.get('executable_sha256'))):
        if not path or not sha:continue
        actual=Path(path)
        add_pin(actual,sha)
    # The exact historical output is copied to this isolated generated source row.
    output.mkdir(parents=True,exist_ok=True);target=output/'SourceDerivedMapRuntime.cs';target.write_bytes(runtime_path.read_bytes())
    wrapper_path=Path(wrapper_path).resolve(strict=True);pins[str(wrapper_path)]=digest(wrapper_path.read_bytes());pins[str(GENERATOR)]=digest(GENERATOR.read_bytes())
    for path,sha in pins.items():
        if digest(Path(path).read_bytes())!=sha:raise ValueError('Alias proof input changed during generation: '+path)
    return {'experimental_only':True,'mission_ready':False,'native_or_player_readiness':False,
        'scope':'Retained S074 logical alias owner from the validated isolated worker seam; no native readiness claim.',
        'source_pins':pins,'outputs':{'SourceDerivedMapRuntime.cs':runtime_sha}}

def generate_program_output(repo, output, wrapper_path):
    source=SOURCE_PROGRAM.resolve(strict=True); text=source.read_text()
    text=once(text,'internal static class SourceSimulationProgram {','internal static class SourceSimulationProgram {\n static SourceDerivedMapRuntime preparedSourceMap;')
    anchor='else foreach(string name in new[]{"camera_profile","camera_values","camera_motion"})Input(run,name);return initial;'
    text=once(text,anchor,'else foreach(string name in new[]{"camera_profile","camera_values","camera_motion"})Input(run,name);if(run["source_derived_map"]!=null){if(preparedSourceMap!=null)throw new InvalidOperationException("Source map preflight already performed");preparedSourceMap=SourceDerivedMapRuntime.Prepare(run,initial);Game.Runtime.SourceDeclaredRuntimeInputs.BindSourceDerivedMap(preparedSourceMap);}return initial;')
    world_anchor='world=new AutonomousWorld();world.PrepareFreshInitialRoads(run);'
    text=once(text,world_anchor,'world=new AutonomousWorld();if(preparedSourceMap!=null)world.BindSourceDerivedMap(preparedSourceMap);world.PrepareFreshInitialRoads(run);')
    prepare_call='preparedSourceMap=SourceDerivedMapRuntime.Prepare(run,initial);'
    declared_bind='Game.Runtime.SourceDeclaredRuntimeInputs.BindSourceDerivedMap(preparedSourceMap);'
    bind_call='world.BindSourceDerivedMap(preparedSourceMap)'
    initialize_call='world.Initialize(initialization,sha,1f/60,Path.GetFullPath((string)run["source_repository_root"]),Path.GetFullPath((string)run["repository_root"]),noCameraDiagnostic);'
    if text.count(prepare_call)!=1 or text.count(declared_bind)!=1 or text.count(bind_call)!=1 or text.index(prepare_call)>text.index(declared_bind) or text.index(bind_call)>text.index(initialize_call):raise ValueError('Same prepared alias instance must be bound once to preflight/world before Initialize')
    output=Path(output);output.mkdir(parents=True,exist_ok=True);target=output/PROGRAM_OUTPUT[0];target.write_text(text)
    wrapper=Path(wrapper_path).resolve(strict=True);pins={str(source):digest(source.read_bytes()),str(wrapper):digest(wrapper.read_bytes()),str(GENERATOR):digest(GENERATOR.read_bytes())}
    return {'experimental_only':True,'mission_ready':False,'native_or_player_readiness':False,'scope':'Exact source simulation program plus one preflight binding to the prepared source map owner.','source_pins':pins,'outputs':{PROGRAM_OUTPUT[0]:digest(target.read_bytes())}}

def generate_declared_inputs_output(repo, output, wrapper_path):
    source=SOURCE_DECLARED_INPUTS.resolve(strict=True);text=source.read_text()
    text=once(text,'internal static class SourceDeclaredRuntimeInputs{','internal static class SourceDeclaredRuntimeInputs{\n static global::SourceDerivedMapRuntime sourceDerivedMap;\n internal static void BindSourceDerivedMap(global::SourceDerivedMapRuntime value){if(value==null||sourceDerivedMap!=null)throw new InvalidOperationException("Exactly one source-derived map runtime instance may be bound");sourceDerivedMap=value;}')
    inactive_anchor='int decodedCount;bool inactiveS026=false;var inactive=initial["placement_domain"]?["s026_inactive_diagnostic"]as JObject;\nif(inactive!=null){'
    alias_branch='int decodedCount;bool inactiveS026=false;var inactive=initial["placement_domain"]?["s026_inactive_diagnostic"]as JObject;\nif(sourceDerivedMap!=null&&sourceDerivedMap.IsLogicalAlias){sourceDerivedMap.VerifyLogicalSelection(initial,setup.OperationMapId,selectedGrid.Width,selectedGrid.Height,selectedGrid.CellSize,selectedGrid.Origin.x,selectedGrid.Origin.y,selectedGrid.Origin.z);decodedCount=sourceDerivedMap.Navigation.Count;}\nelse if(sourceDerivedMap!=null){var exactPackage=JObject.Parse(System.Text.Encoding.UTF8.GetString(AutonomousWorld.Descriptor(sourceDerivedMap.Descriptor,4*1024*1024)));var exactMap=exactPackage["static_map"]as JObject;var exactFrame=exactPackage["frame"]as JObject;var exactOrigin=exactFrame?["origin"]as JArray;if(sourceDerivedMap.IsLogicalAlias||exactMap==null||exactFrame==null||exactOrigin==null||exactOrigin.Count!=3||!JToken.DeepEquals(exactMap,initial["static_map"])||setup.OperationMapId!=(string)exactMap["operation_map_id"]||setup.OperationMapId!=sourceDerivedMap.PhysicalMapId||(int?)exactFrame["width"]!=selectedGrid.Width||(int?)exactFrame["height"]!=selectedGrid.Height||(float?)exactFrame["cell_size"]!=selectedGrid.CellSize||(float?)exactOrigin[0]!=selectedGrid.Origin.x||(float?)exactOrigin[1]!=selectedGrid.Origin.y||(float?)exactOrigin[2]!=selectedGrid.Origin.z)throw new InvalidDataException("Exact source map package does not match selected map/grid");decodedCount=sourceDerivedMap.Navigation.Count;}\nelse if(inactive!=null){'
    text=once(text,inactive_anchor,alias_branch)
    text=once(text,'["actual_camera_constructor_passed"]=cameraConstructed,','["source_derived_map_alias"]=sourceDerivedMap!=null&&sourceDerivedMap.IsLogicalAlias,["source_derived_map_exact"]=sourceDerivedMap!=null&&!sourceDerivedMap.IsLogicalAlias,["source_derived_map_join_sha256"]=sourceDerivedMap!=null&&sourceDerivedMap.IsLogicalAlias?sourceDerivedMap.LogicalJoinSha:null,["source_derived_map_navigation_rows"]=sourceDerivedMap!=null?sourceDerivedMap.Navigation.Count:(int?)null,["source_derived_map_clearance_sha256"]=sourceDerivedMap!=null?sourceDerivedMap.ClearanceSha:null,["native_capture_readback_used"]=sourceDerivedMap==null,["actual_camera_constructor_passed"]=cameraConstructed,')
    output=Path(output);output.mkdir(parents=True,exist_ok=True);target=output/DECLARED_OUTPUT[0];target.write_text(text)
    wrapper=Path(wrapper_path).resolve(strict=True);pins={str(source):digest(source.read_bytes()),str(wrapper):digest(wrapper.read_bytes()),str(GENERATOR):digest(GENERATOR.read_bytes())}
    return {'experimental_only':True,'mission_ready':False,'native_or_player_readiness':False,'scope':'Exact declared preflight with a source-derived logical alias branch; default camera and original native/S026 branches retained.','source_pins':pins,'outputs':{DECLARED_OUTPUT[0]:digest(target.read_bytes())}}

def splice_current_starting_world_reader(text, canonical_text):
    start='var reference=(JObject)request["reference_templates"];string referenceBinaryDirectory;var referenceDomain='
    end=';if(MetadataCompatibility?.FactoryDomainQualified==true'
    def reader_slice(value):
        a=value.find(start);b=value.find(end,a)
        if a<0 or b<0 or value.find(start,a+1)>=0:raise ValueError('Unique StartingWorld template reader boundary required')
        return a,b,value[a:b]
    _,_,canonical_reader=reader_slice(canonical_text);seed_start,seed_end,seed_reader=reader_slice(text)
    current_reader='GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot,request)'
    stale_reader='GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot)'
    if current_reader not in canonical_reader:
        raise ValueError('Current canonical request-bound StartingWorld reader required')
    if stale_reader in seed_reader:
        text=text[:seed_start]+canonical_reader+text[seed_end:]
    elif seed_reader!=canonical_reader:
        raise ValueError('Seed StartingWorld reader must be exact stale form or exact current canonical form')
    if text.count(current_reader)!=1 or text.count('GeneratedReferenceTemplateView.ReadS026Diagnostic(reference,(string)inactive["source_root"],out referenceBinaryDirectory)')!=1:
        raise ValueError('Generated StartingWorld must preserve exact request-bound and S026 diagnostic readers')
    return text

def generate_starting_world_output(repo, output, wrapper_path):
    repo=Path(repo).resolve(strict=True);canonical=STARTING_WORLD_SOURCE.resolve(strict=True);seed=PROOF_SEED.resolve(strict=True)
    if not canonical.is_relative_to(repo) or not seed.is_relative_to(repo):raise ValueError('StartingWorld canonical/seed source escaped repository')
    receipt_path=seed/'generation-receipt.json';receipt=json.loads(receipt_path.read_text());seed_pins=dict(receipt.get('inputs',{}));seed_outputs=dict(receipt.get('outputs',{}))
    if receipt.get('status')!='GeneratedSourceOnly' or receipt.get('mission_ready') is not False:raise ValueError('Current source-only StartingWorld seed required')
    for path,sha in {**seed_pins,**seed_outputs}.items():
        f=Path(path)
        if f.is_symlink() or not f.is_file() or digest(f.read_bytes())!=sha:raise ValueError('StartingWorld seed pin changed: '+path)
    seed_file=next((Path(path) for path in seed_outputs if Path(path).name=='StartingWorld.cs'),None)
    if seed_file is None:raise ValueError('Proof seed lacks StartingWorld output')
    text=seed_file.read_text();canonical_text=canonical.read_text()
    reader_start='var reference=(JObject)request["reference_templates"];string referenceBinaryDirectory;var referenceDomain='
    reader_end=';if(MetadataCompatibility?.FactoryDomainQualified==true'
    seed_start=text.find(reader_start);seed_end=text.find(reader_end,seed_start)
    canonical_start=canonical_text.find(reader_start);canonical_end=canonical_text.find(reader_end,canonical_start)
    if min(seed_start,seed_end,canonical_start,canonical_end)<0:raise ValueError('Unique StartingWorld template reader boundary required for reversion check')
    seed_reader=text[seed_start:seed_end];canonical_reader=canonical_text[canonical_start:canonical_end]
    text=splice_current_starting_world_reader(text,canonical_text)
    if text.count('GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot,request)')!=1 or text.count('GeneratedReferenceTemplateView.ReadS026Diagnostic(reference,(string)inactive["source_root"],out referenceBinaryDirectory)')!=1:
        raise ValueError('Generated StartingWorld must preserve exact S074 proof-aware and S026 diagnostic readers')
    if text.count('SourceStagedLaunch.Open(Entities,toolCatalog,ActualDefinition,ActualCompileMatrix,ActualCompileManifest,Setup,toolIndex)')!=1:raise ValueError('Validated staged launch handoff was dropped')
    for needle in ('GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation)','domain=sourceDerivedMap.Domain','sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask','SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt'):
        if text.count(needle)!=1:raise ValueError('Validated source-map owner handoff changed: '+needle)
    restored=text[:seed_start]+seed_reader+text[seed_start+len(canonical_reader):]
    if restored!=seed_file.read_text():raise ValueError('StartingWorld output changed beyond canonical template-reader handoff')
    # Independently reverse the declared staged-launch + four map handoffs.
    # Equality with the entire current canonical source rejects dropped owner
    # branches that a local assertion could miss.
    sync_pos=canonical_text.index('initialRoads.Sync(Entities,GridEntity);')
    canonical_start=canonical_text.index('\n Session=GeneratedS002SessionConstruction.Create',sync_pos)
    canonical_end=canonical_text.index('\n var reference=(JObject)request["reference_templates"];',canonical_start)
    stage_start=text.index('\n SourceGroundEnemyPolicyAdmission.Require(Setup);')
    stage_end=text.index('\n var reference=(JObject)request["reference_templates"];',stage_start)
    restored=text[:stage_start]+canonical_text[canonical_start:canonical_end]+text[stage_end:]
    replacements=(
        ('if(sourceDerivedMap!=null){AuthoredNavigationCount=GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation);}else if(inactive!=null){','if(inactive!=null){'),
        ('if(sourceDerivedMap!=null)domain=sourceDerivedMap.Domain;else if(inactive!=null)domain=','if(inactive!=null)domain='),
        ('sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask','domain.BuildWaterMask'),
        ('\n if(sourceDerivedMap!=null)SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt(masks.AuthoredBuildings,roads,masks.Sidewalks,waterMask,AuthoredNavigationCount);',''))
    for overlay,original in replacements:
        if restored.count(overlay)!=1:raise ValueError('StartingWorld inverse overlay is not unique: '+overlay[:100])
        restored=restored.replace(overlay,original,1)
    if restored!=canonical_text:raise ValueError('StartingWorld differs from canonical source outside declared staged-launch/map handoffs')
    output=Path(output);output.mkdir(parents=True,exist_ok=True);target=output/'StartingWorld.cs';target.write_text(text)
    wrapper=Path(wrapper_path).resolve(strict=True);pins={**seed_pins,str(receipt_path):digest(receipt_path.read_bytes()),str(canonical):digest(canonical.read_bytes()),str(wrapper):digest(wrapper.read_bytes()),str(GENERATOR):digest(GENERATOR.read_bytes())}
    return {'experimental_only':True,'mission_ready':False,'native_or_player_readiness':False,'scope':'Validated staged StartingWorld seed with only its template reader replaced by the exact current canonical request-bound dispatch; source-map owner handoff preserved.','source_pins':pins,'outputs':{'StartingWorld.cs':digest(target.read_bytes())}}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--manifest',type=Path,required=True);p.add_argument('--clearance-overlay',type=Path,required=True);p.add_argument('--output-dir',type=Path,required=True);p.add_argument('--explicit-paid-rifle-control',action='store_true');p.add_argument('--proof-rows',action='store_true');p.add_argument('--proof-seed',type=Path,help='Current source-map host output used to bind proof rows; valid only with --proof-rows');a=p.parse_args()
    if a.proof_seed and not a.proof_rows:raise ValueError('--proof-seed requires --proof-rows')
    if a.proof_seed:configure_proof_seed(a.proof_seed)
    manifest_path=a.manifest.resolve(strict=True);clearance_path=a.clearance_overlay.resolve(strict=True);out=a.output_dir.absolute()
    if not manifest_path.is_relative_to(ROOT) or not out.is_relative_to(ROOT) or out.exists(): raise ValueError('Fresh repository-owned output and source manifest required')
    raw=manifest_path.read_bytes();m=json.loads(raw)
    if m.get('experimental_only') is not True or not m.get('staged_handoff_overlay'): raise ValueError('Explicit staged source handoff manifest required')
    rows=m['sources']; host=next(x for x in rows if x['id']=='S026_000_AutonomousWorld.cs'); world=next(x for x in rows if x['id']=='307_GeneratedS002StartingWorld.lifetime.cs'); program=next(x for x in rows if x['id']=='526_ActualAutonomousIntegration.terminal.cs')
    clearance=next(x for x in rows if x['id']=='527_GeneratedS002DropClearance.cs')
    clearance_source,clearance_path=validate_clearance_overlay_path(ROOT/clearance['path'],clearance_path,ROOT)
    wrappers=[ROOT/x[1] for x in ALIAS_OUTPUTS.values()]+[ROOT/STARTING_WORLD_OUTPUT[1],ROOT/ALIAS_RUNTIME[1],ROOT/PROGRAM_OUTPUT[1],ROOT/DECLARED_OUTPUT[1]]
    inputs=[manifest_path,GENERATOR,RUNTIME,CLEARANCE,clearance_path,SOURCE_PROGRAM,STARTING_WORLD_SOURCE,SOURCE_DECLARED_INPUTS,DELIVERY_BASIS_COMPONENT,S074_NO_CAMERA_HOST,*wrappers]+[(ROOT/x['path']).resolve(strict=True) for x in (host,world)]
    if a.proof_rows:
        inputs.extend(Path(path) for path in proof_seed_inputs())
        inputs.append(PROOF_SEED.resolve(strict=True)/'generation-receipt.json')
    pins={str(x):digest(x.read_bytes()) for x in inputs};out.mkdir(parents=True);g=out/'generated';g.mkdir()
    h=(ROOT/host['path']).read_text(); w=(ROOT/world['path']).read_text(); s=SOURCE_PROGRAM.read_text()
    # Accept a prior generated source-map overlay as input by removing only the
    # exact insertions this generator owns before applying the current version.
    h=strip_existing_source_map_host_fields(h)
    h=merge_s074_no_camera_host(h,S074_NO_CAMERA_HOST.read_text())
    w=strip_existing_source_map_starting_world(w)
    h=once(h,'        private SourceStagedLaunch stagedLaunch;','        private SourceStagedLaunch stagedLaunch;\n        private SourceDerivedMapRuntime sourceDerivedMap;\n        internal JObject SourceDerivedMapReceipt { get; private set; }\n        internal void BindSourceDerivedMap(SourceDerivedMapRuntime value){if(value==null||sourceDerivedMap!=null)throw new InvalidOperationException("Source-derived map binding must be nonnull and single-use");sourceDerivedMap=value;}')
    w=once(w,'if(inactive!=null){if(Setup.CatalogId!=','if(sourceDerivedMap!=null){AuthoredNavigationCount=GeneratedAuthoredNavigationImporter.Materialize(Entities,sourceDerivedMap.Navigation);}else if(inactive!=null){if(Setup.CatalogId!=')
    w=once(w,'if(inactive!=null)domain=SourcePlacementDomain.LoadS026InactiveDiagnostic','if(sourceDerivedMap!=null)domain=sourceDerivedMap.Domain;else if(inactive!=null)domain=SourcePlacementDomain.LoadS026InactiveDiagnostic')
    w=once(w,'var waterMask=domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);','var waterMask=sourceDerivedMap!=null?sourceDerivedMap.Water:domain.BuildWaterMask(actualWaterMatch,grid.Width,grid.Height,grid.CellSize,grid.Origin.x,grid.Origin.y,grid.Origin.z);')
    anchor=' if(StartingPipeline.SubmitOrBind(Session,Setup,out var reason)'
    w=once(w,anchor,' if(sourceDerivedMap!=null)SourceDerivedMapReceipt=sourceDerivedMap.InstalledReceipt(masks.AuthoredBuildings,roads,masks.Sidewalks,waterMask,AuthoredNavigationCount);\n'+anchor)
    if a.proof_rows:
        starting_world_proof_wrapper=g/'generate_starting_world_from_pinned_seed.py'
        starting_world_proof_wrapper.write_text(starting_world_seed_wrapper(PROOF_SEED))
        starting_detail=generate_starting_world_output(ROOT,g,starting_world_proof_wrapper)
        seed_alias_wrappers={}
        for identity,(output_name,_) in ALIAS_OUTPUTS.items():
            wrapper=g/('generate_'+Path(output_name).stem.lower()+'_from_pinned_seed.py')
            wrapper.write_text(seed_output_wrapper(PROOF_SEED,output_name))
            seed_alias_wrappers[identity]=wrapper
        seed_world=next(Path(path) for path in json.loads((PROOF_SEED/'generation-receipt.json').read_text())['outputs'] if Path(path).name=='StartingWorld.cs')
        if w!=seed_world.read_text():raise ValueError('Current source-map transforms must resolve to the pinned staged StartingWorld seed before canonical reader splice')
        w=(g/STARTING_WORLD_OUTPUT[0]).read_text()
    s=once(s,'internal static class SourceSimulationProgram {','internal static class SourceSimulationProgram {\n static SourceDerivedMapRuntime preparedSourceMap;')
    anchor='if((string)initial["mission_id"]!="S002")'
    # Insert before the explicit input list return rather than changing existing checks.
    check='else foreach(string name in new[]{"mission_scene","camera_profile","camera_values","camera_motion"})Input(run,name);return initial;'
    if a.proof_rows:
        program_detail=generate_program_output(ROOT,g,ROOT/PROGRAM_OUTPUT[1]);s=(g/PROGRAM_OUTPUT[0]).read_text()
    world_anchor='world=new AutonomousWorld();world.PrepareFreshInitialRoads(run);'
    world_bound='world=new AutonomousWorld();if(preparedSourceMap!=null)world.BindSourceDerivedMap(preparedSourceMap);world.PrepareFreshInitialRoads(run);'
    if a.proof_rows:
        if s.count(world_bound)!=1:raise ValueError('Proof generator output must bind the prepared runtime to the world before startup')
    else:s=once(s,world_anchor,world_bound)
    if a.proof_rows:
        if s.count('preparedSourceMap=SourceDerivedMapRuntime.Prepare(run,initial);')!=1 or s.count('world.BindSourceDerivedMap(preparedSourceMap);')!=1 or s.index('world.BindSourceDerivedMap(preparedSourceMap);')>s.index('world.Initialize(initialization,sha,'):
            raise ValueError('Resolved run must bind the exact preflight map instance before Initialize')
        if 'JObject.Parse(File.ReadAllText(Input(run,"initialization")))' not in s or 'var initialization=Input(run,"initialization");' not in s or 'var request = Parse(sourceInitializationBytes);' not in h or 'ConstructSourceStartingWorld(request);' not in h or 'GeneratedReferenceTemplateView.Read(reference,out referenceBinaryDirectory,SourceRepositoryRoot,request)' not in w:
            raise ValueError('Preflight/Initialize/StartingWorld request-context handoff changed')
    s=once(s,'  if(projected!=null){if(!deferredClearance', '  if(preparedSourceMap!=null)clearance=preparedSourceMap.Clearance;\n  else if(projected!=null){if(!deferredClearance')
    s=once(s,'result["source_delivery_events"]=JArray.FromObject(world.ActualDeliveryOwner.DeliveryEvents);',live_owner_result_handoff())
    if a.explicit_paid_rifle_control:
        binding='  var selectedDeliveryGrid=world.Entities.GetComponentData<GridConfig>(world.GridEntity);\n  if(!Game.Simulation.SharedProductionDeliveryKernel.TryResolveAxisAlignedMapFrame(selectedDeliveryGrid.Width,selectedDeliveryGrid.Height,selectedDeliveryGrid.CellSize,selectedDeliveryGrid.Origin,out var logicalDeliveryBasis))throw new InvalidDataException("Actual selected source map grid did not resolve a valid logical delivery basis");\n  logicalDeliveryBasis=BindSessionDeliveryBasis(world,logicalDeliveryBasis);'
        verified=binding+'\n  JObject installedMap=world.SourceDerivedMapReceipt??throw new InvalidDataException("Verified source-derived installed map receipt required for this explicit command control");JObject installedDescriptor=(JObject)installedMap["descriptor"];JObject requestedMap=(JObject)run["source_derived_map"];string installedPath=(string)installedDescriptor["path"],installedSha=(string)installedDescriptor["sha256"];if(installedPath!=(string)requestedMap["path"]||installedSha!=(string)requestedMap["sha256"]||AutonomousWorld.Hash(File.ReadAllBytes(installedPath))!=installedSha)throw new InvalidDataException("Installed source map differs from the hash-bound request");JObject installedPackage=JObject.Parse(File.ReadAllText(installedPath));JObject installedFrame=(JObject)installedPackage["frame"];JArray installedOrigin=(JArray)installedFrame["origin"];if((string)installedPackage["static_map"]?["operation_map_id"]!=world.Setup.OperationMapId||installedOrigin==null||installedOrigin.Count!=3||(int)installedFrame["width"]!=selectedDeliveryGrid.Width||(int)installedFrame["height"]!=selectedDeliveryGrid.Height||Math.Abs((float)installedFrame["cell_size"]-selectedDeliveryGrid.CellSize)>0.0001f||Math.Abs((float)installedOrigin[0]-selectedDeliveryGrid.Origin.x)>0.0001f||Math.Abs((float)installedOrigin[1]-selectedDeliveryGrid.Origin.y)>0.0001f||Math.Abs((float)installedOrigin[2]-selectedDeliveryGrid.Origin.z)>0.0001f)throw new InvalidDataException("Verified source map identity/frame differs from the active source setup/grid");'
        s=once(s,binding,verified)
        command='world.InitializeSourceS002Aria(world.ActualAriaPaid);'
        action='world.InitializeSourceS002Aria(world.ActualAriaPaid);var paidRifleOffer=world.ActualAriaPaid.Read(SkirmishRoleKind.Rifle);var paidRifle=world.ActualAriaPaid.Request(SkirmishRoleKind.Rifle,(float)world.SimulationSeconds);File.WriteAllText(Path.Combine(dir,"explicit-paid-rifle-command.json"),new JObject{["kind"]="ExplicitSemanticTestIssuedCommand",["source_api"]="SourceAriaPaidRecruitment.Read/Request",["role"]="Rifle",["offer_present"]=paidRifleOffer.Present,["can_request"]=paidRifleOffer.CanRequest,["offer_reason"]=paidRifleOffer.Reason,["accepted"]=paidRifle.Accepted,["source_reason"]=paidRifle.SourceReason,["receipt_id"]=paidRifle.ReceiptId.HasValue?(uint?)paidRifle.ReceiptId.Value:null,["world_tick"]=world.Tick,["simulation_seconds"]=(float)world.SimulationSeconds,["session_delivery_basis_readback"]=CaptureSessionDeliveryBasis(world),["native_ui_touch_claimed"]=false,["aria_policy_generated_command"]=false,["native_parity"]=false,["mission_ready"]=false}.ToString(Formatting.Indented));'
        s=once(s,command,action)
    original=CLEARANCE.read_text(); enhanced=clearance_path.read_text();begin=enhanced.index('   if((string)root["kind"]=="SourceDerivedCampaignMapDropClearanceProjection"){');end=enhanced.index('   if((string)root["kind"]=="S026InactiveOriginalDropBoundsProjection"){',begin)
    if enhanced[:begin]+enhanced[end:]!=original:raise ValueError('Clearance overlay changes more than the source-derived admission branch')
    files={'AutonomousWorld.cs':h,'StartingWorld.cs':w,'SourceSimulationProgram.cs':s,'SourceDerivedClearance.cs':enhanced}
    if a.proof_rows:
        declared_detail=generate_declared_inputs_output(ROOT,g,ROOT/DECLARED_OUTPUT[1]);runtime_detail=generate_logical_alias_runtime(ROOT,g,ROOT/ALIAS_RUNTIME[1])
        declared=(g/DECLARED_OUTPUT[0]).read_text()
        (g/ALIAS_RUNTIME[0]).write_bytes((ALIAS_SEAM_ROOT/ALIAS_RUNTIME[0]).read_bytes())
        files['SourceDeclaredRuntimeInputs.cs']=declared
        files[starting_world_proof_wrapper.name]=starting_world_proof_wrapper.read_text()
        for wrapper in seed_alias_wrappers.values():files[wrapper.name]=wrapper.read_text()
    for n,text in files.items(): (g/n).write_text(text)
    host['path']=str((g/'AutonomousWorld.cs').relative_to(ROOT));world['path']=str((g/'StartingWorld.cs').relative_to(ROOT));program['path']=str((g/'SourceSimulationProgram.cs').relative_to(ROOT))
    clearance['path']=str((g/'SourceDerivedClearance.cs').relative_to(ROOT))
    if a.proof_rows:
        program_row=next((x for x in rows if x.get('id')=='526_ActualAutonomousIntegration.terminal.cs'),None)
        declared_row=next((x for x in rows if x.get('id')=='SourceDeclaredRuntimeInputs.cs'),None)
        if program_row is None or declared_row is None:raise ValueError('Source simulation/declaration rows required')
        program_row.update({'kind':'current_original','path':str((g/PROGRAM_OUTPUT[0]).relative_to(ROOT)),'output':PROGRAM_OUTPUT[0],
            'generator':PROGRAM_OUTPUT[1],'generator_helpers':[str(GENERATOR.relative_to(ROOT))],
            'generator_inputs':[str(SOURCE_PROGRAM.relative_to(ROOT))],'generator_output':PROGRAM_OUTPUT[0],
            'source_scope':'Original source simulation host with one binding of its verified prepared alias runtime.'})
        declared_row.update({'kind':'current_original','path':str((g/DECLARED_OUTPUT[0]).relative_to(ROOT)),'output':DECLARED_OUTPUT[0],
            'generator':DECLARED_OUTPUT[1],'generator_helpers':[str(GENERATOR.relative_to(ROOT))],
            'generator_inputs':[str(SOURCE_DECLARED_INPUTS.relative_to(ROOT))],'generator_output':DECLARED_OUTPUT[0],
            'source_scope':'Original declared-input preflight plus validated source logical-alias admission; default camera path retained.'})
    map_runtime_rows=[x for x in rows if x.get('id')=='SourceDerivedMapRuntime']
    map_runtime_row={'id':'SourceDerivedMapRuntime','kind':'finite_adapter','path':str(RUNTIME.relative_to(ROOT)),'role':'host_glue','contains_original_gameplay':False,'scope':'Source-only captured map admission; no active native water or readiness claim.'}
    if map_runtime_rows:
        if len(map_runtime_rows)!=1 or any(map_runtime_rows[0].get(k)!=v for k,v in map_runtime_row.items()):raise ValueError('Unexpected existing SourceDerivedMapRuntime row')
        rows.remove(map_runtime_rows[0])
    rows.append(map_runtime_row)
    component_id='SkirmishProductionDeliveryBasisComponent.cs'
    component_rows=[x for x in rows if x.get('id')==component_id]
    component_row={'id':component_id,'kind':'native_direct','path':str(DELIVERY_BASIS_COMPONENT.relative_to(ROOT)),'require_declarations':['public struct SkirmishProductionDeliveryBasisComponent']}
    if component_rows:
        if len(component_rows)!=1 or component_rows[0].get('path')!=component_row['path'] or component_rows[0].get('kind')!='native_direct':raise ValueError('Unexpected shared delivery basis source row')
    else:rows.append(component_row)
    if a.proof_rows:
        proof_inputs=proof_seed_inputs()
        generator_helper=str(GENERATOR.relative_to(ROOT))
        for identity,(output_name,wrapper) in ALIAS_OUTPUTS.items():
            matches=[row for row in rows if row.get('id')==identity]
            if len(matches)!=1:raise ValueError('Unique generated alias row required: '+identity)
            row=matches[0]
            if row.get('path')!=str((g/output_name).relative_to(ROOT)):raise ValueError('Generated alias output path changed: '+identity)
            row.update({'kind':'current_original','output':output_name,'generator':str(seed_alias_wrappers[identity].relative_to(ROOT)),
                'generator_helpers':[generator_helper],'generator_inputs':proof_inputs,'generator_output':output_name,
                'source_scope':'Experimental source-map host alias; byte-for-byte output must verify against pinned seed generation receipt.'})
        world_row=next((row for row in rows if row.get('id')=='307_GeneratedS002StartingWorld.lifetime.cs'),None)
        if world_row is None or world_row.get('path')!=str((g/STARTING_WORLD_OUTPUT[0]).relative_to(ROOT)):raise ValueError('Generated StartingWorld row must point at the proof output')
        world_row.update({'kind':'current_original','output':STARTING_WORLD_OUTPUT[0],
            'generator':str(starting_world_proof_wrapper.relative_to(ROOT)),
            'generator_helpers':[str(GENERATOR.relative_to(ROOT))],
            'generator_inputs':[str(STARTING_WORLD_SOURCE.relative_to(ROOT)),*proof_inputs],
            'generator_output':STARTING_WORLD_OUTPUT[0],
            'source_scope':'Canonical source StartingWorld plus exact source-map owner handoff; S074 uses request-bound proof-aware template read and S026 diagnostic reader remains unchanged.'})
        runtime_matches=[row for row in rows if row.get('id')=='SourceDerivedMapRuntime']
        if len(runtime_matches)!=1:raise ValueError('Unique SourceDerivedMapRuntime row required')
        runtime_row=runtime_matches[0]
        runtime_row.update({'kind':'current_original','path':str((g/ALIAS_RUNTIME[0]).relative_to(ROOT)),'output':ALIAS_RUNTIME[0],
            'generator':ALIAS_RUNTIME[1],'generator_helpers':[str(GENERATOR.relative_to(ROOT))],
            'generator_inputs':['T Logs/SkirmishProduction/s074-source-derived-worker-alias-overlay-20261107-r3/worker-seam-receipt.json',
                'T Logs/SkirmishProduction/s074-source-derived-worker-alias-overlay-20261107-r3/SourceDerivedMapRuntime.cs',
                'T Logs/SkirmishProduction/s074-source-derived-worker-alias-overlay-20261107-r3/build.json'],
            'generator_output':ALIAS_RUNTIME[0],
            'source_scope':'Experimental S074 logical-to-physical alias admission; validated original-map join and package evidence required.'})
    m['source_derived_map_overlay']={'source_only':True,'mission_ready':False,'scope':'Existing source navigation, placement and water owners; captured source geometry only.'}
    if a.explicit_paid_rifle_control:m['explicit_paid_rifle_control']={'kind':'ExplicitSemanticTestIssuedCommand','role':'Rifle','native_ui_touch_claimed':False,'aria_policy_generated_command':False,'mission_ready':False}
    dest=out/'source-manifest.json';dest.write_text(json.dumps(m,indent=2)+'\n');outputs={str(x):digest(x.read_bytes()) for x in [dest,*[g/n for n in files]]};(out/'generation-receipt.json').write_text(json.dumps({'status':'GeneratedSourceOnly','inputs':pins,'outputs':outputs,'mission_ready':False},indent=2)+'\n')
    for n,sha in pins.items():
        if digest(Path(n).read_bytes())!=sha:raise ValueError('Input changed during generation: '+n)
    print(json.dumps({'status':'GeneratedSourceOnly','manifest':str(dest),'mission_ready':False}))
if __name__=='__main__':main()
