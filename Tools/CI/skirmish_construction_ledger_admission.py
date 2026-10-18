"""Read-only admission of pinned construction boundaries; never paid-resource or mission credit.

CLI: python skirmish_construction_ledger_admission.py --project ROOT --descriptor PATH
Descriptor v1 has hash references job, claim, immutable_claim, native_events,
candidate_manifest (the existing source/build snapshot object). All
artifacts must be under ROOT, with no child symlink/junction/reparse path. The
source manifest must include the literal reviewed policy pins below. Callers of
validate_construction_ledger receive only an in-memory consistency check; inspect()
adds actual bytes, claim-copy equality, source pins and a final stability barrier.
Constructed tests are not live evidence. No producer or operational inspector is changed.
"""
import argparse
import copy
import datetime
import hashlib
import json
import math
import os
from pathlib import Path
import re
import stat

from skirmish_native_identity import verify_job
from skirmish_production_queue import fingerprint
from s005_windows_source_snapshot import candidate_hash

BASELINE = '5f7308fb330bed1fe1a693bb9058a24ee43f4ecf'
POLICY_PINS = {
    "Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.ProductionCanary.cs": "e6efcd87fd7b2494b0506e9354e3f904a747d5c2fffd12e167903c59972ab9c3",
    "Assets/Game/Scripts/Systems/BuildingConstructionResourceTransactionSystemHelper.cs": "38daf611d9103717b131f66112bf5ca1f793e80a79d5f34b9ae850e6356512ff",
    "Assets/Game/Scripts/Systems/BuildingPlacementConstructionTransaction.cs": "a977bccfe30c86e51a9a03a9f649af36f2c9d08e260da97fc41bf86a98343d17",
    "Assets/Game/Scripts/Systems/RuntimeFactionResourceSystemHelper.cs": "dfebb4af2b1a78815442f51aa299bfade96398875c3f8651ea3f6ebd3423e9b1",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyConstructionAdapter.cs": "93ff16981342afe19e4b1aaaeb45a1cabe32215dc0dccd53f762e27ac0af5363",
    "Assets/Game/Scripts/Contracts/MissionConstructionCostPolicy.cs": "96c8df5a3574af4d3b647d51f0c82065f0cd3ddbcf7a0ab0064d868b8579ad88",
    "Assets/Game/Scripts/Systems/FactionConstructionResourceUtilitySystemHelper.cs": "2451eef2ac4b75042b529cb115cdffd6d4597e320d0055347bbe711e6ad57ed4",
    "Assets/Game/Scripts/Systems/FactionTacticalMaterialsUtilitySystemHelper.cs": "10c2827cf812bd9ed6f77b61fafd632e2725e3df51d14ef49d0d139d596bbbf0",
    "Assets/Game/Scripts/Components/FactionTacticalMaterialsComponents.cs": "d62d73d7c12a48592150abc7c79e4b852df4817e88e20b0b02fcd96a026008fb",
    "Assets/Game/Scripts/Components/FactionAIComponents.cs": "7ceb43eca68cbed12dcb7bf5409dc84810b04863f91c6472bc7213e36108bb34",
    "Assets/Game/Scripts/Components/Skirmish/SkirmishEnemyConstructionIntentV1Component.cs": "bdbf7e772efb6cb5c4208da72d2c3474ca38598c329f2a51ee8d31725e61829f",
    "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs": "b81c797aad5a0256fb331d772aa445c52d2337ce47617863f51b80f1011498d5"
}
SCOPE = 'ConstructionCommittedBoundariesPartial'
UNCOVERED = ['PlayerWallMemberIdentities', 'AlternateConstructionEntryPoints',
             'RepairEconomyFuelOrigins', 'WholeAttemptConservation']
I32, U32 = 2147483647, 4294967295
MAX_BYTES, MAX_ROWS = 16*1024*1024, 25000
MAX_SOURCE_FILES = 131072
MAX_PIN_FILE_BYTES = 512*1024*1024
MAX_PIN_TOTAL_BYTES = 8*1024*1024*1024
HASH_CHUNK_BYTES = 65536
ENVELOPE = set('schema_version sequence job_sha256 attempt_id worker frame utc kind facts'.split())
ECONOMY = set('FactionId Money MaterialsOnlyConstruction Oil Fuel OilIncomeRate FuelIncomeRate LastSellTime LastLogTime'.split())
BANK = set('FactionId Current Capacity LifetimeFabricated LifetimeImported LifetimeRewarded LifetimeExported LifetimeSpent LifetimeConstructionSpent LifetimeRepairSpent LifetimeInfrastructureSpent LifetimeUpgradeSpent LifetimeProductionSpent Version'.split())
POINT = set('bank_index bank_version economy bank session_index session_version enemy_intent request'.split())
INTENT = set('version session phase action pad building origin_x origin_y width depth paid request_id candidate desired_count result_stable_id retry_after'.split())
REQUEST = set('id kind status building faction materials preferred_x preferred_y actual_x actual_y actual_width actual_depth runtime_id plan_index plan_version authored_grant allow_nonbuildable require_origin'.split())
ACTOR = set('index version session stable_id faction is_structure shared_actor source_key'.split())
TRANSACTION = set('ledger_schema scope ledger_sequence operation native_session_id native_world_sequence native_config_sha256 native_setup_identity before after transaction_owner_sequence transaction_id credits_quoted materials_quoted prefab_name origin_x origin_y rotated committed_count expected_count actor paid_resources_qualified'.split())
OPS = set('PlayerReserve PlayerPlacementRequest PlayerPlacementOutcome PlayerFinalize PlayerRollback PlayerRollbackDeferred EnemyRequest EnemyRefundDebt EnemyRefund EnemyReenqueue EnemyDelivered'.split())


def need(ok, why):
    if not ok:
        raise ValueError(why)


def integer(v, low=0, high=I32):
    need(type(v) is int and low <= v <= high, 'Typed bounded integer required')
    return v


def string(v, maximum=512, empty=False):
    need(isinstance(v, str) and (empty or bool(v)) and len(v.encode('utf-8')) <= maximum
         and not any(ord(c)<32 for c in v), 'Bounded literal string required')
    return v


def exact(v, keys):
    need(isinstance(v, dict) and set(v)==keys, 'Unsupported or incomplete object schema')


def finite(v):
    need(type(v) in (int,float) and math.isfinite(v), 'Finite typed number required')


def sha(v):
    need(isinstance(v,str) and re.fullmatch('[a-f0-9]{64}',v), 'SHA256 required')


def strict_pairs(pairs):
    result={}
    for k,v in pairs:
        need(k not in result,'Duplicate JSON key')
        result[k]=v
    return result


def parse(text):
    return json.loads(text,object_pairs_hook=strict_pairs,
                      parse_constant=lambda _:need(False,'Nonfinite JSON'))


class Reader:
    """Bounded Reader equivalent, including Python 3.9 and Windows reparse checks."""
    def __init__(self, root):
        self.root=Path(root).resolve(strict=True)
        self.pins={}
        self.pin_sizes={}
        self.pin_total=0

    def path(self, value, byte_limit=None, allow_empty=False):
        if byte_limit is None:byte_limit=MAX_BYTES
        string(str(value),4096)
        raw=str(value).replace('\\','/')
        need(not raw.startswith(('//?/','//./')) and '..' not in raw.split('/'), 'Device/traversal path refused')
        p=Path(value)
        if not p.is_absolute():
            p=self.root/p
        p=Path(os.path.abspath(p))
        need(p!=self.root and self.root in p.parents, 'Artifact escapes project')
        cursor=p
        while cursor!=self.root:
            s=cursor.lstat()
            need(not stat.S_ISLNK(s.st_mode) and not (getattr(s,'st_file_attributes',0)&0x400),
                 'Artifact symlink/junction/reparse path refused')
            cursor=cursor.parent
        need(p.resolve(strict=True)==p and p.is_file(), 'Missing/escaped artifact')
        need((0 if allow_empty else 1)<=p.stat().st_size<=byte_limit,'Empty/oversized bounded artifact')
        return p

    def blob(self,ref):
        exact(ref,{'path','sha256'});sha(ref['sha256'])
        p=self.path(ref['path'])
        with p.open('rb') as stream:
            data=stream.read(MAX_BYTES+1)
        need(0<len(data)<=MAX_BYTES,'Bounded read exceeded')
        need(hashlib.sha256(data).hexdigest()==ref['sha256'],'Artifact hash mismatch')
        self.remember(p,ref['sha256'],MAX_BYTES,False)
        return p,data

    def remember(self,p,digest,limit,allow_empty):
        need(p not in self.pins or self.pins[p][0]==digest,'Conflicting artifact aliases')
        self.pins[p]=(digest,limit,allow_empty)

    def verify(self,ref,byte_limit=MAX_PIN_FILE_BYTES,allow_empty=True):
        """Hash large source/DLL pins with fixed working memory and a total budget."""
        exact(ref,{'path','sha256'});sha(ref['sha256'])
        p=self.path(ref['path'],byte_limit,allow_empty)
        if p not in self.pin_sizes:
            size=p.stat().st_size
            need(self.pin_total+size<=MAX_PIN_TOTAL_BYTES,'Total source/build byte budget exceeded')
            self.pin_total+=size;self.pin_sizes[p]=size
        h=hashlib.sha256();total=0
        with p.open('rb') as stream:
            while True:
                data=stream.read(HASH_CHUNK_BYTES)
                if not data:break
                total+=len(data);need(total<=byte_limit,'Bounded streamed read exceeded')
                h.update(data)
        need((allow_empty or total>0) and h.hexdigest()==ref['sha256'],'Artifact hash mismatch')
        self.remember(p,ref['sha256'],byte_limit,allow_empty)
        return p

    def read(self,ref):
        p,data=self.blob(ref)
        return p,data.decode('utf-8-sig')

    def obj(self,ref):
        p,text=self.read(ref);v=parse(text);need(isinstance(v,dict),'JSON object required')
        return p,v

    def stable(self):
        # Recheck paths as well as hashes: a late reparse replacement must fail.
        for p,(h,limit,allow_empty) in list(self.pins.items()):
            self.verify({'path':str(p),'sha256':h},limit,allow_empty)


def point(p,session):
    exact(p,POINT)
    for k in ('bank_index','bank_version','session_index','session_version'):
        integer(p[k],1)
    e,b=p['economy'],p['bank'];exact(e,ECONOMY);exact(b,BANK)
    integer(e['FactionId'],1,2);integer(e['Money']);integer(e['MaterialsOnlyConstruction'],0,2)
    for k in ECONOMY-{'FactionId','Money','MaterialsOnlyConstruction'}:finite(e[k])
    for k,v in b.items():integer(v,0,U32 if k=='Version' else I32)
    need(b['FactionId']==e['FactionId'] and b['Current']<=b['Capacity'],'Invalid bank state/faction')
    i=p['enemy_intent']
    if i is not None:
        exact(i,INTENT);need(e['FactionId']==2,'Player carries enemy intent')
        integer(i['version'],1,1);need(i['session']==session,'Enemy intent session splice')
        integer(i['phase'],0,5)
        for k in ('action','pad','building','result_stable_id'):string(i[k],128,True)
        for k in ('origin_x','origin_y','candidate'):integer(i[k],-I32-1)
        for k in ('width','depth','paid','request_id','desired_count'):integer(i[k])
        finite(i['retry_after']);need(i['retry_after']>=0,'Negative retry time')
        if i['phase'] in (1,2,3,4):
            need(i['paid']>0 and i['request_id']>0 and i['width']>0 and i['depth']>0
                 and all(i[k] for k in ('action','pad','building')), 'Incomplete paid enemy intent')
    r=p['request']
    if r is not None:
        exact(r,REQUEST);need(i is not None,'Request without intent')
        for k in REQUEST-{'building'}:integer(r[k],-I32-1 if k in ('preferred_x','preferred_y','actual_x','actual_y') else 0)
        need(r['kind']==4 and r['status'] in (0,1,2) and r['faction']==2 and r['id']==i['request_id']
             and r['materials']==i['paid'] and r['building']==i['building']
             and (r['plan_index'],r['plan_version'])==(p['session_index'],p['session_version'])
             and r['authored_grant']==r['allow_nonbuildable']==0 and r['require_origin']==1
             and (r['preferred_x'],r['preferred_y'])==(i['origin_x'],i['origin_y']), 'Enemy request correlation/authority differs')
    return p


def resolve(policy,credits,materials):
    if policy==0:return credits,materials
    if credits==0:return 0,materials
    if policy==1:
        need(materials>0,'Materials-only cost lacks authored materials')
        return 0,materials
    cost={(40000,90):120,(22000,50):70,(6000,15):20,(10000,20):30}.get((credits,materials))
    need(cost is not None,'Unknown DefenseMaterialsV1 authored price')
    return 0,cost


def mutate(e,b,credits,materials,refund=False):
    e,b=copy.deepcopy(e),copy.deepcopy(b)
    c,m=resolve(e['MaterialsOnlyConstruction'],credits,materials)
    if refund:
        need(e['Money']<=I32-c and b['Current']<=b['Capacity']-m
             and b['LifetimeSpent']>=m and b['LifetimeConstructionSpent']>=m,'Impossible refund')
        e['Money']+=c;b['Current']+=m
        b['LifetimeSpent']-=m;b['LifetimeConstructionSpent']-=m
    else:
        need(e['Money']>=c and b['Current']>=m,'Unaffordable debit')
        e['Money']-=c;b['Current']-=m
        for k in ('LifetimeSpent','LifetimeConstructionSpent'):b[k]=min(I32,b[k]+m)
    if m:b['Version']=1 if b['Version']==U32 else b['Version']+1
    return e,b


def actor(v,session,faction,building=None,stable=None):
    exact(v,ACTOR)
    integer(v['index'],1);integer(v['version'],1)
    need(v['session']==session and type(v['faction'])is int and v['faction']==faction
         and type(v['is_structure'])is int and v['is_structure']==1 and v['shared_actor']is True,'Actor ownership differs')
    string(v['stable_id'],128);string(v['source_key'],128)
    if building is not None:need(v['source_key']==building,'Actor prefab splice')
    if stable is not None:need(v['stable_id']==stable,'Actor stable identity splice')


def validate_construction_ledger(job,claim,events):
    """Pure consistency only. Actual artifact/source-byte admission is inspect()."""
    answer={'admission':'construction_boundaries','result':'MissingOrFailedAdmission',
            'qualified':False,'queue_success_enabled':False,'paid_resources_qualified':False,
            'whole_attempt_conservation':False}
    try:
        for k in ('candidate_sha256','build_sha256','setup_sha256','content_sha256'):sha(job[k])
        h=fingerprint(job);need(claim['job_sha256']==h,'Claim/job mismatch')
        string(claim['worker'],80);need(re.fullmatch('attempt-[a-f0-9]{32}',claim['attempt_id']), 'Invalid claim attempt')
        need(isinstance(events,list) and 0<len(events)<=MAX_ROWS,'Bounded native stream required')
        for seq,row in enumerate(events,1):
            exact(row,ENVELOPE);integer(row['schema_version'],1,1);integer(row['sequence'],seq,seq)
            need(row['job_sha256']==h and row['attempt_id']==claim['attempt_id'] and row['worker']==claim['worker'], 'Native attempt ownership/order')
            # Unity frame counters can reset across Editor/Play lifecycle transitions.
            integer(row['frame'])
            string(row['utc'],64);stamp=datetime.datetime.fromisoformat(row['utc'].replace('Z','+00:00'))
            need(stamp.tzinfo is not None,'Timezone-aware native timestamp required')
            string(row['kind'],128);need(isinstance(row['facts'],dict),'Native facts object required')
        def one(kind):
            found=[(n,r['facts']) for n,r in enumerate(events) if r['kind']==kind]
            need(len(found)==1,'Exactly one '+kind+' required');return found[0]
        ci,context=one('ContextCaptured');mi,menu=one('NormalMenuLaunchObserved')
        si,start=one('NativeConstructionLedgerStarted');ei,end=one('NativeConstructionLedgerClosed')
        ri,_=one('RestoreStarting');fi,_=one('ContextRestored')
        need(ci==0 and ci<si<mi<ei<ri<fi==len(events)-1,'Construction capture lifecycle incomplete/order differs')
        need(context['source_sha256']==job['candidate_sha256'] and context['compiled_dll_manifest_sha256']==job['build_sha256'],'Candidate/build splice')
        verify_job(job,menu['native_setup_identity'])
        session=string(menu['native_session_id'],128);world=string(menu['native_world_sequence'],20)
        need(re.fullmatch('[0-9]+',world) and int(world)<=18446744073709551615,'World sequence invalid');sha(menu['native_config_sha256'])
        exact(start,{'ledger_schema','scope','paid_resources_qualified'})
        exact(end,{'ledger_schema','scope','transactions','capture_failed','sink_owned','observer_faults','uncovered_operations','paid_resources_qualified'})
        for f in (start,end):
            integer(f['ledger_schema'],1,1);need(f['scope']==SCOPE and f['paid_resources_qualified']is False,'Unsupported construction scope')
        need(end['capture_failed']is False and end['sink_owned']is True,'Capture failed/unowned sink')
        integer(end['observer_faults'],0,0);integer(end['transactions'],0,4096)
        need(end['uncovered_operations']==UNCOVERED,'Partial-scope exclusions changed')
        need(not any('Construction' in r['kind'] and r['kind'] not in {'NativeConstructionLedgerStarted','NativeConstructionLedgerClosed','NativeConstructionTransaction'} for r in events), 'Construction failure/unknown event')
        rows=[(i,r['facts']) for i,r in enumerate(events) if r['kind']=='NativeConstructionTransaction']
        need(len(rows)==end['transactions'],'Construction count/truncation differs')
        owners={};banks={};session_entity=None;enemy=None;counts={op:0 for op in sorted(OPS)}
        for n,(i,f) in enumerate(rows,1):
            need(mi<i<ei,'Transaction outside native ledger window');exact(f,TRANSACTION)
            integer(f['ledger_schema'],1,1);integer(f['ledger_sequence'],n,n)
            need(f['scope']==SCOPE and f['paid_resources_qualified']is False and f['operation'] in OPS,'Unknown operation/scope')
            need(f['native_session_id']==session and f['native_world_sequence']==world and f['native_config_sha256']==menu['native_config_sha256'],'Transaction subject splice')
            verify_job(job,f['native_setup_identity'])
            for k in ('transaction_owner_sequence','transaction_id','credits_quoted','materials_quoted','committed_count','expected_count'):integer(f[k])
            for k in ('origin_x','origin_y'):integer(f[k],-I32-1)
            need(type(f['rotated'])is bool,'Typed rotation required')
            if f['prefab_name']is not None:string(f['prefab_name'],512)
            a,b=point(f['before'],session),point(f['after'],session)
            need(all(a[k]==b[k] for k in ('bank_index','bank_version','session_index','session_version')),'Bank/session entity changed inside boundary')
            se=(a['session_index'],a['session_version'])
            need(session_entity is None or session_entity==se,'Session entity splice');session_entity=se
            faction=a['economy']['FactionId'];be=(a['bank_index'],a['bank_version'])
            need(faction not in banks or banks[faction]==be,'Faction bank entity splice');banks[faction]=be
            need(len(set(banks.values()))==len(banks),'Shared player/enemy bank')
            op=f['operation'];counts[op]+=1
            expected=(a['economy'],a['bank'])
            if op.startswith('Player'):
                need(faction==1 and a['enemy_intent']is None and b['enemy_intent']is None and a['request']is None and b['request']is None,'Player ownership')
                owner=f['transaction_owner_sequence'];tid=f['transaction_id'];need(owner>0 and tid>0,'Player transaction owner/id')
                state=owners.setdefault(owner,{'last':0,'active':None})
                if op=='PlayerReserve':
                    need(state['active']is None and tid>state['last'],'Duplicate/unsettled player reserve')
                    state['active']={'id':tid,'credits':f['credits_quoted'],'materials':f['materials_quoted'],'request':None,'outcome':None}
                    expected=mutate(*expected,f['credits_quoted'],f['materials_quoted'])
                else:
                    active=state['active'];need(active is not None and active['id']==tid,'Player settle/request lacks owned reserve')
                    need((f['credits_quoted'],f['materials_quoted'])==(active['credits'],active['materials']),'Player quoted cost changed')
                    if op=='PlayerPlacementRequest':
                        need(active['request']is None and active['outcome']is None,'Duplicate/reordered placement request')
                        need(f['prefab_name']is not None and f['committed_count']==f['expected_count']==0,'Placement request shape')
                        active['request']=(f['prefab_name'],f['origin_x'],f['origin_y'],f['rotated'])
                    elif op=='PlayerPlacementOutcome':
                        need(active['request']==(f['prefab_name'],f['origin_x'],f['origin_y'],f['rotated']) and active['outcome']is None,'Placement outcome correlation')
                        need(f['committed_count']<=f['expected_count'],'Placement overcommit')
                        active['outcome']=f['committed_count']
                    else:
                        if active['request']is not None:
                            need(active['outcome']is not None,'Missing placement outcome')
                            need((active['outcome']>0)==(op=='PlayerFinalize'),'Partial commit must finalize; zero commit must refund')
                        if op=='PlayerRollback':expected=mutate(*expected,f['credits_quoted'],f['materials_quoted'],refund=True)
                        if op=='PlayerRollbackDeferred':
                            try:mutate(*expected,f['credits_quoted'],f['materials_quoted'],refund=True)
                            except ValueError:pass
                            else:need(False,'Deferred refund was possible')
                        else:state['last']=tid;state['active']=None
                if op not in ('PlayerPlacementRequest','PlayerPlacementOutcome'):
                    need(f['prefab_name']is None and f['origin_x']==f['origin_y']==0 and f['rotated']is False and f['committed_count']==f['expected_count']==0 and f['actor']is None,'Non-placement player metadata changed')
                if f['actor']is not None:
                    need(op=='PlayerPlacementOutcome' and f['committed_count']>0,'Unexpected player actor')
                    actor(f['actor'],session,1)
                need(a==b or op in ('PlayerReserve','PlayerRollback'),'Unexpected player point mutation')
            else:
                need(faction==2 and a['economy']['MaterialsOnlyConstruction']in (1,2) and f['transaction_owner_sequence']==0 and f['credits_quoted']==0 and f['rotated']is False,'Enemy owner/policy')
                x,y=a['enemy_intent'],b['enemy_intent'];need(y is not None,'Missing enemy intent')
                if enemy is not None:
                    need(x is not None,'Recorded enemy intent disappeared')
                    # CleanupAttempt can move PaidPending to RefundPending before
                    # Refund captures its boundary; all other exposed changes emit.
                    changed={k for k in INTENT if x[k]!=enemy[k]}
                    need(not changed or changed=={'phase'} and enemy['phase']==1 and x['phase']==4,
                         'Enemy history correlation/settlement differs')
                if op=='EnemyRequest':
                    need(x is None or x['action']!=y['action'],'Duplicate enemy action key')
                    debt=x['paid'] if x is not None and x['phase']==4 else 0
                    need(x is None or x['phase'] in (0,3,4,5),'Overlapping paid enemy request')
                    need(y['phase']==1 and y['paid']==f['materials_quoted']>0 and y['request_id']==f['transaction_id'] and y['result_stable_id']=='','Enemy debit intent')
                    need(f['prefab_name']==y['building'] and (f['origin_x'],f['origin_y'])==(y['origin_x'],y['origin_y']) and b['request']is not None and b['request']['status']==0,'Enemy enqueue witness missing')
                    expected=mutate(*expected,0,y['paid'])
                    if debt:expected=mutate(*expected,0,debt,refund=True)
                else:
                    need(x is not None and f['transaction_id']>0,'Enemy operation lacks paid intent')
                    changes={k for k in INTENT if x[k]!=y[k]}
                    if op=='EnemyRefund':
                        need(x['phase']==4 and x['paid']>0 and y['phase']==5 and y['paid']==0 and changes<= {'phase','paid','retry_after'} and f['materials_quoted']==0,'Enemy refund/debt settlement')
                        expected=mutate(*expected,0,x['paid'],refund=True)
                    elif op=='EnemyRefundDebt':
                        need(x['phase']in (1,2) and y['phase']==4 and changes=={'phase'} and f['materials_quoted']==x['paid'],'Enemy debt boundary')
                    elif op=='EnemyReenqueue':
                        need(x['phase']==y['phase']==1 and changes<= {'request_id'} and y['request_id']==f['transaction_id'] and f['materials_quoted']==x['paid'] and b['request']is not None and b['request']['status']==0,'Enemy reenqueue must preserve debit')
                    elif op=='EnemyDelivered':
                        need(x['phase']==1 and y['phase']==3 and changes=={'phase','result_stable_id'} and y['result_stable_id'] and f['committed_count']==f['expected_count']==1 and f['materials_quoted']==x['paid'],'Enemy delivery boundary')
                        need(a['request']is not None and a['request']['status']==1 and a['request']['runtime_id']>0 and a['request']==b['request'],'Native accepted request missing')
                        r=a['request'];need((r['actual_x'],r['actual_y'])==(x['origin_x'],x['origin_y']) and (r['actual_width'],r['actual_depth'])==(x['width'],x['depth']),'Native placement geometry differs')
                        need(f['prefab_name']==y['building'] and (f['origin_x'],f['origin_y'])==(y['origin_x'],y['origin_y']),'Delivered prefab/origin differs')
                        actor(f['actor'],session,2,y['building'],y['result_stable_id'])
                    if op in ('EnemyRefundDebt','EnemyRefund'):
                        need(a['request']==b['request'], 'Debt/refund rewrote native request')
                    if op!='EnemyReenqueue':need(f['transaction_id']==x['request_id']==y['request_id'],'Enemy request ID splice')
                    if op in ('EnemyRefund','EnemyReenqueue'):
                        need(f['prefab_name']is None and f['origin_x']==f['origin_y']==0,'Default enemy metadata differs')
                if op!='EnemyDelivered':need(f['actor']is None and f['committed_count']==f['expected_count']==0,'Unexpected enemy delivery metadata')
                enemy=copy.deepcopy(y)
            need((b['economy'],b['bank'])==expected,'Construction debit/refund/counter boundary differs')
        need(all(s['active']is None for s in owners.values()),'Unsettled player reservation at ledger closure')
        answer.update(result='ConsistentRecordedConstructionBoundaries',transactions=len(rows),operations=counts,
                      job_sha256=h,attempt_id=claim['attempt_id'],candidate_sha256=job['candidate_sha256'],
                      build_sha256=job['build_sha256'],native_session_id=session,native_world_sequence=world,
                      native_config_sha256=menu['native_config_sha256'],uncovered_operations=UNCOVERED,
                      enemy_outstanding_materials=enemy['paid'] if enemy and enemy['phase']in (1,2,4) else 0,
                      source_bytes_verified=False)
    except (ValueError,KeyError,TypeError,IndexError,AttributeError,OverflowError,RecursionError) as error:
        answer['errors']=[str(error)]
    return answer


def inspect(project,descriptor):
    reader=Reader(project)
    p=reader.path(descriptor)
    _,d=reader.obj({'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
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
    need(all(seen.get(p)==h for p,h in POLICY_PINS.items()),'Literal reviewed source/policy pins differ or missing')
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
    answer=validate_construction_ledger(job,claim,events)
    reader.stable()
    if answer['result']=='ConsistentRecordedConstructionBoundaries':
        answer.update(source_bytes_verified=True,build_bytes_verified=True,artifact_barrier='UnchangedPinnedArtifacts',reviewed_policy_baseline=BASELINE)
    return answer


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument('--descriptor',type=Path,required=True)
    args=parser.parse_args()
    try:answer=inspect(args.project,args.descriptor)
    except (ValueError,OSError,KeyError,TypeError,OverflowError,RecursionError) as error:
        answer={'admission':'construction_boundaries','result':'MissingOrFailedAdmission','errors':[str(error)],
                'qualified':False,'queue_success_enabled':False,'paid_resources_qualified':False}
    print(json.dumps(answer,indent=2,allow_nan=False))
    return 0 if answer['result']=='ConsistentRecordedConstructionBoundaries' else 1


if __name__=='__main__':raise SystemExit(main())
