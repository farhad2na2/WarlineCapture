#!/usr/bin/env python3
"""Verify a completed checked asset-only S005 identity capture before reuse."""
import argparse, hashlib, json
from pathlib import Path

def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''): h.update(block)
    return h.hexdigest()

def main():
    p=argparse.ArgumentParser();p.add_argument('directory',type=Path);p.add_argument('log',type=Path);p.add_argument('--wrapper-exit',type=int,required=True);a=p.parse_args()
    metadata=a.directory/'unit-production-metadata.json';identity=a.directory/'unit-prefab-identity-readback.json';receipt=a.directory/'receipt.json'
    if receipt.exists(): raise ValueError('Refuse to overwrite existing receipt')
    raw=a.log.read_bytes(); marker='[SkirmishAuthoredUnitProductionMetadata] result=Passed units=22 transports=1 unsupported=0 fullMission=0'; audit='[SkirmishAuthoredUnitIdentityAudit] result=Passed units=22 distinct=22 transports=1 runtimeTemplates=0 readiness=0'
    if a.wrapper_exit!=0 or b'\0' in raw or raw.count(marker.encode())!=1 or raw.count(audit.encode())!=1: raise ValueError('Wrapper, log integrity or pass markers failed')
    d=json.loads(metadata.read_text());i=json.loads(identity.read_text())
    if d.get('kind')!='AuthoredSkirmishUnitProductionMetadata' or d.get('catalog_id')!='S005' or len(d.get('units',[]))!=22 or d.get('full_mission_initialized') is not False or d.get('native_visual_readiness') is not False: raise ValueError('Metadata scope or identity changed')
    identities=i.get('identities',[])
    if i.get('kind')!='AuthoredUnitPrefabIdentityReadback' or i.get('catalog_id')!='S005' or i.get('seed')!=1 or i.get('setup_hash')!=1865447784 or i.get('metadata_sha256')!=sha(metadata) or i.get('units')!=22 or i.get('transports')!=1 or i.get('runtime_templates_verified') is not False or i.get('mission_ready') is not False or len(identities)!=22: raise ValueError('Independent identity readback differs')
    readback={row['key']:tuple(row[k] for k in ('path','guid','local_id')) for row in identities}
    authored={row['key']:tuple(row['prefab'][k] for k in ('path','guid','local_id')) for row in d['units']}
    if len(readback)!=22 or len(authored)!=22 or readback!=authored: raise ValueError('Prefab identities do not match')
    pins=d.get('source_pins')
    if not isinstance(pins,dict) or not pins: raise ValueError('Actual source pins required')
    for name,expected in pins.items():
        path=Path(name)
        if not path.is_file() or sha(path)!=expected: raise ValueError('Source pin changed: '+name)
    result={'status':'PassedAssetMetadataOnly','wrapper_exit':0,'marker':marker,'identity_marker':audit,'metadata_sha256':sha(metadata),'identity_sha256':sha(identity),'log_sha256':hashlib.sha256(raw).hexdigest(),'catalog_id':'S005','units':22,'transports':1,'source_pins_checked':len(pins),'runtime_templates_verified':False,'native_mission':False,'mission_ready':False}
    receipt.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if __name__=='__main__':main()
