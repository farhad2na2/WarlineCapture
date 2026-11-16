#!/usr/bin/env python3
"""Audit the checked current S004 factory capture without mission admission."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
CAPTURE=ROOT/'Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-r1/data/full-factory-runway-definitions.json'
LOG=ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-T1/editor.log'
EXIT=LOG.parent/'wrapper-exit.json'
OLD=ROOT/'Logs/SkirmishProduction/s004-current-merged-factory-20261105-T2/full-factory-runway-definitions.json'
MARKER='[S004FullFactoryRunwayReference] result=Collected buildings=9 unsupported=0 mission=0 readiness=0'

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()

def audit():
    inputs=(CAPTURE,LOG,EXIT,OLD);before={str(p):sha(p) for p in inputs}
    data=json.loads(CAPTURE.read_bytes());old=json.loads(OLD.read_bytes());exit_info=json.loads(EXIT.read_bytes())
    if (exit_info.get('wrapper_exit_code')!=0 or exit_info.get('exit_code_authority')!='observed from exec session 77150' or
        sum(line.endswith(MARKER) for line in LOG.read_text().splitlines())!=1): raise ValueError('Observed wrapper exit and unique marker required')
    if (data.get('kind')!='S004OriginalFullFactoryRunwayDefinitions' or data.get('authority')!='OriginalS004ProductionOverlayAndDefinitionFactory' or
        data.get('seed')!=104733 or data.get('setup_hash')!=1130726651 or data.get('unsupported_values')!=[] or
        data.get('runway_reader_supplied') is not True or data.get('mission_world_created') is not False or data.get('mission_ready') is not False):
        raise ValueError('Exact current source factory scope required')
    if len(data.get('buildings',[]))!=9 or data['buildings']!=old.get('buildings'):
        raise ValueError('Current and historical source factory rows differ')
    pins=data.get('source_pins')
    if not isinstance(pins,dict) or len(pins)<9000: raise ValueError('Complete factory source pins required')
    def check():
        for name,expected in pins.items():
            path=Path(name);path=path if path.is_absolute() else ROOT/path
            if not path.is_file() or sha(path)!=expected: raise ValueError('Factory source pin changed: '+name)
    check()
    if {str(p):sha(p) for p in inputs}!=before: raise ValueError('Factory evidence changed')
    check()
    return {'status':'Passed','scope':'CurrentDataOnlyFactoryCapture_NoRuntimeAdmission','capture_sha256':sha(CAPTURE),
            'wrapper_exit_observed':0,'marker_count':1,'mismatches':[],'source_pins_checked':len(pins),
            'factory_buildings_equal_historical':True,'historical_capture_current_qualified':False,
            'mission_world_created':False,'runtime_admission':False,'mission_ready':False,'input_sha256':before}

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    result=audit()
    with args.output.open('x') as stream:json.dump(result,stream,indent=2);stream.write('\n')
    print(json.dumps({'status':result['status'],'source_pins_checked':result['source_pins_checked'],'mission_ready':False}))
