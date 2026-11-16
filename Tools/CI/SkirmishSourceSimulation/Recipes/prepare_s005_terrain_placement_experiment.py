#!/usr/bin/env python3
"""Prepare a source-only S005 placement experiment with a surface-validity gate."""
import argparse,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
PREP=ROOT/'T Logs/SkirmishProduction/s005-starting-grant-experiment-inputs-20261105-T2'
HELPER=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005StartingGrantExperiment.cs.txt'
PLACEMENT=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/329_GeneratedStartingBuildingPlacement.cs'
INPUTS=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005PlacementSourceInputs.cs.txt'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def sub(s,a,b):
 if s.count(a)!=1:raise ValueError('Exact source anchor required: '+a[:90])
 return s.replace(a,b)
def prepare(out):
 session=(PREP/'session.cs').read_text();session=sub(session,'args.Length != 7','args.Length != 10')
 session=sub(session,'Path.GetFullPath(args[6]));','Path.GetFullPath(args[6]), Path.GetFullPath(args[7]), Path.GetFullPath(args[8]), Path.GetFullPath(args[9]));')
 helper=HELPER.read_text();helper=sub(helper,'static string projectionPath, factoryPath, readmodelPath;','static string projectionPath, factoryPath, readmodelPath, inputDirectory, physicalPath, surfaceGraphPath;')
 helper=sub(helper,'Configure(JObject sourceGraph,string projection,string factory,string readmodel)','Configure(JObject sourceGraph,string projection,string factory,string readmodel,string inputs,string physical,string surfaceGraph)')
 helper=sub(helper,'graph=sourceGraph;projectionPath=projection;factoryPath=factory;readmodelPath=readmodel;',
   'graph=sourceGraph;projectionPath=projection;factoryPath=factory;readmodelPath=readmodel;inputDirectory=inputs;physicalPath=physical;surfaceGraphPath=surfaceGraph;')
 start=helper.index('        // Constructor-only placeholders;');end=helper.index('        var actual=em.GetBuffer<BuildingConfiguredSpawnableReadModel>',start)
 new='''        var inputs=S005PlacementSourceInputs.Bind(em,gridEntity,grid,inputDirectory,physicalPath,surfaceGraphPath);
        var registry=new Dictionary<int,RuntimeBuildingEntity>();
        var pipeline=new GeneratedStartingBuildingPipeline(em,gridEntity,boundary,definitions.Definitions,true,registry,
            inputs.Buildings,inputs.Roads,inputs.Sidewalks,inputs.Water,grid.Origin.y,
            (x,z,w,h)=>inputs.Blocked(em,gridEntity,x,z,w,h));
        pipeline.BindConfiguredRequestMetadata(order,requestMetadata);
'''
 helper=helper[:start]+new+helper[end:]
 start=helper.index('        return new JObject { ["status"]="OriginalS005StartingRequestsPending"');end=helper.index('\n    }',start)
 new='''        using(var all=em.CreateEntityQuery(new EntityQueryDesc { Options=EntityQueryOptions.IncludePrefab|EntityQueryOptions.IncludeDisabledEntities }))
            RuntimeActorBirthContext.BindNoNativeBirthRecordProfile(em,true,true,all.ToEntityArray(Unity.Collections.Allocator.Temp).ToArray(),
                entity=>em.HasComponentNamed(entity,"Game.Runtime.RuntimeActorBirthContext+RuntimeActorBirthRecord"));
        string failure=null;
        try { for(int frame=0;frame<20;frame++) { pipeline.ProcessRequests(frame/60f,true); if(requests.ToArray().All(r=>r.Status!=BuildingRuntimeSpawnRequest.Pending)) break; } }
        catch(Exception error) { failure=error.ToString(); }
        var actualRows=new JArray();int success=0,failed=0,pending=0;
        var evaluator=new BuildingSurfacePlacementUtilitySystemHelper();
        for(int i=0;i<requests.Length;i++)
        {
            var r=requests[i];if(r.Status==BuildingRuntimeSpawnRequest.Succeeded)success++;else if(r.Status==BuildingRuntimeSpawnRequest.Failed)failed++;else if(r.Status==BuildingRuntimeSpawnRequest.Pending)pending++;
            bool evaluated=false,valid=false;if(r.Status==BuildingRuntimeSpawnRequest.Succeeded)
            {var definition=definitions.Definitions.Single(d=>d.Prefab.name==SkirmishStructureIds.VisualKey(setup.Structures[i].StructureId));
             evaluated=evaluator.TryEvaluateFootprint(em,r.ActualOrigin,definition.FootprintCells,.5f,45f,out var surface);valid=evaluated&&surface.IsValid;
             if(!valid)throw new InvalidDataException("Source registration succeeded on an invalid surface");}
            actualRows.Add(new JObject { ["request_id"]=r.RequestId,["status"]=(int)r.Status,["result_code"]=(int)r.ResultCode,
                ["actual_x"]=r.ActualOrigin.x,["actual_y"]=r.ActualOrigin.y,["runtime_id"]=r.BuildingRuntimeId,
                ["surface_evaluated"]=evaluated,["surface_valid"]=valid });
        }
        bool adopted=false;string adoptionFailure=null;var adoptionReason=SkirmishReasonCode.None;
        if(failure==null && success==14 && failed==0 && pending==0)
        {
            try { adopted=SkirmishStartingBuildingsService.Step(em,session,setup,out adoptionReason); }
            catch(Exception error) { adoptionFailure=error.ToString(); }
        }
        var bound=em.GetBuffer<SkirmishStartingBuildingRequest>(session);
        int actualBound=bound.ToArray().Count(item=>item.BoundEntity!=Entity.Null && em.Exists(item.BoundEntity) && em.HasComponent<SkirmishSharedActorTag>(item.BoundEntity));
        bool forceAttempted=false,forceSucceeded=false;byte visualPending=0;string forceException=null;var forceReason=SkirmishReasonCode.None;
        if(adopted && actualBound==14)
        {
            var blockerState=new SystemState { EntityManager=em,Enabled=true };var blockerOwner=new StaticGridBlockerUpdateSystem();
            blockerState.Begin();blockerOwner.OnCreate(ref blockerState);blockerState.End();blockerState.Begin();blockerOwner.OnUpdate(ref blockerState);blockerState.End();
            var registryEntity=em.CreateEntity();var prefabRegistry=em.AddBuffer<UnitPrefabRegistryEntry>(registryEntity);
            foreach(var entry in templates.Prefabs.OrderBy(pair=>pair.Key,StringComparer.Ordinal)) prefabRegistry.Add(new UnitPrefabRegistryEntry { Prefab=entry.Value });
            forceAttempted=true;
            try { forceSucceeded=SkirmishScenarioSpawnSystem.TrySpawnLedgers(em,session,setup,out forceReason,out visualPending); }
            catch(Exception error) { forceException=error.ToString(); }
        }
        // Capture startup immediately after the spawn owner. Later diagnostic
        // transformations insert recurring owners before the final roster census.
        var initialForceCounts=new Dictionary<string,int>(StringComparer.Ordinal);
        var initialForceIds=new HashSet<string>(StringComparer.Ordinal);
        int initialForceActorCount=0;
        bool initialForceIdsUnique=true;
        using(var initialOwned=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
        using(var initialActors=initialOwned.ToEntityArray(Unity.Collections.Allocator.Temp))
        {
            var initialSessionId=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            foreach(var actor in initialActors)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);
                if(!owner.SessionId.Equals(initialSessionId))continue;
                initialForceActorCount++;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);
                string key=owner.FactionId+"/"+(int)role.Role;
                initialForceCounts.TryGetValue(key,out int count);initialForceCounts[key]=count+1;
                string stable=owner.StableObjectId.ToString();
                if(String.IsNullOrEmpty(stable)||!initialForceIds.Add(stable))initialForceIdsUnique=false;
            }
        }
        var initialExpectedCounts=setup.Forces.GroupBy(f=>f.FactionId+"/"+(int)f.RoleKind)
            .ToDictionary(g=>g.Key,g=>g.Sum(f=>f.Quantity),StringComparer.Ordinal);
        bool initialRosterMatches=forceSucceeded && initialForceIdsUnique &&
            initialExpectedCounts.Count==initialForceCounts.Count &&
            initialExpectedCounts.All(p=>initialForceCounts.TryGetValue(p.Key,out int n)&&n==p.Value);
        var initialRosterSnapshot=new JObject {
            ["stage"]="ImmediatelyAfterInitialSpawnBeforeRecurringOwners",
            ["count"]=initialForceActorCount,["unique_stable_id_count"]=initialForceIds.Count,
            ["stable_ids_unique"]=initialForceIdsUnique,
            ["actual_counts"]=JObject.FromObject(initialForceCounts),
            ["expected_counts"]=JObject.FromObject(initialExpectedCounts),
            ["matches_compiled_counts"]=initialRosterMatches };
        var forceRows=new JArray();var counts=new Dictionary<string,int>(StringComparer.Ordinal);var stableIds=new HashSet<string>(StringComparer.Ordinal);
        using(var owned=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent),typeof(SkirmishUnitRoleComponent)))
        using(var actors=owned.ToEntityArray(Unity.Collections.Allocator.Temp))
        {
            var sessionId=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;
            foreach(var actor in actors)
            {
                var owner=em.GetComponentData<SkirmishAttemptOwnedComponent>(actor);if(!owner.SessionId.Equals(sessionId))continue;
                var role=em.GetComponentData<SkirmishUnitRoleComponent>(actor);string key=owner.FactionId+"/"+(int)role.Role;
                counts.TryGetValue(key,out int count);counts[key]=count+1;string stable=owner.StableObjectId.ToString();
                bool unique=!String.IsNullOrEmpty(stable)&&stableIds.Add(stable);
                var cell=em.HasComponent<UnitGrid>(actor)?em.GetComponentData<UnitGrid>(actor).Cell:default;
                forceRows.Add(new JObject { ["stable_id"]=stable,["stable_id_unique"]=unique,["faction"]=owner.FactionId,
                    ["role"]=(int)role.Role,["has_grid"]=em.HasComponent<UnitGrid>(actor),["cell_x"]=cell.x,["cell_y"]=cell.y,
                    ["prefab_key"]=em.HasComponent<UnitSourcePrefabKey>(actor)?em.GetComponentData<UnitSourcePrefabKey>(actor).Value.ToString():null,
                    ["combat_suppressed"]=em.HasComponent<CampaignMissionCombatSuppressedTag>(actor) });
            }
        }
        var expectedCounts=setup.Forces.GroupBy(f=>f.FactionId+"/"+(int)f.RoleKind).ToDictionary(g=>g.Key,g=>g.Sum(f=>f.Quantity),StringComparer.Ordinal);
        bool rosterMatches=forceSucceeded && expectedCounts.Count==counts.Count && expectedCounts.All(p=>counts.TryGetValue(p.Key,out int n)&&n==p.Value)
            && forceRows.OfType<JObject>().All(r=>(bool)r["stable_id_unique"]);
        return new JObject { ["status"]=failure==null?"PlacementOwnersCompleted":"PlacementOwnerStopped",
            ["native_publisher_rows_matched"]=actual.Length,["queued_requests"]=rows,["results"]=actualRows,
            ["succeeded"]=success,["failed"]=failed,["pending"]=pending,["registered_buildings"]=registry.Count,
            ["exception"]=failure,["physical_nonbuilding_blockers"]=inputs.PhysicalBlockers,
            ["adoption_owner_called"]=failure==null && success==14 && failed==0 && pending==0,
            ["adoption_succeeded"]=adopted,["adoption_reason"]=adoptionReason.ToString(),
            ["adoption_exception"]=adoptionFailure,["bound_buildings"]=actualBound,
            ["force_ledger_owner"]="Source-host SkirmishScenarioSpawnSystem.TrySpawnLedgers projection",
            ["force_owner_attempted"]=forceAttempted,["force_owner_succeeded"]=forceSucceeded,
            ["force_owner_reason"]=forceReason.ToString(),["force_owner_exception"]=forceException,
            ["force_visual_pending"]=visualPending,["session_phase_after_force_owner"]=em.GetComponentData<SkirmishExpandedSessionComponent>(session).Phase.ToString(),
            ["initial_force_roster_before_recurring_owners"]=initialRosterSnapshot,
            ["force_roster"]=forceRows,["force_roster_matches_compiled_counts"]=rosterMatches,
            ["force_roster_scope"]="CensusAtResultTimeMayFollowRecurringOwners",
            ["active_water_mask"]="AssumedEmptyForSourceDiagnostic",["live_occupancy_measured"]=false,
            ["surface_gate"]="Original surface evaluator applied as experimental rejection gate",
            ["mountain_mesh_clearance_proven"]=false,["native_session"]=false,["mission_ready"]=false };
'''
 helper=helper[:start]+new+helper[end:]
 placement=PLACEMENT.read_text();anchor='            if (rotated || !BuildingPlacementValidationUtilitySystemHelper.IsFootprintInsideGrid(origin, footprint, layout)) return false;'
 placement=sub(placement,anchor,anchor+'\n            var terrain = new BuildingSurfacePlacementUtilitySystemHelper();\n            if (!terrain.TryEvaluateFootprint(em, origin, footprint, .5f, 45f, out var surface) || !surface.IsValid) return false;')
 pins={str(p):sha(p) for p in (PREP/'session.cs',PREP/'loader.cs',HELPER,PLACEMENT,INPUTS,Path(__file__).resolve())}
 out.mkdir(parents=True,exist_ok=False)
 for n,s in [('session.cs',session),('helper.cs',helper),('placement.cs',placement)]: (out/n).write_text(s)
 (out/'preparation.json').write_text(json.dumps({'status':'PreparedExperimentalOnly','source_pins':pins,'surface_gate_added':True,
  'scope':'S005 isolated terrain-gated placement; no default/game source modified','mission_ready':False},indent=2)+'\n')
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);prepare(p.parse_args().output.resolve())
