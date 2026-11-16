#!/usr/bin/env python3
"""Bind S005 captured object identities to their exact captured field paths.

This is an inspectable projection, not an importer or gameplay admission.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[4]
CAPTURE=ROOT/'T Logs/SkirmishProduction/s005-prefab-object-reference-capture-20261105-T2/s005-prefab-object-references.json'
RECEIPT=CAPTURE.parent/'receipt.json'
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
def need(value,message):
    if not value:raise ValueError(message)

def project(out, capture=CAPTURE, receipt_path=RECEIPT, selected_units=False):
    evidence=json.loads(receipt_path.read_text());need(evidence['status']=='CollectedAssetIdentityOnly' and evidence['wrapper_exit']==0 and sha(capture)==evidence['data_sha256'], 'Exact captured data/receipt required')
    doc=json.loads(capture.read_text());expected_kind='SelectedUnitPrefabObjectReferenceInspection' if selected_units else 'S005PrefabObjectReferenceInspection'
    need(doc['kind']==expected_kind and doc['replay_ready'] is False and doc['mission_ready'] is False,'Inspection-only capture required')
    if selected_units:
        need(doc.get('selected_unit_metadata_sha256')==evidence.get('metadata_sha256') and len(doc['requested_prefab_keys'])==evidence.get('selected_keys'), 'Selected capture metadata and key domain required')
    pins=doc['source_pins'];need(isinstance(pins,dict) and pins,'Captured source pins required')
    for path,h in pins.items():need(Path(path).is_file() and sha(Path(path))==h,'Captured source changed: '+path)
    for assembly in ('capture_assembly','entities_assembly'):
        row=doc[assembly];need(Path(row['path']).is_file() and sha(Path(row['path']))==row['sha256'],'Captured assembly changed: '+assembly)
    entities={row['id']:row for row in doc['entities']};need(len(entities)==len(doc['entities']),'Duplicate captured entity')
    ledger={row['path']:row for row in doc['unsupported_values']};refs={row['path']:row['identity'] for row in doc['object_reference_identities']}
    need(len(ledger)==len(refs) and (0<=len(refs)<=4096 if selected_units else len(refs)==29) and set(ledger)==set(refs),'Complete exact unresolved identity mapping required')
    identities={};bindings=[]
    for path,identity in sorted(refs.items()):
        parts=path.split('/');need(len(parts)==3 and parts[0] in entities,'Invalid captured reference path')
        record=entities[parts[0]]['components'][parts[1]]['value'];field=record['fields'][parts[1]+'::'+parts[2]]
        need(field['codec']=='unsupported-v1' and field['reason']=='unity_object_ref_requires_bound_source_identity' and ledger[path]['reason']==field['reason'],'Original unsupported leaf differs')
        codec=identity.get('codec');need(codec in ('null-v1','source-asset-reference-v1'),'Unsupported identity kind')
        if codec=='source-asset-reference-v1':
            asset=identity['path'];meta=Path(asset+'.meta');match=re.search(r'^guid:\s*([0-9a-f]{32})\s*$',meta.read_text(),re.M)
            need(identity.get('type')=='UnityEngine.GameObject' and match is not None and match.group(1)==identity['guid'] and
                 asset in pins and asset+'.meta' in pins and isinstance(identity.get('local_id'),int),'Unpinned or mismatched source object identity')
        encoded=json.dumps(identity,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode();key=hashlib.sha256(encoded).hexdigest()
        if key in identities:need(identities[key]==identity,'Identity digest collision')
        identities[key]=identity
        record['fields'][parts[1]+'::'+parts[2]]={'codec':'source-object-identity-v1','identity_sha256':key}
        bindings.append({'path':path,'identity_sha256':key})
    for path,h in pins.items():need(sha(Path(path))==h,'Source changed during projection: '+path)
    out.mkdir(parents=True,exist_ok=False)
    projection={'schema_version':1,'kind':'S005PrefabObjectIdentityProjection','source_capture_sha256':sha(capture),
        'source_receipt_sha256':sha(receipt_path),'scope':'Inspection-only object identity sidecar; no Unity instance IDs or runtime importer admission',
        'template_namespace_id':doc['template_namespace_id'],'requested_prefab_keys':doc['requested_prefab_keys'],
        'opening_prefabs':doc['opening_prefabs'],'entities':doc['entities'],'original_unsupported_values':doc['unsupported_values'],
        'reference_bindings':bindings,'identities':identities,'replay_ready':False,'native_visual_readiness':False,'mission_ready':False}
    target=out/'projection.json';target.write_text(json.dumps(projection,indent=2,ensure_ascii=False)+'\n')
    receipt={'status':'ProjectedInspectionOnly','projection_sha256':sha(target),'capture_sha256':sha(capture),
       'reference_count':len(bindings),'unique_source_identities':len(identities),'source_pins_verified':len(pins),
       'capture_assemblies_rechecked':True,'runtime_import_attempted':False,'mission_ready':False}
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--selected-capture',type=Path);p.add_argument('--selected-receipt',type=Path)
    args=p.parse_args()
    if (args.selected_capture is None)!=(args.selected_receipt is None):raise ValueError('Selected capture and receipt are required together')
    project(args.output.resolve(),args.selected_capture.resolve() if args.selected_capture else CAPTURE,
            args.selected_receipt.resolve() if args.selected_receipt else RECEIPT,args.selected_capture is not None)
