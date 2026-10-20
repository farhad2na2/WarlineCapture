"""Prepare two unqualified canonical claims from native compiled census; never launch Unity."""
import argparse
import hashlib
import json
from pathlib import Path
import uuid

import skirmish_production_queue as queue
from skirmish_native_identity import validate_setup_identity, content_identity, sha256
from s005_windows_source_snapshot import candidate_hash
from prepare_skirmish_production_batch import prepare, _prepare


# Deliberately local to this one preparation call; never a caller-provided cache/token.
MAX_MATRIX_PREPARATION_CHUNK = 16


class _MatrixPreparationObservation:
    def __init__(self, project, expected):
        import time
        import s005_windows_source_snapshot as actual
        self.project = Path(project).resolve()
        self._active = True
        self._bound_jobs = None
        self._extra = {}
        self._packets = {}
        started = time.monotonic()
        source = actual.snapshot(self.project)
        builds, build_hash = actual.build_snapshot(self.project)
        self._observed = dict(source_manifest=source, candidate_sha256=actual.candidate_hash(source),
                              build_manifest=builds, build_sha256=build_hash)
        queue.require(self._observed == expected, "complete actual source/build differs before matrix preparation")
        # Preserve standalone symlink/path containment once for the whole observation.
        for row in source + builds:
            queue.relative_file(self.project, row["path"])
        self._source = {row["path"]: row["sha256"] for row in source}
        self.seconds = time.monotonic() - started
        self.source_files = len(source)
        self.build_files = len(builds)

    def _check(self, project=None):
        queue.require(self._active and (project is None or Path(project).resolve() == self.project),
                      "closed or foreign matrix preparation observation")

    def digest(self, path):
        self._check()
        path = Path(path).resolve()
        queue.require(path.is_relative_to(self.project), "observed project input escapes root")
        relative = path.relative_to(self.project).as_posix()
        queue.relative_file(self.project, relative)
        if relative in self._source:
            return self._source[relative]
        if relative not in self._extra:
            self._extra[relative] = queue.digest(path)
        return self._extra[relative]

    def packet(self, relative, expected):
        self._check()
        # Canonical matrix packets live outside the three imported source trees.
        queue.require(relative.startswith("Design/Roadmap/Skirmish_Expansion/Scenarios/"),
                      "closed matrix path requires canonical scenario packet")
        path = queue.relative_file(self.project, relative)
        if relative not in self._packets:
            data = path.read_bytes()
            observed = hashlib.sha256(data).hexdigest()
            queue.require(relative not in self._extra or self._extra[relative] == observed,
                          "packet changed within preparation observation")
            self._extra[relative] = observed
            self._packets[relative] = data.decode("utf-8").replace("\r\n", "\n").replace("\r", "\n")
        queue.require(self._extra[relative] == expected, "packet missing or changed")
        return self._packets[relative]

    def check_job(self, job, project):
        self._check(project)
        queue.require(type(job.get("schema_version")) is int and job["schema_version"] == 1,
                      "closed matrix preparation supports schema1 only")
        queue.require(job["source_manifest"] == self._observed["source_manifest"] and
                      job["candidate_sha256"] == self._observed["candidate_sha256"] and
                      job["build_sha256"] == self._observed["build_sha256"],
                      "job differs from complete actual preparation observation")
        if self._bound_jobs is not None:
            queue.require(queue.fingerprint(job) in self._bound_jobs, "job changed after preparation validation")

    def bind_jobs(self, jobs):
        self._check()
        queue.require(self._bound_jobs is None and 1 <= len(jobs) <= MAX_MATRIX_PREPARATION_CHUNK,
                      "one bounded preparation chunk required")
        for job in jobs:
            self.check_job(job, self.project)
        keys = tuple(queue.fingerprint(job) for job in jobs)
        queue.require(len(set(keys)) == len(keys), "duplicate preparation job identity")
        self._bound_jobs = keys

    def check_order(self, jobs):
        self._check()
        queue.require(self._bound_jobs is not None and
                      tuple(queue.fingerprint(job) for job in jobs) == self._bound_jobs,
                      "queued job set/order changed during preparation")

    def close(self):
        self._active = False
        self._extra.clear()
        self._packets.clear()
        self._source.clear()
        self._observed = None


def _load_reviewed_matrix_census(args):
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
    if not 1 <= len(slots) <= MAX_MATRIX_PREPARATION_CHUNK:
        raise ValueError("Matrix preparation requires a bounded chunk of 1..16 actual census slots")
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
    return root,worker,request,census,snapshot,authority,slots,install


def prepare_matrix_jobs(args):
    from skirmish_native_identity import normalize_native_locale
    root,worker,request,census,snapshot,authority,slots,install=_load_reviewed_matrix_census(args)
    observation = _MatrixPreparationObservation(root,snapshot)
    try:
        for row in install['files']:
            if observation.digest(root/row['target'])!=row['after_sha256']:
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
            if not relative.startswith('Assets/') or not queue.sha(envelope['definition_asset_sha256']) or observation.digest(path)!=envelope['definition_asset_sha256']:
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
            queue._preflight(job,root,authority,_observation=observation)
            jobs.append(job)
        authority.validate_batch(jobs)
        observation.bind_jobs(jobs)
        directories=[]
        for job,(_,identity,envelope) in zip(jobs,prepared):
            observation.check_job(job,root)
            result=queue._enqueue_preflighted(root/'Logs/SkirmishProduction/Queue',job,root)
            directory=Path(result['job_directory']);queue._claim_checked(directory,worker,root,lambda queued: queue._preflight(queued,root,authority,_observation=observation));directories.append(directory)
            queue.write_new(directory/'NATIVE_PREPARATION_PROVENANCE.json',dict(
                preparation_sha256=queue.digest(args.preparation),preparation_log_sha256=queue.digest(args.preparation_log),
                checked_wrapper_exit_sha256=queue.digest(args.matrix_exit_file),request_sha256=queue.digest(args.matrix_request),
                native_setup_identity=identity,native_config_sha256=envelope['native_config_sha256'],
                definition_asset_path=envelope['definition_asset_path'],definition_asset_sha256=envelope['definition_asset_sha256'],
                integration_manifest_sha256=queue.digest(args.integration_manifest),snapshot_sha256=queue.digest(args.snapshot),
                native_canary_executed=False,queue_success_enabled=False))
        observation.check_order([queue.load(directory/'job.json') for directory in directories])
        descriptor=_prepare(root,directories,args.snapshot,batch_id,authority.path,authority.expected,
                            _validate=lambda queued,project,sampling: queue._preflight(queued,project,sampling,_observation=observation))
        print(json.dumps(dict(result='MatrixSlicePreparedUnqualified',descriptor=str(descriptor),jobs=len(jobs),
            queue_success_enabled=False,native_canaries_executed=0,complete_source_observations=1,
            observed_source_files=observation.source_files,observed_dlls=observation.build_files,
            source_observation_seconds=observation.seconds),indent=2))

    finally:
        observation.close()


def _manual_paid_plane_authority(args,root,census,snapshot):
    from skirmish_production_matrix_authority import strict
    path=Path(args.manual_paid_plane_authority).resolve()
    queue.require(path.is_relative_to(root/'Logs/SkirmishProduction'),'Owned supplemental authority path')
    queue.relative_file(root,path.relative_to(root).as_posix())
    queue.require(queue.sha(args.manual_paid_plane_authority_sha256) and queue.digest(path)==args.manual_paid_plane_authority_sha256,'Pinned supplemental authority drift')
    a=strict(path)
    fields={'schema_version','kind','approved','purpose','subject','native_request','native_census','snapshot','integration_manifest','preparation_log','wrapper_exit','candidate_sha256','build_sha256','queue_success_enabled','normal_mission_accepted'}
    queue.require(set(a)==fields and type(a['schema_version'])is int and a['schema_version']==1 and a['kind']=='ScopedManualPaidPlaneSupplementAuthority-v1' and a['approved']is True and a['purpose']=='NormalPaidTransportCruiseCameraOnly' and a['queue_success_enabled']is False and a['normal_mission_accepted']is False,'Exact unqualified manual supplemental authority')
    refs={'native_request':args.matrix_request,'native_census':args.preparation,'snapshot':args.snapshot,'integration_manifest':args.integration_manifest,'preparation_log':args.preparation_log,'wrapper_exit':args.matrix_exit_file}
    for name,expected in refs.items():
        ref=a[name];queue.require(isinstance(ref,dict) and set(ref)=={'path','sha256'} and queue.sha(ref['sha256']),'Exact authority reference '+name)
        target=Path(ref['path']);target=target if target.is_absolute()else root/target
        queue.require(target.resolve()==Path(expected).resolve() and target.resolve().is_relative_to(root),'Authority path join '+name)
        queue.relative_file(root,target.resolve().relative_to(root).as_posix())
        queue.require(queue.digest(target)==ref['sha256'],'Authority literal join '+name)
    expected=dict(catalog_id='S005',seed=155926,locale='EN',size='Standard',difficulty='Regular',control_mode='manual',packet_path='Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md')
    subject=a['subject'];queue.require(isinstance(subject,dict) and set(subject)==set(expected)|{'packet_sha256'} and all(type(subject.get(k))is type(v) and subject[k]==v for k,v in expected.items()) and queue.sha(subject['packet_sha256']),'Exact manual supplemental subject')
    queue.require(all(a[k]==snapshot[k]==census[k]for k in ('candidate_sha256','build_sha256')),'Supplemental current candidate/build join')
    queue.require(queue.digest(path)==args.manual_paid_plane_authority_sha256,'Authority changed during validation')
    return a


def prepare_manual_paid_plane(args):
    from skirmish_native_identity import normalize_native_locale
    root,worker,request,census,snapshot,authority,slots,install=_load_reviewed_matrix_census(args)
    a=_manual_paid_plane_authority(args,root,census,snapshot);subject=a['subject']
    expected_slot={**subject,'control_mode':'aria'}
    queue.require(slots==[expected_slot],'Exact one actual ARIA configuration census; manual authority remains separate')
    rows=census['native_compiled_setups'];queue.require(isinstance(rows,list) and len(rows)==1,'One actual compiled setup')
    envelope=rows[0]
    fields={'schema_version','collector_kind','slot','native_setup_identity','actual_native_locale','native_config_sha256','definition_asset_path','definition_asset_sha256'}
    queue.require(set(envelope)==fields and type(envelope['schema_version'])is int and envelope['schema_version']==1 and envelope['collector_kind']=='SkirmishNativeCompiledSetupCensus' and envelope['slot']==expected_slot,'Exact static census envelope')
    identity=validate_setup_identity(envelope['native_setup_identity'])
    queue.require(all(type(identity[k])is type(subject[k]) and identity[k]==subject[k]for k in ('catalog_id','seed','locale','size','difficulty')) and normalize_native_locale(envelope['actual_native_locale'])=='EN' and queue.sha(envelope['native_config_sha256']),'Actual native EN configuration identity')
    observation=_MatrixPreparationObservation(root,snapshot)
    try:
        for row in install['files']:
            queue.require(observation.digest(root/row['target'])==row['after_sha256'],'Reviewed integration drift '+row['target'])
        relative=envelope['definition_asset_path'];definition=queue.relative_file(root,relative)
        queue.require(relative.startswith('Assets/') and queue.sha(envelope['definition_asset_sha256']) and observation.digest(definition)==envelope['definition_asset_sha256'],'Actual native definition drift')
        batch_id='batch-'+uuid.uuid4().hex
        job=dict(schema_version=1,job_id=batch_id+'-0-S005',**{k:identity[k]for k in ('catalog_id','definition_id','definition_version','difficulty','size','seed','locale')},candidate_sha256=snapshot['candidate_sha256'],build_sha256=snapshot['build_sha256'],setup_sha256=sha256(identity),content_sha256=sha256(content_identity(identity)),source_manifest=snapshot['source_manifest'],packet_path=subject['packet_path'],packet_sha256=subject['packet_sha256'],control_mode='manual',required_stages=sorted(queue.STAGES),timeout_seconds=3000)
        queue._preflight(job,root,None,_observation=observation);observation.bind_jobs([job])
        directory=Path(queue._enqueue_preflighted(root/'Logs/SkirmishProduction/Queue',job,root)['job_directory'])
        validate=lambda queued,project=root,sampling=None:queue._preflight(queued,project,None,_observation=observation)
        queue._claim_checked(directory,worker,root,lambda queued:validate(queued))
        queue.write_new(directory/'NATIVE_PREPARATION_PROVENANCE.json',dict(supplemental_authority_path=str(args.manual_paid_plane_authority.resolve()),supplemental_authority_sha256=args.manual_paid_plane_authority_sha256,native_setup_identity=identity,native_config_sha256=envelope['native_config_sha256'],static_census_control_mode='aria',job_control_mode='manual',normal_mission_accepted=False,queue_success_enabled=False,native_canary_executed=False))
        observation.check_order([queue.load(directory/'job.json')])
        answer=dict(result='ManualPaidPlaneSupplementPreparedUnqualified',job_path=str(directory/'job.json'),claim_path=str(directory/'claim.json'),provenance_path=str(directory/'NATIVE_PREPARATION_PROVENANCE.json'),jobs=1,complete_source_observations=1,normal_mission_accepted=False,queue_success_enabled=False,native_canaries_executed=0)
        print(json.dumps(answer,indent=2));return answer
    finally:observation.close()


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
    parser.add_argument('--manual-paid-plane-authority',type=Path)
    parser.add_argument('--manual-paid-plane-authority-sha256')
    args = parser.parse_args()
    if (args.manual_paid_plane_authority is None)!=(args.manual_paid_plane_authority_sha256 is None):
        raise ValueError('Both pinned manual supplemental authority arguments required')
    if args.manual_paid_plane_authority is not None:
        if args.matrix_request is None:raise ValueError('Actual checked static matrix census required')
        prepare_manual_paid_plane(args);return
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
