#!/usr/bin/env python3
"""Compare candidate serialization with an existing native layout capture."""
import argparse
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
NATIVE = ROOT / 'T Logs/SkirmishProduction/s005-layout-native-capture-20261105-T1/authored-layout-native.json'
RECEIPT = NATIVE.parent / 'receipt.json'
CANDIDATES = [ROOT / 'T Logs/SkirmishProduction/s006-original-compiler-candidate-20261105-T3/layout-newtonsoft.json',
              ROOT / 'T Logs/SkirmishProduction/s006-original-compiler-candidate-20261105-T4/layout-newtonsoft.json',
              ROOT / 'T Logs/SkirmishProduction/s006-original-compiler-candidate-20261105-T5/layout-newtonsoft.json',
              ROOT / 'T Logs/SkirmishProduction/s006-original-compiler-candidate-20261105-T6/layout-newtonsoft.json']


def sha(raw): return hashlib.sha256(raw).hexdigest()


def compare(a, b, path, failures):
    if isinstance(a, dict) and isinstance(b, dict):
        if list(a) != list(b): failures.append(path + ':field_order_or_keys'); return
        for key in a: compare(a[key], b[key], path + '.' + key, failures)
    elif isinstance(a, list) and isinstance(b, list):
        if len(a) != len(b): failures.append(path + ':length'); return
        for index, (left, right) in enumerate(zip(a, b)): compare(left, right, path + '[' + str(index) + ']', failures)
    elif isinstance(a, float) and isinstance(b, (int, float)) and not isinstance(b, bool):
        if struct.pack('<f', a) != struct.pack('<f', b): failures.append(path + ':f32')
    elif type(a) is not type(b) or a != b:
        failures.append(path + ':value')


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',required=True,type=Path);out=p.parse_args().output.resolve()
    if not out.is_relative_to(ROOT):raise ValueError('Output must be inside repository')
    files=[NATIVE,RECEIPT,*CANDIDATES];raw={str(f):f.read_bytes() for f in files};receipt=json.loads(raw[str(RECEIPT)])
    stored_native=raw[str(NATIVE)]
    if not stored_native.endswith(b'\n') or stored_native.endswith(b'\n\n'):
        raise ValueError('Expected one captured-file line terminator')
    native=stored_native[:-1]
    if receipt.get('status')!='Passed' or receipt.get('wrapper_exit')!=0 or receipt.get('native_json_sha256')!=sha(native):raise ValueError('Exact captured native JSON string receipt required')
    parsed=json.loads(native);results=[]
    for file in CANDIDATES:
        candidate=raw[str(file)];failures=[];compare(parsed,json.loads(candidate),'$',failures)
        first=next((i for i,(left,right) in enumerate(zip(native,candidate)) if left!=right),min(len(native),len(candidate)))
        results.append({'path':str(file),'sha256':sha(candidate),'byte_equal':native==candidate,'native_length':len(native),'candidate_length':len(candidate),
                        'first_byte_difference':None if native==candidate else first,'float32_and_structure_mismatch_count':len(failures),'first_mismatches':failures[:10]})
    if any(f.read_bytes()!=raw[str(f)] for f in files):raise ValueError('Input changed during comparison')
    out.parent.mkdir(parents=True,exist_ok=False)
    report={'kind':'S005NativeLayoutSerializationCandidateAudit','scope':'One captured layout only; native receipt hashes JSON before its single storage newline; no generic serializer parity, source-values serialization, compiler admission, gameplay or readiness',
            'input_sha256':{path:sha(data) for path,data in raw.items()},'candidates':results,'native_parity_proven':False,'mission_ready':False}
    out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'kind':report['kind'],'candidates':results,'mission_ready':False}))


if __name__=='__main__':main()
