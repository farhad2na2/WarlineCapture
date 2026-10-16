"""Check collection completeness/provenance only; preserve all unqualified fit failures."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re

from skirmish_ground_objective30_contract import validate
from s005_windows_source_snapshot import candidate_hash

ROOT=Path(__file__).resolve().parents[2]
MAP='Tools/CI/SkirmishProductionDrafts/Ground30Subset58/manifest.json'
MAP_SHA='e16d04d462d9a4785cfe3b8e653b8f7ee2e845069381baeea12482f741f8cf3e'
INPUT='Tools/CI/SkirmishProductionDrafts/GroundObjective30/ground-objective-thirty.authoring.json'
INPUT_SHA='95a9c7b25b9e4876465c7c7d41111605f6054e65ba5c6a3f37df646678ff10dc'
METHOD='Game.Editor.SkirmishGroundSourceFitSurvey.RunGroundObjective30QualificationCollection'
MARKER='[SkirmishGroundObjective30Collection] result=Collected missions=30 sizeRows=90 physicalSources=4 sourceFitCertified=0 output='
PREPARATION='[SkirmishProductionAuthoringPreparation] result=Passed preview=21 pilotSchema=6 pilotNegativeControls=12 groundAuthoring=30 groundNegativeControls=60 budget=5 collector=3 sourceFitCertified=0 normalMissionReadiness=Pending'
PROFILE_FREE_PREPARATION='[SkirmishGroundObjective30Preparation] result=Passed candidates=30 negativeControls=60 budget=5 collector=3 sourceFitCertified=0 normalMissionReadiness=Pending'
PIN_KEYS=('sourcePath','sourceAssetSha256','gridPath','gridAssetSha256','surfacePath','surfaceAssetSha256','operationMapId','sourceIdentityHash','contentHash','sourceSceneGuid','authoredSubSceneGuid')
SIZES={'Standard','War','LargeWar'}
WORKFLOW_TOOLS=('Tools/CI/invoke_skirmish_ground_objective30_collection.py',
       'Tools/CI/qualify_skirmish_ground_objective30_collection.py',
       'Tools/CI/skirmish_ground_objective30_contract.py','Tools/CI/build_skirmish_authoring_batches.py','Tools/CI/s005_windows_source_snapshot.py',
       'Tools/CI/InvokeUnityExecuteMethodValidation.ps1','Tools/CI/InvokeUnity.ps1','Tools/CI/ResolveUnityEditor.ps1')

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def load(path):return json.loads(Path(path).read_text(encoding='utf-8-sig'))

def preparation_scope(text):
    variants={'BroadProductionAuthoring':PREPARATION,'ProfileFreeGround30':PROFILE_FREE_PREPARATION}
    counts={key:text.count(marker) for key,marker in variants.items()}
    if sum(counts.values())!=1 or re.findall(r'\[UnityInvoke\] ExitCode: (-?\d+)',text)!=['0'] or re.search(r'Crash!!!|result=Failed|suiteFailed=|error CS\d+:|accepting explicit success marker',text):
        raise ValueError('One exact checked broad or profile-free G30 preparation marker and actual zero required')
    return next(key for key,count in counts.items() if count==1)

def check_snapshot(value):
    sources=value['source_manifest'];build=value['build_manifest']
    for rows in (sources,build):
        if not rows or len({r['path'] for r in rows})!=len(rows):raise ValueError('Empty or duplicate complete manifest')
        for row in rows:
            if set(row)!={'path','sha256'} or not re.fullmatch('[0-9a-f]{64}',row['sha256']):raise ValueError('Malformed complete manifest row')
    if candidate_hash(sources)!=value['candidate_sha256']:raise ValueError('Source census digest differs')
    digest=hashlib.sha256(json.dumps(build,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    if digest!=value['build_sha256']:raise ValueError('DLL census digest differs')

def qualify(raw,inputs,installed_map,before,after,receipt,log,raw_path):
    errors=[];summaries=[]
    def need(test,message):
        if not test:errors.append(message)
    need(receipt.get('scope')=='GroundObjective30StaticCollection' and receipt.get('method')==METHOD,'Checked collection receipt scope/method differs')
    need(type(receipt.get('wrapper_exit')) is int and receipt['wrapper_exit']==0,'Actual checked child wrapper zero required')
    need(re.findall(r'\[UnityInvoke\] ExitCode: (-?\d+)',log)==['0'],'Actual owned Unity zero missing or ambiguous')
    need(not re.search(r'Crash!!!|result=Failed|suiteFailed=|error CS\d+:|accepting explicit success marker',log),'Native failure preserved')
    lines=[line[len(MARKER):].strip() for line in log.splitlines() if line.startswith(MARKER)]
    need(len(lines)==1 and Path(lines[0]).resolve()==Path(raw_path).resolve(),'Unique native collection marker/output join missing')
    need(receipt.get('input_sha256')==INPUT_SHA and receipt.get('import_map_sha256')==MAP_SHA,'Receipt input/import map join differs')
    need(receipt.get('preparation_checked_zero') is True,'Checked prerequisite evidence absent')
    need('launcher_error' not in receipt and 'after_capture_error' not in receipt and receipt.get('tool_bytes_unchanged') is True,'Launcher/endpoint/tool mutation failure preserved')
    try:check_snapshot(before);check_snapshot(after)
    except (KeyError,ValueError,TypeError):need(False,'Malformed complete source/DLL endpoint')
    need(before==after,'Full source/DLL endpoint bytes differ')
    source_rows={row['path']:row['sha256'] for row in before.get('source_manifest',[])}
    need(all(source_rows.get(row['target'])==row['sha256'] for row in installed_map['files']),'Installed seven-target source pins differ from complete census')
    expected={mission['catalog_id']:mission for mission in inputs['missions']}
    need(raw.get('schema')==1 and raw.get('scope')=='StaticAuthoringSourceQueries_NotCompiledMissionQualification','Raw collection schema/scope differs')
    need(raw.get('inputSha256')==INPUT_SHA and raw.get('sourceFitCertified') is False and raw.get('playerReady') is False,'Raw input identity or unqualified boundary differs')
    need(raw.get('result')=='CollectedUnqualified' and type(raw.get('collectionErrors')) is int and raw['collectionErrors']==0,'Native collection errors retained')
    need(raw.get('sourceWorldsDisposed')==4 and 'cleanupError' not in raw,'Actual four-source disposal/census check did not complete')
    sources=raw.get('sources',[]);need(isinstance(sources,list) and len(sources)==4,'Exactly four physical source result groups required')
    seen=[];source_paths=[]
    for source in sources:
        path=source.get('sourcePath');source_paths.append(path)
        need('sourceError' not in source,'Native source infrastructure failure: '+str(path))
        for mission in source.get('missions',[]):
            ident=mission.get('catalogId');seen.append(ident);original=expected.get(ident)
            need(original is not None,'Unexpected mission row: '+str(ident))
            if original is None:continue
            pin=original['source_candidate'];observed=source.get('sourcePins',{})
            need(path==pin['sourcePath'] and all(observed.get(k)==pin[k] for k in PIN_KEYS),'Mission/physical source pin splice: '+ident)
            need(mission.get('canonicalRowsSha256')==original['canonical_size_rows_sha256'],'Canonical budget rows changed: '+ident)
            need('error' not in mission and mission.get('sourceFitCertified') is False,'Native candidate failure or readiness drift: '+ident)
            sizes=mission.get('sizes',[])
            need(len(sizes)==3 and Counter(row.get('size') for row in sizes)==Counter(SIZES),'Exact three unique size rows required: '+ident)
            details=[]
            for row in sizes:
                need('error' not in row and row.get('sourceFitCertified') is False,'Native size collection failure: '+ident+'/'+str(row.get('size')))
                need(type(row.get('normalCompilerPassed')) is bool and isinstance(row.get('normalCompileReasons'),list),'Actual normal compiler outcome/reasons missing: '+ident)
                need(type(row.get('staticBudgetCanonicalQuantitiesExact')) is bool and isinstance(row.get('staticBudgetErrors'),list),'Typed canonical budget outcome missing: '+ident)
                packing=row.get('nativeForcePackingSourceOnly');facilities=row.get('allSizeFacilities')
                need(isinstance(packing,list) and isinstance(facilities,list),'Native force/facility raw rows missing: '+ident)
                complete=row.get('completeCanonicalFacilityConfigurationMeasured')
                relocated=row.get('normalFacilityRelocationProposals',[])
                need(type(complete) is bool,'Complete-facility outcome absent: '+ident)
                if complete:
                    need(row.get('staticBudgetCanonicalQuantitiesExact') is True and isinstance(facilities,list) and len(facilities)>0 and
                         len(relocated)==len(facilities) and all(v.get('entireConfigurationFound') is True for v in relocated),
                         'Partial/noncanonical facility set presented as complete: '+ident)
                else:
                    need(row.get('nativeForcePackingWithCompleteFacilities')==[] and not row.get('nativeRouteQueriesWithCompleteFacilities',{}).get('queries'),
                         'Incomplete facilities used as complete packing/path evidence: '+ident)
                queries=row.get('nativeRouteQueriesWithCompleteFacilities',{}).get('queries',[])
                details.append(dict(size=row.get('size'),normal_compiler_passed=row.get('normalCompilerPassed'),
                    normal_compile_reasons=row.get('normalCompileReasons'),static_budget_errors=row.get('staticBudgetErrors'),
                    static_budget_canonical_quantities_exact=row.get('staticBudgetCanonicalQuantitiesExact'),
                    source_only_force_statuses=dict(Counter(v.get('status','MissingStatus') for v in packing or [])),
                    facility_metadata_statuses=dict(Counter(v.get('status','MissingStatus') for v in facilities or [])),
                    complete_canonical_facilities=row.get('completeCanonicalFacilityConfigurationMeasured'),
                    normal_facility_status=row.get('normalFacilityRelocationStatus'),
                    relocated_facility_statuses=dict(Counter(v.get('status','MissingStatus') for v in row.get('normalFacilityRelocationProposals',[]))),
                    complete_facility_force_statuses=dict(Counter(v.get('status','MissingStatus') for v in row.get('nativeForcePackingWithCompleteFacilities',[]))),
                    native_route_statuses=dict(Counter(v.get('status','MissingStatus') for v in queries)),
                    native_route_failure_codes=dict(Counter(str(v.get('failureCode','Missing')) for v in queries if v.get('status')!='NativeQueryFoundExactGoal')),
                    fit_certified=False))
            summaries.append(dict(catalog_id=ident,source_path=path,size_rows=details,fit_certified=False))
    need(Counter(seen)==Counter(expected.keys()),'Missing, duplicate or spliced mission results; all thirty required')
    need(len(source_paths)==4 and len(set(source_paths))==4,'Duplicate physical source result group')
    need(sum(len(m['size_rows']) for m in summaries)==90,'All ninety actual size rows required')
    rejected=sum(d['normal_compiler_passed'] is False for m in summaries for d in m['size_rows'])
    need(raw.get('normalCompilerRejectedRows')==rejected,'Native normal-compiler rejection count differs')
    return dict(result='CollectedUnqualified' if not errors else 'FailedCollectionEvidence',collection_complete=not errors,
        errors=errors,mission_rows=len(seen),size_rows=sum(len(m['size_rows']) for m in summaries),normal_compiler_rejected_rows=rejected,
        summaries=summaries,qualified=False,source_fit_certified=False,mission_readiness=0,
        source_guard_scope='External complete source/DLL equality and installed exact source pins; this static entry emits no Launch/Finish native hash',
        cleanup_scope='Pinned native code verified World/default reference/scene/preview census; raw per-object census is not exported',
        pending=['Complete static and live source fit','Three independent legal approaches','Measured typed objective binding','Per-source infantry/aircraft camera evidence','Normal mission/result/return','Player/device acceptance'])

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for name in ('raw','receipt','log','before','after','preparation-log','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();result=None
    try:
        if sha(ROOT/MAP)!=MAP_SHA or sha(ROOT/INPUT)!=INPUT_SHA:raise ValueError('Reviewed import map/input bytes changed')
        inputs=load(ROOT/INPUT);validate(inputs,ROOT);receipt=load(args.receipt)
        log=args.log.read_text(encoding='utf-8-sig',errors='replace');prep=args.preparation_log.read_text(encoding='utf-8-sig',errors='replace')
        if receipt.get('preparation_sha256')!=sha(args.preparation_log):raise ValueError('Actual checked preparation hash differs')
        preparation_scope(prep)
        for field,path in (('raw_sha256',args.raw),('log_sha256',args.log),('before_sha256',args.before),('after_sha256',args.after)):
            if receipt.get(field)!=sha(path):raise ValueError('Receipt artifact hash differs: '+field)
        child_exit=args.receipt.parent/'checked-child-exit.txt'
        if child_exit.read_text(encoding='utf-8-sig').strip()!='0' or receipt.get('checked_child_exit_sha256')!=sha(child_exit):raise ValueError('Actual checked child exit file/hash differs')
        pins=receipt.get('tool_pins',[])
        if Counter(v['path'] for v in pins)!=Counter(WORKFLOW_TOOLS) or any(sha(ROOT/v['path'])!=v['sha256'] for v in pins):raise ValueError('Workflow tools missing, duplicated or changed')
        result=qualify(load(args.raw),inputs,load(ROOT/MAP),load(args.before),load(args.after),receipt,log,args.raw)
        result['provenance']={key:dict(path=str(path.resolve()),sha256=sha(path)) for key,path in vars(args).items() if key!='output'}
    except (OSError,KeyError,ValueError,TypeError,AttributeError,IndexError) as error:
        result=dict(result='FailedCollectionEvidence',collection_complete=False,errors=[str(error)],qualified=False,source_fit_certified=False,mission_readiness=0)
    with args.output.open('x',encoding='utf-8') as stream:json.dump(result,stream,indent=2);stream.write('\n')
    print(json.dumps({key:result[key] for key in ('result','collection_complete','errors','qualified')},indent=2))
    return 0 if result['collection_complete'] else 1

if __name__=='__main__':raise SystemExit(main())
