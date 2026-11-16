#!/usr/bin/env python3
"""Exercise original S006 starting-grant request owner in a disposable source fixture."""
import hashlib
import json
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
BASE = LOGS / 's006-current-typed-pad-replay-20261106-T7'
OUT = LOGS / 's006-original-grant-fixture-20261106-T5'
BUILD = LOGS / 's003-current-prefab-import-build-20261104-T1/build.json'
CAPTURE = ROOT / 'Logs/SkirmishProduction/s005-production-factory-capture-20261105-T2/production-factory-runway-definitions.json'


def sha(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for b in iter(lambda: f.read(1024 * 1024), b''):
            h.update(b)
    return h.hexdigest()


def main():
    OUT.mkdir(exist_ok=False)
    try:
        prior = json.loads((BASE / 'receipt.json').read_bytes())
        if prior.get('status') != 'CollectedUnqualifiedCurrentTypedPadReplay' or prior.get('mission_ready') is not False:
            raise ValueError('Exact unqualified prior input required')
        pins = dict(prior['source_pins'])
        search_receipt = LOGS / 's006-original-initial-search-20261106-T1/receipt.json'
        if prior['source_pins'].get(str(search_receipt)) != sha(search_receipt) or json.loads(search_receipt.read_bytes())['source_pins'].get(str(CAPTURE)) != sha(CAPTURE):
            raise ValueError('Exact historically captured factory data required')
        for path in (Path(__file__), BASE / 'receipt.json', BASE / 'compile.rsp', BASE / 'S006TypedPadResolutionControl.cs', BUILD, CAPTURE):
            pins[str(path)] = sha(path)
        def verify():
            for path, expected in pins.items():
                if not Path(path).is_file() or sha(path) != expected:
                    raise ValueError('Pinned input changed: ' + path)
        verify()
        driver = (BASE / 'S006TypedPadResolutionControl.cs').read_text()
        start = driver.index(' var surfaceSource=new SourceConfigMaterializer(sourceSurfaceGraph)')
        end = driver.index(' return new JObject{["constrained_search"]=constrained,', start)
        fixture = r''' var em=new Unity.Entities.EntityManager{LogicalAllocatorAdmitted=true};int nextEntity=600000;em.AllocateLogicalIdentity=()=>new Unity.Entities.Entity{Index=nextEntity++,Version=1};
 var gridEntity=em.CreateEntity();em.AddComponentData(gridEntity,grid);var boundary=em.CreateEntity();em.AddComponentData(boundary,new Game.Components.BuildingRuntimeStateTag());var models=em.AddBuffer<Game.Components.BuildingConfiguredSpawnableReadModel>(boundary);em.AddBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary);
 var captured=((JArray)sourceFactory["buildings"]).OfType<JObject>().ToDictionary(x=>(string)x["prefab"]["key"],x=>x,StringComparer.Ordinal);var keys=new Dictionary<string,Unity.Mathematics.int2>(StringComparer.Ordinal);foreach(JObject item in candidates){string key=(string)item["visual_key"];if(!captured.TryGetValue(key,out var factory))throw new InvalidOperationException("Captured source building missing");var field=(JObject)factory["fields"]["FootprintCells"];if((string)field["codec"]!="struct-fields-v1"||(string)field["type"]!="UnityEngine.Vector2Int")throw new InvalidOperationException("Exact captured source footprint required");var xy=(JObject)field["fields"];var size=new Unity.Mathematics.int2(int.Parse((string)xy["UnityEngine.Vector2Int::m_X"]["value"],System.Globalization.CultureInfo.InvariantCulture),int.Parse((string)xy["UnityEngine.Vector2Int::m_Y"]["value"],System.Globalization.CultureInfo.InvariantCulture));var fp=(JArray)item["footprint"];if(size.x!=(int)fp[0]||size.y!=(int)fp[1])throw new InvalidOperationException("Candidate differs from captured source footprint");if(keys.TryGetValue(key,out var before)&&(before.x!=size.x||before.y!=size.y))throw new InvalidOperationException("Inconsistent retained fixture footprint");keys[key]=size;}
 foreach(var row in keys)models.Add(new Game.Components.BuildingConfiguredSpawnableReadModel{BuildingId=new Unity.Collections.FixedString128Bytes(row.Key),FootprintCells=row.Value});
 var session=em.CreateEntity();var state=new Game.Components.SkirmishExpandedSessionComponent{SessionId=new Unity.Collections.FixedString64Bytes("s006-fixture"),CatalogId=new Unity.Collections.FixedString64Bytes(actual.CatalogId),DefinitionId=new Unity.Collections.FixedString64Bytes(actual.DefinitionId),ContentVersion=actual.ContentVersion,SetupHash=actual.SetupHash,Seed=actual.Seed,SizeId=actual.SizeId,DifficultyId=actual.DifficultyId,ObjectiveKind=actual.ObjectiveKind};em.AddComponentData(session,state);em.AddComponentObject(session,new SkirmishResolvedSetupRecord{Setup=actual});em.AddComponentObject(session,new SkirmishLaunchDefinitionRecord(definition,actual,true));
 bool complete=SkirmishStartingBuildingsService.Step(em,session,actual,out var reason);var requestRows=new JArray();if(em.HasBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary)){var requests=em.GetBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary);for(int i=0;i<requests.Length;i++){var item=requests[i];requestRows.Add(new JObject{["entry_index"]=item.EntryIndex,["request_id"]=item.RequestId,["building"]=item.BuildingId.ToString(),["faction"]=item.FactionId,["authored_starting_grant"]=item.AuthoredStartingGrant,["require_typed_pad"]=item.RequireTypedPad,["preferred_x"]=item.PreferredOrigin.x,["preferred_y"]=item.PreferredOrigin.y,["status"]=(int)item.Status});}}
 var grantResult=new JObject{["step_complete"]=complete,["reason"]=reason.ToString(),["count"]=requestRows.Count,["requests"]=requestRows,["owner_present"]=em.HasComponent<Game.Components.BuildingStartingGrantOwner>(session),["read_model_scope"]="Fixture constructed from captured historical factory dimensions; not native publisher"};
'''
        driver = driver[:start] + fixture + driver[start:]
        factory_entry=' static JObject sourceProposals,sourceSearch,sourceSurfaceGraph;static string sourcePrefixPath;'
        if driver.count(factory_entry)!=1:raise ValueError('Exact source fixture field seam required')
        driver=driver.replace(factory_entry,' static JObject sourceProposals,sourceSearch,sourceSurfaceGraph,sourceFactory;static string sourcePrefixPath;',1)
        setup_entry='sourcePrefixPath=a[10];'
        if driver.count(setup_entry)!=1:raise ValueError('Exact source fixture argument seam required')
        driver=driver.replace(setup_entry,'sourcePrefixPath=a[10];sourceFactory=JObject.Parse(File.ReadAllText(a[11]));',1)
        original_preferred = 'var preferred=new Vector2Int(center.x-fp.x/2,center.y-fp.y/2);'
        if driver.count(original_preferred) != 1:
            raise ValueError('Exact original search input seam required')
        driver = driver.replace(original_preferred, 'var grant=em.GetBuffer<Game.Components.BuildingRuntimeSpawnRequest>(boundary)[index];if(grant.EntryIndex!=index||grant.RequireTypedPad!=1||grant.AuthoredStartingGrant!=1||grant.Status!=Game.Components.BuildingRuntimeSpawnRequest.Pending)throw new InvalidOperationException("Exact original pending typed grant required");var preferred=new Vector2Int(grant.PreferredOrigin.x,grant.PreferredOrigin.y);', 1)
        driver = driver.replace('["constrained_search"]=constrained,', '["original_grant_fixture"]=grantResult,["constrained_search"]=constrained,', 1)
        driver_path = OUT / 'S006OriginalGrantFixture.cs'; driver_path.write_text(driver); pins[str(driver_path)] = sha(driver_path)
        options = [json.loads(line) for line in (BASE / 'compile.rsp').read_text().splitlines() if line.strip()]
        old = str(BASE / 'S006TypedPadResolutionControl.cs')
        if options.count(old) != 1:
            raise ValueError('Exact existing fixture driver required')
        options = [str(driver_path) if x == old else '-out:' + str(OUT / 'control.exe') if x.startswith('-out:') else x for x in options]
        rsp = OUT / 'compile.rsp'; rsp.write_text(''.join(json.dumps(x) + '\n' for x in options))
        csc = next(Path(x) for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
        build = subprocess.run(['dotnet', str(csc), '@' + str(rsp)], cwd=ROOT, capture_output=True, text=True, timeout=300)
        (OUT / 'compile.log').write_text(build.stdout + build.stderr)
        if build.returncode: raise RuntimeError('Grant fixture source compilation failed')
        runtime = json.loads(BUILD.read_bytes())['runtime_selection']; env = os.environ.copy()
        env['MONO_PATH'] = os.pathsep.join((str(BUILD.parent / 'placement'), runtime['engine'], str(Path(runtime['newtonsoft']).parent), str(ROOT / 'Library/ScriptAssemblies'), str(OUT)))
        graph=LOGS/'s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json';matrix=ROOT/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv';delta=LOGS/'s006-external-config-candidate-20261105-T1/data.json';proposal=LOGS/'s006-static-footprint-proposals-20261105-T1/report.json';search=LOGS/'s006-original-initial-search-20261106-T1/result.json';surface=ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json';prefix=LOGS/'s005-campaign-placement-inputs-20261105-T1/sourceInvalidPrefix.bin';config=LOGS/'s004-current-physical-input-projection-20261104-T1/mono-config.xml'
        command=[runtime['mono'],'--config',str(config),'--optimize=-float32',str(OUT/'control.exe'),str(graph),str(matrix),str(OUT/'result.json'),str(OUT/'layout.json'),'1',str(delta),pins[str(delta)],str(proposal),str(search),str(surface),str(prefix),str(CAPTURE)]
        run=subprocess.run(command,cwd=ROOT,env=env,capture_output=True,text=True,timeout=300);(OUT/'run.log').write_text(run.stdout+run.stderr)
        if run.returncode or run.stdout.splitlines().count('[S006SourceCandidate] result=Collected sizes=3 candidateOnly=1 readiness=0')!=1:raise RuntimeError('Original grant fixture failed')
        verify();result=json.loads((OUT/'result.json').read_bytes());rows=[]
        for row in result['rows']:
            projection=row['disposable_pad_projection'];grant=projection['original_grant_fixture'];rows.append({'size':row['size'],'count':grant['count'],'reason':grant['reason'],'step_complete':grant['step_complete'],'owner_present':grant['owner_present'],'typed':sum(x['require_typed_pad']==1 for x in grant['requests']),'constrained_origins_found':sum(x['found'] is True for x in projection['constrained_search'])})
        report={'status':'CollectedUnqualifiedOriginalGrantFixture','source_pins':pins,'result_sha256':sha(OUT/'result.json'),'rows':rows,'scope':'Original service in disposable source EntityManager using fixture read-model rows decoded from retained historical factory footprints, whose full capture source pins are stale. Actual pending owner requests feed original typed search with inactive masks and fixture occupancy. No processor, registration, native input or gameplay readiness.','runtime_admission':False,'mission_ready':False};(OUT/'receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'rows':rows,'mission_ready':False}))
    except Exception as error:
        (OUT / 'failure.json').write_text(json.dumps({'status': 'Failed', 'error': repr(error), 'mission_ready': False}, indent=2) + '\n');raise


if __name__ == '__main__': main()
