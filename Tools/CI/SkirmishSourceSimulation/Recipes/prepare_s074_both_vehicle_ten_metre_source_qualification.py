#!/usr/bin/env python3
"""Prepare a non-readiness S074 source-only qualification descriptor for the both-vehicle 10m candidate."""
import argparse,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
CONTEXT=ROOT/'T Logs/SkirmishProduction/s074-both-vehicle-ten-metre-factory-metadata-20261114-r1/s074/source-factory-context.json'
REPLAY=ROOT/'T Logs/SkirmishProduction/s074-both-vehicle-ten-metre-factory-metadata-20261114-r1/receipt.json'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,required=True);a=p.parse_args();out=a.output.resolve()
 if not out.is_relative_to(ROOT/'T Logs') or out.exists():raise ValueError('Fresh repository T Logs output required')
 context=json.loads(CONTEXT.read_bytes());receipt=json.loads(REPLAY.read_bytes());target=context.get('target_context',{})
 if receipt.get('status')!='ProjectedSourceOnly' or target.get('catalog_id')!='S074' or target.get('seed')!=104803 or target.get('size')!='Standard' or target.get('difficulty')!='Regular' or target.get('setup_hash')!=1532795717 or target.get('layout_fingerprint')!='6bc845164d9309e7b963a686b9ea9fc4269aaaa94e591eb3bd9fb49bee2f87d7':raise ValueError('Exact fresh both-vehicle 10m factory context required')
 if receipt.get('source_factory_context',{}).get('path')!=str(CONTEXT.resolve()) or receipt.get('source_factory_context',{}).get('sha256')!=sha(CONTEXT):raise ValueError('Factory receipt/context descriptor mismatch')
 for k in ('world_allocated','native_S050_capture','active_mission_water_qualified','runtime_loader_admitted','source_fit_certified','mission_ready'):
  if context.get(k) is not False:raise ValueError('Context gate must remain false: '+k)
 out.mkdir(parents=True);path=out/'qualification.json'
 q={'schema_version':1,'kind':'ExperimentalGroundConfigurationQualification','runtime_backend':'TypedGroundS026V1','compiler_context':{'path':str(CONTEXT.resolve()),'sha256':sha(CONTEXT)},'scope':'OriginalCompilerAndQualifiedFactoryInputs_SourceOnly','native_context_qualified':False,'mission_ready':False}
 path.write_text(json.dumps(q,indent=2,sort_keys=True)+'\n');receipt_out={'kind':'S074BothVehicleTenMetreSourceOnlyQualificationReceipt','status':'PreparedSourceOnly','qualification':{'path':str(path),'sha256':sha(path)},'source_factory_context':{'path':str(CONTEXT),'sha256':sha(CONTEXT)},'factory_projection_receipt':{'path':str(REPLAY),'sha256':sha(REPLAY)},'target_context':target,'world_allocated':False,'native_context_qualified':False,'source_fit_certified':False,'mission_ready':False};(out/'receipt.json').write_text(json.dumps(receipt_out,indent=2,sort_keys=True)+'\n');print(json.dumps(receipt_out))
if __name__=='__main__':main()
