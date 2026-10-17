"""Prepare two unqualified canonical claims from native compiled census; never launch Unity."""
import argparse
import hashlib
import json
from pathlib import Path
import uuid

import skirmish_production_queue as queue
from skirmish_native_identity import validate_setup_identity, content_identity, sha256
from s005_windows_source_snapshot import candidate_hash
from prepare_skirmish_production_batch import prepare


def prepare_matrix_jobs(args):
    """Consume actual checked native census; does not launch, publish or qualify."""
    from skirmish_production_matrix_authority import SamplingAuthority, strict
    from skirmish_native_identity import normalize_native_locale
    import re
    root=args.project.resolve(); worker=queue.identifier(args.worker)
    request=strict(args.matrix_request); census=strict(args.preparation)
    expected_request={'schema_version','sampling_plan_path','sampling_plan_sha256',
                      'candidate_manifest_path','candidate_manifest_sha256','slots'}
    if set(request)!=expected_request or type(request['schema_version']) is not int or request['schema_version']!=1:
        raise ValueError('Exact matrix census request required')
    authority=SamplingAuthority(root,request['sampling_plan_path'],request['sampling_plan_sha256'])
    slots=request['slots']
    if not isinstance(slots,list) or any(set(s)!={'catalog_id','seed','locale','size','difficulty','control_mode','packet_path','packet_sha256'} for s in slots):
        raise ValueError('Exact selected slots required')
    authority.validate_batch(slots)
    expected_census={'schema_version','collector_kind','request_path','request_sha256','candidate_manifest_path',
        'candidate_manifest_sha256','candidate_sha256','build_sha256','sampling_plan_path','sampling_plan_sha256',
        'locale_before','locale_after','native_compiled_setups','gameplay_certified','queue_success_enabled'}
    if set(census)!=expected_census or type(census['schema_version']) is not int or census['schema_version']!=1 or census['collector_kind']!='SkirmishMatrixCompiledSetupCensus':
        raise ValueError('Actual native matrix census schema differs')
    if census['request_sha256']!=queue.digest(args.matrix_request) or Path(census['request_path']).resolve()!=args.matrix_request.resolve():
        raise ValueError('Native request ownership/hash differs')
    for key in ('candidate_manifest_path','candidate_manifest_sha256','sampling_plan_path','sampling_plan_sha256'):
        if (Path(census[key]).resolve()!=Path(request[key]).resolve() if key.endswith("_path") else census[key]!=request[key]):
            raise ValueError("Native request join differs: "+key)
    if census['locale_before']!=census['locale_after'] or census['gameplay_certified'] is not False or census['queue_success_enabled'] is not False:
        raise ValueError('Locale restoration or unqualified boundary differs')
    if Path(request['candidate_manifest_path']).resolve()!=args.snapshot.resolve() or queue.digest(args.snapshot)!=request['candidate_manifest_sha256']:
        raise ValueError('Census immutable candidate snapshot differs')
    snapshot=queue.load(args.snapshot)
    if set(snapshot)!={'source_manifest','candidate_sha256','build_manifest','build_sha256'} or candidate_hash(snapshot['source_manifest'])!=snapshot['candidate_sha256'] or queue.fingerprint(snapshot['build_manifest'])!=snapshot['build_sha256']:
        raise ValueError('Complete actual source/build snapshot required')
    if any(census[k]!=snapshot[k] for k in ('candidate_sha256','build_sha256')):
        raise ValueError('Native candidate/build differs')
    log=args.preparation_log.read_text(encoding='utf-8-sig',errors='replace')
    marker='[SkirmishMatrixCompiledSetupCensus] result=Passed slots='+str(len(slots))+' gameplayCertified=0 queueSuccessEnabled=0 output='
    outputs=[line.split(marker,1)[1].strip() for line in log.splitlines() if marker in line]
    if len(outputs)!=1 or Path(outputs[0]).resolve()!=args.preparation.resolve() or re.findall(r'\[UnityInvoke\] ExitCode: (-?\d+)',log)!=['0'] or 'process exit code was unavailable' in log.lower():
        raise ValueError('Actual checked native census/log/exit join missing')
    if re.search(r'Crash!!!|result=Failed|suiteFailed=|error CS\d+:',log):
        raise ValueError('Native failure retained despite a marker')
    if args.matrix_exit_file is None or args.matrix_exit_file.read_text(encoding='utf-8-sig').strip()!='0':
        raise ValueError('Owned checked wrapper terminal exit file required')
    install=queue.load(args.integration_manifest)
    for row in install['files']:
        if queue.digest(root/row['target'])!=row['after_sha256']:
            raise ValueError('Reviewed census integration changed: '+row['target'])
    rows=census['native_compiled_setups']
    if not isinstance(rows,list) or len(rows)!=len(slots):raise ValueError('Every selected slot needs one actual native census')
    prepared=[]
    # Complete all validation before creating queue directories/claims.
    for slot,envelope in zip(slots,rows):
        expected_row={'schema_version','collector_kind','slot','native_setup_identity','actual_native_locale',
                      'native_config_sha256','definition_asset_path','definition_asset_sha256'}
        if set(envelope)!=expected_row or type(envelope['schema_version']) is not int or envelope['schema_version']!=1 or envelope['collector_kind']!='SkirmishNativeCompiledSetupCensus' or envelope['slot']!=slot:
            raise ValueError('Ordered native slot census differs')
        identity=validate_setup_identity(envelope['native_setup_identity'])
        if slot['catalog_id'] in ('S001','S025','S073'):
            raise ValueError('Protected legacy/counterpart uses existing separate schema2/3 authority')
        if any(identity[k]!=slot[k] or type(identity[k]) is not type(slot[k]) for k in ('catalog_id','seed','locale','size','difficulty')) or normalize_native_locale(envelope['actual_native_locale'])!=slot['locale']:
            raise ValueError('Actual native compiled configuration differs')
        if not queue.sha(envelope['native_config_sha256']):raise ValueError('Actual native config hash missing')
        relative=envelope['definition_asset_path']; path=queue.relative_file(root,relative)
        if not relative.startswith('Assets/') or not queue.sha(envelope['definition_asset_sha256']) or queue.digest(path)!=envelope['definition_asset_sha256']:
            raise ValueError('Actual definition asset changed')
        prepared.append((slot,identity,envelope))
    batch_id='batch-'+uuid.uuid4().hex; jobs=[]
    for index,(slot,identity,envelope) in enumerate(prepared):
        job=dict(schema_version=1,job_id=batch_id+'-'+str(index)+'-'+identity['catalog_id'],
            **{k:identity[k] for k in ('catalog_id','definition_id','definition_version','difficulty','size','seed','locale')},
            candidate_sha256=snapshot['candidate_sha256'],build_sha256=snapshot['build_sha256'],
            setup_sha256=sha256(identity),content_sha256=sha256(content_identity(identity)),
            source_manifest=snapshot['source_manifest'],packet_path=slot['packet_path'],packet_sha256=slot['packet_sha256'],
            control_mode=slot['control_mode'],required_stages=sorted(queue.STAGES),timeout_seconds=3000)
        queue.preflight(job,root,authority)
        jobs.append(job)
    authority.validate_batch(jobs)
    directories=[]
    for job,(_,identity,envelope) in zip(jobs,prepared):
        result=queue.enqueue(root/'Logs/SkirmishProduction/Queue',job,root,authority)
        directory=Path(result['job_directory']);queue.claim(directory,worker,root,authority);directories.append(directory)
        queue.write_new(directory/'NATIVE_PREPARATION_PROVENANCE.json',dict(
            preparation_sha256=queue.digest(args.preparation),preparation_log_sha256=queue.digest(args.preparation_log),
            checked_wrapper_exit_sha256=queue.digest(args.matrix_exit_file),request_sha256=queue.digest(args.matrix_request),
            native_setup_identity=identity,native_config_sha256=envelope['native_config_sha256'],
            definition_asset_path=envelope['definition_asset_path'],definition_asset_sha256=envelope['definition_asset_sha256'],
            integration_manifest_sha256=queue.digest(args.integration_manifest),snapshot_sha256=queue.digest(args.snapshot),
            native_canary_executed=False,queue_success_enabled=False))
    descriptor=prepare(root,directories,args.snapshot,batch_id,authority.path,authority.expected)
    print(json.dumps(dict(result='MatrixSlicePreparedUnqualified',descriptor=str(descriptor),jobs=len(jobs),
        queue_success_enabled=False,native_canaries_executed=0),indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--preparation', type=Path, required=True)
    parser.add_argument('--preparation-log', type=Path, required=True)
    parser.add_argument('--snapshot', type=Path, required=True)
    parser.add_argument('--worker', required=True)
    parser.add_argument('--integration-manifest', type=Path, required=True)
    parser.add_argument("--matrix-request", type=Path)
    parser.add_argument("--matrix-exit-file", type=Path)
    args = parser.parse_args()
    if args.matrix_request is not None:
        prepare_matrix_jobs(args); return
    root = args.project.resolve(); worker = queue.identifier(args.worker)
    census = queue.load(args.preparation); log = args.preparation_log.read_text(encoding='utf-8-sig', errors='replace')
    if census.get('schema_version') != 1 or census.get('collector_kind') != 'SkirmishProductionBatchNativePreparation' or \
            '[SkirmishProductionBatchPreparation] result=Passed sources=2 compiledIdentities=2 gameplayCertified=0' not in log or \
            '[UnityInvoke] ExitCode: 0' not in log or 'process exit code was unavailable' in log.lower():
        raise ValueError('Exact actual checked native preparation required; no caller-supplied setup hashes')
    install = queue.load(args.integration_manifest)
    for row in install['files']:
        if queue.digest(root / row['target']) != row['after_sha256']:
            raise ValueError('Exact batch integration source not installed: ' + row['target'])
    snapshot = queue.load(args.snapshot)
    if set(snapshot) != {'source_manifest', 'candidate_sha256', 'build_manifest', 'build_sha256'} or \
            candidate_hash(snapshot['source_manifest']) != snapshot['candidate_sha256'] or \
            queue.fingerprint(snapshot['build_manifest']) != snapshot['build_sha256']:
        raise ValueError('Complete frozen source/DLL manifest required')
    packet = 'Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md'
    batch_id = 'batch-' + uuid.uuid4().hex
    rows = census.get('native_compiled_setups')
    if not isinstance(rows, list) or len(rows) != 2:
        raise ValueError('Exactly two actual compiled census rows required')
    identities = [validate_setup_identity(row['native_setup_identity']) for row in rows]
    if [row['catalog_id'] for row in identities] != ['S004', 'S005']:
        raise ValueError('Exact ordered S004/S005 census required')
    jobs = []
    for identity, envelope in zip(identities, rows):
        expected = dict(size='Standard', difficulty='Regular', locale='EN', seed=155925 if identity['catalog_id']=='S004' else 155926)
        if any(identity[key] != value or type(identity[key]) is not type(value) for key, value in expected.items()) or \
                envelope.get('actual_native_locale') != 'en' or not queue.sha(envelope.get('native_config_sha256')):
            raise ValueError('Census differs from exact native two-canary preparation')
        job = dict(schema_version=1, job_id=batch_id+'-'+identity['catalog_id'],
                   **{key: identity[key] for key in ('catalog_id','definition_id','definition_version','difficulty','size','seed','locale')},
                   candidate_sha256=snapshot['candidate_sha256'], build_sha256=snapshot['build_sha256'],
                   setup_sha256=sha256(identity), content_sha256=sha256(content_identity(identity)),
                   source_manifest=snapshot['source_manifest'], packet_path=packet, packet_sha256=queue.digest(root/packet),
                   control_mode='aria', required_stages=sorted(queue.STAGES), timeout_seconds=3000)
        # Existing queue v1 independently rechecks actual source bytes. These
        # preparation passes are measured overhead, not hidden benchmark savings.
        result = queue.enqueue(root/'Logs/SkirmishProduction/Queue', job, root)
        directory = Path(result['job_directory']); queue.claim(directory, worker, root); jobs.append(directory)
        queue.write_new(directory/'NATIVE_PREPARATION_PROVENANCE.json', dict(
            preparation_sha256=queue.digest(args.preparation), preparation_log_sha256=queue.digest(args.preparation_log),
            native_setup_identity=identity, native_config_sha256=envelope['native_config_sha256'],
            integration_manifest_sha256=queue.digest(args.integration_manifest), snapshot_sha256=queue.digest(args.snapshot),
            native_canary_executed=False, queue_success_enabled=False))
    descriptor = prepare(root, jobs, args.snapshot, batch_id)
    print(json.dumps(dict(result='TwoDevelopmentCanariesPrepared', descriptor=str(descriptor),
                         queue_success_enabled=False, native_canaries_executed=0), indent=2))


if __name__ == '__main__': main()
