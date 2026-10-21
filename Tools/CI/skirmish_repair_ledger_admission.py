"""Strict read-only repair boundary admission. No paid-resource, queue or mission credit.

Descriptor/actual-byte barrier uses the frozen construction Reader, without changing
that consumer. validate_repair_ledger() is pure consistency only; inspect() also
verifies canonical owned artifacts, exact reviewed C# pins and current DLL bytes.
Missing banks/owners, unsupported DTOs and missing earlier debit fail closed.
Progress, unchanged debt retries, starting origins and whole-attempt conservation
are explicitly outside scope. Constructed fixtures never establish native readiness.
"""
import argparse
import copy
import datetime
import hashlib
import json
import math
import re
import struct
from pathlib import Path
from skirmish_construction_ledger_admission import (Reader, need, integer, string,
    exact, finite, sha, parse, BANK, ENVELOPE, I32, U32, MAX_BYTES, MAX_ROWS,
    MAX_SOURCE_FILES)
from skirmish_native_identity import verify_job
from skirmish_production_queue import fingerprint
from s005_windows_source_snapshot import candidate_hash

BASELINE='9f75b2f224cde04ea87f25b8c9fc097765889bcf'
POLICY_PINS={
'Assets/Game/Scripts/Runtime/Skirmish/VehicleFieldRepairService.cs':'1490f369ab3de1761b9c2a23eecaf026a0bd844620127f61b2cc52fea09a6192',
'Assets/Game/Scripts/Runtime/Skirmish/VehicleRepairRules.cs':'43d1cc13c12c7db4b008ef17c66d8897d1e2015b1d2ffb89e9435f335c67631b',
'Assets/Game/Scripts/Components/VehicleRepairComponents.cs':'07a200ff94a67b24ddbf466e665e8855acc2eee60de1a697e61792573ff107b3',
'Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs':'10c2827cf812bd9ed6f77b61fafd632e2725e3df51d14ef49d0d139d596bbbf0',
'Assets/Game/Scripts/Components/FactionTacticalMaterialsComponents.cs':'d62d73d7c12a48592150abc7c79e4b852df4817e88e20b0b02fcd96a026008fb',
'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.ProductionCanary.cs':'829fca0f4bc03d689c1f9fb7aad0b26c1c1c18f4bc21bd7d31ed338b2394ac34',
'Assets/Tests/Editor/SkirmishExpansion/VehicleFieldRepairNativeTests.cs':'5972c3cda9e314403cde4718e933e03c7bf597c1f1de9136b8a11f959fc122d8'}
SCOPE='RepairCommittedBoundariesPartial'
FLAGS=set('unchanged_debt_retries_measured progress_history_measured paid_resources_qualified'.split())
COMMON={'ledger_schema','scope'}|FLAGS
TRANSACTION=COMMON|set('ledger_sequence operation native_world_sequence native_session_id native_setup_identity native_config_sha256 before after policy_state'.split())
POINT=set('actor_index actor_version bank_index bank_version bank state health owner'.split())
STATE=set('session_index session_version bank_index bank_version facility_index facility_version attempt_id actor_stable_id facility_stable_id pad_id faction request_id setup_hash phase paid refund_due refunded duration_ticks remaining_ticks fraction_numerator fraction_denominator last_clock_seconds fractional_ticks incoming_damage new_movement accepted_position pad_position facility_position last_health last_maximum'.split())
POSITIONS={'accepted_position','pad_position','facility_position'}
STRINGS={'attempt_id','actor_stable_id','facility_stable_id','pad_id'}
ENTITIES=set('session_index session_version bank_index bank_version facility_index facility_version'.split())
PHASES={'None','Working','Completed','Cancelled','Destroyed','AccountingFailure'}
OPS={'DebitCommitted','Completed','Destroyed','CancelledNoRefund','RefundCommitted','RefundDeferred','RefundSettled'}
IMMUTABLE=ENTITIES|STRINGS|set('faction setup_hash paid duration_ticks fraction_numerator fraction_denominator'.split())|POSITIONS
UNOBSERVED={'remaining_ticks','last_clock_seconds','fractional_ticks','incoming_damage','new_movement','last_health','last_maximum'}


def policy_pin_matches(path, actual):
    # Literal bytes only; Windows CRLF variants need separate peer-reviewed pins.
    return actual==POLICY_PINS[path]


def scope(f):
    integer(f['ledger_schema'],1,1)
    need(f['scope']==SCOPE and all(f[k] is False for k in FLAGS),'Unsupported repair scope/credit')


def state(s, allow_default=False):
    exact(s,STATE);need(s['phase'] in PHASES,'Unknown phase')
    default=s['phase']=='None'
    need(not default or allow_default,'Unexpected default state')
    for k in ENTITIES:integer(s[k],0 if default else 1)
    for k in STRINGS:string(s[k],61,default)
    integer(s['faction'],0 if default else 1,0 if default else 2)
    for k in ('request_id','setup_hash'):integer(s[k],0 if default else 1,U32)
    for k in ('paid','refund_due','refunded','last_health','last_maximum'):integer(s[k],-I32-1 if k=='last_health' else 0)
    for k in ('duration_ticks','remaining_ticks','fraction_numerator','fraction_denominator'):integer(s[k],0,9223372036854775807)
    for k in ('last_clock_seconds','fractional_ticks'):finite(s[k]);need(s[k]>=0,'Negative clock/fraction')
    need(s['fractional_ticks']<1,'Invalid fractional ticks')
    for k in ('incoming_damage','new_movement'):integer(s[k],0,1)
    for k in POSITIONS:
        need(isinstance(s[k],list) and len(s[k])==3,'Position shape')
        for x in s[k]:
            finite(x)
            need(struct.unpack('<f',struct.pack('<f',x))[0]==x,'Position is not exact widened float32')
    if default:
        need(all(s[k]=='' for k in STRINGS) and all(s[k]==[0,0,0] for k in POSITIONS)
             and all(s[k]==0 for k in STATE-STRINGS-POSITIONS-{'phase'}),'Nondefault None receipt')
    else:
        need(s['request_id']>0 and 0<s['paid']<=20 and 0<s['duration_ticks']<=200000000
             and s['remaining_ticks']<=s['duration_ticks'] and 0<s['fraction_numerator']<=s['fraction_denominator']//4
             and s['refunded']<=s['refund_due']<=s['paid'],'Invalid paid receipt arithmetic')
    return s


def point(p, policy, session):
    exact(p,POINT)
    for k in ('actor_index','actor_version','bank_index','bank_version'):integer(p[k],1)
    b=p['bank'];exact(b,BANK)
    for k,v in b.items():integer(v,0,U32 if k=='Version' else I32)
    need(b['FactionId']==policy['faction'] and b['Current']<=b['Capacity'],'Bank invalid/foreign')
    o=p['owner'];exact(o,{'attempt_id','stable_id','faction','is_structure','capacity_released'})
    string(o['attempt_id'],61);string(o['stable_id'],61)
    integer(o['faction'],1,2);integer(o['is_structure'],0,0);integer(o['capacity_released'],0,1)
    need(o['attempt_id']==session==policy['attempt_id'] and o['stable_id']==policy['actor_stable_id']
         and o['faction']==policy['faction'],'Actor ownership splice')
    need((p['bank_index'],p['bank_version'])==(policy['bank_index'],policy['bank_version']),'Bank generation splice')
    h=p['health']
    if h is not None:
        exact(h,{'current','maximum'});integer(h['current'],-I32-1);integer(h['maximum'],0)
    if p['state'] is not None:
        old=state(p['state'],True)
        if old['phase']!='None':
            need(old['attempt_id']==session and old['actor_stable_id']==o['stable_id'] and old['faction']==o['faction']
                 and all(old[k]==policy[k] for k in ('session_index','session_version','setup_hash','bank_index','bank_version')),
                 'Current receipt does not belong to actual actor/session/setup/bank')
    return p


def bank_mutation(b, amount, refund=False):
    b=copy.deepcopy(b)
    if refund:
        need(amount>0 and amount<=b['Capacity']-b['Current'] and amount<=b['LifetimeSpent']
             and amount<=b['LifetimeRepairSpent'],'Impossible refund')
        b['Current']+=amount
        for k in ('LifetimeSpent','LifetimeRepairSpent'):b[k]-=amount
    else:
        need(amount>0 and b['Current']>=amount,'Unaffordable repair')
        b['Current']-=amount
        for k in ('LifetimeSpent','LifetimeRepairSpent'):b[k]=min(I32,b[k]+amount)
    b['Version']=1 if b['Version']==U32 else b['Version']+1
    return b


def progressed(old, new):
    if old['phase']!='Working':
        need(old==new,'Unrecorded terminal/debt receipt mutation');return
    need(all(old[k]==new[k] for k in STATE-UNOBSERVED),'Unrecorded disposition/identity/request change')
    need(new['remaining_ticks']<=old['remaining_ticks'] and new['last_clock_seconds']>=old['last_clock_seconds']
         and new['incoming_damage']>=old['incoming_damage'] and new['new_movement']>=old['new_movement'],
         'Repair clock/progress/interrupt regression')


def validate_repair_ledger(job,claim,events):
    answer=dict(admission='repair_boundaries',result='MissingOrFailedAdmission',qualified=False,
        queue_success_enabled=False,paid_resources_qualified=False,whole_attempt_conservation=False,
        normal_mission_ready=False,source_bytes_verified=False,progress_history_measured=False,
        unchanged_debt_retries_measured=False)
    try:
        for k in ('candidate_sha256','build_sha256','setup_sha256','content_sha256'):sha(job[k])
        h=fingerprint(job);need(claim['job_sha256']==h,'Claim/job mismatch')
        string(claim['worker'],80);need(re.fullmatch('attempt-[a-f0-9]{32}',claim['attempt_id']),'Invalid attempt')
        need(isinstance(events,list) and 0<len(events)<=MAX_ROWS,'Bounded native stream required')
        for n,r in enumerate(events,1):
            exact(r,ENVELOPE);integer(r['schema_version'],1,1);integer(r['sequence'],n,n);integer(r['frame'])
            need(r['job_sha256']==h and r['attempt_id']==claim['attempt_id'] and r['worker']==claim['worker'],'Envelope ownership/order')
            string(r['utc'],64);need(datetime.datetime.fromisoformat(r['utc'].replace('Z','+00:00')).tzinfo is not None,'Timestamp timezone')
            string(r['kind'],128);need(isinstance(r['facts'],dict),'Facts object required')
        def one(kind):
            rows=[(i,r['facts']) for i,r in enumerate(events) if r['kind']==kind]
            need(len(rows)==1,'Exactly one '+kind+' required');return rows[0]
        ci,c=one('ContextCaptured');si,start=one('NativeRepairLedgerStarted');mi,menu=one('NormalMenuLaunchObserved')
        ei,end=one('NativeRepairLedgerClosed');ri,_=one('RestoreStarting');fi,_=one('ContextRestored')
        need(ci==0 and ci<si<mi<ei<ri<fi==len(events)-1,'Incomplete/ordered repair lifecycle')
        need(c['source_sha256']==job['candidate_sha256'] and c['compiled_dll_manifest_sha256']==job['build_sha256'],'Candidate/build splice')
        verify_job(job,menu['native_setup_identity']);sha(menu['native_config_sha256'])
        session=string(menu['native_session_id'],61);world=string(menu['native_world_sequence'],20)
        need(re.fullmatch('[0-9]+',world) and 0<int(world)<=18446744073709551615,'World sequence')
        exact(start,COMMON);scope(start)
        exact(end,COMMON|{'transactions','sink_owned','capture_failed','observer_faults'});scope(end)
        integer(end['transactions'],0,4096);integer(end['observer_faults'],0,0)
        need(end['sink_owned'] is True and end['capture_failed'] is False,'Unowned/failed capture')
        need(not any('Repair' in r['kind'] and r['kind'] not in {'NativeRepairLedgerStarted','NativeRepairTransaction','NativeRepairLedgerClosed'} for r in events),'Repair failure/unknown event')
        rows=[(i,r['facts']) for i,r in enumerate(events) if r['kind']=='NativeRepairTransaction']
        need(len(rows)==end['transactions'],'Repair count/truncation')
        actors={};stable={};actor_identities={};banks={};session_entity=None;counts={op:0 for op in sorted(OPS)}
        for n,(i,f) in enumerate(rows,1):
            need(mi<i<ei,'Transaction outside capture');exact(f,TRANSACTION);scope(f);integer(f['ledger_sequence'],n,n)
            need(f['operation'] in OPS,'Unknown operation')
            need(all(f[k]==menu[k] for k in ('native_session_id','native_world_sequence','native_config_sha256','native_setup_identity')),'Native subject splice')
            verify_job(job,f['native_setup_identity'])
            p=state(f['policy_state']);a=point(f['before'],p,session);b=point(f['after'],p,session)
            integer(p['setup_hash'],0,U32)
            need(p['setup_hash']==menu['native_setup_identity']['native_setup_hash_uint32'],'Receipt setup splice')
            se=(p['session_index'],p['session_version']);need(session_entity is None or session_entity==se,'Session generation splice');session_entity=se
            key=(a['actor_index'],a['actor_version']);be=(a['bank_index'],a['bank_version'])
            need(all(a[k]==b[k] for k in POINT-{'bank','state','health'}) ,'Boundary actor/bank/owner change')
            faction=p['faction'];need(faction not in banks or banks[faction]==be,'Faction bank generation changed');banks[faction]=be
            need(len(set(banks.values()))==len(banks),'Shared foreign bank')
            sid=p['actor_stable_id'];need(sid not in stable or stable[sid]==key,'Stable actor rebound');stable[sid]=key
            identity=tuple(p[k] for k in ('actor_stable_id','faction','attempt_id','setup_hash','session_index','session_version'))
            need(key not in actor_identities or actor_identities[key]==identity,'Actor entity generation changed stable/faction/session/setup identity')
            actor_identities[key]=identity
            prior=actors.get(key);x=a['state'];op=f['operation'];counts[op]+=1
            if prior is not None:
                need(x is not None,'Recorded receipt erased');progressed(prior,x)
            elif op!='DebitCommitted':need(False,'Disposition/debt lacks observed debit')
            y=copy.deepcopy(p);expectedbank=a['bank'];expectedhealth=a['health']
            if op=='DebitCommitted':
                need(x is None or x['phase'] in {'None','Completed','Cancelled','Destroyed'},'Overlapping repair/debt')
                need(p['request_id']==(x['request_id'] if x else 0)+1,'Start request sequence')
                hp=a['health'];need(hp is not None and 0<hp['current']<hp['maximum'],'Invalid quote health')
                numerator=min((hp['maximum']-hp['current'])*4,hp['maximum']);denominator=hp['maximum']*4
                paid=(80*numerator+denominator-1)//denominator;duration=max(1,(800000000*numerator+denominator-1)//denominator)
                need((p['fraction_numerator'],p['fraction_denominator'],p['paid'],p['duration_ticks'],p['remaining_ticks'])==
                     (numerator,denominator,paid,duration,duration),'Quote fraction/materials/ticks differ')
                need(p['phase']=='Working' and p['refund_due']==p['refunded']==p['fractional_ticks']==p['incoming_damage']==p['new_movement']==0
                     and (p['last_health'],p['last_maximum'])==(hp['current'],hp['maximum']) and a['owner']['capacity_released']==0,'Debit receipt differs')
                expectedbank=bank_mutation(a['bank'],paid)
            else:
                need(x is not None and x['phase'] in {'Working','AccountingFailure'},'Duplicate/terminal disposition')
                need(all(x[k]==p[k] for k in IMMUTABLE),'Paid policy identity/fraction changed')
                need(p['last_clock_seconds']>=x['last_clock_seconds'] and p['remaining_ticks']<=x['remaining_ticks'],'Boundary clock regressed')
                need(p['request_id']==x['request_id'] or x['phase']=='Working' and p['request_id']==x['request_id']+1 and op.startswith('Refund'),'Cancel request sequence')
                if op!='Completed':
                    need(all(p[k]==x[k] for k in ('last_health','last_maximum','incoming_damage','new_movement')),'Cancellation rewrote health/interrupt history')
                if x['phase']=='Working':
                    if op=='Destroyed':
                        need(all(p[k]==x[k] for k in UNOBSERVED),'Destroyed path invented accrual')
                    else:
                        ticks=(p['last_clock_seconds']-x['last_clock_seconds'])*10000000+x['fractional_ticks']
                        need(math.isfinite(ticks) and 0<=ticks<9223372036854775808,'Invalid boundary tick delta')
                        elapsed=math.floor(ticks)
                        need(p['remaining_ticks']==max(0,x['remaining_ticks']-elapsed) and p['fractional_ticks']==ticks-elapsed,'Local boundary accrual differs')
                if op=='Completed':
                    hp=a['health'];need(hp is not None and hp['maximum']>0 and hp['current']>0 and p['phase']=='Working'
                        and p['remaining_ticks']==p['refund_due']==p['refunded']==p['incoming_damage']==p['new_movement']==0
                        and p['request_id']==x['request_id'] and a['owner']['capacity_released']==0,'Completion policy')
                    need((p['last_health'],p['last_maximum'])==(hp['current'],hp['maximum']),'Completion last health')
                    # Pinned VehicleRepairRules.TookDamage, including nearest integer health research scaling.
                    need(x['last_maximum']>0 and hp['maximum']>0 and hp['current']>0,'Invalid completion health basis')
                    expected=(x['last_health']*hp['maximum']+x['last_maximum']//2)//x['last_maximum']
                    need(hp['current']>=expected,'Native damage predicate requires cancellation before completion')
                    heal=max(0,min(hp['maximum']-hp['current'],hp['maximum']*p['fraction_numerator']//p['fraction_denominator']))
                    expectedhealth=dict(current=hp['current']+heal,maximum=hp['maximum']);y['phase']='Completed'
                else:
                    if x['phase']=='Working':
                        due=0 if op=='Destroyed' else p['paid'] if p['remaining_ticks']==p['duration_ticks'] else p['paid']*3//4
                        need(p['refund_due']==due and p['refunded']==0 and p['phase']==('Destroyed' if op=='Destroyed' else 'Cancelled'),'Cancellation quote/phase')
                    elif op=='Destroyed':
                        need(p['phase']=='Destroyed' and p['refund_due']==0 and p['refunded']==x['refunded'],'Destroyed debt policy')
                    else:
                        need(p==x,'Debt retry changed policy')
                    remaining=p['refund_due']-p['refunded']
                    if op=='RefundCommitted':
                        expectedbank=bank_mutation(a['bank'],remaining,True);y.update(phase='Cancelled',refunded=p['refund_due'])
                    elif op=='RefundDeferred':
                        need(not(x['phase']=='AccountingFailure' and x['refund_due']==p['refund_due'] and x['refunded']==p['refunded']),'Unchanged debt retry invented')
                        try:bank_mutation(a['bank'],remaining,True)
                        except ValueError:pass
                        else:need(False,'Deferred refund was possible')
                        need(remaining>0,'No deferred debt');y['phase']='AccountingFailure'
                    elif op=='RefundSettled':need(remaining==0,'Unpaid debt marked settled');y['phase']='Cancelled'
                    elif op in {'Destroyed','CancelledNoRefund'}:need(p['refund_due']==0,'No-refund operation carries refund')
                    else:need(False,'Unknown disposition')
            need(b['state']==y and b['bank']==expectedbank and b['health']==expectedhealth,'Committed state/resource/health boundary differs')
            actors[key]=copy.deepcopy(y)
        answer.update(result='ConsistentRecordedRepairBoundaries',transactions=len(rows),operations=counts,
            job_sha256=h,attempt_id=claim['attempt_id'],candidate_sha256=job['candidate_sha256'],build_sha256=job['build_sha256'],
            native_session_id=session,native_world_sequence=world,native_config_sha256=menu['native_config_sha256'],
            outstanding_working=sum(s['phase']=='Working' for s in actors.values()),
            outstanding_refund_debt=sum(s['refund_due']-s['refunded'] for s in actors.values() if s['phase']=='AccountingFailure'),
            limitations=['UnobservedProgressAndUnchangedDebtRetries','StartingResourceOrigins','ActualGeometryAndLiveFacilityChecks',
                         'WholeAttemptConservation','NormalMissionAndDeviceReadiness'])
    except (ValueError,KeyError,TypeError,IndexError,AttributeError,OverflowError,RecursionError,struct.error) as error:
        answer['errors']=[str(error)]
    return answer

def inspect(project,descriptor):
    reader=Reader(project)
    p=reader.path(descriptor)
    with p.open('rb') as stream:data=stream.read(MAX_BYTES+1)
    need(0<len(data)<=MAX_BYTES,'Bounded descriptor required')
    _,d=reader.obj({'path':str(p),'sha256':hashlib.sha256(data).hexdigest()})
    exact(d,{'schema_version','job','claim','immutable_claim','native_events','candidate_manifest'});integer(d['schema_version'],1,1)
    jp,job=reader.obj(d['job']);cp,claim=reader.obj(d['claim']);ip,immutable=reader.obj(d['immutable_claim'])
    need(claim==immutable,'Immutable claim differs')
    folder=Path(claim['artifact_directory'])
    need(folder.is_absolute() and reader.root in folder.parents and folder.name==claim['attempt_id']
         and folder.parent==jp.parent and jp.name=='job.json' and jp.parent.name==job['job_id']
         and cp==jp.parent/'claim.json' and ip==folder/'claim.json', 'Canonical job/claim/attempt directory ownership differs')
    ep=reader.path(d['native_events']['path'])
    need(ep==folder/'adapter-native-events.jsonl', 'Native journal outside owned attempt')
    _,store=reader.obj(d['candidate_manifest'])
    exact(store,{'source_manifest','candidate_sha256','build_manifest','build_sha256'})
    need(store['source_manifest']==job['source_manifest'] and store['candidate_sha256']==job['candidate_sha256']
         and store['build_sha256']==job['build_sha256'], 'Candidate store/job source or build splice')
    pins=job['source_manifest'];need(isinstance(pins,list) and 0<len(pins)<=MAX_SOURCE_FILES,'Bounded source manifest')
    seen={}
    for ref in pins:
        exact(ref,{'path','sha256'});need(ref['path'] not in seen,'Duplicate source manifest path')
        need(isinstance(ref['path'],str) and not Path(ref['path']).is_absolute() and '\\' not in ref['path'], 'Relative forward-slash source row required')
        reader.verify(ref);seen[ref['path']]=ref['sha256']
    need(all(policy_pin_matches(p,seen.get(p)) for p in POLICY_PINS),'Literal reviewed source/policy pins differ or missing')
    need(candidate_hash(pins)==job['candidate_sha256'], 'Source manifest does not derive candidate identity')
    builds=store['build_manifest']
    need(isinstance(builds,list) and 0<len(builds)<=1024 and fingerprint(builds)==job['build_sha256'], 'Build manifest does not derive compiled identity')
    build_paths=[]
    for ref in builds:
        exact(ref,{'path','sha256'})
        name=ref['path'];need(isinstance(name,str) and re.fullmatch(r'Library/ScriptAssemblies/[^/\\]+[.]dll',name), 'Compiled DLL row outside ScriptAssemblies')
        reader.verify(ref,allow_empty=False);build_paths.append(name)
    need(build_paths==sorted(set(build_paths)), 'Unique ordered compiled DLL manifest required')
    _,text=reader.read(d['native_events']);need(text.endswith('\n'),'Truncated journal without final newline')
    lines=text.splitlines();need(0<len(lines)<=MAX_ROWS and all(0<len(s)<=262144 for s in lines),'Bounded JSONL required')
    events=[parse(s) for s in lines]
    answer=validate_repair_ledger(job,claim,events)
    reader.stable()
    if answer['result']=='ConsistentRecordedRepairBoundaries':
        answer.update(source_bytes_verified=True,build_bytes_verified=True,artifact_barrier='UnchangedPinnedArtifacts',reviewed_policy_baseline=BASELINE)
    return answer



def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument('--descriptor',type=Path,required=True)
    args=parser.parse_args()
    try:answer=inspect(args.project,args.descriptor)
    except (ValueError,OSError,KeyError,TypeError,OverflowError,RecursionError) as error:
        answer={'admission':'repair_boundaries','result':'MissingOrFailedAdmission','errors':[str(error)],
                'qualified':False,'queue_success_enabled':False,'paid_resources_qualified':False}
    print(json.dumps(answer,indent=2,allow_nan=False))
    return 0 if answer['result']=='ConsistentRecordedRepairBoundaries' else 1


if __name__=='__main__':raise SystemExit(main())
