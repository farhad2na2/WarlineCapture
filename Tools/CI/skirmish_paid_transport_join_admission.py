"""Read-only recorded S005 Queue -> producer -> Deliver consistency, not gameplay credit.

Pure validate() accepts constructed data and proves no native origin. inspect()
adds the existing immutable Reader barrier and literal local producer pins.
This draft neither runs Unity nor changes a job, claim, journal or runtime.
"""
import argparse
import copy
import datetime
import hashlib
import json
from pathlib import Path
import re

from skirmish_construction_ledger_admission import (
    Reader, need, integer, string, exact, sha, parse, BANK, ENVELOPE,
    I32, U32, MAX_BYTES, MAX_ROWS, MAX_SOURCE_FILES, fingerprint,
    candidate_hash, verify_job)

MAX_LINEAGE_BYTES = 512*1024*1024
MAX_LINEAGE_ROWS = 200000
MAX_LINEAGE_LINE_BYTES = 262144

SCOPE = 'ProductionCommittedOriginsPartial'
UNCOVERED = ['Construction', 'Research', 'Repair', 'FabricationExchangeReward', 'PhysicalFuelHistory']
POINT = set('faction bank_index bank_version materials fuel bank reservation'.split())
RESERVATION = set('id role faction phase producer paid members delivered remaining supply'.split())
DECISION = set('materials fuel refund accepted reason reservation'.split())
ACTOR = set('index version session stable_id faction reservation shared_actor is_structure capacity_released role source_key'.split())
TRANSACTION = set('ledger_schema scope ledger_sequence operation member_ordinal native_world_sequence native_session_id native_setup_identity native_config_sha256 before after decision actor paid_resources_qualified'.split())
OPS = set('Queue BindProducer StartNativeQueue Deliver DirectDebit DirectDispatch DirectMember StartQueued QueuedDirectDispatch Cancel FailedDispatchRefund ProducerLost DeathRelease'.split())
PIN_SCOPE = 'WindowsReviewedTypedProducerAndLineageContractOnly_NoNativeAuthentication'
POLICY_PINS = {
    'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.NormalPaidPlaneLineageCanary.cs': '1679fdb1289e18bb081d245260c15ce770228c809fb8e3a4f6559c9039e9ab40',
    'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.ProductionCanary.cs': '373a29aa058fc275e4c33f8eb8077963b63f1e96b3389a5e526e8289b0fa63a2',
    'Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionService.cs': '1931e5073ecbbcfdd18e82ddae70bf6aa609f99733ba54aa84572c3411a7877f',
    'Assets/Game/Scripts/Runtime/Skirmish/SkirmishProductionService.Native.cs': 'd46c0eb8fc3eb9436d0588b1020f856422eb9bd31b76e9e56493578fc79273e4',
    'Assets/Game/Scripts/Systems/BuildingProductionUpdateCompositionSystemHelper.cs': 'b520961e2139c99c5925895f436bfefc0254b0068571ddb9464349f1b9eb659c',
    'Assets/Game/Scripts/Runtime/Skirmish/SkirmishMaterialsService.cs': 'a7da50462d1390425cb4406d0a715ba22ff42a3a15831d2511403d3348938c20',
    'Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs': '10c2827cf812bd9ed6f77b61fafd632e2725e3df51d14ef49d0d139d596bbbf0',
}


def point(p):
    exact(p, POINT)
    integer(p['faction'], 1, 2)
    integer(p['bank_index']); integer(p['bank_version'], 1)
    integer(p['materials']); integer(p['fuel'])
    b = p['bank']; exact(b, BANK)
    for k, v in b.items(): integer(v, 0, U32 if k == 'Version' else I32)
    need(b['FactionId'] == p['faction'] and b['Current'] == p['materials'] <= b['Capacity'], 'Bank projection/faction differs')
    r = p['reservation']
    if r is not None:
        exact(r, RESERVATION)
        for k in ('faction', 'producer', 'paid', 'members', 'delivered', 'remaining', 'supply'): integer(r[k])
        integer(r['id'], 1, U32); integer(r['faction'], 1, 2)
        string(r['role'], 64); need(r['phase'] in {'Reserved','Producing','Live','Released'}, 'Unknown reservation phase')
        need(r['faction'] == p['faction'] and 0 <= r['delivered'] <= r['members'] and 0 <= r['remaining'] <= r['members'], 'Reservation faction/counts differ')
    return p


def production_debit(bank, amount):
    need(bank['Current'] >= amount > 0, 'Unaffordable committed debit')
    b = copy.deepcopy(bank); b['Current'] -= amount
    for k in ('LifetimeSpent', 'LifetimeProductionSpent'): b[k] = min(I32, b[k] + amount)
    b['Version'] = 1 if b['Version'] == U32 else b['Version'] + 1
    return b


def select_lineage(rows):
    """Select required rows while checking every parsed row for explicit failure."""
    selected = {}
    for row in rows:
        need(isinstance(row, dict), 'Lineage object required')
        need(row.get('stage') != 'Failure', 'Recorded lineage failure')
        if row.get('stage') in {'PaidAirportHandoff', 'PaidPlaneDeliveredAtHome'}:
            need(row['stage'] not in selected, 'Duplicate lineage boundary')
            selected[row['stage']] = row
    need(set(selected) == {'PaidAirportHandoff','PaidPlaneDeliveredAtHome'}, 'Required paid Airport/Plane boundaries absent')
    return selected


def _lineage_rows(reader, path):
    """Bound every streamed byte, including one optional initial UTF-8 BOM."""
    checked=reader.path(path,MAX_LINEAGE_BYTES)
    with checked.open('rb') as stream:
        count=0; total=0
        while True:
            line=stream.readline(MAX_LINEAGE_LINE_BYTES+1)
            if not line: break
            total+=len(line); count+=1
            need(total<=MAX_LINEAGE_BYTES,'Lineage running byte budget exceeded')
            need(count<=MAX_LINEAGE_ROWS and line.endswith(b'\n') and
                 len(line)<=MAX_LINEAGE_LINE_BYTES,'Bounded complete lineage rows required')
            if count==1 and line.startswith(b'\xef\xbb\xbf'): line=line[3:]
            yield parse(line.decode('utf-8'))


def validate(job, claim, events, lineage):
    answer = dict(result='MissingOrFailedAdmission', recorded_join_consistent=False,
        qualified=False, paid_resources_qualified=False, normal_mission_ready=False,
        queue_success_enabled=False, whole_attempt_conservation=False, normal_720_credit=0,
        player_device_accepted=False, source_bytes_verified=False, build_bytes_verified=False,
        native_capture_authenticated=False, source_manifest_completeness_measured=False,
        native_compiler_input_consumption_closure='Pending',
        aircraft_fuel_spend_attributed=False, fuel_scope='FactionPooledFuel',
        scope='RecordedTransportQueueToDeliveryOnly', policy_scope=PIN_SCOPE)
    try:
        for k in ('candidate_sha256','build_sha256','setup_sha256','content_sha256'): sha(job[k])
        need(all(job.get(k) == v and type(job.get(k)) is type(v) for k,v in
            dict(catalog_id='S005', definition_id='skirmish.s005', seed=155926, locale='EN', size='Standard', difficulty='Regular', control_mode='manual').items()), 'Exact scoped manual S005 subject required')
        digest = fingerprint(job)
        need(claim['job_sha256'] == digest, 'Claim/job mismatch')
        string(claim['worker'],80); need(re.fullmatch('attempt-[a-f0-9]{32}',claim['attempt_id']), 'Invalid attempt')
        need(isinstance(events,list) and 0 < len(events) <= MAX_ROWS, 'Bounded native stream required')
        for n,row in enumerate(events,1):
            exact(row,ENVELOPE); integer(row['schema_version'],1,1); integer(row['sequence'],n,n); integer(row['frame'])
            need(row['job_sha256']==digest and row['attempt_id']==claim['attempt_id'] and row['worker']==claim['worker'], 'Envelope ownership/order')
            string(row['utc'],64); need(datetime.datetime.fromisoformat(row['utc'].replace('Z','+00:00')).tzinfo is not None, 'Timestamp timezone')
            string(row['kind'],128); need(isinstance(row['facts'],dict),'Facts object required')
        def one(kind):
            rows=[(i,row) for i,row in enumerate(events) if row['kind']==kind]
            need(len(rows)==1,'Exactly one '+kind+' required'); return rows[0]
        ci,context=one('ContextCaptured'); si,start=one('NativeProductionLedgerStarted')
        mi,menu=one('NormalMenuLaunchObserved'); ei,end=one('NativeProductionLedgerClosed')
        ri,_=one('RestoreStarting'); fi,_=one('ContextRestored')
        need(ci==0 and ci<si<mi<ei<ri<fi==len(events)-1,'Incomplete production lifecycle/order')
        need(context['facts']['source_sha256']==job['candidate_sha256'] and context['facts']['compiled_dll_manifest_sha256']==job['build_sha256'],'Candidate/build splice')
        subject=menu['facts']; verify_job(job,subject['native_setup_identity']); sha(subject['native_config_sha256'])
        session=string(subject['native_session_id'],128); world=string(subject['native_world_sequence'],20)
        need(re.fullmatch('[0-9]+',world) and 0<int(world)<=18446744073709551615,'World sequence')
        for row in (start,end):
            f=row['facts']; integer(f['ledger_schema'],1,1)
            need(f['scope']==SCOPE and f['paid_resources_qualified'] is False,'Unsupported production scope')
        exact(start['facts'],{'ledger_schema','scope','paid_resources_qualified'})
        close=end['facts']; exact(close,{'ledger_schema','scope','transactions','capture_failed','observer_faults','sink_owned','paid_resources_qualified','uncovered_operations'})
        integer(close['transactions'],1,4096); integer(close['observer_faults'],0,0)
        need(close['capture_failed'] is False and close['sink_owned'] is True and close['uncovered_operations']==UNCOVERED,'Failed/unowned/changed partial capture')
        need(not any('Production' in r['kind'] and r['kind'] not in {'NativeProductionLedgerStarted','NativeProductionTransaction','NativeProductionLedgerClosed'} for r in events),'Production failure/unknown event')
        selected=select_lineage(lineage); airport=selected['PaidAirportHandoff']; delivered=selected['PaidPlaneDeliveredAtHome']
        for row in (airport,delivered):
            need(row['attempt']==session and row['catalog']=='S005' and row['definition']=='skirmish.s005' and type(row['seed']) is int and row['seed']==155926,'Lineage subject splice')
            integer(row['frame'],1); need(type(row['timeScale']) in (int,float) and row['timeScale']==1 and row['normalBudget'] is True and row['fullMissionCertified'] is False,'Lineage speed/budget/scope')
            for k in ('unexpected','interventions','violations'): integer(row[k],0,0)
        need(airport['frame'] < delivered['frame'],'Delivery precedes Airport handoff')
        for k in ('producerRuntimeId','airportIndex','airportVersion'): integer(airport[k],1)
        need(airport['airportStable']=='constructed.'+str(airport['producerRuntimeId']) and airport['normalMenu'] is True,'Paid Airport identity')
        target=integer(delivered['reservation'],1,U32)
        need(delivered['paid']==440 and type(delivered['paid']) is int and delivered['role']=='TransportPlane' and delivered['phase']=='Live','Delivered paid Plane differs')
        need(delivered['members']==delivered['delivered']==delivered['remaining']==1 and all(type(delivered[k]) is int for k in ('members','delivered','remaining')),'Single live delivered member required')
        need(delivered['producerRuntimeId']==airport['producerRuntimeId'] and (delivered['airportIndex'],delivered['airportVersion'],delivered['airportStable'])==(airport['airportIndex'],airport['airportVersion'],airport['airportStable']),'Airport producer/actor rebound')
        for k in ('producerRuntimeId','airportIndex','airportVersion','actorHp'): integer(delivered[k],1)
        integer(delivered['actorIndex'],1); integer(delivered['actorVersion'],1)
        need(delivered['stable']=='production.'+str(target)+'.0' and delivered['actorNativeShared'] is True and delivered['actorHp']>0,'Delivered actor identity/liveness')
        rows=[(i,r) for i,r in enumerate(events) if r['kind']=='NativeProductionTransaction']
        need(len(rows)==close['transactions'],'Transaction count/truncation')
        target_rows=[]; banks={}; bank_indices={}; actor_owners={}
        for n,(i,row) in enumerate(rows,1):
            f=row['facts']; exact(f,TRANSACTION); integer(f['ledger_schema'],1,1); integer(f['ledger_sequence'],n,n); integer(f['member_ordinal'],-1,I32)
            need(mi<i<ei and f['scope']==SCOPE and f['paid_resources_qualified'] is False and f['operation'] in OPS,'Transaction scope/window/operation')
            need(all(f[k]==subject[k] for k in ('native_session_id','native_world_sequence','native_config_sha256','native_setup_identity')),'Native subject splice')
            verify_job(job,f['native_setup_identity'])
            a=point(f['before']); b=point(f['after'])
            need((a['faction'],a['bank_index'],a['bank_version'])==(b['faction'],b['bank_index'],b['bank_version']),'Boundary bank owner/generation changed')
            identity=(a['bank_index'],a['bank_version']); faction=a['faction']
            need(faction not in banks or banks[faction]==identity,'Faction bank generation rebound'); banks[faction]=identity
            need(len(set(banks.values()))==len(banks),'Foreign factions share a bank')
            need(identity[0] not in bank_indices or bank_indices[identity[0]]==(faction,identity[1]),'Bank entity index rebound across factions/generations')
            bank_indices[identity[0]]=(faction,identity[1])
            if f['decision'] is not None:
                exact(f['decision'],DECISION)
                need(type(f['decision']['accepted']) is bool,'Decision accepted must be boolean')
                string(f['decision']['reason'],128)
                for k in ('materials','fuel','refund','reservation'): integer(f['decision'][k],0,U32 if k=='reservation' else I32)
            if f['actor'] is not None:
                actor=f['actor']; exact(actor,ACTOR)
                integer(actor['index']); integer(actor['version'],1); integer(actor['reservation'],1,U32)
                integer(actor['faction'],1,2); integer(actor['is_structure'],0,1); integer(actor['capacity_released'],0,1)
                need(type(actor['shared_actor']) is bool and actor['session']==session and actor['faction']==faction,'Actor subject/type differs')
                string(actor['stable_id'],64); string(actor['role'],64); string(actor['source_key'],64)
                generation=(actor['index'],actor['version'])
                ownership=(actor['session'],actor['faction'],actor['reservation'],actor['stable_id'])
                need(generation not in actor_owners or actor_owners[generation]==ownership,
                     'Actor entity generation ownership rebound across production rows')
                actor_owners[generation]=ownership
                receipts=[p['reservation'] for p in (a,b) if p['reservation'] is not None]
                need(receipts and all(actor['reservation']==r['id'] and actor['role']==r['role'] for r in receipts),'Actor reservation/role splice')
            if any(p['reservation'] is not None and p['reservation']['id']==target and p['faction']==1 for p in (a,b)):
                target_rows.append(row)
        # Admit only the committed acquisition span. Later lifecycle rows are
        # preserved and explicitly excluded, rather than converted into a win.
        ops=[r['facts']['operation'] for r in target_rows]
        need(ops[:4]==['Queue','BindProducer','StartNativeQueue','Deliver'],'Exact queued production/deployment chain absent or reordered')
        need(all(op in {'DeathRelease','ProducerLost'} for op in ops[4:]),'Target acquisition replay/rebinding after delivery')
        queue,bind,start_row,delivery=target_rows[:4]
        need(all(airport['frame']<=r['frame']<=delivered['frame'] for r in target_rows[:4]),'Committed chain outside lineage frame span')
        need(all(r['facts']['member_ordinal']==-1 for r in target_rows[:4]),'Queued boundary ordinal differs from typed producer')
        need(all(b['frame']>=a['frame'] for a,b in zip(target_rows[:3],target_rows[1:4])),'Target native frame order')
        q=queue['facts']; a=q['before']; b=q['after']; decision=q['decision']
        exact(decision,DECISION); need(decision['accepted'] is True and decision['reason']=='None','Rejected/unknown Queue decision')
        integer(decision['materials'],440,440); integer(decision['fuel']); integer(decision['refund'],0,0); integer(decision['reservation'],target,target)
        need(a['reservation'] is None and q['actor'] is None,'Queue is not new or already claims an actor')
        receipt=b['reservation']; need(receipt is not None,'Queued reservation absent')
        need(receipt['id']==target and receipt['role']=='TransportPlane' and receipt['faction']==1 and receipt['phase']=='Reserved' and receipt['producer']==0 and receipt['paid']==440 and receipt['members']==receipt['remaining']==1 and receipt['delivered']==0,'Queued Plane440 receipt differs')
        need(b['bank']==production_debit(a['bank'],440) and a['materials']-b['materials']==440 and a['fuel']-b['fuel']==decision['fuel'],'Exact committed Materials/Fuel payment absent')
        previous=copy.deepcopy(receipt)
        for row in (bind,start_row,delivery):
            f=row['facts']; a=f['before']; b=f['after']; need(a['reservation']==previous,'Target reservation continuity/rebinding')
            need(a['bank']==b['bank'] and a['materials']==b['materials'] and a['fuel']==b['fuel'],'Non-debit stage mutates resources')
            need(f['decision'] is None,'Unexpected stage decision')
            expected=copy.deepcopy(previous)
            if f['operation']=='BindProducer': expected['producer']=airport['producerRuntimeId']
            elif f['operation']=='StartNativeQueue': expected['phase']='Producing'
            else: expected.update(phase='Live',delivered=1)
            need(b['reservation']==expected,'Native production phase/producer/delivery differs'); previous=expected
            if f['operation']!='Deliver': need(f['actor'] is None,'Actor appears before native delivery')
        actor=delivery['facts']['actor']; exact(actor,ACTOR)
        for k in ('index','version','faction','is_structure','capacity_released'): integer(actor[k])
        integer(actor['reservation'],1,U32)
        need((actor['index'],actor['version'],actor['stable_id'])==(delivered['actorIndex'],delivered['actorVersion'],delivered['stable']),'Native delivered entity generation/stable identity differs')
        need(actor['session']==session and actor['reservation']==target and actor['faction']==1 and actor['shared_actor'] is True and actor['is_structure']==actor['capacity_released']==0 and actor['role']=='TransportPlane' and actor['source_key']=='Unit_Veh_Plane_Transport','Native delivered ownership/role/default source differs')
        answer.update(result='ConsistentRecordedTransportQueueToDelivery',recorded_join_consistent=True,
            reservation=target,actor=[actor['index'],actor['version']],producer=airport['producerRuntimeId'],
            committed_materials=440,observed_post_delivery_rows=len(target_rows)-4,
            unadmitted_post_delivery_operations=ops[4:],unadmitted_non_target_production_rows=len(rows)-len(target_rows),
            pending=['Actual native wrapper/provenance and normal-input/camera qualification','Airport construction admission','Non-target and post-delivery production lifecycle','Physical Fuel history and whole-attempt conservation','Victory/result/return and manual/ARIA/device acceptance'])
    except (KeyError,ValueError,TypeError,OverflowError) as error:
        answer['error']=type(error).__name__+': '+str(error)
    return answer


def inspect(project, descriptor):
    reader=Reader(project); p=reader.path(descriptor)
    with p.open('rb') as stream: data=stream.read(MAX_BYTES+1)
    need(0<len(data)<=MAX_BYTES,'Bounded descriptor required')
    _,d=reader.obj({'path':str(p),'sha256':hashlib.sha256(data).hexdigest()})
    exact(d,{'schema_version','job','claim','immutable_claim','native_events','candidate_manifest','lineage'}); integer(d['schema_version'],1,1)
    jp,job=reader.obj(d['job']); cp,claim=reader.obj(d['claim']); ip,immutable=reader.obj(d['immutable_claim'])
    exact(claim,{'attempt_id','token','worker','started_utc','job_sha256','artifact_directory'})
    need(isinstance(claim['token'],str) and re.fullmatch('[a-f0-9]{32}',claim['token']),'Invalid claim ownership token')
    string(claim['started_utc'],64); need(datetime.datetime.fromisoformat(claim['started_utc'].replace('Z','+00:00')).tzinfo is not None,'Claim timestamp timezone')
    need(claim==immutable,'Immutable claim differs'); folder=Path(claim['artifact_directory'])
    need(folder.is_absolute() and reader.root in folder.parents and folder.name==claim['attempt_id'] and folder.parent==jp.parent and jp.name=='job.json' and jp.parent.name==job['job_id'] and cp==jp.parent/'claim.json' and ip==folder/'claim.json','Canonical owned job/claim/attempt differs')
    need(reader.path(d['native_events']['path'])==folder/'adapter-native-events.jsonl','Journal outside owned attempt')
    need(reader.path(d['lineage']['path'],MAX_LINEAGE_BYTES)==folder/'paid-plane.jsonl','Lineage outside owned attempt')
    _,store=reader.obj(d['candidate_manifest']); exact(store,{'source_manifest','candidate_sha256','build_manifest','build_sha256'})
    need(store['source_manifest']==job['source_manifest'] and store['candidate_sha256']==job['candidate_sha256'] and store['build_sha256']==job['build_sha256'],'Candidate/build splice')
    pins=job['source_manifest']; need(isinstance(pins,list) and 0<len(pins)<=MAX_SOURCE_FILES,'Bounded source manifest')
    seen={}
    for ref in pins:
        exact(ref,{'path','sha256'}); need(isinstance(ref['path'],str) and not Path(ref['path']).is_absolute() and '\\' not in ref['path'] and ref['path'] not in seen,'Unique relative source path required')
        reader.verify(ref); seen[ref['path']]=ref['sha256']
    need(all(seen.get(p)==h for p,h in POLICY_PINS.items()),'Reviewed local typed-producer pins differ; Windows compatibility review required')
    need(candidate_hash(pins)==job['candidate_sha256'],'Candidate identity does not derive from source')
    builds=store['build_manifest']; need(isinstance(builds,list) and 0<len(builds)<=1024 and fingerprint(builds)==job['build_sha256'],'Compiled identity differs')
    names=[]
    for ref in builds:
        exact(ref,{'path','sha256'}); need(isinstance(ref['path'],str) and re.fullmatch(r'Library/ScriptAssemblies/[^/\\]+[.]dll',ref['path']),'Invalid DLL path')
        reader.verify(ref,allow_empty=False); names.append(ref['path'])
    need(names==sorted(set(names)),'Unique ordered DLLs required')
    _,text=reader.read(d['native_events']); need(text.endswith('\n'),'Truncated journal')
    raw=text.splitlines(); need(0<len(raw)<=MAX_ROWS and all(0<len(x)<=262144 for x in raw),'Bounded JSONL required')
    events=[parse(x) for x in raw]
    lp=reader.verify(d['lineage'],MAX_LINEAGE_BYTES,False)
    result=validate(job,claim,events,_lineage_rows(reader,lp)); reader.stable()
    if result['recorded_join_consistent']: result.update(source_bytes_verified=True,build_bytes_verified=True,artifact_barrier='UnchangedPinnedArtifacts')
    return result


def main():
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('--project',type=Path,required=True); p.add_argument('--descriptor',type=Path,required=True)
    a=p.parse_args()
    try: result=inspect(a.project,a.descriptor)
    except (OSError,UnicodeError,ValueError,KeyError,TypeError) as error: result=dict(result='MissingOrFailedAdmission',recorded_join_consistent=False,qualified=False,paid_resources_qualified=False,normal_mission_ready=False,queue_success_enabled=False,normal_720_credit=0,error=type(error).__name__+': '+str(error))
    print(json.dumps(result,sort_keys=True)); return 0 if result['recorded_join_consistent'] else 1


if __name__=='__main__': raise SystemExit(main())
