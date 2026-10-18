"""Disabled-by-default finite Mac normal-mission worker. No queue-success API.

--plan reads a disabled contract without writing or claiming. Execution requires a
separate exact SHA-bound, expiring Root grant. Constructed tests are not gameplay.
"""
import argparse
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import stat
import subprocess
import sys
import time
import zlib

import mac_tanker_probe35_controller as life
import skirmish_production_queue as queue
from skirmish_production_matrix_authority import SamplingAuthority
from prepare_skirmish_production_batch import prepare
from invoke_skirmish_production_batch import METHOD, observe_endpoint, verify_mac_wrapper
from collect_skirmish_batch_inspection import build_spec, collect_and_inspect
from skirmish_production_operational_inspector import Reader, one, ownership

LIFECYCLE_SHA = '1ffe15144cde97e971e7cd97e137bab0040020ad56f71fb25edde9ef0d18d6b6'
STAGES = ('setup', 'opening', 'result', 'return')
SUFFIX = dict(setup='.setup.png', opening='.opening.png',
              result='.terminal-result.png', **{'return': '.terminal-destination.png'})
MAX_ARTIFACT = 16 * 1024 * 1024
REVIEWS = {'source_build', 'normal_menu_input_api', 'source_fit', 'settings_global_gui', 'toolchain',
           'dispatch_partition'}


def host_identity():
    return dict(node=platform.node(), uid=getattr(os, 'getuid', lambda: None)())


def require_runtime():
    need(sys.version_info >= (3,12), 'Use an already-installed Python3.12+ runtime; shipping Reader is unchanged')


def runtime_identity():
    p=Path(sys.executable).resolve()
    return dict(executable=str(p),sha256=digest(p),version=list(sys.version_info[:3]))


def need(value, message):
    if not value:
        raise ValueError(message)


def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda: f.read(65536), b''):
            h.update(block)
    return h.hexdigest()


def strict(text):
    def pairs(rows):
        value = {}
        for k, v in rows:
            need(k not in value, 'Duplicate JSON key')
            value[k] = v
        return value
    return json.loads(text, object_pairs_hook=pairs,
                      parse_constant=lambda _: (_ for _ in ()).throw(ValueError('Nonfinite JSON')))


def bounded_file(path, limit=MAX_ARTIFACT):
    p = Path(path).absolute()
    for ancestor in (p, *p.parents):
        s = ancestor.lstat()
        need(not stat.S_ISLNK(s.st_mode) and not (getattr(s, 'st_file_attributes', 0) & 0x400),
             'Symlink/reparse artifact refused')
    fd = os.open(str(p), os.O_RDONLY | getattr(os, 'O_NOFOLLOW', 0))
    try:
        before = os.fstat(fd)
        need(stat.S_ISREG(before.st_mode) and 0 < before.st_size <= limit, 'Bounded regular artifact required')
        chunks = []; size = 0
        while True:
            block = os.read(fd, min(65536, limit + 1 - size))
            if not block:
                break
            chunks.append(block); size += len(block)
            need(size <= limit, 'Artifact grew beyond byte budget')
        after = os.fstat(fd)
        need((before.st_dev, before.st_ino, before.st_size, before.st_mtime_ns) ==
             (after.st_dev, after.st_ino, after.st_size, after.st_mtime_ns) and size == before.st_size,
             'Artifact changed during read')
        return b''.join(chunks)
    finally:
        os.close(fd)


def read_ref(ref):
    need(isinstance(ref, dict) and set(ref) == {'path', 'sha256'}, 'Exact byte reference required')
    b = bounded_file(ref['path'])
    need(hashlib.sha256(b).hexdigest() == ref['sha256'], 'Pinned artifact bytes changed')
    return strict(b.decode('utf-8-sig'))


def full_settings(root, global_roots):
    """No filters or restoration. Global scope must be independently reviewed."""
    result = {}
    for tree in [root/'ProjectSettings', root/'UserSettings', *map(Path, global_roots)]:
        need(tree.is_absolute(), 'Absolute settings inventory root required')
        for ancestor in (tree, *tree.parents):
            need(not ancestor.is_symlink(), 'Settings root symlink refused')
        rows = []
        if tree.exists():
            need(tree.is_dir(), 'Settings inventory root is not a directory')
            files = sorted(tree.rglob('*'))
            need(len(files) <= 100000, 'Settings inventory entry budget exceeded')
            for p in files:
                s = p.lstat()
                need(not stat.S_ISLNK(s.st_mode), 'Settings symlink refused')
                if p.is_file():
                    b = bounded_file(p, 64*1024*1024)
                    rows.append(dict(path=p.relative_to(tree).as_posix(), sha256=hashlib.sha256(b).hexdigest(),
                                     size=len(b), uid=s.st_uid, gid=s.st_gid, mode=stat.S_IMODE(s.st_mode)))
                else:
                    need(p.is_dir(), 'Unexpected settings object')
                    rows.append(dict(path=p.relative_to(tree).as_posix(), directory=True))
        result[str(tree)] = dict(exists=tree.exists(), rows=rows)
    return result


def core_snapshot(value):
    return {k: value[k] for k in ('source_manifest', 'candidate_sha256', 'build_manifest', 'build_sha256')}


def checked_command(root, descriptor, timeout):
    # This real entry schedules readiness/Playing/return and exits itself. NO -quit.
    return ['rtk', 'proxy', str(root/'Tools/CI/invoke_unity_macos.sh'), '--project', str(root),
            '--timeout', str(timeout), '--log', str(Path(descriptor['artifact_directory'])/'unity.log'),
            '--', '-buildTarget', 'Android', '-executeMethod', METHOD]


def child_environment(parent, descriptor):
    env = {k: v for k, v in parent.items() if not k.upper().startswith('WARLINE_')}
    env['WARLINE_SKIRMISH_PRODUCTION_BATCH'] = str(descriptor)
    return env


def validate_grant(grant, expected_sha, raw, root, now=None):
    """A proposed schema, not an existing native API or permission source."""
    require_runtime()
    fields = {'schema_version', 'launch_authorized', 'nonce', 'issued_utc', 'expires_utc', 'project',
              'host', 'worker', 'controller_sha256', 'dependency_pins', 'source', 'settings',
              'sampling_plan', 'jobs', 'timeout_seconds', 'capacity', 'tool_pins', 'reviews', 'python_runtime'}
    need(isinstance(grant, dict) and set(grant) == fields, 'Exact new worker grant schema required')
    need(hashlib.sha256(raw).hexdigest() == expected_sha, 'Explicit grant SHA mismatch')
    need(grant['schema_version'] == 1 and type(grant['schema_version']) is int and
         grant['launch_authorized'] is True, 'Fresh Root execution grant required')
    need(re.fullmatch('[a-f0-9]{32}', grant['nonce'] or ''), 'Fresh unique nonce required')
    current = now or dt.datetime.now(dt.timezone.utc)
    issued = dt.datetime.fromisoformat(grant['issued_utc']); expires = dt.datetime.fromisoformat(grant['expires_utc'])
    need(issued.tzinfo is not None and expires.tzinfo is not None and issued <= current < expires
         and 0 < (expires-issued).total_seconds() <= 900, 'Grant expired or not bounded to fifteen minutes')
    need(grant['project'] == str(root.resolve()), 'Grant project binding differs')
    need(grant['host'] == host_identity(), 'Per-host/UID grant differs')
    need(grant['python_runtime'] == runtime_identity(), 'Actual Python runtime bytes/version differ from grant')
    queue.identifier(grant['worker'])
    need(type(grant['timeout_seconds']) is int and 1 <= grant['timeout_seconds'] <= 28800, 'Bounded wrapper timeout')
    need(grant['controller_sha256'] == digest(Path(__file__)), 'Worker code not bound to grant')
    need(isinstance(grant['source'],dict) and set(grant['source']) ==
         {'head','parent','branch','candidate_sha256','build_sha256','manifest_ref','wrapper_sha256'},
         'Exact new source/candidate/build contract required')
    need(all(isinstance(grant['source'][k],str) and re.fullmatch('[a-f0-9]{40}',grant['source'][k])
             for k in ('head','parent')) and isinstance(grant['source']['branch'],str) and grant['source']['branch'],
         'Actual new source Git identities required')
    need(all(isinstance(grant['source'][k],str) and re.fullmatch('[a-f0-9]{64}',grant['source'][k])
             for k in ('candidate_sha256','build_sha256','wrapper_sha256')), 'Exact source/build/wrapper hashes required')
    need(grant['dependency_pins'] and isinstance(grant['dependency_pins'], dict), 'Reviewed dependency closure missing')
    # Closed imported local Python dependency census, not a caller-selected subset.
    actual = local_dependency_pins()
    need(grant['dependency_pins'] == actual, 'Complete actual worker dependency bytes differ')
    need(digest(Path(life.__file__)) == LIFECYCLE_SHA, 'Reviewed lifecycle implementation changed')
    need(isinstance(grant['jobs'], list) and 1 <= len(grant['jobs']) <= 720 and all(
         isinstance(row,dict) and set(row)=={'directory','job_sha256'} and
         isinstance(row['directory'],str) and Path(row['directory']).is_absolute() and
         isinstance(row['job_sha256'],str) and re.fullmatch('[a-f0-9]{64}',row['job_sha256'])
         for row in grant['jobs']), 'Exact finite queued-job hash references required')
    need(set(grant['reviews']) == REVIEWS, 'All execution-scope reviews required')
    for kind, ref in grant['reviews'].items():
        review = read_ref(ref)
        need(review.get('kind') == kind and review.get('execution_scope_reviewed') is True
             and review.get('candidate_sha256') == grant['source']['candidate_sha256']
             and review.get('build_sha256') == grant['source']['build_sha256'], 'Review subject mismatch')
        if kind in ('source_fit','dispatch_partition'):
            need(review.get('reviewed_job_sha256s') == sorted(row['job_sha256'] for row in grant['jobs']),
                 'Source-fit review does not bind every exact queued setup/slot')
        if kind == 'dispatch_partition':
            need(review.get('worker') == grant['worker'] and review.get('host') == grant['host'] and
                 review.get('nonce') == grant['nonce'] and review.get('exclusive_slots_reserved_by_coordinator') is True,
                 'Cross-host canonical slot partition is not bound to this host/worker/nonce')
    need(isinstance(grant['settings'],dict) and set(grant['settings']) ==
         {'global_gui_scope_reviewed','global_roots','shared_writer_policy','baseline_ref'},
         'Exact settings/global scope contract required')
    need(grant['settings'].get('global_gui_scope_reviewed') is True and
         grant['settings'].get('global_roots') and grant['settings'].get('shared_writer_policy') == 'NoOtherEditors',
         'Explicit complete global-GUI scope/shared writer review pending')
    roots = grant['settings']['global_roots']
    need(isinstance(roots,list) and len(roots)==len(set(roots)) and
         all(isinstance(p,str) and Path(p).is_absolute() and '..' not in Path(p).parts for p in roots),
         'Exact unique absolute global GUI inventory roots required')
    need(isinstance(grant['capacity'],dict) and set(grant['capacity']) ==
         {'minimum_free_bytes','minimum_memory_pressure_free_percent','max_native_workers_on_host'},
         'Exact per-host capacity limits required')
    need(type(grant['capacity']['minimum_free_bytes']) is int and grant['capacity']['minimum_free_bytes'] >= 25*1024**3
         and type(grant['capacity']['minimum_memory_pressure_free_percent']) is int
         and grant['capacity']['minimum_memory_pressure_free_percent'] >= 15
         and grant['capacity']['minimum_memory_pressure_free_percent'] <= 100
         and type(grant['capacity']['max_native_workers_on_host']) is int
         and grant['capacity']['max_native_workers_on_host'] == 1, 'Per-host capacity grant weaker than checked gates')
    need(isinstance(grant['jobs'], list) and 1 <= len(grant['jobs']) <= 720, 'Finite reviewed baseline slice required')
    need(len({x['directory'] for x in grant['jobs']}) == len(grant['jobs']), 'Duplicate canonical job directory')


def local_dependency_pins():
    import ast
    base = Path(__file__).parent
    pending = [Path(__file__)]; found = {}
    while pending:
        p = pending.pop()
        if p.name in found:
            continue
        found[p.name] = digest(p)
        for node in ast.walk(ast.parse(p.read_text())):
            modules = [a.name.split('.')[0] for a in node.names] if isinstance(node, ast.Import) else (
                [node.module.split('.')[0]] if isinstance(node, ast.ImportFrom) and node.module else [])
            pending.extend(base/(m+'.py') for m in modules if (base/(m+'.py')).is_file())
    return found


def validate_jobs(root, grant):
    authority = SamplingAuthority(root, grant['sampling_plan']['path'], grant['sampling_plan']['sha256'])
    need(authority.kind == 'Baseline720' and len(authority.slots) == 720, 'Full canonical720 baseline authority required')
    jobs = []; directories = []
    for row in grant['jobs']:
        need(set(row) == {'directory', 'job_sha256'}, 'Exact queued-job reference required')
        folder = Path(row['directory']).absolute()
        need(folder.is_relative_to(root/'Logs/SkirmishProduction') and not folder.is_relative_to(root/'Assets'),
             'Use canonical project-local durable queue directories')
        job = strict(bounded_file(folder/'job.json').decode('utf-8-sig'))
        need(queue.fingerprint(job) == row['job_sha256'] and folder.name == job['job_id'], 'Queued job byte identity differs')
        need(not (folder/'claim.json').exists(), 'Fresh claim only; existing claim is never adopted/reclaimed')
        need(job['control_mode'] == 'aria' and job['size'] == 'Standard' and job['difficulty'] == 'Regular',
             'Normal ARIA baseline only; no manual/scoped cruise replacement')
        need(job.get('schema_version') != 2,
             'Legacy preset schema2 identity lacks published normal collector support; retain all720 slots and require a separately reviewed typed legacy adapter before claiming, never automatic counterpart substitution')
        need(job.get('schema_version') != 3,
             'Development-only schema3 counterpart is not the protected legacy normal720 identity; preserve the slot and do not substitute practice evidence')
        need(job['candidate_sha256'] == grant['source']['candidate_sha256'] and
             job['build_sha256'] == grant['source']['build_sha256'], 'Job candidate/build differs')
        queue.preflight(job, root, authority)
        jobs.append(job); directories.append(folder)
    authority.validate_batch(jobs)
    return authority, directories


def claim_and_prepare(root, grant, authority, directories, snapshot_path, claimed):
    """Real canonical transactions. A partial claim failure remains owned/preserved."""
    for folder in directories:
        record = queue.claim(folder, grant['worker'], root, authority)
        claimed.append(dict(directory=str(folder), claim=record))
    return prepare(root, directories, snapshot_path, 'mac-'+grant['nonce'],
                   authority.path, authority.expected)


class HostLease:
    def __init__(self, path, nonce):
        self.path = Path(path); self.nonce = nonce; self.identity = None

    def acquire(self):
        fd = os.open(str(self.path), os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        try:
            os.write(fd, self.nonce.encode('ascii')); s = os.fstat(fd)
            self.identity = s.st_dev, s.st_ino
        finally:
            os.close(fd)

    def release_after_shutdown(self, safe):
        need(safe is True and self.identity is not None, 'No verified safe lease release')
        s = self.path.lstat()
        need(not self.path.is_symlink() and (s.st_dev, s.st_ino) == self.identity
             and bounded_file(self.path) == self.nonce.encode('ascii'), 'Host lease identity changed; preserve it')
        self.path.unlink()  # Only this controller's exclusive metadata lease, never Unity/cache/settings.


class NormalOwnedTree(life.OwnedProcessTree):
    """A shared GUI writer is a latched failure, never a process to terminate."""
    def observe(self, rows, root):
        owned = super().observe(rows, root)
        keys = {life.process_key(row) for row in owned}
        need(all(life.process_key(row) in keys for row in rows if life.editor(row)),
             'Another Editor entered the shared GUI lane; preserve it and latch failure')
        return owned


def png_shape(path):
    data = bounded_file(path)
    need(data[:8] == b'\x89PNG\r\n\x1a\n', 'Actual PNG bytes required')
    offset = 8; chunks = []; dimensions = None
    while offset < len(data):
        need(offset+12 <= len(data), 'Truncated PNG chunk')
        length = int.from_bytes(data[offset:offset+4], 'big'); kind = data[offset+4:offset+8]
        end = offset+12+length; need(end <= len(data), 'Truncated PNG payload')
        payload = data[offset+8:offset+8+length]
        need(zlib.crc32(kind+payload) & 0xffffffff == int.from_bytes(data[end-4:end], 'big'), 'PNG CRC differs')
        if not chunks:
            need(kind == b'IHDR' and length == 13, 'PNG IHDR required')
            dimensions = int.from_bytes(payload[:4], 'big'), int.from_bytes(payload[4:8], 'big')
            need(all(1 <= n <= 16384 for n in dimensions) and dimensions[0]*dimensions[1] <= 67108864,
                 'Bounded PNG dimensions required')
        chunks.append(kind); offset = end
        if kind == b'IEND':
            need(length == 0 and offset == len(data), 'Trailing PNG bytes')
            break
    need(b'IDAT' in chunks and chunks[-1] == b'IEND', 'PNG image payload/terminal chunk missing')
    return dict(sha256=hashlib.sha256(data).hexdigest(), size=len(data), width=dimensions[0], height=dimensions[1],
                visual_review='Pending_BytesAndStructureOnly_NotRenderedAcceptance')


def normal_artifacts(root, spec, inspection):
    """Join actual collector subjects; no screenshot-only or scoped success."""
    need(inspection.get('result') == 'NormalCompletedUnqualified' and not inspection.get('errors'),
         'Abort/Blocked/incomplete run stops worker; preserve partial evidence')
    need(len(spec['attempts']) == len(inspection['attempts']), 'All normal attempts required')
    profiles = inspection.get('admission', {}).get('profile_receipts', [])
    need(len(profiles) == len(spec['attempts']) and profiles and
         all(row.get('profile_restored') is True for row in profiles),
         'Every normal attempt requires all three actual profile boundaries')
    read = Reader(root); receipts = []
    _, batch_events = read.lines(spec['batch_events'])
    baseline = one(batch_events, 'BatchCaptured')['baseline_ownership_census']
    before_play = [r['facts'] for r in batch_events if r['kind'] == 'Dispatch']
    cleanup = [r['facts'] for r in batch_events if r['kind'] == 'CleanupOwnershipCensus']
    need(len(before_play) == len(cleanup) == len(spec['attempts']), 'All three actor accounting boundaries required')
    for index, (item, measured) in enumerate(zip(spec['attempts'], inspection['attempts'])):
        _, job = read.obj(item['job']); _, claim = read.obj(item['claim'])
        need(measured['job_sha256'] == queue.fingerprint(job) and measured['attempt_id'] == claim['attempt_id']
             and measured['outcome'] in ('Victory', 'Defeat', 'Draw'), 'Actual normal outcome/attempt differs')
        need(before_play[index]['job_sha256'] == cleanup[index]['job_sha256'] == measured['job_sha256'] and
             before_play[index]['attempt_id'] == claim['attempt_id'], 'Actor accounting subject splice')
        before_census = before_play[index]['before_play_ownership_census']
        need(ownership(baseline) == ownership(before_census) == ownership(cleanup[index]['census']),
             'Baseline/before-play/after-return actor or scene accounting differs')
        _, native = read.lines(item['native_events'])
        snapshots = [r['facts'] for r in native if r['kind'] == 'NormalMenuLaunchObserved']
        need(len(snapshots) == 1, 'One normal Menu owner required')
        live = Path(snapshots[0]['native_trace_path'])
        need(live.is_relative_to(Path(claim['artifact_directory'])), 'Screenshot live trace escapes owned attempt')
        images = {}
        for stage in STAGES:
            path = Path(str(live)+SUFFIX[stage]); pinned = read.derived(path)
            image = png_shape(path)
            need(image['sha256'] == read.pins[pinned], 'Screenshot changed between ownership pin and byte capture')
            images[stage] = dict(path=str(path), **image)
        need(len({v['path'] for v in images.values()}) == 4, 'Same-attempt distinct stage images required')
        receipts.append(dict(job_sha256=measured['job_sha256'], attempt_id=claim['attempt_id'],
                             outcome=measured['outcome'], recorded_loss=measured['outcome'] in ('Defeat', 'Draw'),
                             screenshots=images, actor_accounting=dict(batch_events=spec['batch_events'],
                                 baseline=baseline, before_play=before_census, after_owned_return=cleanup[index]['census'],
                                 scope='NativeOwnedObjectAndSceneTeardown_NotWholeMissionActorConservation')))
    read.stable()
    return dict(attempts=receipts, normal_outcomes_recorded_unqualified=True,
                three_boundary_profile=profiles,
                three_boundary_camera_actor='Pending_NoCompletePublishedNormalWitness',
                qualified=False, queue_success_enabled=False, normal_720_credit=0,
                visual_player_device_acceptance='Pending')


def prelaunch(root, grant):
    def git(*args):
        return subprocess.check_output(['rtk', 'proxy', 'git', '-C', str(root), *args], text=True).strip()
    source = grant['source']
    need(git('rev-parse', 'HEAD') == source['head'] and git('rev-parse', 'HEAD^') == source['parent']
         and git('branch', '--show-current') == source['branch'] and
         not git('status', '--porcelain=v1', '--untracked-files=normal'), 'Strict complete clean candidate Git identity differs')
    need(digest(root/'Tools/CI/invoke_unity_macos.sh') == source['wrapper_sha256'], 'Checked wrapper bytes differ')
    # Global GUI is shared. Preserve every other Editor; defer rather than stop it.
    need(not any(life.editor(row) for row in life.processes()), 'Another Editor exists; preserve shared GUI ownership')
    capacity = life.capacity(root)
    need(capacity['free_bytes'] >= grant['capacity']['minimum_free_bytes'], 'Per-host disk admission failed')
    match = re.search(r'free percentage:\s*(\d+)%', capacity['memory_pressure'])
    need(match and int(match[1]) >= grant['capacity']['minimum_memory_pressure_free_percent'], 'Per-host memory admission failed')
    tooling = life.tools_state(root)
    need(tooling['actual_tool_pins'] == grant['tool_pins'], 'Actual host toolchain differs from reviewed grant')
    value = life.state_snapshot(root)
    need(core_snapshot(value) == read_ref(source['manifest_ref']), 'Complete source/build candidate differs')
    need(value['candidate_sha256'] == source['candidate_sha256'] and
         value['build_sha256'] == source['build_sha256'], 'Grant candidate/build labels differ from actual rows')
    settings = full_settings(root, grant['settings']['global_roots'])
    need(settings == read_ref(grant['settings']['baseline_ref']), 'Exact project/user/global settings baseline differs')
    return value, settings, capacity, tooling


def execute(root, grant, grant_sha):
    """One finite native batch. All claims stay unqualified for canonical review."""
    output = root/'Logs/SkirmishProduction/MacNormalWorkers'/grant['nonce']
    output.mkdir(parents=True, exist_ok=False)
    result = dict(schema_version=1, result='Incomplete', claims=[], launch_performed=False,
                  qualified=False, queue_success_enabled=False, normal_720_credit=0, lifecycle_errors=[])
    parent = dict(os.environ); child = None; tree = None; descriptor_path = None
    lease = HostLease(Path('/private/tmp/warline-mac-normal-worker.host-lock'), grant['nonce'])
    lease.acquire(); safe = False
    try:
        before, settings, capacity, tools = prelaunch(root, grant)
        authority, folders = validate_jobs(root, grant)
        queue.write_new(output/'before-state.json', before)
        queue.write_new(output/'before-settings.json', settings)
        queue.write_new(output/'capacity.json', capacity); queue.write_new(output/'toolchain.json', tools)
        snapshot = output/'candidate.json'; queue.write_new(snapshot, core_snapshot(before))
        descriptor_path = claim_and_prepare(root, grant, authority, folders, snapshot, result['claims'])
        descriptor = queue.load(descriptor_path); batch = Path(descriptor['artifact_directory'])
        observe_endpoint(root, descriptor, batch, 'before')
        # Fresh admission after claim/descriptor IO; never launder the old gate.
        prelaunch(root, grant); authority.recheck()
        env = child_environment(parent, descriptor_path); argv = checked_command(root, descriptor, grant['timeout_seconds'])
        baseline = life.processes()
        with (batch/'checked-wrapper-console.log').open('x', encoding='utf-8') as console:
            child = subprocess.Popen(argv, cwd=root, env=env, stdout=console, stderr=subprocess.STDOUT)
            result.update(launch_performed=True, wrapper_pid=child.pid, argv=argv)
            tree = NormalOwnedTree(child.pid, baseline)
            identity = life.kernel_process_identity(child.pid)
            queue.write_new(output/'original-wrapper-handle.json', dict(pid=child.pid, identity=identity,
                            controller_identity=life.kernel_process_identity(os.getpid()), grant_sha256=grant_sha))
            life.wait_original_wrapper(child, tree, root, result)
        shutdown, _ = life.owned_shutdown_barrier(tree, root)
        result['owned_shutdown'] = shutdown
        need(shutdown['closed'], 'Owned process shutdown incomplete')
        safe = result.get('wrapper_reaped') is True
        after = life.state_snapshot(root); after_settings = full_settings(root, grant['settings']['global_roots'])
        queue.write_new(output/'raw-after-state.json', after); queue.write_new(output/'raw-after-settings.json', after_settings)
        observe_endpoint(root, descriptor, batch, 'after')
        unchanged = before == after and settings == after_settings
        outer = dict(result, schema_version=1, platform='macos', project=str(root), execute_method=METHOD,
                     checked_wrapper='Tools/CI/invoke_unity_macos.sh', descriptor_sha256=digest(descriptor_path),
                     descriptor_fingerprint=queue.fingerprint(descriptor), checked_wrapper_exit=result.get('wrapper_exit'),
                     wrapper_birth=list(tree.root_key) if tree.root_key else None,
                     parent_environment_unchanged=dict(os.environ) == parent, source_settings_unchanged=unchanged,
                     observed_target_editor=tree.saw_target_editor, grant_sha256=grant_sha,
                     timeout_seconds=grant['timeout_seconds'])
        queue.write_new(batch/'outer-wrapper-receipt.json', outer)
        verify_mac_wrapper(descriptor, outer, root)
        ref = dict(path=str(descriptor_path), sha256=digest(descriptor_path))
        inspection = collect_and_inspect(root, ref, wrapper_platform='macos')
        queue.write_new(output/'normal-inspection.json', inspection)
        spec = build_spec(root, ref)
        measured = normal_artifacts(root, spec, inspection)
        queue.write_new(output/'normal-artifacts.json', measured)
        result.update(result='NormalCompletedUnqualified', normal=measured)
    except BaseException as error:
        life.lifecycle_error(result, 'normal_worker', error)
    finally:
        # Never abandon an original Popen on a file/observer exception.
        if child is not None and not result.get('wrapper_reaped'):
            # An unavailable tree remains unavailable. Never reconstruct/adopt
            # ownership from a later PID inventory merely to pass shutdown.
            life.wait_original_wrapper(child, tree, root, result)
        if child is not None and not safe:
            try:
                shutdown, _ = life.owned_shutdown_barrier(tree, root)
                result['owned_shutdown'] = shutdown
                safe = result.get('wrapper_reaped') is True and shutdown['closed'] is True
            except BaseException as error:
                life.lifecycle_error(result, 'final_owned_shutdown', error)
        if child is None:
            safe = True
        result.update(parent_environment_unchanged=dict(os.environ) == parent,
                      claims_disposition='Preserved_Unqualified_NoFinishOrAutomaticReclaim',
                      descriptor_path=None if descriptor_path is None else str(descriptor_path))
        if not result['parent_environment_unchanged']:
            life.lifecycle_error(result, 'final_parent_environment', ValueError('Parent environment changed'))
        if safe:
            try:
                lease.release_after_shutdown(True)
                result['host_lease_released'] = True
            except BaseException as error:
                life.lifecycle_error(result, 'host_lease_release', error)
        if result.get('lifecycle_errors') or result.get('controller_error'):
            result['result'] = 'Incomplete'
        queue.write_new(output/'worker-result.json', result)
    return result


def main():
    require_runtime()
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--contract', type=Path)
    p.add_argument('--run', action='store_true')
    p.add_argument('--authorization', type=Path)
    p.add_argument('--authorization-sha256')
    args = p.parse_args()
    if not args.run:
        need(args.contract is not None, 'Disabled contract required for read-only plan')
        contract = strict(bounded_file(args.contract).decode('utf-8-sig'))
        need(contract.get('enabled') is False and contract.get('launch_authorized') is False, 'Use disabled review contract')
        print(json.dumps(dict(result='DisabledPlanOnly', execute_method=METHOD, claims=0, launches=0,
                             quit_argument=False, queue_success_enabled=False,
                             dependency_pins=local_dependency_pins()), indent=2)); return 0
    need(platform.system() == 'Darwin', 'Checked GUI Mac route only')
    need(args.authorization is not None and args.authorization_sha256, 'Explicit new Root grant path and SHA required')
    raw = bounded_file(args.authorization); grant = strict(raw.decode('utf-8-sig'))
    root = Path(grant['project']).resolve()
    validate_grant(grant, args.authorization_sha256, raw, root)
    answer = execute(root, grant, args.authorization_sha256)
    print(json.dumps(answer, indent=2))
    return 0 if answer['result'] == 'NormalCompletedUnqualified' else 1


if __name__ == '__main__':
    raise SystemExit(main())
