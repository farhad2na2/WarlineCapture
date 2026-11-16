#!/usr/bin/env python3
"""Independently verify fresh S003 prefab capture; no runtime admission."""
import argparse,hashlib,json,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];sys.path.insert(0,str(ROOT/'Tools/CI/SkirmishSourceSimulation/Tests'))
from finalize_s003_refreshed_prefab_capture import qualify

def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--capture',type=Path,required=True);parser.add_argument('--log',type=Path,required=True);parser.add_argument('--metadata',type=Path,required=True);parser.add_argument('--metadata-audit',type=Path,required=True);parser.add_argument('--wrapper-exit',type=int,required=True);parser.add_argument('--output',type=Path,required=True);a=parser.parse_args()
 if a.output.exists():raise ValueError('Refuse to overwrite receipt')
 log=a.log.read_bytes()
 if b'\0' in log:raise ValueError('Capture log contains NUL bytes')
 metadata_audit=json.loads(a.metadata_audit.read_text());msha=sha(a.metadata);asha=sha(a.metadata_audit)
 if metadata_audit.get('status')!='Passed' or metadata_audit.get('capture_sha256')!=msha or metadata_audit.get('wrapper_exit_observed')!=0 or metadata_audit.get('source_pin_mismatches')!=[] or metadata_audit.get('mission_ready') is not False or metadata_audit.get('native_context_qualified') is not False:raise ValueError('Exact independently audited current metadata required')
 result=qualify(ROOT,a.capture.resolve(strict=True),a.log.resolve(strict=True),a.wrapper_exit);capture=json.loads(a.capture.read_text())
 if capture.get('native_metadata_capture_sha256')!=msha or capture.get('independent_metadata_audit_sha256')!=asha or capture.get('key_metadata_epoch')!='HistoricalKeys_CurrentContextComparedExactly':raise ValueError('Fresh metadata/capture identity mismatch')
 if sha(a.metadata)!=msha or sha(a.metadata_audit)!=asha or sha(a.log)!=hashlib.sha256(log).hexdigest():raise ValueError('Evidence changed during validation')
 # The generic finalizer assumes an earlier metadata epoch; this capture explicitly
 # binds fresh metadata and must not inherit that historical assertion.
 result.pop('native_metadata_predates_added_capture_observer',None)
 result.update({'scope':'CurrentDataOnlyPrefabCapture_IndependentlyAuditedMetadata_NotRuntimeAdmission','metadata_epoch':'fresh_same_source_focus_boundary','metadata_capture_sha256':msha,'independent_metadata_audit_sha256':asha,'finalizer_sha256':sha(ROOT/'Tools/CI/SkirmishSourceSimulation/Tests/finalize_s003_refreshed_prefab_capture.py'),'finalizer_note':'Historical metadata ordering flag in generic finalizer is inapplicable to this fresh same-source capture.'})
 a.output.parent.mkdir(parents=True,exist_ok=True)
 with a.output.open('x') as f:json.dump(result,f,indent=2);f.write('\n')
 print(json.dumps(result))
if __name__=='__main__':main()
