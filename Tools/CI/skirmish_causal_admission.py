"""Strict v8 artifact/cursor admission. No simulation or computed-state bank.

Numeric execution and every native-state comparison remain retained-Mono jobs.
This module never turns a native request/balance into a derived result.
"""
import base64
import collections
import hashlib
import importlib.util
import math
from pathlib import Path
from types import SimpleNamespace
import re
import struct

IO_SOURCE = Path(__file__).parent/'skirmish_causal_io.py'
IO_SHA = '120464153c0f7338069e1880adeb57537c9414b865d7b6be8d46f6c144246732'
CONTRACT = Path(__file__).parent/'SkirmishOfflineSim/RetainedCausal/contracts/v8-row-contract.json'
CONTRACT_SHA = '50f1428c252e942b4a23aaf02f02bc6564d30bbe85fe65ab223240eaf072ae8f'
EVENT_LIMIT, BYTE_LIMIT, WORK_LIMIT = 131072, 2097152, 1500000
SIDECAR = 'continuous-causal-window-v8.json'

def need(ok, reason):
    if not ok:
        raise ValueError(reason)

def sha(raw):
    return hashlib.sha256(raw).hexdigest()

# Reuse the already-reviewed regular-file/identity/duplicate-JSON implementation.
# Import does not read a trace, call its legacy decoder, or run its CLI.
need(sha(IO_SOURCE.read_bytes()) == IO_SHA, 'Frozen evidence reader changed')
spec = importlib.util.spec_from_file_location('existing_native_admission_io', IO_SOURCE)
io = importlib.util.module_from_spec(spec)
spec.loader.exec_module(io)
read, parse = io.read, io.parse
contract_raw = read(CONTRACT, 131072)
need(sha(contract_raw) == CONTRACT_SHA, 'Frozen v8 row contract changed')
SCHEMA = parse(contract_raw)
# Explicit frozen diagnostic successor, never a self-declared cap from evidence.
DIAGNOSTIC_CONTRACT=Path(__file__).parent/'SkirmishOfflineSim/RetainedCausal/contracts/diagnostic-budget-contract.json'
DIAGNOSTIC_SHA='49d0fa6da851a8efbde8a536f9c1a2dbbef803c1fe032956adacd7624d457d2e'
diag_raw=read(DIAGNOSTIC_CONTRACT,65536)
need(sha(diag_raw)==DIAGNOSTIC_SHA,'Frozen diagnostic successor contract changed')
DIAG=parse(diag_raw)
RECORD_TIMING_SCOPE='Record-body-only; excludes pre-Record source reads/copies, legacy snapshots/serialization, final export and logging; not total observer overhead'
need(DIAG['sidecar_top_additions']['timing_measurement']=='exact string '+RECORD_TIMING_SCOPE,
     'Frozen diagnostic description/emitted literal relationship differs')
need(DIAG['pinned_byte_limit']==BYTE_LIMIT and DIAG['work_limit']==WORK_LIMIT and DIAG['global_event_limit']==EVENT_LIMIT,
     'Pinned diagnostic constants differ')
SCHEMA['reference_success']['diagnostic_byte_limit']='exact integer2097152'
SCHEMA['sidecar_top_required'] += ['diagnostic_budget_revision','record_callback_stopwatch_ticks','record_callback_measurement_calls','stopwatch_frequency']
SCHEMA['event_kinds']['window-start']['types']['byte_limit']='int32=2097152'
from skirmish_material_cursor import MaterialCursor, C as MATERIAL_CONTRACT, REVISION as MATERIAL_REVISION
SCHEMA['sidecar_top_required'] += list(MATERIAL_CONTRACT['top_level_additions'])
SCHEMA['event_kinds']['window-end']['required'] += ['terminal_material_ordinal']
SCHEMA['event_kinds']['window-end']['types']['terminal_material_ordinal']='int64'
for material_kind, material_schema in MATERIAL_CONTRACT['events'].items():
    SCHEMA['event_kinds'][material_kind]={'required':material_schema['fields'],'types':{}}



def exact(value, required, optional=()):
    need(type(value) is dict and set(required) <= set(value) <= set(required) | set(optional),
         'Unknown or missing fields: ' + str(required))

def integer(value, lo=0, hi=EVENT_LIMIT-1):
    need(type(value) is int and lo <= value <= hi, 'Exact bounded integer required')
    return value

def flag(value, expected=None):
    need(type(value) is bool and (expected is None or value is expected), 'Exact bool required')

def identity(value):
    exact(value, ('index', 'version'))
    return (integer(value['index'], 0, 2147483647), integer(value['version'], 1, 2147483647))

def fixed(value, allow_empty=False):
    need(type(value) is str and (0 if allow_empty else 1) <= len(value.encode('utf8')) <= 61, 'Lossless FixedString64 payload required')
    need(not any(0xd800 <= ord(c) <= 0xdfff for c in value), 'Unpaired surrogate rejected')
    return value

def f32(value):
    integer(value, 0, 0xffffffff)
    number = struct.unpack('<f', struct.pack('<I', value))[0]
    need(math.isfinite(number), 'Nonfinite float bits rejected')
    return number

def f64(value):
    integer(value, 0, 0xffffffffffffffff)
    number = struct.unpack('<d', struct.pack('<Q', value))[0]
    need(math.isfinite(number), 'Nonfinite double bits rejected')
    return number

def array(value, length=None, maximum=4096):
    need(type(value) is list and len(value) <= maximum and (length is None or len(value) == length),
         'Exact bounded array required')
    return value

def bits_tree(value, depth=0, budget=None):
    """Bound opaque component assertions; consumed fields get typed Mono checks.

    StorageBits has no floating JSON numbers. This is not a replacement for the
    pinned typed component importer or its field-width validation.
    """
    budget = [65536] if budget is None else budget
    budget[0] -= 1
    need(depth <= 16 and budget[0] >= 0, 'Bounded component assertion tree required')
    if value is None or type(value) is bool:
        return
    if type(value) is int:
        integer(value, -2147483648, 0xffffffffffffffff)
    elif type(value) is str:
        need(len(value.encode('utf8')) <= 2048, 'Bounded assertion string required')
    elif type(value) is list:
        array(value, maximum=4096)
        for item in value:
            bits_tree(item, depth+1, budget)
    elif type(value) is dict:
        need(len(value) <= 128 and all(type(k) is str and len(k) <= 128 for k in value), 'Bounded component fields required')
        for item in value.values():
            bits_tree(item, depth+1, budget)
    else:
        raise ValueError('Unknown StorageBits value kind')

def typed(value, description):
    # The pinned contract supplies field shapes. Only explicit scalar and fixed
    # vector spellings are interpreted here; compound schemas are checked below
    # or by the existing typed importer. Descriptions never execute code.
    if value is None and description.startswith('null|'):
        return
    if description.startswith('null|'):
        description = description[5:]
    vector = re.fullmatch(r'(uint32|int32)\[(\d+)\]', description)
    if vector:
        for v in array(value, int(vector[2])):
            integer(v, 0 if vector[1] == 'uint32' else -2147483648,
                    0xffffffff if vector[1] == 'uint32' else 2147483647)
        return
    m = re.match(r'^(uint8|uint32|uint64|int32|int64)(?:\b|=|<=)', description)
    if m:
        lo, hi = {'uint8': (0,255), 'uint32': (0,0xffffffff), 'uint64': (0,0xffffffffffffffff),
                  'int32': (-2147483648,2147483647), 'int64': (0,EVENT_LIMIT-1)}[m[1]]
        integer(value,lo,hi)
        equals = re.match(r'^\w+=(\d+)$', description)
        if equals:
            need(value == int(equals[1]), 'Source constant differs')
        ceiling = re.match(r'^\w+<=(\d+)$', description)
        if ceiling:
            need(value <= int(ceiling[1]), 'Source domain exceeded')
    elif description.startswith('bool'):
        flag(value, True if description == 'bool=true' else False if description == 'bool=false' else None)
    elif description.startswith('Entity{'):
        identity(value)
    elif description.startswith('losslessFixedString64'):
        fixed(value)
        if '=' in description:
            need(value == description.split('=',1)[1], 'Fixed identity differs')
    elif description.startswith('StorageBits'):
        bits_tree(value)
    elif description.startswith('Entity[]'):
        for e in array(value,maximum=16): identity(e)
    elif description.startswith('null|bool'):
        flag(value)
    elif '|' in description and all(re.fullmatch(r'[A-Za-z0-9_.-]+', x) for x in description.split('|')):
        need(value in description.split('|'), 'Unknown source enum spelling')
    elif description in ('v8-causal-window-only','first-observed-S002-before-production',
                          'BuildingResourceProductionEcsSystem.ApplyStorageQuery.before-production',
                          'Expanded+ResolvedSetup+EconomyStock+EnemyStock','SkirmishExpandedSessionComponent-only',
                          'TryResolveCachedInput.actual-resolver-expression','BuildingResourceStorageComponent defaultquery',
                          'entity8-storage57-profileflag1-profile28-le-v1'):
        need(value == description, 'Source encoding/context constant differs')

def shape_event(row, ordinal):
    need(type(row) is dict and row.get('observer_ordinal') == ordinal and type(row['observer_ordinal']) is int,
         'Dense shared canonical v8 ordinals required; no renumbering')
    kind = row.get('kind')
    need(kind in SCHEMA['event_kinds'], 'Unknown source event kind')
    schema = SCHEMA['event_kinds'][kind]
    if 'variants' in schema:
        need(row.get('provider_kind') in schema['variants'], 'Unknown provider variant')
        schema = schema['variants'][row['provider_kind']]
    exact(row, ['observer_ordinal','kind']+schema['required'], schema.get('optional',()))
    for key, description in schema['types'].items():
        need(row[key] is not None or key in schema.get('nullable',()), 'Unexpected null source field: '+key)
        if row[key] is not None:
            typed(row[key],description)
    return kind

STORAGE_FIELDS = ('RuntimeBuildingId','OwnerFactionId','OilStorageCapacity','FuelStorageCapacity',
 'OilBarrelsPerDay','FuelBarrelsPerDay','StoredOilBarrels','StoredFuelBarrels','ReservedOilInboundBarrels',
 'ReservedOilOutboundBarrels','ReservedFuelInboundBarrels','ReservedFuelOutboundBarrels',
 'CivilianFuelReserveBarrels','ReceivedHaulerFuelBarrels','Version')
PROFILE_FIELDS = ('PeriodSeconds','OilPerPeriod','FuelPerPeriod','OilPerFuelBatch','FuelPerBatch',
                  'FuelBatchSeconds','FuelBatchProgressSeconds')

def production_rows(row):
    result = {}
    for item in array(row['readonly_query_snapshot'],maximum=51):
        key = item['id']
        need(type(key) is str and re.fullmatch(r'(0|[1-9]\d*):[1-9]\d*',key), 'Canonical production ECS ID required')
        eid = tuple(int(x) for x in key.split(':'))
        identity(dict(zip(('index','version'),eid)))
        need(eid not in result, 'Duplicate production identity')
        s, p = item['storage'],item['profile']
        exact(s,STORAGE_FIELDS)
        integer(s['RuntimeBuildingId'],-2147483648,2147483647);integer(s['OwnerFactionId'],0,255)
        for field in ('OilStorageCapacity','FuelStorageCapacity'):integer(s[field],-2147483648,2147483647)
        integer(s['Version'],0,0xffffffff)
        for field in STORAGE_FIELDS[4:-1]:
            need(type(s[field]) in (int,float) and math.isfinite(s[field]), 'Finite actual production float required')
            struct.pack('<f',s[field])
        if p is not None:
            exact(p,PROFILE_FIELDS)
            for field in PROFILE_FIELDS:
                need(type(p[field]) in (int,float) and math.isfinite(p[field]), 'Finite actual production profile required')
                struct.pack('<f',p[field])
        result[eid] = p is not None
    need(result, 'Initial full bank missing')
    return result

def terminal_rows(row, initial):
    integer(row['count'],1,51);integer(row['wire_bytes'],1,4794)
    text = row['rows_base64']
    need(type(text) is str and len(text) == 4*((row['wire_bytes']+2)//3), 'Terminal canonical base64 size differs')
    raw = base64.b64decode(text,validate=True)
    need(len(raw) == row['wire_bytes'] and base64.b64encode(raw).decode('ascii') == text, 'Noncanonical terminal wire')
    at=0;result={}
    for _ in range(row['count']):
        need(at+66 <= len(raw), 'Truncated terminal row')
        index,version=struct.unpack_from('<ii',raw,at);eid=identity({'index':index,'version':version});at+=8
        storage=struct.unpack_from('<iBii10fI',raw,at);at+=57
        need(all(math.isfinite(x) for x in storage[4:-1]), 'Nonfinite terminal storage')
        presence=raw[at];at+=1;need(presence in (0,1), 'Unknown profile presence')
        if presence:
            need(at+28 <= len(raw),'Truncated terminal profile')
            need(all(math.isfinite(x) for x in struct.unpack_from('<7f',raw,at)), 'Nonfinite terminal profile');at+=28
        need(eid not in result and eid in initial and bool(presence) == initial[eid], 'Terminal domain/profile presence differs')
        result[eid]=raw[at-(94 if presence else 66):at]
    need(at == len(raw) and set(result) == set(initial), 'Terminal bank missing/new/trailing data')
    return result

def admit_files(trace_path, expected_trace_sha256, *, canonical_query_order_inputs=False):
    """Artifact integrity + cursor only. Caller must separately prove native
    terminal/reap, original source closure/baseline and typed replay admission.
    """
    trace_path=Path(trace_path).absolute()
    raw=read(trace_path,32*1024*1024);need(sha(raw)==expected_trace_sha256,'Raw legacy trace pin differs')
    legacy=parse(raw)
    need(legacy.get('capture_complete') is True and legacy.get('capture_failure') is None and
         legacy.get('storage_writer_failure') is None,'Legacy terminal data capture incomplete')
    ref=legacy.get('continuous_causal_window_v8')
    exact(ref,SCHEMA['reference_success'])
    need(ref['schema_version']==8 and type(ref['schema_version']) is int and ref['path']==SIDECAR and
         ref['ordinal_space']=='v8-causal-window-only' and ref['window_complete'] is True and ref['failure'] is None,
         'Complete fixed-basename v8 reference required')
    need(type(ref['diagnostic_byte_limit']) is int and ref['diagnostic_byte_limit']==BYTE_LIMIT,'Explicit pinned diagnostic reference cap required')
    integer(ref['bytes'],1,BYTE_LIMIT)
    for key,ceiling in [('charged_work',WORK_LIMIT),('events',EVENT_LIMIT),('sourcewriter_facts',512),
                        ('temporary_utf8_buffer_allocated_bytes',2147483647)]:integer(ref[key],0,ceiling)
    need(type(ref['sha256']) is str and re.fullmatch('[0-9a-f]{64}',ref['sha256']),'Exact sidecar hash required')
    side_path=trace_path.parent/SIDECAR
    side_raw=read(side_path,BYTE_LIMIT)
    need(len(side_raw)==ref['bytes'] and sha(side_raw)==ref['sha256'],'Sidecar length/hash differs')
    window=parse(side_raw)
    result=admit_window(window,legacy,canonical_query_order_inputs=canonical_query_order_inputs)
    integer(ref['legacy_early_causal_utf8_bytes'],0,BYTE_LIMIT)
    integer(ref['aggregate_causal_actual_utf8_bytes'],1,BYTE_LIMIT)
    need(ref['aggregate_causal_actual_utf8_bytes']==len(side_raw)+ref['legacy_early_causal_utf8_bytes'],
         'One aggregate causal byte ledger required')
    need(ref['aggregate_causal_actual_utf8_bytes']<=window['aggregate_causal_charged_utf8_bytes_including_header_reserve'],
         'Actual aggregate exceeds source charged ledger including header reservation')
    for rk,wk in [('charged_work','charged_work'),('events',None),('sourcewriter_facts','sourcewriter_facts'),
                  ('legacy_early_causal_utf8_bytes','legacy_early_causal_pretty_utf8_bytes_including_indent_separator')]:
        need(ref[rk] == (len(window['events']) if wk is None else window[wk]),'Reference summary differs')
    # Export serializes the artifact, then allocates final sidecar raw bytes,
    # increments its counter and builds the reference. The artifact counter is
    # therefore exactly one final UTF8 buffer earlier than the reference.
    need(ref['temporary_utf8_buffer_allocated_bytes']==window['temporary_utf8_buffer_allocated_bytes']+len(side_raw),
         'Reference final UTF8 buffer allocation accounting differs')
    need(read(trace_path,32*1024*1024)==raw and read(side_path,BYTE_LIMIT)==side_raw,'Evidence changed during admission')
    result.update(trace_sha256=sha(raw),sidecar_sha256=sha(side_raw),sidecar_bytes=len(side_raw),
                  aggregate_causal_bytes=ref['aggregate_causal_actual_utf8_bytes'])
    return result

def admit_window(window,legacy, *, canonical_query_order_inputs=False):
    flag(canonical_query_order_inputs)
    exact(window,SCHEMA['sidecar_top_required'])
    need(type(window['schema_version']) is int and window['schema_version']==8 and
         window['ordinal_space']=='v8-causal-window-only','Explicit v8 space required')
    need(window['diagnostic_budget_revision']==DIAG['revision'],'Explicit pinned diagnostic revision required')
    integer(window['record_callback_stopwatch_ticks'],0,0x7fffffffffffffff)
    integer(window['record_callback_measurement_calls'],0,EVENT_LIMIT)
    integer(window['stopwatch_frequency'],1,0x7fffffffffffffff)
    need(window['timing_measurement']==RECORD_TIMING_SCOPE,'Exact partial Record timing scope required')
    for key in ('legacy_v7_space_distinct','start_observed','end_observed','window_complete'):flag(window[key],True)
    need(window['failure'] is None,'Failed window cannot be admitted')
    for key,limit in [('work_limit',WORK_LIMIT),('byte_limit',BYTE_LIMIT),('global_event_limit',EVENT_LIMIT)]:
        need(type(window[key]) is int and window[key]==limit,'V8 limit differs')
    for key,limit in [('charged_work',WORK_LIMIT),('aggregate_causal_charged_utf8_bytes_including_header_reserve',BYTE_LIMIT),
                      ('legacy_early_causal_pretty_utf8_bytes_including_indent_separator',BYTE_LIMIT),
                      ('sourcewriter_facts',512),('owner_loops',128),('dispatches',16),('fuel_calls',32),('provider_events',512),
                      ('root_rows_including_end_header',40),('end_header_reserved_bytes',BYTE_LIMIT),
                      ('temporary_utf8_buffer_allocated_bytes',2147483647)]:integer(window[key],0,limit)
    need(window['end_header_reserved_bytes']==16384,'Exact source end-header reservation required')
    need(type(window['managed_object_allocations_measurement']) is str and
         type(window['timing_measurement']) is str and
         len(window['managed_object_allocations_measurement'])<=1024 and len(window['timing_measurement'])<=1024,
         'Explicit bounded allocation/timing scope required')
    rows=array(window['events'],maximum=EVENT_LIMIT)
    need(len(rows)>=10,'Complete source window missing')
    need(window['record_callback_measurement_calls']==len(rows),'Successful window must measure each Record exactly once')
    counts=collections.Counter()
    start=None;roots=None;bank=None;owner=None;dispatch=None;selection=None;sync=None;status=None;fuel=None
    owner_n=dispatch_n=selection_n=fuel_n=provider_n=production_n=0
    production_open=None;unit_domain=None;first_haulers=None;first_candidates=None
    bindings={}
    calls=[];call_meta={};root_entry=None;root_exit=None;certificate=None;terminal=None;ended=False
    legacy_writers=[];legacy_productions=[];last_owner_after=None
    initial_legacy=legacy.get('initial_hauler_dispatch')
    need(type(initial_legacy) is dict and initial_legacy.get('census_complete') is True,'Initial raw caller census missing')
    initial_candidates=initial_legacy.get('full_runtime_buildings_enumeration')
    need(type(initial_candidates) is list and len(initial_candidates)<=18,'Complete initial candidate input missing')
    literal_candidate_ref='legacy.initial_hauler_dispatch.full_runtime_buildings_enumeration (component/static inputs only; never legacy provider scope)'
    production_history=legacy.get('production_boundary_history')
    writer_history=legacy.get('storage_writer_journal')
    need(type(production_history) is list and type(writer_history) is list,'Referenced legacy arrays missing')
    need(production_history,'Genuine initial BEFORE bank required for material seed identity')
    material=MaterialCursor(SimpleNamespace(need=need,identity=identity,integer=integer,f32=f32,flag=flag,array=array,exact=exact),production_rows(production_history[0]))

    def session(row):
        if 'session_entity' in row:need(identity(row['session_entity'])==identity(start['session_entity']),'Session entity changed')
        if 'session_id' in row:need(row['session_id']==start['session_id'],'Session identity changed')

    def binding(number,runtime_id,entity):
        integer(number,1,18);integer(runtime_id,-2147483648,2147483647)
        value=(runtime_id,identity(entity))
        if number in bindings:need(bindings[number]==value,'Runtime binding identity changed')
        else:bindings[number]=value

    def scope(row):
        need(dispatch is not None and row['dispatch_id']==dispatch['dispatch_id'],'Source dispatch scope differs')
        need(selection is not None and row['selection_id']==selection['selection_id'] and
             identity(row['unit'])==identity(selection['unit']),'Source selector owner differs')

    for ordinal,row in enumerate(rows):
        need(not ended,'Events after complete window end')
        kind=shape_event(row,ordinal);counts[kind]+=1
        if start is not None:session(row)
        if production_open is not None:need(kind=='production' and row.get('boundary')=='after-production','Event interleaved inside synchronous production body')
        if fuel is not None:need(kind.startswith('fuel-'),'Event interleaved inside synchronous fuel body')
        if sync is not None:need(kind=='sync-after','Event interrupted synchronous Sync pre/post')
        if status is not None:need(kind=='status-after-write-assertion','Event interrupted synchronous status pre/post')
        if calls:need(kind=='storage-writer','Event interrupted original writer calltree')
        if root_exit is not None:
            need(kind in ('terminal-material-subsystem-assertion','terminal-full-bank-assertion','window-end'),'Gameplay observation after chosen ROOT EXIT')
        if material.frame is not None or material.ingress is not None:
            need(kind in MATERIAL_CONTRACT['events'],'Event interrupted synchronous material source scope')
        if kind in MATERIAL_CONTRACT['events']:
            ordinary=(bank is not None and owner is None and dispatch is None and selection is None and fuel is None and production_open is None and sync is None and status is None and not calls)
            material.consume(row,ordinary_outer=ordinary,seed_frontier=(ordinal==2 and roots is not None and bank is None),root_exit=None if root_exit is None else root_exit['observer_ordinal'])
            continue
        if kind=='window-start':
            need(ordinal==0 and start is None,'Window start must be first/unique')
            start=row;need(row['phase']==5 and row['is_legacy']==0 and row['initialization_complete']==1 and
                          row['spawn_complete']==1,'Actual initialized Playing S002 required; phase5 from pinned contract')
        elif kind=='initial-broad-expanded-sessions':
            need(start is not None and roots is None and bank is None,'Initial broad census order differs')
            roots={}
            for item in array(row['rows'],maximum=16):
                exact(item,('entity','SessionId','IsLegacy'));eid=identity(item['entity'])
                need(eid not in roots,'Duplicate broad root');fixed(item['SessionId'],allow_empty=True);integer(item['IsLegacy'],0,255)
                roots[eid]=item
            need(identity(start['session_entity']) in roots and
                 roots[identity(start['session_entity'])]['SessionId']==start['session_id'], 'Observed session absent from broad census')
        elif kind=='production':
            need(material.fabs is not None,'Genuine material seed must precede first original production')
            need(roots is not None and owner is None and fuel is None and dispatch is None,'Production interleaving unsupported')
            i=integer(row['legacy_production_history_index'],0,len(production_history)-1)
            need(i==len(legacy_productions),'First/complete original production prefix required')
            original=production_history[i]
            need(type(original) is dict and original['observer_ordinal']==row['legacy_observer_ordinal'] and
                 original['stamp']['boundary']==row['boundary'],'Production reference/ordinal differs')
            stamp=original['stamp'];session(stamp)
            need(stamp['session_phase']=='Playing' and stamp['engine_frame']==row['frame'] and
                 original.get('actual_loop_order_proven') is False,'Original unordered production stamp required')
            for key in ('seconds_per_day_bits','delta_time_bits','oil_ratio_bits'):
                need((original[key]&0xffffffff)==row[key],'Original production scalar differs');f32(row[key])
            membership=production_rows(original)
            if bank is None:
                need(i==0 and row['boundary']=='before-production' and row['snapshot_role']=='initial-before-input',
                     'Genuine first BEFORE bank input required')
                bank=membership
            else:
                need(membership==bank and row['snapshot_role']=='assertion-only-against-carried-bank',
                     'Later native bank must be same-domain assertions only')
            if row['boundary']=='before-production':
                need(production_open is None and production_n<4,'Production nesting/bound differs')
                production_open=row;production_n+=1
            else:
                need(production_open is not None,'Production AFTER without BEFORE')
                for key in ('session_entity','seconds_per_day_bits','delta_time_bits','oil_ratio_bits','frame'):
                    need(row[key]==production_open[key],'Production pair invocation differs')
                production_open=None
            legacy_productions.append(i)
        elif kind.startswith('owner-hauler-'):
            need(bank is not None and production_open is None and fuel is None,'Owner starts outside valid boundary')
            now=f64(row['realtime_now_bits']);need(0<=now<=1e9,'Owner clock outside admitted range')
            f64(row['before_due_bits']);f64(row['after_due_bits'])
            if kind=='owner-hauler-check-before':
                need(owner is None and dispatch is None and selection is None,'Nested owner loop')
                owner_n+=1;need(row['owner_loop']==owner_n<=128,'Actual owner order/bound differs')
                need(row['due_role']==('initial-private-input' if owner_n==1 else 'assertion-derived-owner-private'),
                     'Only first BEFORE may initialize private owner clock')
                need(row['before_due_bits']==row['after_due_bits'],'Owner BEFORE already changed clock')
                if last_owner_after is not None:need(row['before_due_bits']==last_owner_after,'Native private clock carry differs')
                owner={'before':row,'invoke':None,'dispatch_seen':False}
            else:
                need(owner is not None and row['owner_loop']==owner_n and
                     row['due_role']=='assertion-derived-owner-private','Owner invoke/post missing matching BEFORE')
                for key in ('realtime_now_bits','before_due_bits','disabled','callback_present','frame'):
                    need(row[key]==owner['before'][key],'Owner borrowed argument context differs')
                if kind=='owner-hauler-invoke':
                    need(owner['invoke'] is None and row['disabled'] is False,'Duplicate/disabled owner invoke')
                    owner['invoke']=row
                else:
                    need(dispatch is None and selection is None and sync is None,'Owner returned before child scope closed')
                    expected=owner['invoke']['after_due_bits'] if owner['invoke'] else owner['before']['before_due_bits']
                    need(row['after_due_bits']==expected,'Owner native post clock carry differs')
                    need(owner['dispatch_seen']==bool(owner['invoke'] and row['callback_present']),
                         'Missing/extra actual owner callback dispatch')
                    last_owner_after=row['after_due_bits'];owner=None
        elif kind=='bridge-dispatch-before':
            need(owner is not None and owner['invoke'] is not None and owner['invoke']['callback_present'] and
                 not owner['dispatch_seen'] and dispatch is None,'Dispatch not linked to actual owner invoke')
            dispatch_n+=1;need(row['dispatch_id']==dispatch_n<=16 and row['owner_loop']==owner_n,'Dispatch ID/order differs')
            need(row['private_state_role']==('initial-input' if dispatch_n==1 else 'assertion-only'),
                 'Later caller private values must remain assertions')
            need(f32(row['now_argument_bits'])>=0,'Actual independent Bridge Time.time required')
            need(row['candidate_static_initial_input_ref']==literal_candidate_ref,'Unknown candidate reference')
            partition=row['field_partition']
            exact(partition,('static_exact_guard','exogenous_before_inputs','computed_or_outside_pre_pair_reads_assertions',
                             'presence_only_guard','UnitMove_values','candidate_static','candidate_state_assertions'))
            need(partition['UnitMove_values']=='outside supported pre-pair reads; assertions only' and
                 partition['candidate_static']=='all fields except listed state paths; fabrication recipe/owner exact guard; profile constants exact guard; storage metadata exact guard' and
                 partition['candidate_state_assertions']==['storage.Stored*/Reserved*/CivilianFuelReserve/ReceivedHaulerFuel/Version',
                 'production_profile.FuelBatchProgressSeconds','fabrication.ProductionEnabled/CycleProgressSeconds/Status/BlockReason/Version'],
                 'Candidate/UnitMove field-role contract differs')
            for key,source in [('static_exact_guard','static_exact'),('exogenous_before_inputs','exogenous_before'),
                               ('computed_or_outside_pre_pair_reads_assertions','assertion_or_unused_pre_pair'),
                               ('presence_only_guard','presence_only')]:
                need(partition.get(key)==SCHEMA['hauler_partition'][source],'Caller field role changed')
            hs=array(row['hauler_order_and_observed_components'],maximum=8)
            order=[]
            expected_h=set(sum(SCHEMA['hauler_partition'].values(),[]))
            for i,h in enumerate(hs):
                exact(h,expected_h);need(h['query_ordinal']==i,'Actual hauler query order lost')
                eid=identity(h['entity']);need(eid not in order,'Duplicate hauler');order.append(eid)
                for key,value in h.items():
                    if key not in ('query_ordinal','entity'):bits_tree(value)
            need(order,'Complete hauler census missing')
            if first_haulers is None:
                first_haulers=order
                need(hs==initial_legacy['full_hauler_query_order_and_components'],'First caller census reference differs')
            else:
                if canonical_query_order_inputs:
                    need(len(order)==len(first_haulers) and set(order)==set(first_haulers),
                         'Canonical query permutation changed complete Entity/version domain')
                else:
                    need(order==first_haulers,'Changed query order unsupported by retained caller')
                # Static guards follow Entity identity; current source query order
                # is retained verbatim, never sorted or used to reseed storage.
                initial_by_entity={identity(h['entity']):h for h in initial_legacy['full_hauler_query_order_and_components']}
                for h in hs:
                    initial=initial_by_entity[identity(h['entity'])]
                    need(h['UnitMove'] is not None and all(h[k]==initial[k] for k in SCHEMA['hauler_partition']['static_exact']),
                         'Changed static hauler field/presence')
            cs=array(row['candidate_order_and_observed_components'],maximum=18)
            candidate_order=[]
            for i,c in enumerate(cs):
                exact(c,('enumeration_ordinal','dictionary_key','runtime_id','combat_entity','runtime_binding_ref',
                         'static_equals_first_dispatch_guard','storage_assertion','profile_assertion','fabrication_assertion'))
                need(c['enumeration_ordinal']==i,'Candidate order lost');integer(c['dictionary_key'],-2147483648,2147483647)
                flag(c['static_equals_first_dispatch_guard'],True)
                original=initial_candidates[i]
                need(c['dictionary_key']==original['dictionary_key'],'Candidate binding order changed')
                null=original.get('runtime','not-null') is None
                need((c['runtime_id'] is None)==null and (c['combat_entity'] is None)==null,'Candidate presence differs')
                if null:need(c['runtime_binding_ref'] is None,'Null candidate cannot acquire runtime binding')
                if not null:
                    need(c['runtime_id']==original['runtime_id'] and identity(c['combat_entity'])==identity(original['combat_entity']),
                         'Candidate ECS/runtime identity changed');binding(c['runtime_binding_ref'],c['runtime_id'],c['combat_entity'])
                for k in ('storage_assertion','profile_assertion','fabrication_assertion'):bits_tree(c[k])
                candidate_order.append((c['dictionary_key'],c['runtime_id'],c['combat_entity'],c['runtime_binding_ref']))
            need(len(cs)==len(initial_candidates),'Candidate membership incomplete')
            if first_candidates is None:first_candidates=candidate_order
            else:need(first_candidates==candidate_order,'Candidate domain/order changed')
            owner['dispatch_seen']=True;dispatch=row
        elif kind=='bridge-dispatch-after':
            need(dispatch is not None and selection is None and not calls and sync is None and
                 row['dispatch_id']==dispatch_n and row['owner_loop']==owner_n,'Dispatch post without closed children')
            dispatch=None
        elif kind=='selector-entry':
            need(dispatch is not None and selection is None,'Selector outside actual dispatch')
            selection_n+=1;need(row['selection_id']==selection_n<=128 and row['dispatch_id']==dispatch_n and
                               row['entry_ordinal']==ordinal and row['result_assertion'] is None and
                               identity(row['unit']) in first_haulers,'Actual selector entry/owner differs')
            selection=row;provider_n+=1
        elif kind=='selector-exit':
            scope(row);need(row['entry_ordinal']==selection['entry_ordinal'] and row['result_assertion'] is False and
                            status is None and not calls,'Earlier false selector scope incomplete')
            selection=None;provider_n+=1
        elif kind=='selector-provider':
            scope(row);provider_n+=1
            if row['provider_kind']=='focus-return':
                need(row['callsite'] in {'automatic-route.focus-'+str(i) for i in range(5)},'Unknown focus expression')
                integer(row['runtime_id'],-2147483648,2147483647)
                need(row['runtime_id'] in {c[1] for c in first_candidates},'Focus outside candidate domain')
                for v in row['position_bits']:f32(v)
            else:
                vals=array(row['input'],5)
                for i,v in enumerate(vals):integer(v,0 if i==3 else -2147483648,0xffffffff if i==3 else 2147483647)
                f32(vals[3])
        elif kind=='broad-root-use-guard':
            need(selection is not None and row['dispatch_id']==dispatch_n and row['selection_id']==selection_n,
                 'Broad query not in actual selector')
            ids=[identity(e) for e in row['exact_order']]
            need(len(ids)==len(set(ids)) and set(ids)==set(roots),'Actual broad query membership differs')
        elif kind.startswith('status-'):
            scope(row);exact(row['prepared_status_assertion'],('StatusCode','ReasonCode','ResourceKind'))
            for v in row['prepared_status_assertion'].values():integer(v,0,255)
            if kind=='status-before-write-assertion':need(status is None,'Nested status write');status=row
            else:
                need(status is not None and all(row[k]==status[k] for k in row if k not in ('kind','observer_ordinal')),
                     'Status post identity/assertion mismatch');status=None
        elif kind.startswith('sync-'):
            need((owner is None or (owner['invoke'] is not None and owner['dispatch_seen'])) and
                 dispatch is None and selection is None and production_open is None and fuel is None,
                 'Sync must be independent or post-bridge in invoked owner; no caller/selector/production/fuel interleaving')
            need(row['dispatch_id']==dispatch_n and row['production_call_id']==production_n,'Sync source context differs')
            binding(row['runtime_binding_ref'],row['runtime_id'],row['target_entity'])
            exact(row['runtime_metadata_inputs'],('owner','oil_capacity','fuel_capacity','oil_rate_bits','fuel_rate_bits'))
            for k,v in row['runtime_metadata_inputs'].items():
                integer(v,0 if k=='owner' or k.endswith('_bits') else -2147483648,
                        255 if k=='owner' else 0xffffffff if k.endswith('_bits') else 2147483647)
            if kind=='sync-before':need(sync is None and row['pre_ordinal']==ordinal,'Nested/missing Sync pre');sync=row
            else:
                need(sync is not None and row['pre_ordinal']==sync['observer_ordinal'] and
                     all(row[k]==sync[k] for k in ('target_entity','runtime_binding_ref','runtime_id','runtime_metadata_inputs',
                         'before_storage_assertion','prepared_storage_assertion','profile_assertion')),'Sync post/binding differs')
                sync=None
        elif kind=='fuel-before-ground-fuel-consumer':
            need(bank is not None and roots is not None and owner is None and dispatch is None and fuel is None and production_open is None,'Nested/uninitialized fuel/owner stage')
            fuel_n+=1;need(row['fuel_call_id']==fuel_n<=32,'Fuel call order differs')
            fuel={'before':row,'units':[],'unit_ids':set(),'requested':None,'visits':{},'last_visit':None,'last_unit':None,'posts':set()}
        elif kind=='fuel-unit-before':
            need(fuel is not None and fuel['requested'] is None and row['fuel_call_id']==fuel_n,'Fuel unit outside source query')
            eid=identity(row['entity']);need(eid not in fuel['unit_ids'] and row['query_index']==len(fuel['units'])<34,'Fuel query order/uniqueness differs')
            flag(row['initial_state_input'],fuel_n==1)
            need(row['fuel_state_semantics']==('initial-before-input' if fuel_n==1 else 'assertion-only-carried-state'),
                 'Later native fuel state cannot be initialized')
            a=array(row['observed_state'],3);integer(a[0],-2147483648,2147483647);integer(a[1],-2147483648,2147483647);integer(a[2],0,255)
            if fuel_n==1:need(row['static_input_delta'] is not None,'Initial five component fuel inputs missing')
            if row['static_input_delta'] is not None:
                a=array(row['static_input_delta'],6)
                for v in a[:3]:integer(v,0,255)
                f32(a[3]);f32(a[4]);flag(a[5]);need(a[5]==row['air'],'Air presence delta differs')
            if row['air_position_bits'] is not None:
                need(row['air'] and row['transform_used_presence'],'Unused air transform overclaim')
                for v in array(row['air_position_bits'],3):f32(v)
            else:need(not(row['air'] and row['transform_used_presence']),'Used raw air position omitted')
            fuel['units'].append(eid);fuel['unit_ids'].add(eid);fuel['last_unit']=eid
        elif kind=='fuel-state-post-assertion':
            need(fuel is not None and fuel['requested'] is None and row['fuel_call_id']==fuel_n and
                 identity(row['entity'])==fuel['last_unit'] and fuel['last_unit'] not in fuel['posts'],
                 'Fuel state post missing/duplicate actual row')
            fuel['posts'].add(fuel['last_unit'])
        elif kind=='fuel-requested-assertions':
            need(fuel is not None and fuel['requested'] is None and row['fuel_call_id']==fuel_n,'Duplicate/outside requested assertion')
            for v in row['bits_assertion']:need(f32(v)>=0,'Negative native request assertion')
            fuel['requested']=row['bits_assertion']
        elif kind=='fuel-drain-visit':
            need(fuel is not None and fuel['requested'] is not None and row['fuel_call_id']==fuel_n,'Drain before computed-query boundary')
            faction=integer(row['faction'],0,255);need(f32(fuel['requested'][faction])>0 and f32(row['remaining_before_bits_assertion'])>0,
                                                   'Native drain inconsistent with its assertion vector')
            need(not fuel['visits'] or faction>=max(fuel['visits']),'Native faction loop order regressed')
            visits=fuel['visits'].setdefault(faction,[]);eid=identity(row['entity'])
            need(eid in bank and eid not in visits and row['visit_index']==len(visits),'Drain prefix identity/order differs')
            visits.append(eid);fuel['last_visit']=row
        elif kind=='fuel-drain-post':
            need(fuel is not None and fuel['last_visit'] is rows[ordinal-1] and row['fuel_call_id']==fuel_n and
                 row['entity']==fuel['last_visit']['entity'] and row['faction']==fuel['last_visit']['faction'],
                 'Drain post not linked to immediate actual visit')
            need(f32(row['remaining_after_bits_assertion'])>=0,'Negative remaining native assertion');f32(row['fuel_balance_bits_assertion'])
        elif kind=='fuel-after-ground-fuel-consumer':
            need(fuel is not None and fuel['requested'] is not None and row['fuel_call_id']==fuel_n,'Fuel AFTER missing complete preceding query')
            for key in row:
                if key not in ('kind','observer_ordinal'):need(row[key]==fuel['before'][key],'Fuel invocation context changed')
            if unit_domain is None:unit_domain=fuel['unit_ids']
            else:need(unit_domain==fuel['unit_ids'],'Fuel query birth/death unsupported')
            fuel=None
        elif kind=='storage-writer':
            scope(row);need(fuel is None and sync is None and status is None,'Writer nested in unclosed independent mutation')
            i=integer(row['legacy_writer_index'],0,len(writer_history)-1)
            need(i==len(legacy_writers),'All original first-root writer rows must be retained')
            original=writer_history[i]
            for k in ('invocation_id','parent_invocation_id','operation','stage'):need(original[k]==row[k],'Original writer reference differs')
            need(identity(original['unit']['entity'])==identity(row['unit']),'Original writer unit differs');session(original['stamp'])
            need(original['stamp']['session_phase']=='Playing','Writer left actual Playing phase')
            inv=row['invocation_id'];parent=row['parent_invocation_id']
            integer(inv,1,512)
            if parent is not None:integer(parent,1,512)
            need(row['operation'] in ('ReservePair','TryReserveSource','TryReserveDestination','ReleaseSourceReservation') and
                 row['stage'] in ('entry','exit','raw-ecs-before-sync','synchronized-local','before-ecs-commit','after-ecs-commit',
                 'before-runtime-mirror','after-runtime-mirror','before-oil-mirror-write','after-oil-mirror-write',
                 'before-fuel-mirror-write','after-fuel-mirror-write'),'Unsupported source writer stage/operation')
            if row['stage']=='entry':
                need(inv not in call_meta and parent==(calls[-1] if calls else None),'Writer parent/calltree differs')
                if not calls:
                    need(root_entry is None and row['operation']=='ReservePair','Unknown earlier root writer')
                    h=next(h for h in dispatch['hauler_order_and_observed_components'] if identity(h['entity'])==identity(row['unit']))
                    own=h['attempt_owned'];need(type(own) is dict and own['SessionId']==start['session_id'] and own['CapacityReleased']==0,
                                               'First root not actual active attempt-owned unit')
                    root_entry=row
                else:need(row['operation']!='ReservePair','Nested root writer unsupported')
                calls.append(inv);call_meta[inv]=row
            else:
                need(calls and calls[-1]==inv and parent==call_meta[inv]['parent_invocation_id'] and
                     row['operation']==call_meta[inv]['operation'],'Writer event lost active call identity')
                if row['stage']=='exit':
                    calls.pop()
                    if not calls:
                        need(inv==root_entry['invocation_id'] and 'window_end_certificate' in row,'Root exit certificate missing')
                        root_exit=row;certificate=row['window_end_certificate']
                        exact(certificate,SCHEMA['root_exit_certificate'])
                        for k in ('terminal_owner_after_absent','terminal_selector_exit_absent','prior_selector_scopes_closed',
                                  'all_child_calls_closed','provider_prefix_complete_through_root_exit'):flag(certificate[k],True)
                        need(owner is not None and owner['invoke'] is not None,'Terminal open owner source invoke missing')
                        expected={'session_entity':start['session_entity'],'session_id':start['session_id'],
                            'dispatch_id':dispatch_n,'selection_id':selection_n,'selection_entry_ordinal':selection['observer_ordinal'],
                            'owner_loop':owner_n,'owner_pre_ordinal':owner['before']['observer_ordinal'],
                            'owner_invoke_ordinal':owner['invoke']['observer_ordinal'],'unit':selection['unit'],
                            'root_invocation_id':root_entry['invocation_id'],'root_entry_ordinal':root_entry['observer_ordinal'],
                            'root_exit_ordinal':ordinal}
                        need(all(certificate[k]==v for k,v in expected.items()),'Certificate not linked to complete admitted cursor')
            if root_exit is None:need('window_end_certificate' not in row,'Future certificate before actual root exit')
            legacy_writers.append(i)
        elif kind=='terminal-full-bank-assertion':
            need(root_exit is not None and terminal is None and material.terminal==root_exit['observer_ordinal']+1 and ordinal==root_exit['observer_ordinal']+2 and
                 row['root_exit_ordinal']==root_exit['observer_ordinal'],'Terminal census not at chosen root frontier')
            terminal_rows(row,bank);terminal=row
        elif kind=='window-end':
            need(terminal is not None and ordinal==terminal['observer_ordinal']+1 and ordinal==len(rows)-1 and
                 row['terminal_bank_ordinal']==terminal['observer_ordinal'] and not calls and sync is None and status is None and
                 fuel is None and production_open is None,'Incomplete terminal source window')
            for k in ('terminal_selector_exit_absent','terminal_owner_after_absent','all_prior_scopes_closed','all_root_children_closed'):flag(row[k],True)
            for k in ('root_exit_ordinal','root_invocation_id','session_entity','session_id','dispatch_id','selection_id',
                       'selection_entry_ordinal','unit','owner_loop','owner_pre_ordinal','owner_invoke_ordinal'):
                need(row[k]==certificate[k],'Window end identity differs from actual root certificate')
            ended=True
    need(ended and root_exit is not None and terminal is not None and selection is not None and dispatch is not None and owner is not None,
         'Exactly terminal open selector/dispatch/owner required')
    need(window['source_event_counts']==dict(counts) and all(type(n) is int and n>0 for n in window['source_event_counts'].values()),
         'Source kind counters differ from entire admitted cursor')
    for key,value in [('owner_loops',owner_n),('dispatches',dispatch_n),('fuel_calls',fuel_n),('provider_events',provider_n),
                      ('sourcewriter_facts',len(legacy_writers)),('root_rows_including_end_header',len(legacy_writers))]:
        need(window[key]==value,'Source summary count differs: '+key)
    need(provider_n<=512 and len(legacy_writers)<=40,'Independent source provider/root bounds exceeded')
    result={'result':'V8StructuralCursorAdmitted_TypedReplayPending','schema_version':8,'global_max_exclusive':EVENT_LIMIT,
            'diagnostic_budget_revision':DIAG['revision'],'record_callback_stopwatch_ticks':window['record_callback_stopwatch_ticks'],
            'record_callback_measurement_calls':window['record_callback_measurement_calls'],'stopwatch_frequency':window['stopwatch_frequency'],
            'timing_scope':window['timing_measurement'],'events':len(rows),'event_counts':dict(counts),'initial_store_count':len(bank),'owner_loops':owner_n,
            'dispatches':dispatch_n,'fuel_calls':fuel_n,'provider_events':provider_n,'root_exit_ordinal':root_exit['observer_ordinal'],
            'terminal_bank_ordinal':terminal['observer_ordinal'],'terminal_selector_open':True,'terminal_owner_open':True,
            'canonical_query_order_input':canonical_query_order_inputs,'query_order_provenance':'source-observed full per-dispatch Entity/version order' if canonical_query_order_inputs else 'fixed initial order',
            'standalone_ecs_query_order_derived':False,
            'terminal_bank_is_assertion_only':True,'native_request_imported_to_generator':False,'numeric_execution':False,
            'typed_replay_admitted':False,'computed_final_bank_matched':False,'full_frame_derived':False,'mission_qualified':False}
    result.update(material.finish(window,rows[-1]))
    return result
