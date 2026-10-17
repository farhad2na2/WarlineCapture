"""Scoped normal paid-plane + raw camera evidence joins. Never camera/full mission acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import re
from s005_paid_plane_lineage_qualifier import qualify
from s005_plane_camera_evidence import inspect
from s005_windows_source_snapshot import candidate_hash


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def rows(path):return [json.loads(line) for line in path.read_text(encoding='utf-8-sig').splitlines() if line.strip()]


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for option in ('raw','log','source-before','source-after','output'):parser.add_argument('--'+option,type=Path,required=True)
    parser.add_argument('--wrapper-exit',type=int,required=True)
    parser.add_argument('--cruise',type=Path,help='New strict actual normal Transport cruise JSONL; omission retains delivered-only scope')
    parser.add_argument('--pin',action='append',required=True,help='Reviewed imported relativepath=sha256; repeat for observer, Editor collector and view-profile files')
    args=parser.parse_args();errors=[]
    before,after=read(args.source_before),read(args.source_after)
    if before!=after:errors.append('Full source/build snapshot changed')
    manifest=before.get('source_manifest') or []
    builds=before.get('build_manifest') or []
    try:
        if not manifest or before.get('candidate_sha256')!=candidate_hash(manifest):errors.append('Exact Windows native source digest invalid')
        build_digest=hashlib.sha256(json.dumps(builds,sort_keys=True,separators=(',',':')).encode()).hexdigest()
        if not builds or build_digest!=before.get('build_sha256'):errors.append('Exact DLL manifest digest invalid')
        indexed={row['path']:row['sha256'] for row in manifest}
        for pin in args.pin:
            path,digest=pin.rsplit('=',1)
            if not re.fullmatch('[0-9a-f]{64}',digest) or indexed.get(path)!=digest:errors.append('Reviewed imported source pin mismatch: '+path)
    except (KeyError,TypeError,ValueError) as error:errors.append('Malformed source/build manifest: '+str(error))
    raw=rows(args.raw);log=args.log.read_text(encoding='utf-8-sig',errors='replace')
    for owner in ('LaunchCatalog','Finish'):
        values=re.findall(r'\[SkirmishCandidateFingerprint\] phase=Completed owner='+owner+r'[^\r\n]*sha256=([0-9a-f]{64})',log)
        if values!=[before.get('candidate_sha256')]:errors.append('Actual native '+owner+' source fingerprint differs/missing')
    if args.wrapper_exit!=0 or '[UnityInvoke] ExitCode: 0' not in log:errors.append('Actual checked wrapper zero exit absent')
    attempts={row.get('attempt') for row in raw if row.get('attempt')}
    traces={row.get('tracePath') for row in raw if row.get('attempt')}
    if len(traces)!=1 or not next(iter(traces),None):
        errors.append('Unique actual current native contact trace path absent/ambiguous')
        native_failures=re.findall(r'\[SkirmishNativeRendering\] result=Failed reason=([^\s]+)',log)
        pre_attempt_failure=not attempts and args.wrapper_exit!=0 and bool(native_failures)
        result=dict(result='Failed',qualified=False,
            failure_category='NativePreAttemptRenderingFailure' if pre_attempt_failure else 'MissingOrAmbiguousContactEvidence',
            native_failure_reasons=native_failures,wrapper_exit=args.wrapper_exit,
            native_attempt_observed=bool(attempts),provenance_errors=errors,
            cameraAccepted=False,fullMissionCertified=False,playerReady=False,
            cruise_exercised=False,pending=['native rendering readiness diagnosis','fresh normal-input capture','owner screenshot/readability review','normal mission Victory/result/return'])
        with args.output.open('x',encoding='utf-8') as output:json.dump(result,output,indent=2);output.write('\n')
        print(json.dumps(result,indent=2))
        return 1
    trace=Path(next(iter(traces)))
    contacts=rows(Path(str(trace)+'.touch.jsonl'))
    if args.cruise:
        from s005_normal_transport_cruise_qualification import qualify as cruise_qualify
        result=cruise_qualify(raw,rows(args.cruise),rows(Path(str(args.raw)+'.camera.jsonl')),contacts,
                              rows(Path(str(trace)+'.commands.jsonl')),log,args.wrapper_exit,not errors)
        result['provenance_errors']=errors
        with args.output.open('x',encoding='utf-8') as output:json.dump(result,output,indent=2);output.write('\n')
        print(json.dumps({key:result[key] for key in ('result','cameraAccepted','fullMissionCertified','playerReady','provenance_errors')},indent=2))
        return 0 if result['qualified'] else 1
    lineage=qualify(raw,contacts,log,args.wrapper_exit,not errors)
    camera=inspect(rows(Path(str(args.raw)+'.camera.jsonl')),raw)
    result=dict(result='PassedScopedLineageAndCapturedCamera' if lineage['qualified'] and camera['evidenceCaptured'] and not errors else 'Failed',
        lineage=lineage,camera=camera,provenance_errors=errors,cameraAccepted=False,fullMissionCertified=False,playerReady=False,
        cruise_exercised=False,pending=['actual cruising Transport camera proof','owner screenshot/readability review','normal mission Victory/result/return'])
    with args.output.open('x',encoding='utf-8') as output:json.dump(result,output,indent=2);output.write('\n')
    print(json.dumps({key:result[key] for key in ('result','cameraAccepted','fullMissionCertified','playerReady','provenance_errors')},indent=2))
    return 0 if result['result']=='PassedScopedLineageAndCapturedCamera' else 1

if __name__=='__main__':raise SystemExit(main())
