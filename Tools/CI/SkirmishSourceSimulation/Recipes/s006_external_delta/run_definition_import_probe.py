#!/usr/bin/env python3
"""Probe original retained S005 factory decoder for the S006 diagnostic domain."""
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-original-grant-fixture-20261106-T5'
OUT = LOGS / 's006-source-processor-probe-20261106-T13'
PROJECTION = LOGS / 's005-combined-prefab-current-projection-20261105-T1/projection.json'
LOADER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/S005ProductionDefinitions.cs.txt'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
REPLAY = LOGS / 's005-current-production-world-20261105-T9/replayed-layout.json'
NATIVE_LOG = LOGS / 's005-layout-native-capture-20261105-T1/editor.log'
NATIVE_RECEIPT = LOGS / 's005-current-production-world-20261105-T9/run-receipt.json'
JSON_SHIM = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/050_SourceConfigJson.cs'
JSON_OWNER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/SourceValidatedLayoutJson.cs'
S005_IMPORTER = LOGS / 's005-identity-import-inputs-20261105-T1/importer.cs'
S005_CODEC = LOGS / 's005-identity-import-inputs-20261105-T1/codec.cs'
S005_HANDLES = LOGS / 's005-current-prefab-import-control-20261105-T3/SourceIdentityHandles.cs'
S005_RSP = LOGS / 's005-current-production-world-20261105-T9/compile.rsp'
INPUT_DIR = LOGS / 's005-campaign-placement-inputs-20261105-T1'
PHYSICAL = ROOT / 'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json'
INPUT_OWNER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/S005PlacementSourceInputs.cs.txt'
SESSION_OWNER = ROOT / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs'
SESSION_FRAGMENT = LOGS / 's004-session-owner-compile-20261104-T1/S004OriginalOwnerFragments.cs'
SESSION_PREPARER = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/prepare_s005_session_owner_probe.py'
WORLD_OWNER = ROOT / 'Assets/Game/Scripts/Systems/SkirmishWorldSetup.cs'
SUPPRESSION = ROOT / 'Assets/Game/Scripts/Systems/SkirmishStartupConfigSuppression.cs'
FRAMING = LOGS / 's006-current-config-only-candidate-20261106-T22/unreviewed-framing-template.json'
FORCE_CONSTRAINT = ROOT / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishTypedSpawnConstraint.cs'
FORCE_OWNER = ROOT / 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs'
FORCE_EXTRACTOR = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/probe_s004_spawn_owners_compile.py'
STRUCTURE_FRAGMENT = LOGS / 's004-current-epoch-s002-build-20261105-T1/source/generated/335_SkirmishScenarioStructureBinding.source.cs'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        prior = json.loads((BASE / 'receipt.json').read_bytes())
        if prior.get('status') != 'CollectedUnqualifiedOriginalGrantFixture' or prior.get('mission_ready') is not False:
            raise ValueError('Exact prior diagnostic fixture required')
        pins = dict(prior['source_pins'])
        input_receipt=json.loads((INPUT_DIR/'receipt.json').read_bytes())
        if input_receipt.get('status')!='ExtractedSourceInputsOnly' or input_receipt.get('campaign_capture_sha256')!=sha(PHYSICAL):raise ValueError('Exact retained inactive Campaign inputs required')
        for artifact in input_receipt['artifacts'].values():
            path=Path(artifact['path'])
            if sha(path)!=artifact['sha256']:raise ValueError('Captured input differs: '+str(path))
            pins[str(path)]=artifact['sha256']
        for path in (Path(__file__), BASE / 'receipt.json', BASE / 'compile.rsp', BASE / 'S006OriginalGrantFixture.cs', PROJECTION, LOADER, BUILD, REPLAY, NATIVE_LOG, NATIVE_RECEIPT, JSON_SHIM, JSON_OWNER, S005_IMPORTER, S005_CODEC, S005_HANDLES, S005_RSP, INPUT_DIR/'receipt.json', PHYSICAL, INPUT_OWNER, SESSION_OWNER, SESSION_FRAGMENT, SESSION_PREPARER, WORLD_OWNER, SUPPRESSION, FRAMING, FORCE_CONSTRAINT, FORCE_OWNER, FORCE_EXTRACTOR, STRUCTURE_FRAGMENT): pins[str(path)] = sha(path)
        def verify():
            for path, expected in pins.items():
                if not Path(path).is_file() or sha(path) != expected: raise ValueError('Pinned source changed: ' + path)
        verify()
        native_receipt=json.loads(NATIVE_RECEIPT.read_bytes())
        if native_receipt.get('exit_code')!=0 or native_receipt.get('input_hashes',{}).get(str(NATIVE_LOG))!=pins[str(NATIVE_LOG)]:raise ValueError('Exact retained source replay evidence required')
        marker='[S005OriginalLayoutNativeJson] authored asset='
        captured=[line.split(' json=',1)[1].rstrip('\r') for line in NATIVE_LOG.read_text().splitlines() if marker in line and ' json=' in line]
        if len(captured)!=1 or captured[0].encode('utf-8')!=REPLAY.read_bytes():raise ValueError('Replay bytes differ from original native capture')
        prior_options=[json.loads(line) for line in S005_RSP.read_text().splitlines() if line.strip()]
        if any(prior_options.count(str(path))!=1 for path in (S005_IMPORTER,S005_CODEC,S005_HANDLES)):raise ValueError('Exact retained S005 importer profile required')
        text = (BASE / 'S006OriginalGrantFixture.cs').read_text()
        field = 'static JObject sourceProposals,sourceSearch,sourceSurfaceGraph,sourceFactory;'
        anchor = 'var combined=source.ByPath<SkirmishScenarioDefinitionConfig>("Assets/Game/Configs/SkirmishExpansion/Scenarios/S005/SkirmishScenario_S005.asset");'
        site = ' var gridEntity=em.CreateEntity();'
        if text.count(field) != 1 or text.count(anchor) != 1 or text.count(site) != 1: raise ValueError('Exact source integration seams required')
        text = text.replace(field, field + 'static SkirmishScenarioDefinitionConfig s005Definition;static SkirmishMissionViewProfile fixtureView;static JObject fixtureFraming;static string candidateOutput;static string projectionPath,factoryPath,replayPath,replayHash;', 1)
        text = text.replace(anchor, anchor + 's005Definition=combined;fixtureView=new SourceConfigMaterializer(JObject.Parse(File.ReadAllText(a[19]))).ByPath<SkirmishMissionViewProfile>("Assets/Game/Resources/SkirmishMissionViewProfile_S005.asset");fixtureFraming=JObject.Parse(File.ReadAllText(a[17]));candidateOutput=a[18];projectionPath=a[12];factoryPath=a[11];replayPath=a[13];replayHash=a[14];', 1)
        addition = r''' var sourceManifest=new SkirmishContentManifest{RequiredFeatureIds=s005Definition.RequiredFeatureIds};
 SkirmishResolvedSetup s005Setup;using(var replay=SourceValidatedLayoutJson.Open(s005Definition.MapLayout,replayPath,replayHash)){if(!SkirmishSetupCompiler.TryCompile(s005Definition,SkirmishDifficultyId.Regular,SkirmishSizeId.Standard,1,sourceManifest,matrix,out s005Setup,out var s005Reasons))throw new InvalidOperationException("Original S005 setup required for retained factory importer");}
 var projection=JObject.Parse(File.ReadAllText(projectionPath));var templateKeys=((JArray)projection["requested_prefab_keys"]).Values<string>().ToArray();
 var templates=GeneratedReferenceTemplateImporter.Import(em,projection,Path.GetDirectoryName(projectionPath),templateKeys);
 var imported=GeneratedBuildingDefinitions.LoadS005ProductionDefinitions(factoryPath,"2f4796f95a1ef0cfd16056bd1abf65656d7aabad19b7bb98d2ec4bf0b6ccdf91",templates.Prefabs,s005Setup,s005Definition.ArmyProfileConfig);
 var factoryDomain=imported.Definitions.ToDictionary(x=>x.Prefab.name,x=>x,StringComparer.Ordinal);var importedRows=new JArray();foreach(JObject p in candidates){string key=(string)p["visual_key"];bool definitionFound=factoryDomain.TryGetValue(key,out var d);var fp=(JArray)p["footprint"];importedRows.Add(new JObject{["structure_index"]=p["structure_index"],["key"]=key,["definition_found"]=definitionFound,["captured_footprint_matches"]=definitionFound&&d.FootprintCells.x==(int)fp[0]&&d.FootprintCells.y==(int)fp[1]});}
'''
        text = text.replace(site, addition + site, 1).replace('["original_grant_fixture"]=grantResult,', '["definition_import_probe"]=importedRows,["processor_probe"]=processorProbe,["original_grant_fixture"]=grantResult,', 1)
        grid_anchor='em.AddComponentData(gridEntity,grid);var boundary=em.CreateEntity();'
        if text.count(grid_anchor)!=1:raise ValueError('Exact fixture grid seam required')
        text=text.replace(grid_anchor,'em.AddComponentData(gridEntity,grid);var sourceInputs=S005PlacementSourceInputs.Bind(em,gridEntity,grid,aInputDirectory,aPhysicalPath,aSurfacePath);var boundary=em.CreateEntity();',1)
        field_anchor='static string sourcePrefixPath;'
        if text.count(field_anchor)!=1:raise ValueError('Exact input fixture fields required')
        text=text.replace(field_anchor,'static string sourcePrefixPath,aInputDirectory,aPhysicalPath,aSurfacePath;',1)
        text=text.replace('replayHash=a[14];','replayHash=a[14];aInputDirectory=a[15];aPhysicalPath=a[16];aSurfacePath=a[9];',1)
        launch_anchor='em.AddComponentObject(session,new SkirmishLaunchDefinitionRecord(definition,actual,true));'
        if text.count(launch_anchor)!=1:raise ValueError('Exact fixture session launch seam required')
        initialization='''var initState=new Unity.Entities.SystemState{EntityManager=em,Enabled=true};var initOwner=new SkirmishSessionInitializationSystem();initOwner.OnUpdate(ref initState);var initialized=em.GetComponentData<Game.Components.SkirmishExpandedSessionComponent>(session);if(initialized.InitializationComplete!=1||initialized.Phase!=SkirmishSessionPhase.Spawning)throw new InvalidOperationException("Original initialization did not reach spawning: "+initialized.Phase+"/"+initialized.FailureCode);'''
        binding='''var probe=S006PartialLogicalBindingProbe.Probe(definition,actual,fixtureView,true,fixtureFraming,candidateOutput);string bindingPath=Path.Combine(candidateOutput,"binding-"+actual.DifficultyId+"-"+actual.SizeId+".json");var bindingBytes=File.ReadAllBytes(bindingPath);var candidateBinding=SourceUnqualifiedLogicalBindingCandidate.Load(bindingPath,AutonomousWorld.Hash(bindingBytes),actual);definition.BindMissionLogicalBinding(candidateBinding);var unboundSetup=actual;if(!SkirmishSetupCompiler.TryCompile(definition,unboundSetup.DifficultyId,unboundSetup.SizeId,unboundSetup.Seed,manifest,matrix,out var rebound,out var reboundReasons)||rebound==null||!JToken.DeepEquals(JArray.FromObject(unboundSetup.Structures),JArray.FromObject(rebound.Structures))||!JToken.DeepEquals(JArray.FromObject(unboundSetup.Forces),JArray.FromObject(rebound.Forces))||unboundSetup.MaterialsEach!=rebound.MaterialsEach||unboundSetup.OilEach!=rebound.OilEach||unboundSetup.UsableFuelEach!=rebound.UsableFuelEach)throw new InvalidOperationException("Recompiled binding changed fixed setup: "+String.Join(";",reboundReasons));actual=rebound;if(!SkirmishMissionLogicalBindingProvider.TryCompileForLaunch(definition,actual,out var logicalCompiled,out var logicalError))throw new InvalidOperationException("Original logical compiler rejected diagnostic candidate: "+logicalError);'''
        attach='''if(!SkirmishMissionLogicalBindingProvider.TryAttachCompiled(em,session,definition,actual,logicalCompiled,out logicalError)||!SkirmishMissionLogicalBindingProvider.TryRead(em,session,out var logicalRead)||!Object.ReferenceEquals(logicalCompiled,logicalRead))throw new InvalidOperationException("Original logical provider rejected diagnostic candidate: "+logicalError);'''
        session_anchor='var session=em.CreateEntity();'
        if text.count(session_anchor)!=1:raise ValueError('Exact source session creation seam required')
        text=text.replace(session_anchor,binding+session_anchor,1)
        state_anchor='ObjectiveKind=actual.ObjectiveKind};em.AddComponentData(session,state);'
        if text.count(state_anchor)!=1:raise ValueError('Exact constructed fixture session state required')
        text=text.replace(state_anchor,'ObjectiveKind=actual.ObjectiveKind,Phase=SkirmishSessionPhase.Queued};em.AddComponentData(session,state);',1)
        text=text.replace(launch_anchor,launch_anchor+attach+initialization,1)
        insertion=' var surfaceSource=new SourceConfigMaterializer(sourceSurfaceGraph)'
        if text.count(insertion)!=1:raise ValueError('Exact source processor seam required')
        processor=r''' var registry=new Dictionary<int,RuntimeBuildingEntity>();var selected=imported.Definitions.Where(x=>keys.ContainsKey(x.Prefab.name)).ToArray();if(selected.Length!=keys.Count)throw new InvalidOperationException("Exact captured definition subset required");
 var pipeline=new GeneratedStartingBuildingPipeline(em,gridEntity,boundary,selected,true,registry,sourceInputs.Buildings,sourceInputs.Roads,sourceInputs.Sidewalks,sourceInputs.Water,grid.Origin.y,(x,z,w,h)=>sourceInputs.Blocked(em,gridEntity,x,z,w,h));
 using(var all=em.CreateEntityQuery(new Unity.Entities.EntityQueryDesc{Options=Unity.Entities.EntityQueryOptions.IncludePrefab|Unity.Entities.EntityQueryOptions.IncludeDisabledEntities}))RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em,true,true,all.ToEntityArray(Unity.Collections.Allocator.Temp).ToArray(),entity=>em.HasComponentNamed(entity,"Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
 string processError=null;int scans=0;try{for(int frame=0;frame<20;frame++){pipeline.ProcessRequests(frame/60f,true);scans++;if(em.GetBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary).ToArray().All(r=>r.Status!=Game.Components.BuildingRuntimeSpawnRequest.Pending))break;}}catch(Exception e){processError=e.ToString();}var processed=em.GetBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary);var processedRows=new JArray();foreach(var req in processed)processedRows.Add(new JObject{["entry_index"]=req.EntryIndex,["status"]=(int)req.Status,["runtime_id"]=req.BuildingRuntimeId,["actual_origin"]=new JArray(req.ActualOrigin.x,req.ActualOrigin.y)});bool adopted=false;string adoptionError=null,adoptionReason=null;try{adopted=pipeline.SubmitOrBind(session,actual,out var actualReason);adoptionReason=actualReason.ToString();}catch(Exception e){adoptionError=e.ToString();}int bound=0;if(em.HasBuffer<SkirmishStartingBuildingRequest>(session)){var tracked=em.GetBuffer<SkirmishStartingBuildingRequest>(session);foreach(var item in tracked)if(item.BoundEntity!=Unity.Entities.Entity.Null&&em.Exists(item.BoundEntity))bound++;}
 var registeredRows=new JArray();var actorOwner=new BuildingRuntimeEntityCompositionSystemHelper();foreach(var req in processed){if(req.Status!=Game.Components.BuildingRuntimeSpawnRequest.Succeeded||!registry.TryGetValue(req.BuildingRuntimeId,out var building)){registeredRows.Add(new JObject{["entry_index"]=req.EntryIndex,["registered"]=false});continue;}var expected=actual.Structures[req.EntryIndex];bool typed=SkirmishStartingGrantPlacementConstraint.TryResolvePad(layout,actual,req.EntryIndex,out var ownedPad);bool contained=typed&&SkirmishStartingGrantPlacementConstraint.ContainsFootprint(layout,ownedPad,grid,map.Bounds,new RectInt(building.OriginCell,building.Definition.FootprintCells));bool blockerLive=building.BlockerEntity!=Unity.Entities.Entity.Null&&em.Exists(building.BlockerEntity);bool sourceWantsBlocker=actorOwner.ShouldRuntimeBuildingBlockPathing(building.Definition);var surfaceOwner=new BuildingSurfacePlacementUtilitySystemHelper();bool surfaceRead=surfaceOwner.TryEvaluateFootprint(em,building.OriginCell,building.Definition.FootprintCells,.5f,45f,out var surfaceResult);registeredRows.Add(new JObject{["entry_index"]=req.EntryIndex,["registered"]=true,["prefab"]=building.Definition.Prefab.name,["expected_prefab"]=SkirmishStructureIds.VisualKey(expected.StructureId),["id"]=building.Id,["combat_live"]=building.CombatEntity!=Unity.Entities.Entity.Null&&em.Exists(building.CombatEntity),["owner_known"]=building.HasOwnerFaction,["owner"]=building.OwnerFactionId,["expected_owner"]=expected.FactionId,["blocker_live"]=blockerLive,["source_wants_blocker"]=sourceWantsBlocker,["typed_pad_contained"]=contained,["surface_read"]=surfaceRead,["surface_valid"]=surfaceRead&&surfaceResult.IsValid});}
 bool constraintOpened=SkirmishTypedSpawnConstraint.TryForLaunch(em,session,actual,out var constraint);var forceRows=new JArray();if(constraintOpened&&constraint!=null){try{var occupancy=em.GetComponentData<Game.Components.DynamicOccupancyComponent>(gridEntity).Occupied;foreach(var force in actual.Forces){bool requested=constraint.TryRequestedWorld(force,0,out var requestedWorld);bool masked=constraint.TryMask(force,occupancy,out var mask);if(masked)mask.Dispose();forceRows.Add(new JObject{["faction"]=force.FactionId,["role"]=force.RoleId,["quantity"]=force.Quantity,["requested_world_available"]=requested,["requested_world"]=requested?(JToken)new JArray(requestedWorld.x,requestedWorld.y,requestedWorld.z):JValue.CreateNull(),["typed_mask_created"]=masked});}}finally{constraint.Dispose();}}
 var forceProbe=new JObject{["attempted"]=false};if(adopted&&bound==actual.Structures.Length&&constraintOpened){var blockerState=new Unity.Entities.SystemState{EntityManager=em,Enabled=true};var blockerOwner=new StaticGridBlockerUpdateSystem();blockerState.Begin();blockerOwner.OnCreate(ref blockerState);blockerState.End();blockerState.Begin();blockerOwner.OnUpdate(ref blockerState);blockerState.End();var prefabEntity=em.CreateEntity();var prefabRegistry=em.AddBuffer<Game.Components.UnitPrefabRegistryEntry>(prefabEntity);foreach(var pair in templates.Prefabs.OrderBy(x=>x.Key,StringComparer.Ordinal))prefabRegistry.Add(new Game.Components.UnitPrefabRegistryEntry{Prefab=pair.Value});bool forceSuccess=false;byte forceVisualPending=0;string forceError=null,forceReason=null;try{forceSuccess=SkirmishScenarioSpawnSystem.TrySpawnLedgers(em,session,actual,out var observedReason,out forceVisualPending);forceReason=observedReason.ToString();}catch(Exception ex){forceError=ex.ToString();}int actors=0;using(var query=em.CreateEntityQuery(typeof(Game.Components.SkirmishAttemptOwnedComponent),typeof(Game.Components.SkirmishUnitRoleComponent)))using(var entities=query.ToEntityArray(Unity.Collections.Allocator.Temp))foreach(var entity in entities)if(em.GetComponentData<Game.Components.SkirmishAttemptOwnedComponent>(entity).SessionId.Equals(state.SessionId))actors++;forceProbe=new JObject{["attempted"]=true,["success"]=forceSuccess,["reason"]=forceReason,["error"]=forceError,["visual_pending"]=forceVisualPending,["attempt_owned_role_actors"]=actors,["source_player_mode_assumed"]=true};}
 var processorProbe=new JObject{["initialization_complete"]=initialized.InitializationComplete,["initialization_phase"]=initialized.Phase.ToString(),["error"]=processError,["scans"]=scans,["registry_count"]=registry.Count,["rows"]=processedRows,["registered_rows"]=registeredRows,["adopted"]=adopted,["adoption_reason"]=adoptionReason,["adoption_error"]=adoptionError,["bound_count"]=bound,["force_constraint_opened"]=constraintOpened,["force_rows"]=forceRows,["force_spawn_probe"]=forceProbe,["fixture_read_model"]=true,["water_scope"]="empty diagnostic mask; not verified"};
'''
        text=text.replace(insertion,processor+insertion,1)
        old_search=text.index(' var surfaceSource=new SourceConfigMaterializer(sourceSurfaceGraph)')
        return_pos=text.index(' return new JObject{["definition_import_probe"]',old_search)
        text=text[:old_search]+text[return_pos:]
        text=text.replace('["constrained_search"]=constrained,','',1)
        driver = OUT / 'S006DefinitionImportProbe.cs'; driver.write_text(text); pins[str(driver)] = sha(driver)
        options = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
        old = str(BASE / 'S006OriginalGrantFixture.cs')
        if options.count(old) != 1: raise ValueError('Exact prior driver required')
        definitions = [x for x in options if x.endswith('/DeclaredGround/300_GeneratedBuildingDefinitions.s003.cs')]
        if len(definitions) != 1: raise ValueError('Exact original loader seam required')
        old_json = [x for x in options if x.endswith('/ObservedSourceConfigJson.cs')]
        if len(old_json) != 1: raise ValueError('Exact JSON boundary seam required')
        old_importer = [x for x in options if x.endswith('/305_GeneratedReferenceTemplateImporter.s003.cs')]
        old_codec = [x for x in options if x.endswith('/080_ExactInitialValueCodec.cs')]
        if len(old_importer)!=1 or len(old_codec)!=1:raise ValueError('Exact importer/codec seams required')
        original = Path(definitions[0]).read_text(); declaration = 'internal sealed class GeneratedBuildingDefinitions'
        if original.count(declaration) != 1: raise ValueError('Exact definition declaration required')
        partial = OUT / 'GeneratedBuildingDefinitions.partial.cs'; partial.write_text(original.replace(declaration, 'internal sealed partial class GeneratedBuildingDefinitions', 1)); pins[str(partial)] = sha(partial)
        import importlib.util
        spec=importlib.util.spec_from_file_location('source_session_extract',SESSION_PREPARER);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        fragment=SESSION_FRAGMENT.read_text();native=SESSION_OWNER.read_text()
        for signature in ('public void OnUpdate(', 'private static void TryInitialize(', 'private static void ProjectRoles(', 'private static void Fail('):
            if module.extract(native,signature)!=module.extract(fragment,signature):raise ValueError('Original session owner changed: '+signature)
        constraint_sources=[x for x in options if x.endswith('/typed-SkirmishTypedSpawnConstraint.cs')]
        if len(constraint_sources)!=1:raise ValueError('Exact generated typed force owner required')
        generated_constraint=Path(constraint_sources[0]).read_text();current_constraint=FORCE_CONSTRAINT.read_text()
        for signature in ('public static bool TryForLaunch(', 'public bool TryMask(', 'public bool TryRequestedWorld(', 'public static bool HasCompleteAllocator('):
            if module.extract(current_constraint,signature)!=module.extract(generated_constraint,signature):raise ValueError('Original typed force owner changed: '+signature)
        empty='public partial struct SkirmishScenarioSpawnSystem {\n\n}\n'
        if fragment.count(empty)!=1:raise ValueError('Exact empty owner wrapper required')
        session_fragment=OUT/'OriginalSessionOwner.cs';session_fragment.write_text(fragment.replace(empty,'',1));pins[str(session_fragment)]=sha(session_fragment)
        original_world=WORLD_OWNER.read_text();signature='public static void SuppressUnselectedStartupConfigs('
        begin=original_world.index(signature);opening=original_world.index('{',begin);depth=0;ending=None
        for i in range(opening,len(original_world)):
            if original_world[i]=='{':depth+=1
            elif original_world[i]=='}':
                depth-=1
                if depth==0:ending=i+1;break
        if ending is None:raise ValueError('Complete original suppression wrapper required')
        world_fragment=OUT/'OriginalWorldSuppression.cs';world_fragment.write_text('using Unity.Entities; namespace Game.Runtime { public static class SkirmishWorldSetup { '+original_world[begin:ending]+' } }');pins[str(world_fragment)]=sha(world_fragment)
        source=FORCE_OWNER.read_text();structure=STRUCTURE_FRAGMENT.read_text()
        for signature in ('private static void SetOrAdd<T>(', 'public static void BindStructure('):
            if module.extract(source,signature)!=module.extract(structure,signature):raise ValueError('Original structure binding owner changed: '+signature)
        spec=importlib.util.spec_from_file_location('force_extract',FORCE_EXTRACTOR);force_extract=importlib.util.module_from_spec(spec);spec.loader.exec_module(force_extract)
        methods={name:force_extract.extract_method(source,name) for name in ('TrySpawnLedgers','BuildPrefabEntityLookup','CreateStructure','DestroyAttemptOwned')}
        ledger=methods['TrySpawnLedgers']
        if ledger.count('Application.isPlaying')!=2 or ledger.count('CreateForceMember(')!=1:raise ValueError('Exact source playing-mode and force-member seams required')
        ledger=ledger.replace('Application.isPlaying','sourcePlayerSession').replace('CreateForceMember(','GeneratedTypedOpeningActorSource.CreateForceMember(',1)
        openbrace=ledger.index('{');ledger=ledger[:openbrace+1]+'\n const bool sourcePlayerSession=true; // Explicit headless gameplay diagnostic mode.\n'+ledger[openbrace+1:]
        force_fragment=OUT/'OriginalForceLedger.cs';force_fragment.write_text('using System;using System.Collections.Generic;using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;using UnityEngine;namespace Game.Runtime { public partial struct SkirmishScenarioSpawnSystem {\n'+ledger+'\n'+'\n'.join(methods[k] for k in ('BuildPrefabEntityLookup','CreateStructure','DestroyAttemptOwned'))+'\n} }');pins[str(force_fragment)]=sha(force_fragment)
        options = [str(driver) if x == old else str(partial) if x == definitions[0] else str(JSON_SHIM) if x == old_json[0] else str(S005_IMPORTER) if x == old_importer[0] else str(S005_CODEC) if x == old_codec[0] else '-out:' + str(OUT / 'control.exe') if x.startswith('-out:') else x for x in options]; options.extend([str(LOADER),str(JSON_OWNER),str(S005_HANDLES),str(INPUT_OWNER),str(session_fragment),str(world_fragment),str(SUPPRESSION),str(STRUCTURE_FRAGMENT),str(force_fragment)])
        rsp = OUT / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in options)); csc = next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
        build = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], cwd=ROOT, capture_output=True, text=True, timeout=300); (OUT / 'compile.log').write_text(build.stdout + build.stderr)
        if build.returncode: raise RuntimeError('Definition import source fixture compilation failed')
        runtime = json.loads(BUILD.read_bytes())['runtime_selection']; env = os.environ.copy(); env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT / 'Library/ScriptAssemblies'),str(OUT)))
        graph=LOGS/'s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json';matrix=ROOT/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv';delta=LOGS/'s006-external-config-candidate-20261105-T1/data.json';proposal=LOGS/'s006-static-footprint-proposals-20261105-T1/report.json';search=LOGS/'s006-original-initial-search-20261106-T1/result.json';surface=ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json';prefix=LOGS/'s005-campaign-placement-inputs-20261105-T1/sourceInvalidPrefix.bin';config=LOGS/'s004-current-physical-input-projection-20261104-T1/mono-config.xml';capture=ROOT/'Logs/SkirmishProduction/s005-production-factory-capture-20261105-T2/production-factory-runway-definitions.json'
        command=[runtime['mono'],'--config',str(config),'--optimize=-float32',str(OUT/'control.exe'),str(graph),str(matrix),str(OUT/'result.json'),str(OUT/'layout.json'),'1',str(delta),pins[str(delta)],str(proposal),str(search),str(surface),str(prefix),str(capture),str(PROJECTION),str(REPLAY),pins[str(REPLAY)],str(INPUT_DIR),str(PHYSICAL),str(FRAMING),str(OUT),str(LOGS/'s005-view-profile-source-graph-20261105-T1/source-config-graph.json')]
        run=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(OUT/'run.log').write_text(run.stdout+run.stderr)
        if run.returncode or run.stdout.splitlines().count('[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0')!=1:raise RuntimeError('Definition import fixture failed')
        verify();result=json.loads((OUT/'result.json').read_bytes());rows=[]
        for row in result['rows']:
            p=row.get('disposable_pad_projection');values=p.get('definition_import_probe') if p else None;probe=p.get('processor_probe') if p else None;rr=probe['registered_rows'] if probe else [];forces=probe['force_rows'] if probe else [];rows.append({'size':row['size'],'boundary':row.get('source_host_boundary'),'structures':len(values) if values is not None else None,'definitions_found':sum(v['definition_found'] is True for v in values) if values is not None else None,'initialization_complete':probe['initialization_complete'] if probe else None,'initialization_phase':probe['initialization_phase'] if probe else None,'processor_error':probe['error'] if probe else None,'scans':probe['scans'] if probe else None,'registry_count':probe['registry_count'] if probe else None,'request_statuses':[v['status'] for v in probe['rows']] if probe else None,'adopted':probe['adopted'] if probe else None,'adoption_reason':probe['adoption_reason'] if probe else None,'adoption_error':probe['adoption_error'] if probe else None,'bound_count':probe['bound_count'] if probe else None,'registry_identity_valid':sum(x.get('registered') is True and x['prefab']==x['expected_prefab'] and x['combat_live'] is True and x['owner_known'] is True and x['owner']==x['expected_owner'] for x in rr),'typed_contained':sum(x.get('typed_pad_contained') is True for x in rr),'surface_valid':sum(x.get('surface_valid') is True for x in rr),'blocker_matches_owner':sum(x.get('registered') is True and x['blocker_live']==x['source_wants_blocker'] for x in rr),'helipads':[{'blocker_live':x['blocker_live'],'source_wants_blocker':x['source_wants_blocker']} for x in rr if x.get('prefab')=='Building_Helipad'],'force_constraint_opened':probe['force_constraint_opened'] if probe else None,'force_spawn_probe':probe['force_spawn_probe'] if probe else None,'force_rows':len(forces),'force_requested_worlds':sum(x['requested_world_available'] is True for x in forces),'force_masks_created':sum(x['typed_mask_created'] is True for x in forces)})
        report={'status':'CollectedUnqualifiedSourceProcessorProbe','source_pins':pins,'result_sha256':sha(OUT/'result.json'),'rows':rows,'scope':'Diagnostic same-manager source processor with stale S005 factory capture, fixture read model, retained inactive Campaign input, empty water mask and unqualified S006 candidate. No native/source fit or readiness.','runtime_admission':False,'mission_ready':False};(OUT/'receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'rows':rows,'mission_ready':False}))
    except Exception as error:
        (OUT/'failure.json').write_text(json.dumps({'status':'Failed','error':repr(error),'mission_ready':False},indent=2)+'\n');raise


if __name__ == '__main__': main()
