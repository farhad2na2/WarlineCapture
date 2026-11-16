#!/usr/bin/env python3
"""Bind the checked current S004 readmodel capture to its wrapper evidence."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
RAW=ROOT/'Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-r3/data/configured-spawnable-readmodel.json'
LOG=ROOT/'T Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-T3/editor.log'
EXIT=LOG.parent/'wrapper-exit.json'
FINAL=LOG.parent/'finalization/capture-receipt.json'
MARKER='[SkirmishS004ConfiguredSpawnableReadModel] result=Passed keys=9 scene=0 missionBuildings=0 readiness=0'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def audit():
    paths=(RAW,LOG,EXIT,FINAL);before={str(p):sha(p) for p in paths}
    data=json.loads(RAW.read_bytes());exit_info=json.loads(EXIT.read_bytes());final=json.loads(FINAL.read_bytes())
    if (exit_info.get('wrapper_exit_code')!=0 or exit_info.get('exit_code_authority')!='observed from exec session 73120' or
        sum(line.endswith(MARKER) for line in LOG.read_text().splitlines())!=1 or
        final.get('schema')!='s004-configured-spawnable-readmodel-receipt-v1' or final.get('rawExportSha256')!=sha(RAW) or
        final.get('sourcePinsVerified') is not True or final.get('missionReadiness') is not False):
        raise ValueError('Observed checked wrapper and independent readmodel finalizer required')
    pins=data.get('source_pins')
    if not isinstance(pins,dict) or len(pins)<9000:raise ValueError('Complete source pins required')
    def check():
        for name,expected in pins.items():
            path=Path(name);path=path if path.is_absolute() else ROOT/path
            if not path.is_file() or sha(path)!=expected:raise ValueError('Readmodel source changed: '+name)
    check()
    if {str(p):sha(p) for p in paths}!=before:raise ValueError('Readmodel evidence changed')
    check()
    return {'status':'Passed','scope':'CurrentDataOnlyReadmodelCapture_NoRuntimeAdmission','capture_sha256':sha(RAW),
            'wrapper_exit_observed':0,'marker_count':1,'source_pins_checked':len(pins),'finalizer_receipt_sha256':sha(FINAL),
            'mission_world_created':False,'runtime_admission':False,'mission_ready':False,'input_sha256':before}

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    result=audit()
    with args.output.open('x') as stream:json.dump(result,stream,indent=2);stream.write('\n')
    print(json.dumps({'status':result['status'],'source_pins_checked':result['source_pins_checked'],'mission_ready':False}))
