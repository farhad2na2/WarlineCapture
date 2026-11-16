"""Compile original helipad resolver against the source host; never admit delivery."""
import argparse
import hashlib
import json
import re
import shutil
import subprocess
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
BUILD=ROOT/'T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json'
OLD=ROOT/'T Logs/SkirmishProduction/s004-explicit-aircraft-request-run-20261105-T1/compile.rsp'
SPAWN=ROOT/'Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs'
GROUND=ROOT/'T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/source/generated/468_SourceProductionGroundWorld.cs'
DEFINITIONS=ROOT/'Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs'
BRIDGE=ROOT/'Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs'


def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def extract(source,name):
    matches=list(re.finditer(r'\b(?:public|private|internal)\s+(?:static\s+)?(?:bool|void)\s+'+re.escape(name)+r'\s*\(',source))
    if len(matches)!=1:raise ValueError('Unique source owner method required: '+name)
    start=matches[0].start();opening=source.index('{',matches[0].end());level=1;i=opening+1
    while level and i<len(source):
        if source[i]=='{':level+=1
        elif source[i]=='}':level-=1
        i+=1
    if level:raise ValueError('Unterminated source method: '+name)
    return source[start:i]


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',required=True);parser.add_argument('--bind-original-matcher',action='store_true');parser.add_argument('--include-slot-resolver',action='store_true');parser.add_argument('--include-align-owner',action='store_true');a=parser.parse_args()
    if a.include_slot_resolver and not a.bind_original_matcher:raise ValueError('Slot resolver requires the original matcher')
    if a.include_align_owner and not a.include_slot_resolver:raise ValueError('Alignment owner requires the original slot resolver')
    output=Path(a.output).absolute();output.mkdir(parents=True,exist_ok=False)
    build=json.loads(BUILD.read_bytes());pins=build['source_and_reference_pins']
    inputs=(BUILD,OLD,SPAWN,GROUND,DEFINITIONS) if a.bind_original_matcher else (BUILD,OLD,SPAWN,GROUND)
    if a.include_align_owner:inputs=(*inputs,BRIDGE)
    for p in ((SPAWN,GROUND,DEFINITIONS,BRIDGE) if a.include_align_owner else (SPAWN,GROUND,DEFINITIONS) if a.bind_original_matcher else (SPAWN,GROUND)):
        if str(p) not in pins or sha(p)!=pins[str(p)]:raise ValueError('Current exact source-build pin required: '+str(p))
    before={str(p):sha(p) for p in inputs}
    source=SPAWN.read_text();names=('TryResolveAvailableFactionHelipadSpawn','TryResolveHelicopterSpawnForFaction',
        'TryResolveHelicopterSpawnForFactionFromReadModel','TryGetRuntimeBuildingSourcePositionFromReadModel',
        'IsOwnedRuntimeBuildingForFaction','TryGetFactionRuntimeBuildingCenter')
    if a.include_slot_resolver:names+=('TryResolveProductionSlotAtCell','TryResolveProductionSlotAtCellFromReadModel','FootprintsOverlap')
    spans={name:extract(source,name) for name in names}
    generated=GROUND.read_text();closing='\n }\n}'
    if not generated.endswith(closing+'\n'):raise ValueError('Exact source ground class boundary required')
    if a.bind_original_matcher:
        original_matcher=extract(DEFINITIONS.read_text(),'RuntimeBuildingMatchesId')
        matcher=original_matcher.replace('public static bool RuntimeBuildingMatchesId','private static bool SourceOriginalBuildingMatchesId',1)
        matcher=matcher.replace('RuntimeDefinitionMatchesId(', 'BuildingDefinitionPrefabSystemHelper.RuntimeDefinitionMatchesId(')
        boundary='new BuildingProductionSlotUtilitySystemHelper(),null,null,(EntityManager manager,out Entity entity)'
        if generated.count(boundary)!=1:raise ValueError('Exact source queue-context boundary required')
        generated=generated.replace(boundary,'new BuildingProductionSlotUtilitySystemHelper(),null,SourceOriginalBuildingMatchesId,(EntityManager manager,out Entity entity)')
    else:matcher=''
    if a.include_slot_resolver:
        old='UnsupportedBranch("override aircraft slot resolver")'
        replacement='TryResolveProductionSlotAtCell(context, em, grid, cell, unitFootprint, out productionSlotBuilding, out productionSlotIndex)'
        if generated.count(old)!=1:raise ValueError('Exact unsupported aircraft override branch required')
        generated=generated.replace(old,replacement)
    projected=[]
    for name,body in spans.items():
        if name=='TryGetFactionRuntimeBuildingCenter':
            body=body.replace('private static bool','private bool',1)
        if name in ('TryResolveProductionSlotAtCell','TryResolveProductionSlotAtCellFromReadModel'):
            body=body.replace('private static bool','internal bool' if name=='TryResolveProductionSlotAtCell' else 'private bool',1)
        body=re.sub(r'\bContext context\b','BuildingProductionQueueCompositionSystemHelper.QueueContext context',body)
        body=body.replace('context.RuntimeBuildings','Buildings')
        body=body.replace('NormalizeSpawnableKey(', 'SpawnableKeyText.Normalize(')
        body=body.replace('IsProductionSlotOccupied(context, em,', 'SourceProductionSpawnReadModels.IsProductionSlotOccupied(context, em,')
        body=body.replace('building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[i])',
                          'SourceProbeProducerPoint(building,building.ProductionSpawnLocalPositions[i])')
        projected.append(body)
    helper='''
    Vector3 SourceProbeProducerPoint(RuntimeBuildingEntity building,Vector3 local) {
        var point=ProducerPoint(building,new float3(local.x,local.y,local.z));
        return new Vector3(point.x,point.y,point.z);
    }
'''
    bridge_spans={}
    if a.include_align_owner:
        bridge=BRIDGE.read_text()
        for name in ('AlignNewestProducedUnitRotation','TryGetNewestProducedUnit','TryGetNewestProducedUnitFromReadModel'):
            bridge_spans[name]=extract(bridge,name)
        for name,body in bridge_spans.items():
            body=re.sub(r'\bContext context\b','BuildingProductionQueueCompositionSystemHelper.QueueContext context',body)
            body=body.replace('context.SpawnContext.TryGetRuntimeBoundaryEntity','context.TryGetRuntimeBoundaryEntity')
            if name=='AlignNewestProducedUnitRotation':
                anchor='if (context.TryGetEntityManager == null || !context.TryGetEntityManager(out EntityManager em))\n                return;'
                if body.count(anchor)!=1:raise ValueError('Exact original entity-manager bridge required')
                body=body.replace(anchor,'EntityManager em = Em;')
                body=body.replace('public void AlignNewestProducedUnitRotation','internal void SourceAlignNewestProducedUnitRotation',1)
            projected.append(body)
    text=generated[:-len(closing+'\n')]+helper+'\n'+matcher+'\n'+'\n'.join(projected)+closing+'\n'
    text='using UnityEngine;\n'+text
    output_source=output/'SourceAirSelfHelipadResolverProbe.cs';output_source.write_text(text)
    args=[json.loads(x) for x in OLD.read_text().splitlines()]
    matches=[i for i,x in enumerate(args) if x==str(GROUND)]
    if len(matches)!=1:raise ValueError('Exact generated source owner required')
    args[matches[0]]=str(output_source)
    args=[('-out:'+str(output/'probe.exe')) if x.startswith('-out:') else x for x in args]
    rsp=output/'compile.rsp';rsp.write_text('\n'.join(json.dumps(x) for x in args)+'\n')
    csc=next(x for x in pins if x.endswith('/Roslyn/bincore/csc.dll'))
    result=subprocess.run([shutil.which('dotnet'),csc,'@'+str(rsp)],cwd=ROOT,capture_output=True,text=True,timeout=180)
    (output/'compile.log').write_text(result.stdout+result.stderr)
    after={str(p):sha(p) for p in inputs}
    receipt={'status':'PassedCompileOnly' if result.returncode==0 and before==after else 'FailedCompileProbe',
        'compile_exit':result.returncode,'input_sha256':before,'inputs_unchanged':before==after,
        'original_methods_sha256':{**{name:hashlib.sha256(body.encode()).hexdigest() for name,body in spans.items()},
            **({'RuntimeBuildingMatchesId':hashlib.sha256(original_matcher.encode()).hexdigest()} if a.bind_original_matcher else {}),
            **{name:hashlib.sha256(body.encode()).hexdigest() for name,body in bridge_spans.items()}},
        'projection_sha256':sha(output_source),'original_matcher_bound':a.bind_original_matcher,'original_slot_resolver_included':a.include_slot_resolver,'original_alignment_owner_included':a.include_align_owner,'scope':'OriginalSourceOwnerProjection_CompileOnly_NoWorldOrDelivery',
        'mission_ready':False}
    (output/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps({'status':receipt['status'],'compile_exit':result.returncode}))
    if result.returncode:raise SystemExit(1)


if __name__=='__main__':main()
