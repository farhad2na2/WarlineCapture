#!/usr/bin/env python3
"""Evaluate source-owned S005 startup predicates; never assert native readiness."""
import argparse
import hashlib
import importlib.util
import json
import re
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
spec = importlib.util.spec_from_file_location("roads", HERE / "prepare_s005_initial_road_experiment.py")
roads = importlib.util.module_from_spec(spec)
spec.loader.exec_module(roads)

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def prepare(out):
    scene = ROOT / "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity"
    meta = Path(str(scene) + ".meta")
    city = ROOT / "Assets/Game/Configs/Scene/Game_RuntimeCitySpawner_Config.asset"
    if len(re.findall(r"(?m)^guid: dad0bd13fb20943dfb2f881cbe225f05\r?$", meta.read_text())) != 1:
        raise ValueError("S005 source scene GUID mismatch")
    if len(re.findall(r"(?m)^  canonicalPresentationMode: 2\r?$", scene.read_text())) != 1:
        raise ValueError("S005 entity-scene source mode required")
    if len(re.findall(r"(?m)^  spawnOnStart: 1\r?$", city.read_text())) != 1 or len(re.findall(r"(?m)^  cityCount: 0\r?$", city.read_text())) != 1:
        raise ValueError("Source city startup facts differ")
    roads.prepare(out)
    once = roads.groups.epoch.roster.base.once
    session = out / "session.cs"
    source = session.read_text()
    source = once(source, "S005StartingGrantExperiment.Run(em, entity, setup)", "S005StartingGrantExperiment.Run(em, entity, setup, definition.CampaignMap)")
    session.write_text(source)
    helper = out / "helper.cs"
    source = helper.read_text()
    source = once(source, "internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup)",
                  "internal static JObject Run(EntityManager em,Entity session,SkirmishResolvedSetup setup,OperationMapDefinition actualMap)")
    anchor = "        bool ready=SkirmishStartingBuildingsService.Step(em,session,setup,out var reason);"
    source = once(source, anchor, '''        using(var gameplayQuery=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent)))
            Need(gameplayQuery.CalculateEntityCount()==0,"Fresh source gameplay state required");
        RuntimeGameplayStartTransition.ResetForGameplayStart(em);
''' + anchor)
    anchor = '        var forceRows=new JArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);var stableIds=new HashSet<string>(StringComparer.Ordinal);'
    block = '''        JObject sourceActivation = new JObject { ["attempted"] = false };
        if (forceSucceeded)
        {
            Need(actualMap != null && actualMap.PresentationKind == OperationMapPresentationKind.EntityScene &&
                actualMap.SourceSceneReference.AssetGUID == "dad0bd13fb20943dfb2f881cbe225f05", "Actual S005 source map identity required");
            string bindingScene = Path.GetFullPath("SCENE_PATH");
            Need(AutonomousWorld.Hash(File.ReadAllBytes(bindingScene)) == "SCENE_HASH", "S005 source binding scene changed");
            string bindingText = File.ReadAllText(bindingScene);
            Need(System.Text.RegularExpressions.Regex.Matches(bindingText,@"(?m)^  canonicalPresentationMode: 2\\r?$").Count==1,"Entity-scene mode required");
            bool noBuildings = String.IsNullOrEmpty(actualMap.BuildingPlacementsReference.AssetGUID);
            bool noVehicles = String.IsNullOrEmpty(actualMap.VehiclePlacementsReference.AssetGUID);
            bool buildingReady = GameplayStartCompletionPredicate.BuildingStartupComplete(adopted && actualBound==14 && registry.Count==14 &&
                GameplayStartCompletionPredicate.MapPlacementOwnerComplete(noBuildings,false,false,false,false,false) &&
                GameplayStartCompletionPredicate.MapPlacementOwnerComplete(noVehicles,false,false,false,false,false));
            bool cityReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(false,
                GameplayStartCompletionPredicate.RuntimeCityOwnerSpawned(false,0));
            bool blockerReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(
                GameplayStartCompletionPredicate.LegacyPresentationEnabled(true),false);
            bool decorationReady = GameplayStartCompletionPredicate.OptionalRuntimeOwnerReady(false,false);
            using var configs = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig));
            using var initialized = em.CreateEntityQuery(typeof(InitialUnitsSpawnConfig),typeof(InitialUnitsSpawnInitialized));
            int configCount=configs.CalculateEntityCount(), initializedCount=initialized.CalculateEntityCount();
            bool configReady=GameplayStartCompletionPredicate.InitialSpawnConfigsComplete(configCount,initializedCount);
            bool worldReady=GameplayStartCompletionPredicate.RuntimeWorldAvailable(em.World!=null,em.World!=null && em.World.IsCreated);
            using var stateQuery=em.CreateEntityQuery(typeof(RuntimeGameplayStateComponent));
            Need(stateQuery.CalculateEntityCount()==1,"Unique source gameplay state required");
            var before=stateQuery.GetSingleton<RuntimeGameplayStateComponent>();
            bool requested=GameplayStartCompletionPredicate.GameplayStartRequested(Object.ReferenceEquals(retainedWorld.Entities,em),before.PlayRequested!=0);
            bool gate = noBuildings && noVehicles && buildingReady && cityReady && blockerReady && decorationReady && configReady && worldReady && requested &&
                (bool)roadReceipt["actual_original_sync_executed"] && em.GetComponentData<SkirmishExpandedSessionComponent>(session).SpawnComplete==1;
            bool activated=RuntimeGameplayStartTransition.TryActivateSourceHostDiagnostic(em,gate);
            var after=stateQuery.GetSingleton<RuntimeGameplayStateComponent>();
            sourceActivation = new JObject { ["attempted"]=true,["gate_complete"]=gate,["activated"]=activated,
                ["play_requested"]=(int)after.PlayRequested,["simulation_active"]=(int)after.SimulationActive,
                ["same_world"]=Object.ReferenceEquals(retainedWorld.Entities,em),["initial_configs"]=configCount,
                ["initialized_configs"]=initializedCount,["physical_scene_validated"]=false,
                ["scope"]="Source-only authored predicate; source start request, not native input" };
        }
'''.replace("SCENE_PATH", str(scene.relative_to(ROOT))).replace("SCENE_HASH", sha(scene)) + anchor
    source = once(source, anchor, block)
    source = once(source, 'return new JObject { ["initial_roads"]=roadReceipt,',
                  'return new JObject { ["source_activation"]=sourceActivation, ["initial_roads"]=roadReceipt,')
    helper.write_text(source)
    prep=out / "preparation.json"
    doc=json.loads(prep.read_text())
    for path in (helper,session,scene,meta,city,Path(__file__).resolve()): doc["source_pins"][str(path)]=sha(path)
    doc["source_start_scope"]="Authored source-only startup predicate; no native physical scene or input claim"
    prep.write_text(json.dumps(doc,indent=2)+"\n")

if __name__ == "__main__":
    p=argparse.ArgumentParser();p.add_argument("output",type=Path);prepare(p.parse_args().output.resolve())
