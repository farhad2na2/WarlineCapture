#!/usr/bin/env python3
"""Replay the original S074 C# setup/binding owners for the provisional six-metre layout.

This derives setup and binding fingerprints from the current compiler. The retained
origin view/framing fields are copied without camera observation or readiness claims.
"""
from __future__ import annotations
import argparse, csv, hashlib, json, os, subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
R9=ROOT/'T Logs/SkirmishProduction/s074-source-factory-replay-20261107-r9'
ORIGIN=ROOT/'T Logs/SkirmishProduction/s074-source-origin-authoring-20261107-r1'
ORIGIN_DRAFT=ROOT/'T Logs/SkirmishProduction/s074-source-origin-authoring-draft-20261107-r1/binding.json'
ORIGIN_FACTORY=ROOT/'T Logs/SkirmishProduction/s074-source-origin-factory-export-20261107-r1'
ORIGIN_QUAL=ROOT/'T Logs/SkirmishProduction/s074-source-origin-runtime-inputs-20261107-r1/qualification.json'
HOST=ROOT/'T Logs/SkirmishProduction/s074-origin-source-build-20261113-r6'
GRAPH=ROOT/'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/s074-industrial-source-graph-r1/source-config-graph.json'
MATRIX=ROOT/'T Logs/SkirmishProduction/five-mission-pilot-20261106-T1/experimental-opening-mix-matrix.csv'
CANDIDATE=ROOT/'T Logs/SkirmishProduction/s074-provisional-six-metre-spawn-pads-20261113-r1'
LAYOUT=CANDIDATE/'layout.json'; CANDIDATE_RECEIPT=CANDIDATE/'receipt.json'
BASE_LAYOUT=ROOT/'T Logs/SkirmishProduction/s074-source-origin-centered-candidate-20261107-r1/layout.json'
GRID_OBSERVATION=ROOT/'T Logs/SkirmishProduction/s074-origin-grid-observer-run-20261113-r2/process.log'
AUTHOR_SOURCE=R9/'candidate/S074CameraBindingAuthor.cs'; AUTHOR_RECEIPT=R9/'candidate/receipt.json'
CODEC=ROOT/'Assets/Game/Scripts/Editor/Skirmish/SkirmishBakedActorSnapshotExporter.S026LogicalBinding.cs'
CODEC_SHA='9d72990488994613ad98b3713d351461cd984315834973f4643aedec26e443fa'
CONTEXT=ORIGIN_FACTORY/'s074/source-factory-context.json'
PASS='[S074SixMetreBindingAuthor] result=PassedSourceOnly'

def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def need(ok,msg):
    if not ok: raise ValueError(msg)
def extract_method(source, signature):
    start=source.find(signature); need(start>=0 and source.find(signature,start+len(signature))<0,'Unique original portable codec method required')
    brace=source.find('{',start+len(signature)); depth=0
    for i in range(brace,len(source)):
        if source[i]=='{': depth+=1
        elif source[i]=='}':
            depth-=1
            if depth==0: return source[start:i+1]
    raise ValueError('Unclosed codec method')
def semantic(v):
    if isinstance(v,dict): return {k:semantic(x) for k,x in v.items() if k not in ('name','hideFlags')}
    if isinstance(v,list): return [semantic(x) for x in v]
    if isinstance(v,float):
        import struct
        return struct.unpack('!f',struct.pack('!f',v))[0]
    return v

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output-dir',type=Path,required=True);a=p.parse_args()
    out=a.output_dir.resolve();need(out.is_relative_to(ROOT) and not out.exists(),'Fresh repository-owned T output required')
    need(sha(CODEC)==CODEC_SHA,'Original portable codec pin changed')
    host=json.loads((HOST/'build.json').read_bytes());need(host.get('status')=='Passed' and host.get('compile_exit')==0 and host.get('mission')=='S074' and host.get('seed')==104803,'Passed current S074 host build required')
    host_pins=host.get('source_and_reference_pins');need(isinstance(host_pins,dict),'Host pin map required')
    for path,digest in host_pins.items(): need(Path(path).is_file() and sha(path)==digest,'Host source pin drift: '+path)
    retained_receipt=json.loads(AUTHOR_RECEIPT.read_bytes());need(retained_receipt.get('status')=='PassedSourceOwnerCompile' and retained_receipt.get('source_inputs_sha256',{}).get(str(AUTHOR_SOURCE.resolve()))==sha(AUTHOR_SOURCE),'Pinned original C# author required')
    candidate=json.loads(CANDIDATE_RECEIPT.read_bytes());layout=json.loads(LAYOUT.read_bytes())
    need(candidate.get('kind')=='S074ProvisionalSpawnPadCandidate' and candidate.get('status')=='PreparedUnqualified' and candidate.get('source_fit_certified') is False and candidate.get('mission_ready') is False and candidate.get('world_initialized') is False,'Explicit unqualified six-metre candidate required')
    need(candidate.get('layout',{}).get('path')==str(LAYOUT) and candidate['layout'].get('sha256')==sha(LAYOUT),'Candidate layout receipt pin mismatch')
    need(candidate.get('base_layout')=={'path':str(BASE_LAYOUT),'sha256':sha(BASE_LAYOUT)},'Exact retained origin base layout pin required')
    observation=candidate.get('source_grid_diagnostic',{});need(observation.get('path')==str(GRID_OBSERVATION) and observation.get('sha256')==sha(GRID_OBSERVATION),'Exact pre-unit grid observation pin required')
    changes=candidate.get('changes',[])
    changed_ids={'s074.provisional.spawn.infantry.player.pad','s074.provisional.spawn.infantry.enemy.pad','s074.provisional.spawn.vehicle.player.pad','s074.provisional.spawn.vehicle.enemy.pad'}
    need(len(changes)==4 and {x.get('pad') for x in changes}==changed_ids and all(x.get('new_metres')==[6,6] for x in changes),'Exactly four named six-metre spawn-pad changes required')
    base_layout=json.loads(BASE_LAYOUT.read_bytes())
    def layout_without_candidate_extents(value):
        result=json.loads(json.dumps(value));result.pop('contentHash',None)
        for pad in result.get('pads',[]):
            if pad.get('PadId') in changed_ids:
                original=next(x for x in base_layout['pads'] if x['PadId']==pad['PadId'])
                pad['WidthMetres']=original['WidthMetres'];pad['DepthMetres']=original['DepthMetres']
        return result
    need(layout_without_candidate_extents(layout)==layout_without_candidate_extents(base_layout),'Candidate changes outside four spawn-pad extents')
    base_context=json.loads(CONTEXT.read_bytes());base_target=base_context.get('target_context',{})
    need(base_context.get('kind')=='OriginalCompilerDerivedNativeFactoryContext' and base_target.get('catalog_id')=='S074' and base_target.get('seed')==104803 and base_target.get('setup_hash')==1716123768 and base_context.get('world_allocated') is False and base_context.get('mission_ready') is False,'Retained exact S074 origin compiler context required')
    qual=json.loads(ORIGIN_QUAL.read_bytes());need(qual.get('native_context_qualified') is False and qual.get('mission_ready') is False,'Retained source-only qualification must remain unqualified')
    rows=[r for r in csv.DictReader(MATRIX.open(newline='')) if r.get('catalog_id')=='S074' and r.get('size_id')=='Standard']
    need(len(rows)==1 and rows[0].get('objective_id')=='BA' and rows[0].get('player_rifle')=='10' and rows[0].get('player_gunner')=='6' and rows[0].get('player_rocketeer')=='4' and rows[0].get('player_car')=='1' and rows[0].get('player_apc')=='1' and rows[0].get('player_tank')=='1' and rows[0].get('enemy_rifle')=='10' and rows[0].get('enemy_gunner')=='6' and rows[0].get('enemy_rocketeer')=='4' and rows[0].get('enemy_car')=='1' and rows[0].get('enemy_apc')=='1' and rows[0].get('enemy_tank')=='1','Exact retained Standard BA opening matrix required')

    owner=out/'owner';owner.mkdir(parents=True)
    author=owner/'S074CameraBindingAuthor.cs';source=AUTHOR_SOURCE.read_text();codec=CODEC.read_text()
    start='            var camera = source.Cameras.ToArray().Single(x => x.CameraId == source.BattleCameraId);'
    end='            var bindingJson = Serialize(binding, variant, view, rows);'
    need(source.count(start)==1 and source.count(end)==1,'Original C# owner segment changed')
    replacement='''            // Read original source camera metadata for provenance only; no native framing observation is made.
            var camera = source.Cameras.ToArray().Single(x => x.CameraId == source.BattleCameraId);
            string focusId = inputs.ActualOpeningPlayerActorIds.First(x => x.StartsWith("role.rifle.1.", StringComparison.Ordinal));
            var retainedDraft = JObject.Parse(File.ReadAllText(args[3]));
            var retainedBinding = materializer.LogicalBinding(retainedDraft);
            var variant = retainedBinding.Variants.Single();
            var view = variant.View;
            var rows = variant.Framing;
            if (view == null || rows == null || rows.Length != inputs.RequiredFramingRoles.Length)
                throw new InvalidOperationException("Retained origin binding does not cover exact compiler roles.");
            variant.SourceMinimumElevation = inputs.SourceMinimumElevation;
            variant.SourceMaximumElevation = inputs.SourceMaximumElevation;
            view.LayoutContentHash = definition.MapLayout.ContentHash;
            view.GroundReferenceY = inputs.SourceMinimumElevation;
            variant.ReviewedViewFingerprint = SkirmishMissionLogicalBindingCompiler.ViewFingerprint(variant);
            var binding = ScriptableObject.CreateInstance<SkirmishMissionLogicalBindingConfig>();
            binding.SchemaVersion = 1;
            binding.ContentVersion = 1;
            binding.DefinitionVersion = definition.ContentVersion;
            binding.ValueFormat = inputs.ValueFormat;
            binding.CatalogId = definition.CatalogId;
            binding.DefinitionId = definition.DefinitionId;
            binding.SourceOperationMapId = source.OperationMapId;
            binding.SourceSceneGuid = source.SourceSceneReference.AssetGUID;
            binding.SourceContentHash = source.ContentHash;
            binding.SourceSemanticFingerprint = inputs.SourceSemanticFingerprint;
            binding.LayoutId = definition.MapLayout.LayoutId;
            binding.ReviewedLayoutSemanticFingerprint = inputs.LayoutSemanticFingerprint;
            binding.ObjectiveKind = setup.ObjectiveKind;
            binding.Variants = new[] { variant };
            var bindingJson = Serialize(binding, variant, view, rows);'''
    i=source.index(start);j=source.index(end,i)+len(end);source=source[:i]+replacement+source[j:]
    source=source.replace('if (args.Length != 4)','if (args.Length != 5)',1).replace('var outDir = Path.GetFullPath(args[3]);','var outDir = Path.GetFullPath(args[4]);',1)
    source=source.replace('if (bound.SetupHash != 1716123768) throw new InvalidOperationException("Origin bound setup hash changed: " + bound.SetupHash);','if (bound.SetupHash != setup.SetupHash) throw new InvalidOperationException("Binding changed compiled setup fingerprint.");',1)
    source=source.replace('if (portableBound.SetupHash != 1716123768) throw new InvalidOperationException("Original portable reader changed S074 setup hash: " + portableBound.SetupHash);','if (portableBound.SetupHash != bound.SetupHash) throw new InvalidOperationException("Portable reader changed compiled setup fingerprint.");',1)
    # Insert the exact original portable field codec and roundtrip check, as the retained recipe does.
    method=extract_method(codec,'static JToken PortableLogicalValue(');marker='    private static JObject Serialize('
    need(source.count(marker)==1,'Unique original C# serializer insertion point required');source=source.replace(marker,method+'\n\n'+marker,1)
    anchor='            var outDir = Path.GetFullPath(args[4]);'
    portable_code='''            var portableBinding = new JObject { ["schema_version"] = 1, ["kind"] = "DeclaredOriginalLogicalBindingFieldsWithInlineViews", ["float_encoding"] = "IEEE754_float32_hex_bits", ["unity_object_ids_exported"] = false, ["fields"] = PortableLogicalValue(roundTrippedBinding, 0) };
            var portableDecoded = SourceNativeMissionLogicalBindingReference.DecodeDeclaredFields(portableBinding["fields"]);
            definition.BindMissionLogicalBinding(portableDecoded);
            if (!SkirmishSetupCompiler.TryCompile(definition, bound.DifficultyId, bound.SizeId, bound.Seed, manifest, matrix, out var portableBound, out var portableReasons)) throw new InvalidOperationException("Original reader-decoded portable binding rejected: " + string.Join(";", portableReasons.Select(x => x.Code + ":" + x.Field + ":" + x.Detail)));
            if (portableBound.SetupHash != bound.SetupHash) throw new InvalidOperationException("Portable reader changed compiled setup fingerprint.");
            if (!SkirmishMissionLogicalBindingCompiler.TryCompile(definition, portableBound, out var portableCompiled, out var portableError) || portableCompiled.Fingerprint != compiledBinding.Fingerprint) throw new InvalidOperationException("Original portable reader/compiler roundtrip changed binding: " + portableError);
'''
    need(source.count(anchor)==1,'Unique C# owner output anchor required');source=source.replace(anchor,portable_code+anchor,1)
    source=source.replace('["kind"] = "S074CameraSeededLogicalBindingCandidate"','["kind"] = "S074SixMetreOriginalCompilerBindingReplay"',1)
    source=source.replace('["status"] = "CompiledSourceOnlyCandidate"','["status"] = "CompiledSourceOnlyCandidate"',1)
    source=source.replace('["setup_hash_without_binding"] = setup.SetupHash, ["setup_hash_with_binding"] = bound.SetupHash,','["setup_hash_without_binding"] = setup.SetupHash, ["setup_hash_with_binding"] = bound.SetupHash, ["original_reader_decode_and_compiler_roundtrip"] = true, ["portable_roundtrip_setup_hash"] = portableBound.SetupHash,',1)
    source=source.replace('["framing_thresholds_authority"] = "AuthoredCandidate_PresentationTargets_NotObserved"','["framing_thresholds_authority"] = "RetainedOriginBinding_PresentationTargets_NotObserved", ["camera_framing_unobserved"] = true',1)
    source=source.replace('["world_allocated"] = false, ["placement_performed"] = false,','["retained_origin_view_semantically_preserved"] = true, ["world_allocated"] = false, ["placement_performed"] = false,',1)
    source=source.replace('Console.WriteLine("[S074CameraBindingAuthor] result=PassedSourceOnlyCandidate setup=" + bound.SetupHash +\n                " roles=" + rows.Length + " world=0 placement=0 readiness=0");','Console.WriteLine("[S074SixMetreBindingAuthor] result=PassedSourceOnly setup=" + bound.SetupHash + " roles=" + rows.Length + " framing=retained-unobserved world=0 placement=0 readiness=0");',1)
    write='            File.WriteAllText(Path.Combine(outDir, "mission-logical-binding.json"), bindingJson.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");'
    need(source.count(write)==1,'Original owner raw binding write anchor required')
    source=source.replace(write,write+'\n            File.WriteAllText(Path.Combine(outDir, "portable-binding.json"), portableBinding.ToString(Newtonsoft.Json.Formatting.Indented) + "\\n");',1)
    author.write_text(source)

    csc=next((Path(x) for x in host_pins if x.endswith('/Roslyn/bincore/csc.dll')),None);need(csc and sha(csc)==host_pins[str(csc)],'Pinned Roslyn compiler required')
    rsp_rows=[json.loads(x) for x in (HOST/'compile.rsp').read_text().splitlines() if x.strip()]
    oldout='-out:'+str(HOST/'SourceSimulation.exe');need(rsp_rows.count('-main:SourceSimulationProgram')==1 and rsp_rows.count(oldout)==1,'Exact host compiler response required')
    rsp_rows=[('-main:S074CameraBindingAuthor' if x=='-main:SourceSimulationProgram' else '-out:'+str(owner/'source-owner.exe') if x==oldout else x) for x in rsp_rows]
    rsp_rows.append(str(author));rsp=owner/'compile.rsp';rsp.write_text(''.join(json.dumps(x)+'\n' for x in rsp_rows))
    compiled=subprocess.run(['/usr/local/share/dotnet/dotnet',str(csc),'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True,timeout=300);(owner/'compile.log').write_text(compiled.stdout+compiled.stderr)
    need(compiled.returncode==0 and (owner/'source-owner.exe').is_file(),f'Current original C# owner compile failed ({compiled.returncode})')
    runtime=host['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(HOST/'placement'),str(owner)))
    run=subprocess.run([runtime['mono'],'--optimize=-float32',str(owner/'source-owner.exe'),str(GRAPH),str(LAYOUT),str(MATRIX),str(ORIGIN_DRAFT),str(owner)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=600)
    (owner/'run.stdout.log').write_text(run.stdout);(owner/'run.stderr.log').write_text(run.stderr)
    need(run.returncode==0 and PASS in run.stdout,f'Original compiler owner replay failed ({run.returncode}); see preserved owner logs')
    proof=json.loads((owner/'candidate-proof.json').read_bytes());raw=json.loads((owner/'mission-logical-binding.json').read_bytes());portable=json.loads((owner/'portable-binding.json').read_bytes());old=json.loads(ORIGIN_DRAFT.read_bytes())
    need(proof.get('catalog_id')=='S074' and proof.get('seed')==104803 and isinstance(proof.get('setup_hash_without_binding'),int) and proof.get('setup_hash_with_binding')==proof.get('portable_roundtrip_setup_hash') and proof.get('logical_binding_fingerprint') and proof.get('world_allocated') is False and proof.get('placement_performed') is False and proof.get('source_fit_verified') is False and proof.get('mission_ready') is False and proof.get('camera_framing_unobserved') is True,'Source-only compiler proof/gates mismatch')
    view_fields=('MinHeight','MaxHeight','NormalHeight','BuildHeight','Pitch','Yaw','NormalFieldOfView','BuildFieldOfView','GroundReferenceY','MaximumGroundCenterSlantDistance','InfantryMinimumViewportHeight','PlaneMaximumViewportDimension','AircraftPitch','AircraftFieldOfView','AircraftMinimumDistance','AircraftMaximumDistance')
    actual_view=semantic(raw['Variants'][0]['View']);old_view=semantic(old['Variants'][0]['View'])
    need({k:actual_view.get(k) for k in view_fields}=={k:old_view.get(k) for k in view_fields} and semantic(raw.get('Variants'))[0].get('Framing')==semantic(old.get('Variants'))[0].get('Framing'),'Retained origin camera framing changed semantically')
    need(raw.get('LayoutId')==layout.get('layoutId') and raw.get('ReviewedLayoutSemanticFingerprint')==proof.get('layout_fingerprint') and portable.get('kind')=='DeclaredOriginalLogicalBindingFieldsWithInlineViews' and portable.get('unity_object_ids_exported') is False,'Candidate layout portable binding identity mismatch')
    out.mkdir(parents=True,exist_ok=True)
    context={
      'schema_version':1,'kind':'OriginalCompilerDerivedS074SixMetreCandidateSetupContext',
      'authority':'Fresh original C# setup and logical-binding compilers; retained source map/factory inputs',
      'parent_origin_context':{'path':str(CONTEXT.resolve()),'sha256':sha(CONTEXT)},
      'candidate_layout':{'path':str(LAYOUT.resolve()),'sha256':sha(LAYOUT)},
      'target_context':{'catalog_id':'S074','definition_id':raw.get('DefinitionId'),'seed':104803,'size':'Standard','difficulty':'Regular','setup_hash':proof['setup_hash_with_binding'],'operation_map_id':raw.get('SourceOperationMapId'),'source_content_hash':raw.get('SourceContentHash'),'source_fingerprint':proof.get('source_fingerprint'),'layout_fingerprint':proof.get('layout_fingerprint'),'mission_logical_binding_fingerprint':proof.get('logical_binding_fingerprint')},
      'world_allocated':False,'identities_allocated':0,'runtime_loader_admitted':False,'native_capture':False,'native_context_qualified':False,'source_fit_certified':False,'active_mission_water_qualified':False,'camera_framing_observed':False,'mission_ready':False}
    context_path=out/'candidate-compiler-context.json';context_path.write_text(json.dumps(context,indent=2,sort_keys=True)+'\n')
    qual_candidate={'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification','runtime_backend':'TypedGroundS026V1','compiler_context':{'path':str(context_path.resolve()),'sha256':sha(context_path)},'scope':'OriginalCompilerAndSixMetreCandidate_SourceOnly_Unqualified','native_context_qualified':False,'mission_ready':False,'source_fit_certified':False,'active_mission_water_qualified':False,'camera_framing_observed':False}
    qual_path=out/'candidate-qualification.json';qual_path.write_text(json.dumps(qual_candidate,indent=2,sort_keys=True)+'\n')
    wrapper={'schema_version':1,'kind':'OriginalCompilerDeclaredLogicalBinding','metadata_compiler_context':{'path':str(context_path.resolve()),'sha256':sha(context_path)},'target_context':context['target_context'],'portable_binding':portable,'source_owner_binding':{'path':str((owner/'mission-logical-binding.json').resolve()),'sha256':sha(owner/'mission-logical-binding.json')},'camera_framing_unobserved':True,'world_allocated':False,'native_capture':False,'native_context_qualified':False,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False}
    wrapper_path=out/'logical-binding.json';wrapper_path.write_text(json.dumps(wrapper,indent=2,sort_keys=True)+'\n')
    explicit_inputs=[HOST/'build.json',AUTHOR_SOURCE,AUTHOR_RECEIPT,CODEC,GRAPH,MATRIX,LAYOUT,CANDIDATE_RECEIPT,BASE_LAYOUT,GRID_OBSERVATION,ORIGIN_DRAFT,CONTEXT,ORIGIN_QUAL,ORIGIN/'authoring-request.json',ORIGIN/'authoring-result.json',ORIGIN/'authoring-receipt.json',ORIGIN_FACTORY/'factory-context-receipt.json']
    source_pins=dict(host_pins)
    for path in explicit_inputs: source_pins[str(path.resolve())]=sha(path)
    receipt={'kind':'S074SixMetreOriginalCompilerLogicalBindingReplay','status':'PassedSourceOwnerCompile','setup_hash':proof['setup_hash_with_binding'],'layout_fingerprint':proof['layout_fingerprint'],'mission_logical_binding_fingerprint':proof['logical_binding_fingerprint'],'compiler':'Current SkirmishSetupCompiler + SkirmishMissionLogicalBindingCompiler + original portable codec','owner_executable':{'path':str((owner/'source-owner.exe').resolve()),'sha256':sha(owner/'source-owner.exe')},'owner_proof':{'path':str((owner/'candidate-proof.json').resolve()),'sha256':sha(owner/'candidate-proof.json')},'binding':{'path':str(wrapper_path.resolve()),'sha256':sha(wrapper_path)},'candidate_layout':{'path':str(LAYOUT.resolve()),'sha256':sha(LAYOUT)},'retained_origin_binding':{'path':str(ORIGIN_DRAFT.resolve()),'sha256':sha(ORIGIN_DRAFT)},'source_and_reference_pins':source_pins,'retained_origin_view_semantically_preserved':True,'camera_framing_observed':False,'world_allocated':False,'placement_performed':False,'runtime_admission':False,'native_context_qualified':False,'source_fit_certified':False,'mission_ready':False,'base_qualification_unchanged':{'path':str(ORIGIN_QUAL.resolve()),'sha256':sha(ORIGIN_QUAL)}}
    receipt_path=out/'receipt.json';receipt_path.write_text(json.dumps(receipt,indent=2,sort_keys=True)+'\n')
    print(json.dumps({'status':receipt['status'],'setup_hash':receipt['setup_hash'],'layout_fingerprint':receipt['layout_fingerprint'],'logical_binding_fingerprint':receipt['mission_logical_binding_fingerprint'],'receipt':str(receipt_path)}))

if __name__=='__main__':
    raise SystemExit(main())
