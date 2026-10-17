"""Prepare two already claimed development canaries; never dispatch or qualify."""
import argparse
import hashlib
import json
from pathlib import Path
import uuid

from skirmish_production_queue import load,preflight,fingerprint,identifier
from s005_windows_source_snapshot import candidate_hash
from skirmish_production_matrix_authority import SamplingAuthority


def prepare(project,job_directories,snapshot_path,batch_id,sampling_plan=None,sampling_sha=None):
    # Public standalone descriptor preparation remains fresh-validating.
    return _prepare(project,job_directories,snapshot_path,batch_id,sampling_plan,sampling_sha)


def _prepare(project,job_directories,snapshot_path,batch_id,sampling_plan=None,sampling_sha=None,*,_validate=None):
    _validate=preflight if _validate is None else _validate
    project=project.resolve()
    batch_id=identifier(batch_id)
    authority=SamplingAuthority(project,sampling_plan,sampling_sha) if sampling_plan is not None else None
    if not 1<=len(job_directories)<=(1200 if authority else 2) or len({p.resolve() for p in job_directories})!=len(job_directories) or (authority is None and len(job_directories)!=2):
        raise ValueError('Exactly two distinct already-claimed development jobs required')
    snapshot_bytes=snapshot_path.read_bytes();snapshot=json.loads(snapshot_bytes.decode('utf-8-sig'))
    if set(snapshot)!={'source_manifest','candidate_sha256','build_manifest','build_sha256'}:
        raise ValueError('Complete candidate snapshot envelope required')
    if not snapshot['source_manifest'] or not snapshot['build_manifest'] or candidate_hash(snapshot['source_manifest'])!=snapshot['candidate_sha256'] or fingerprint(snapshot['build_manifest'])!=snapshot['build_sha256']:
        raise ValueError('Snapshot rows do not derive candidate/build identity')
    jobs=[]
    for directory in job_directories:
        directory=directory.resolve();job=load(directory/'job.json');_validate(job,project,authority)
        claim=load(directory/'claim.json');artifact=directory/claim['attempt_id']
        if claim['job_sha256']!=fingerprint(job) or load(artifact/'claim.json')!=claim:
            raise ValueError('Canonical live claim differs from immutable attempt')
        if (artifact/'adapter-started.json').exists() or (artifact/'result.json').exists():
            raise ValueError('Attempt already started or finished')
        if (authority is None and job['catalog_id'] not in ('S004','S005')) or job['size']!='Standard' or job['difficulty']!='Regular':
            raise ValueError('Bounded adapter supports S004/S005 Regular Standard only')
        if job['candidate_sha256']!=snapshot['candidate_sha256'] or job['build_sha256']!=snapshot['build_sha256']:
            raise ValueError('Jobs and complete snapshot must pin one candidate/build')
        jobs.append(dict(job_path=str(directory/'job.json'),claim_path=str(directory/'claim.json')))
    if authority is not None: authority.validate_batch([load(Path(row["job_path"])) for row in jobs])
    # No queue metadata is mutated here. Native Read/Reserve rechecks both live
    # claims, and the start transaction remains exclusive under .metadata-lock.
    digest=hashlib.sha256(snapshot_bytes).hexdigest()
    store=project/'Logs/SkirmishProduction/CandidateManifests';store.mkdir(parents=True,exist_ok=True)
    manifest=store/(digest+'.json')
    if manifest.exists():
        if manifest.read_bytes()!=snapshot_bytes:raise ValueError('Immutable candidate-store collision')
    else:
        with manifest.open('xb') as stream:stream.write(snapshot_bytes)
    output=project/'Logs/SkirmishProduction/Batches'/batch_id
    output.mkdir(parents=True,exist_ok=False)
    descriptor=dict(schema_version=1,batch_id=batch_id,artifact_directory=str(output),candidate_sha256=snapshot['candidate_sha256'],
        build_sha256=snapshot['build_sha256'],candidate_manifest_path=str(manifest),candidate_manifest_sha256=digest,jobs=jobs)
    if authority is not None:
        descriptor.update(schema_version=2,sampling_plan_path=str(authority.path),sampling_plan_sha256=authority.expected)
    with (output/'batch.json').open('x',encoding='utf-8') as stream:json.dump(descriptor,stream,indent=2);stream.write('\n')
    return output/'batch.json'


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project',type=Path,required=True);parser.add_argument('--job',type=Path,action='append',required=True)
    parser.add_argument('--snapshot',type=Path,required=True)
    parser.add_argument('--sampling-plan',type=Path)
    parser.add_argument('--sampling-sha')
    args=parser.parse_args()
    result=prepare(args.project,args.job,args.snapshot,'batch-'+uuid.uuid4().hex,args.sampling_plan,args.sampling_sha)
    print('[SkirmishProductionBatchPrepare] result=Prepared native=0 queueSuccessEnabled=0 descriptor='+str(result))
