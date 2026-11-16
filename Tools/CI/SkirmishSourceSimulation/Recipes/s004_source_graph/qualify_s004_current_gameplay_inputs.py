#!/usr/bin/env python3
"""Join current S004 factory, prefab and blocker captures without admission."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
PAIR = ROOT / 'T Logs/SkirmishProduction/s004-current-factory-pair-20261105-T1/package.json'
SHARED = ROOT / 'Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-r1/data/prefab-reference-data.json'
SHARED_AUDIT = ROOT / 'T Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-T1/independent-audit.json'
MISSING = ROOT / 'Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-r1/data/prefab-reference-data.json'
MISSING_AUDIT = ROOT / 'T Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-T1/receipt.json'
BLOCKERS = ROOT / 'Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-r1/data/static-blockers.json'
BLOCKER_AUDIT = ROOT / 'T Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-T1/receipt.json'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def desc(path): return {'path':str(path.resolve(strict=True)),'sha256':sha(path),'bytes':path.stat().st_size}
def load(path): return json.loads(path.read_bytes())

def qualify(output):
    if output.exists(): raise ValueError('Fresh package output required')
    paths=(PAIR,SHARED,SHARED_AUDIT,MISSING,MISSING_AUDIT,BLOCKERS,BLOCKER_AUDIT)
    before={str(p):sha(p) for p in paths}
    pair, shared, sa, missing, ma, blockers, ba = map(load, paths)
    if pair.get('status')!='CollectedPartialSourceOnly' or pair.get('mission_ready') is not False or pair.get('buildings')!=9:
        raise ValueError('Qualified partial current factory pair required')
    for key in ('factory','factory_log','readmodel','readmodel_receipt','setup'):
        entry=pair.get(key,{});path=Path(entry.get('path',''))
        if not path.is_absolute() or not path.is_file() or sha(path)!=entry.get('sha256'):
            raise ValueError('Factory pair evidence changed: '+key)
    if (sa.get('status')!='PassedDataOnlyCapture' or sa.get('wrapper_exit_code')!=0 or sa.get('required_marker_present') is not True or
        sa.get('capture',{}).get('sha256')!=sha(SHARED) or sa.get('mission_ready') is not False or
        ma.get('status')!='PassedScopedSourceOnlyCapture' or ma.get('wrapper_exit_code')!=0 or ma.get('marker_present') is not True or
        ma.get('input_sha256',{}).get(str(MISSING))!=sha(MISSING) or ma.get('mission_ready') is not False):
        raise ValueError('Current independently audited source templates required')
    if (ba.get('status')!='PassedIsolatedBakedBlockerCapture' or ba.get('wrapper_exit_code')!=0 or ba.get('required_marker_present') is not True or
        ba.get('capture_sha256')!=sha(BLOCKERS) or ba.get('blocker_count')!=348 or ba.get('mission_ready') is not False):
        raise ValueError('Current source blocker capture required')
    captures=(shared,missing,blockers); checked={}
    for capture in captures:
        pins=capture.get('source_pins')
        if not isinstance(pins,dict) or len(pins)<8000: raise ValueError('Complete source pin map required')
        for name, expected in pins.items():
            p=Path(name);p=(p if p.is_absolute() else ROOT/p).resolve(strict=True);key=str(p)
            if key not in checked: checked[key]=sha(p)
            if checked[key]!=expected: raise ValueError('Current source pin changed: '+name)
    shared_keys=shared.get('requested_prefab_keys'); missing_keys=missing.get('requested_prefab_keys')
    if not isinstance(shared_keys,list) or len(shared_keys)!=19 or not isinstance(missing_keys,list) or len(missing_keys)!=4 or set(shared_keys)&set(missing_keys):
        raise ValueError('Exact distinct prefab namespace domains required')
    for capture, keys in ((shared,shared_keys),(missing,missing_keys)):
        if set(capture.get('opening_prefabs',{}))!=set(keys) or capture.get('full_mission_initialized') is not False or capture.get('mission_ready') is not False:
            raise ValueError('Capture roots or source-only scope differ')
    if len(shared.get('unsupported_values',[]))!=32 or len(missing.get('unsupported_values',[]))!=9:
        raise ValueError('Exact audited unsupported presentation ledgers required')
    factory_path=Path(pair['factory']['path']);setup_path=Path(pair['setup']['path'])
    if sha(factory_path)!=pair['factory']['sha256'] or sha(setup_path)!=pair['setup']['sha256']:
        raise ValueError('Factory or setup input changed')
    factory=load(factory_path);setup=load(setup_path)['compiled_setup'];template_keys=set(shared_keys)|set(missing_keys)
    opening={row['RuntimePrefabKey'] for row in setup['Forces']}; production=set(); slot_count=0
    for building in factory['buildings']:
        for slot in building['fields']['ProductionSlots']['items']:
            prefab=slot['fields']['SpawnUnitPrefab']
            if prefab.get('codec')!='source-asset-reference-v1' or not prefab.get('key'):
                raise ValueError('Original production prefab identity missing')
            production.add(prefab['key']);slot_count+=1
    if len(opening)!=9 or not opening.issubset(template_keys) or not production.issubset(template_keys):
        raise ValueError('Opening or production prefab template domain incomplete')
    for name, expected in checked.items():
        if sha(Path(name))!=expected: raise ValueError('Source changed during package preparation: '+name)
    if {str(p):sha(p) for p in paths}!=before: raise ValueError('Capture input changed during package preparation')
    output.mkdir(parents=True)
    package={'kind':'S004CurrentGameplaySourceInputs','status':'CollectedSourceDataOnly',
             'factory_pair':desc(PAIR),'shared_prefabs':desc(SHARED),'shared_prefab_audit':desc(SHARED_AUDIT),
             's004_prefabs':desc(MISSING),'s004_prefab_audit':desc(MISSING_AUDIT),
             'static_blockers':desc(BLOCKERS),'static_blocker_audit':desc(BLOCKER_AUDIT),
             'template_namespace_count':2,'template_roots':len(template_keys),'opening_prefab_keys':sorted(opening),
             'production_prefab_keys':sorted(production),'production_slot_count':slot_count,
             'source_paths_checked_before_after':len(checked),'unsupported_presentation_values':len(shared['unsupported_values'])+len(missing['unsupported_values']),
             'physical_scene_validated':False,'live_occupancy_observed':False,'native_input_observed':False,
             'world_initialized':False,'runtime_admission':False,'source_fit_certified':False,'mission_ready':False}
    with (output/'package.json').open('x') as stream: json.dump(package,stream,indent=2);stream.write('\n')
    return package

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    p=qualify(args.output.resolve());print(json.dumps({'status':p['status'],'roots':p['template_roots'],
        'opening_keys':len(p['opening_prefab_keys']),'production_keys':len(p['production_prefab_keys']),
        'source_paths_checked_before_after':p['source_paths_checked_before_after'],'mission_ready':False}))
