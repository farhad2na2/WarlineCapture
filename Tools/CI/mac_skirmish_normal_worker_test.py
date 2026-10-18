"""Constructed portable controls. No Unity, production claim or mission credit."""
import contextlib
import copy
import datetime as dt
import hashlib
import io
import json
import os
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zlib

import mac_skirmish_normal_worker as w
import skirmish_production_operational_inspector as inspector
import skirmish_profile_journal_admission as profile
import skirmish_profile_journal_admission_test as profile_fixture
from skirmish_operational_test_fixture import OperationalFixture, ROOT, build_baseline, write


def mac_receipt(descriptor, root):
    return dict(schema_version=1, platform='macos', project=str(root), execute_method=w.METHOD,
                checked_wrapper='Tools/CI/invoke_unity_macos.sh', descriptor_fingerprint=w.queue.fingerprint(descriptor),
                checked_wrapper_exit=0, wrapper_reaped=True, wrapper_pid=123, wrapper_birth=[123, 100, 1],
                owned_shutdown=dict(closed=True, observed_absence_samples=2, survivors=[]),
                parent_environment_unchanged=True, source_settings_unchanged=True, observed_target_editor=True,
                queue_success_enabled=False, grant_sha256='a'*64, lifecycle_errors=[],
                timeout_seconds=1000, argv=w.checked_command(root, descriptor, 1000))


def png():
    def chunk(kind, value):
        return struct.pack('>I', len(value))+kind+value+struct.pack('>I', zlib.crc32(kind+value)&0xffffffff)
    return (b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR', struct.pack('>IIBBBBB', 1, 1, 8, 2, 0, 0, 0))+
            chunk(b'IDAT', zlib.compress(b'\x00\x00\x00\x00'))+chunk(b'IEND', b''))


class NormalReceipts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = build_baseline(ROOT)

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(); self.addCleanup(self.tmp.cleanup)
        self.f = OperationalFixture(Path(self.tmp.name).resolve()/'project', self.plan)
        self.f.log = self.f.log.replace('[UnityInvoke] ExitCode: 0\n', '')
        self.f.outer = mac_receipt(self.f.descriptor, self.f.root)

    def inspect(self):
        return inspector.inspect(self.f.root, self.f.materialize(), wrapper_platform='macos')

    def test_all_real_outcome_kinds_keep_losses_and_unqualified_claims(self):
        result = self.inspect()
        self.assertEqual(result['result'], 'NormalCompletedUnqualified', result)
        self.assertEqual([r['outcome'] for r in result['attempts']], ['Victory', 'Defeat', 'Draw'])
        self.assertFalse(result['qualified']); self.assertFalse(result['queue_success_enabled'])
        for paths in self.f.paths:
            self.assertTrue(paths['claim'].exists())
            self.assertFalse((paths['immutable'].parent/'result.json').exists())

    def test_windows_default_still_refuses_mac_only_wrapper(self):
        result = inspector.inspect(self.f.root, self.f.materialize())
        self.assertEqual(result['result'], 'Incomplete')
        self.assertIn('Unique actual checked exit0', str(result['errors']))

    def test_marker_only_cannot_replace_original_child_receipt(self):
        self.f.outer.pop('wrapper_reaped')
        self.assertEqual(self.inspect()['result'], 'Incomplete')

    def test_exit_birth_shutdown_failure_latch_and_gui_command_refusals(self):
        original = copy.deepcopy(self.f.outer)
        edits = [lambda x:x.update(checked_wrapper_exit=True), lambda x:x.update(wrapper_reaped=False),
                 lambda x:x.update(source_settings_unchanged=False), lambda x:x.update(parent_environment_unchanged=False),
                 lambda x:x.update(observed_target_editor=False), lambda x:x.update(wrapper_birth=[124,100,1]),
                 lambda x:x['owned_shutdown'].update(observed_absence_samples=1),
                 lambda x:x['owned_shutdown'].update(survivors=[{'pid':1}]),
                 lambda x:x.update(lifecycle_errors=[{'error':'observer failed'}]),
                 lambda x:x['argv'].append('-quit'), lambda x:x['argv'].append('-batchmode'),
                 lambda x:x.update(grant_sha256=''), lambda x:x.update(project='/foreign')]
        for edit in edits:
            with self.subTest(edit=edits.index(edit)):
                self.f.outer = copy.deepcopy(original); edit(self.f.outer)
                self.assertEqual(self.inspect()['result'], 'Incomplete')

    def test_abort_blocked_and_diagnostic_endings_do_not_become_losses(self):
        self.f.records[0][3]['facts'].update(phase='Playing', outcome='None')
        self.f.csvrows[0].update(result='Abort', end_reason='ariaStopped:Blocked')
        self.f.event('BatchFailed', error='ariaStopped:Blocked')
        result = self.inspect()
        self.assertEqual(result['result'], 'Incomplete'); self.assertEqual(result['wins'], 0)
        self.assertEqual(result['failure_records'][0]['error'], 'ariaStopped:Blocked')

    def test_no_return_intervention_localization_and_actor_leak_are_not_bypassed(self):
        cases = [('no-return', lambda:self.f.records[0][4].update(kind='MissingReturn')),
                 ('input', lambda:self.f.contacts[0][0].update(interventions=1)),
                 ('locale', lambda:self.f.records[0][4]['facts']['rendered_result_presentation'].update(locale='foreign')),
                 ('actors', lambda:next(r for r in self.f.batch if r['kind']=='CleanupOwnershipCensus')['facts']['census']['worlds'][0].update(owned_actors=1))]
        for name, edit in cases:
            with self.subTest(name=name):
                saved = copy.deepcopy((self.f.records,self.f.contacts,self.f.batch)); edit()
                self.assertEqual(self.inspect()['result'], 'Incomplete')
                self.f.records,self.f.contacts,self.f.batch = saved

    def screenshots(self):
        for row in self.f.batch:
            if row['kind']=='Dispatch':row['facts']['before_play_ownership_census']=copy.deepcopy(self.f.baseline)
        for index, paths in enumerate(self.f.paths):
            claim = w.queue.load(paths['claim']); live = Path(claim['artifact_directory'])/'live.jsonl'
            paths['live'] = live
            for row in self.f.records[index]:
                if 'native_trace_path' in row['facts']:row['facts']['native_trace_path'] = str(live)
            for suffix in w.SUFFIX.values():Path(str(live)+suffix).write_bytes(png())
            # Constructed journal facts, joined through the real profile validator.
            fixture=profile_fixture.ProfileControls();fixture.setUp()
            def convert(value):
                if isinstance(value,str):return value.replace('D:/fixture/Logs/Q/job/attempt-synthetic',claim['artifact_directory']).replace('C:/user/persistent',str(self.f.root/'OriginalProfile')).replace('S005','S026').replace('s005','s026')
                if isinstance(value,dict):return {k:convert(v) for k,v in value.items()}
                if isinstance(value,list):return [convert(v) for v in value]
                return value
            journal={r['kind']:convert(r['facts']) for r in fixture.events if r['kind'].startswith('Profile')}
            native=[]
            for row in self.f.records[index]:
                if row['kind']=='RestoreStarting':
                    native.append(dict(row,kind='ProfileBeforeRestore',facts=journal['ProfileBeforeRestore']))
                if row['kind']=='ContextRestored':
                    native.append(dict(row,kind='ProfileAfterRestore',facts=journal['ProfileAfterRestore']))
                native.append(row)
                if row['kind']=='ContextCaptured':
                    native.append(dict(row,kind='ProfileBeforeDispatch',facts=journal['ProfileBeforeDispatch']))
            for sequence,row in enumerate(native,1):row['sequence']=sequence
            self.f.records[index]=native
        spec = self.f.materialize()
        # A Windows physical artifact root cannot be a POSIX native profile
        # root. On that host, exercise the SAME artifact/accounting worker seam
        # with the existing constructed Windows inspector fixture. Mac wrapper
        # receipts and POSIX journals are separately tested above/below. No real
        # Mac log is rewritten and production execution always selects macos.
        mode='macos'
        if os.name=='nt':
            self.f.log+='\n[UnityInvoke] ExitCode: 0\n'
            spec=self.f.materialize();mode='windows'
        result = inspector.inspect(self.f.root, spec, wrapper_platform=mode)
        return spec, result

    def test_four_actual_owned_png_paths_and_losses_still_have_zero720_credit(self):
        spec, inspected = self.screenshots()
        result = w.normal_artifacts(self.f.root, spec, inspected)
        self.assertEqual([r['recorded_loss'] for r in result['attempts']], [False,True,True])
        self.assertEqual(set(result['attempts'][0]['screenshots']), set(w.STAGES))
        self.assertEqual(result['normal_720_credit'], 0)
        self.assertIn('Pending', result['three_boundary_camera_actor'])

    def test_missing_result_png_not_replaced_by_cruise_png(self):
        spec, inspected = self.screenshots(); live = self.f.paths[0]['live']
        Path(str(live)+w.SUFFIX['result']).rename(Path(str(live)+'.cruise.png'))
        with self.assertRaises((ValueError,FileNotFoundError)):w.normal_artifacts(self.f.root,spec,inspected)

    def test_one_missing_profile_boundary_blocks_whole_worker_observation(self):
        spec, inspected=self.screenshots()
        inspected['admission']['profile_receipts'][1]['profile_restored']=False
        with self.assertRaisesRegex(ValueError,'three actual profile boundaries'):w.normal_artifacts(self.f.root,spec,inspected)

    def test_before_play_actor_census_is_not_hidden_by_clean_return(self):
        spec, inspected=self.screenshots()
        row=next(r for r in self.f.batch if r['kind']=='Dispatch')
        row['facts']['before_play_ownership_census']['worlds'][0]['owned_actors']=1
        spec=self.f.materialize()
        with self.assertRaisesRegex(ValueError,'Owned native objects remain'):w.normal_artifacts(self.f.root,spec,inspected)

    def test_screenshot_crc_and_trailing_payload_refused(self):
        spec, inspected = self.screenshots(); p = Path(str(self.f.paths[0]['live'])+w.SUFFIX['opening'])
        for b in [png()+b'foreign', png()[:-1]+b'X']:
            with self.subTest(length=len(b)):
                p.write_bytes(b)
                with self.assertRaises(ValueError):w.normal_artifacts(self.f.root,spec,inspected)

    def test_photo_aba_rewrite_cannot_splice_a_different_captured_hash(self):
        spec,inspected=self.screenshots();real=w.png_shape;changed=False
        def capture(path):
            nonlocal changed
            if changed:return real(path)
            changed=True;original=path.read_bytes()
            # A different valid PNG byte stream exists only during capture.
            modified=original[:33]+struct.pack('>I',1)+b'tEXt'+b'x'+struct.pack('>I',zlib.crc32(b'tEXt'+b'x')&0xffffffff)+original[33:]
            path.write_bytes(modified)
            try:return real(path)
            finally:path.write_bytes(original)
        with patch.object(w,'png_shape',side_effect=capture):
            with self.assertRaisesRegex(ValueError,'between ownership pin'):w.normal_artifacts(self.f.root,spec,inspected)

    def test_same_attempt_image_path_cannot_escape_to_another_attempt(self):
        spec, inspected = self.screenshots()
        row = next(r for r in self.f.records[0] if r['kind']=='NormalMenuLaunchObserved')
        row['facts']['native_trace_path'] = str(self.f.paths[1]['live'])
        spec = self.f.materialize()
        with self.assertRaisesRegex(ValueError,'escapes owned attempt'):w.normal_artifacts(self.f.root,spec,inspected)

    def test_real_queue_existing_claim_is_not_adopted(self):
        grant = dict(source={'candidate_sha256':self.f.store['candidate_sha256'],'build_sha256':self.f.store['build_sha256']},
                     sampling_plan={'path':str(self.f.planpath),'sha256':w.digest(self.f.planpath)},
                     jobs=[{'directory':str(self.f.paths[0]['job'].parent),'job_sha256':w.queue.fingerprint(w.queue.load(self.f.paths[0]['job']))}])
        # Put a real valid canonical job/claim in the worker's required durable lane.
        job=w.queue.load(self.f.paths[0]['job']);job['job_id']='claimed-worker-control'
        authority=w.SamplingAuthority(self.f.root,self.f.planpath,w.digest(self.f.planpath))
        folder=Path(w.queue.enqueue(self.f.root/'Logs/SkirmishProduction/Queue',job,self.f.root,authority)['job_directory'])
        original=w.queue.claim(folder,'other-worker',self.f.root,authority)
        grant['jobs']=[dict(directory=str(folder),job_sha256=w.queue.fingerprint(job))]
        with self.assertRaisesRegex(ValueError,'Fresh claim only'):w.validate_jobs(self.f.root,grant)
        self.assertEqual(w.queue.load(folder/'claim.json'),original)

    def test_fresh_canonical_claims_and_preparer_are_used_without_qualification(self):
        authority=w.SamplingAuthority(self.f.root,self.f.planpath,w.digest(self.f.planpath));folders=[]
        for i,paths in enumerate(self.f.paths):
            job=w.queue.load(paths['job']);job['job_id']='fresh-worker-'+str(i)
            folders.append(Path(w.queue.enqueue(self.f.root/'Logs/SkirmishProduction/Queue',job,self.f.root,authority)['job_directory']))
        claimed=[];grant=dict(worker='mac-fixture',nonce='a'*32)
        descriptor=w.claim_and_prepare(self.f.root,grant,authority,folders,self.f.storepath,claimed)
        data=w.queue.load(descriptor)
        self.assertEqual(data['schema_version'],2);self.assertEqual(len(claimed),3)
        self.assertEqual(data['sampling_plan_sha256'],authority.expected)
        for row in claimed:
            folder=Path(row['directory']);self.assertEqual(w.queue.load(folder/'claim.json'),row['claim'])
            self.assertEqual(w.queue.load(Path(row['claim']['artifact_directory'])/'claim.json'),row['claim'])
            with self.assertRaises(w.queue.ContractError):w.queue.finish(folder,row['claim']['token'],'qualified')

    def test_partial_claim_collision_preserves_original_owned_attempt(self):
        authority=w.SamplingAuthority(self.f.root,self.f.planpath,w.digest(self.f.planpath));folders=[]
        for i,paths in enumerate(self.f.paths[:2]):
            job=w.queue.load(paths['job']);job['job_id']='partial-worker-'+str(i)
            folders.append(Path(w.queue.enqueue(self.f.root/'Logs/SkirmishProduction/Queue',job,self.f.root,authority)['job_directory']))
        foreign=w.queue.claim(folders[1],'other-worker',self.f.root,authority);claimed=[]
        with self.assertRaisesRegex(w.queue.ContractError,'already claimed'):
            w.claim_and_prepare(self.f.root,dict(worker='mac-fixture',nonce='c'*32),authority,folders,self.f.storepath,claimed)
        self.assertEqual(len(claimed),1)
        self.assertEqual(w.queue.load(folders[0]/'claim.json'),claimed[0]['claim'])
        self.assertEqual(w.queue.load(folders[1]/'claim.json'),foreign)
        self.assertFalse((self.f.root/'Logs/SkirmishProduction/Batches'/('mac-'+'c'*32)).exists())

    def test_legacy_slot_is_pending_before_claim_not_silently_skipped_or_relabelled(self):
        authority=w.SamplingAuthority(self.f.root,self.f.planpath,w.digest(self.f.planpath))
        slot=next(s for s in self.plan['slots'] if s['catalog_id']=='S001')
        job=w.queue.load(self.f.paths[0]['job']);job.update(schema_version=2,job_id='legacy-s001-control',catalog_id='S001',
            seed=slot['seed'],locale=slot['locale'],legacy_preset_index=0,legacy_preset_resource='SkirmishBaseAssault')
        job.pop('definition_id');job.pop('definition_version')
        preset=self.f.root/'Assets/Game/Resources/SkirmishBaseAssault.asset';preset.parent.mkdir(parents=True);preset.write_text('constructed preset bytes')
        job['legacy_preset_sha256']=w.digest(preset);job['packet_path']=authority.packet(authority.catalog['S001']);job['packet_sha256']=authority.pins[job['packet_path']]
        folder=Path(w.queue.enqueue(self.f.root/'Logs/SkirmishProduction/Queue',job,self.f.root,authority)['job_directory'])
        grant=dict(source={'candidate_sha256':job['candidate_sha256'],'build_sha256':job['build_sha256']},
            sampling_plan={'path':str(self.f.planpath),'sha256':w.digest(self.f.planpath)},
            jobs=[dict(directory=str(folder),job_sha256=w.queue.fingerprint(job))])
        with self.assertRaisesRegex(ValueError,'Legacy preset schema2'):w.validate_jobs(self.f.root,grant)
        self.assertFalse((folder/'claim.json').exists());self.assertEqual(len(authority.slots),720)

    def test_development_counterpart_cannot_replace_the_protected_normal_slot(self):
        authority=w.SamplingAuthority(self.f.root,self.f.planpath,w.digest(self.f.planpath))
        slot=next(s for s in self.plan['slots'] if s['catalog_id']=='S001')
        job=w.queue.load(self.f.paths[0]['job']);job.update(schema_version=3,job_id='practice-substitution-control',catalog_id='S001',
            definition_id='skirmish.s001',seed=slot['seed'],locale=slot['locale'],counterpart_authority={'path':'unapproved','sha256':'f'*64})
        folder=self.f.root/'Logs/SkirmishProduction/Queue'/job['job_id'];write(folder/'job.json',job)
        grant=dict(source={'candidate_sha256':job['candidate_sha256'],'build_sha256':job['build_sha256']},
            sampling_plan={'path':str(self.f.planpath),'sha256':w.digest(self.f.planpath)},
            jobs=[dict(directory=str(folder),job_sha256=w.queue.fingerprint(job))])
        with self.assertRaisesRegex(ValueError,'Development-only schema3'):w.validate_jobs(self.f.root,grant)
        self.assertFalse((folder/'claim.json').exists());self.assertEqual(len(authority.slots),720)


class PortableBoundaries(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name).resolve()

    def test_host_lease_excludes_second_worker_and_never_reclaims_by_age(self):
        path=self.root/'lease';first=w.HostLease(path,'a'*32);first.acquire();os.utime(path,(1,1))
        with self.assertRaises(FileExistsError):w.HostLease(path,'b'*32).acquire()
        with self.assertRaises(ValueError):first.release_after_shutdown(False)
        first.release_after_shutdown(True);self.assertFalse(path.exists())

    def test_lease_inode_replacement_is_not_removed(self):
        path=self.root/'lease';first=w.HostLease(path,'a'*32);first.acquire()
        path.rename(self.root/'preserved');path.write_text('a'*32)
        with self.assertRaisesRegex(ValueError,'identity changed'):first.release_after_shutdown(True)
        self.assertTrue(path.exists())

    def test_actual_file_and_parent_symlinks_are_rejected(self):
        actual=self.root/'actual';actual.mkdir();(actual/'file').write_bytes(b'hello')
        alias=self.root/'alias'
        try:alias.symlink_to(actual,target_is_directory=True)
        except OSError as e:self.skipTest(str(e))
        with self.assertRaisesRegex(ValueError,'Symlink/reparse'):w.bounded_file(alias/'file')
        alias.unlink();alias.symlink_to(actual/'file')
        with self.assertRaisesRegex(ValueError,'Symlink/reparse'):w.bounded_file(alias)

    def test_actual_file_growth_after_initial_size_gate_is_bounded(self):
        path=self.root/'file';path.write_bytes(b'1234');real=os.read;grew=False
        def read(fd,size):
            nonlocal grew
            if not grew:
                grew=True
                with path.open('ab') as f:f.write(b'56789')
            return real(fd,size)
        with patch.object(w.os,'read',side_effect=read):
            with self.assertRaisesRegex(ValueError,'grew beyond'):w.bounded_file(path,4)

    def test_source_settings_and_global_gui_inventories_keep_every_added_path(self):
        (self.root/'ProjectSettings').mkdir();(self.root/'UserSettings').mkdir();globalroot=self.root/'global';globalroot.mkdir()
        before=w.full_settings(self.root,[str(globalroot)]);(globalroot/'unexpected').write_text('changed')
        after=w.full_settings(self.root,[str(globalroot)]);self.assertNotEqual(before,after)
        self.assertEqual(after[str(globalroot)]['rows'][0]['path'],'unexpected')

    def test_child_env_is_local_and_drops_all_stale_probe_injections(self):
        original={'WARLINE_TIME_SCALE':'100','warline_fake':'1','PATH':'fixture'};saved=dict(original)
        actual=w.child_environment(original,self.root/'batch.json')
        self.assertEqual(original,saved);self.assertEqual(set(actual),{'PATH','WARLINE_SKIRMISH_PRODUCTION_BATCH'})

    def test_actual_benign_child_reaped_after_persistent_observer_failure(self):
        child=subprocess.Popen([sys.executable,'-c','import sys;sys.stdin.readline()'],stdin=subprocess.PIPE,stdout=subprocess.DEVNULL)
        class Broken:
            def observe(self,*a):
                if not child.stdin.closed:child.stdin.write(b'finish\n');child.stdin.close()
                raise ValueError('unknown owned worker')
        result={}
        try:
            with patch.object(w.life,'processes',return_value=[]):w.life.wait_original_wrapper(child,Broken(),self.root,result)
        finally:
            if not child.stdin.closed:child.stdin.close()
            child.wait(timeout=5)
        self.assertEqual(result['wrapper_exit'],0);self.assertTrue(result['wrapper_reaped'])
        self.assertTrue(result['lifecycle_errors']);self.assertFalse(result['operationalControlPassed'])

    def test_disabled_plan_writes_no_attempt_or_launch(self):
        contract=self.root/'disabled.json';contract.write_text('{"enabled":false,"launch_authorized":false}')
        with patch.object(sys,'argv',['worker','--contract',str(contract)]),patch.object(w.queue,'claim',side_effect=AssertionError('claim forbidden')),patch.object(w.subprocess,'Popen',side_effect=AssertionError('launch forbidden')),contextlib.redirect_stdout(io.StringIO()) as output:
            self.assertEqual(w.main(),0)
        self.assertEqual(json.loads(output.getvalue())['claims'],0)
        self.assertEqual(list(self.root.iterdir()),[contract])

    def test_posix_profile_three_boundaries_preserve_case_and_refuse_windows_splices(self):
        fixture=profile_fixture.ProfileControls();fixture.setUp()
        def convert(value):
            if isinstance(value,str):return value.replace('D:/fixture','/Volumes/Test/Fixture').replace('C:/user/persistent','/Users/Test/Persistent')
            if isinstance(value,dict):return {k:convert(v) for k,v in value.items()}
            if isinstance(value,list):return [convert(v) for v in value]
            return value
        claim=convert(fixture.claim);events=convert(fixture.events)
        receipt=profile.validate_profile_journal(fixture.job,claim,events,path_platform='macos')
        self.assertTrue(receipt['profile_restored'],receipt);self.assertFalse(receipt['qualified'])
        for root in ['/Users/Test/Persistent/sub','/Users/test/Persistent','C:/foreign','/Users/Test/../foreign']:
            with self.subTest(root=root):
                bad=copy.deepcopy(events);next(r for r in bad if r['kind']=='ProfileAfterRestore')['facts']['actual_effective_root']=root
                self.assertFalse(profile.validate_profile_journal(fixture.job,claim,bad,path_platform='macos')['profile_restored'])
        self.assertFalse(profile.validate_profile_journal(fixture.job,claim,events)['profile_restored'])

    def test_other_editor_or_reused_pid_never_inherits_shared_gui_ownership(self):
        own=dict(pid=123,identity_verified=True,start_sec=10,start_usec=1,executable=sys.executable)
        foreign=dict(pid=123,identity_verified=True,start_sec=11,start_usec=1,executable=sys.executable)
        tree=w.NormalOwnedTree(123,[])
        with patch.object(w.life.OwnedProcessTree,'observe',return_value=[own]),patch.object(w.life,'editor',return_value=True):
            self.assertEqual(tree.observe([own],self.root),[own])
            with self.assertRaisesRegex(ValueError,'Another Editor'):tree.observe([foreign],self.root)


class GrantControls(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name).resolve()
        self.now=dt.datetime.now(dt.timezone.utc)
        refs={}
        for kind in w.REVIEWS:
            p=self.root/(kind+'.json');value=dict(kind=kind,execution_scope_reviewed=True,candidate_sha256='c'*64,build_sha256='d'*64)
            if kind in ('source_fit','dispatch_partition'):value['reviewed_job_sha256s']=['e'*64]
            if kind=='dispatch_partition':value.update(worker='mac-fixture',host=w.host_identity(),nonce='a'*32,exclusive_slots_reserved_by_coordinator=True)
            p.write_text(json.dumps(value));refs[kind]=dict(path=str(p),sha256=w.digest(p))
        self.grant=dict(schema_version=1,launch_authorized=True,nonce='a'*32,
            issued_utc=self.now.isoformat(),expires_utc=(self.now+dt.timedelta(minutes=10)).isoformat(),
            project=str(self.root),host=w.host_identity(),python_runtime=w.runtime_identity(),worker='mac-fixture',controller_sha256=w.digest(Path(w.__file__)),
            dependency_pins=w.local_dependency_pins(),source=dict(head='a'*40,parent='b'*40,branch='codex/fixture',
                candidate_sha256='c'*64,build_sha256='d'*64,manifest_ref=dict(path=str(self.root/'pending-manifest'),sha256='1'*64),wrapper_sha256='2'*64),
            settings=dict(global_gui_scope_reviewed=True,global_roots=[str(self.root/'global')],shared_writer_policy='NoOtherEditors',
                baseline_ref=dict(path=str(self.root/'pending-baseline'),sha256='3'*64)),
            sampling_plan=dict(path=str(self.root/'pending-plan'),sha256='4'*64),jobs=[dict(directory=str(self.root/'pending-job'),job_sha256='e'*64)],
            timeout_seconds=1000,capacity=dict(minimum_free_bytes=25*1024**3,minimum_memory_pressure_free_percent=15,max_native_workers_on_host=1),
            tool_pins=[],reviews=refs)

    def check(self,grant=None):
        value=self.grant if grant is None else grant;raw=json.dumps(value).encode()
        with patch.object(w.queue,'claim',side_effect=AssertionError('validation must not claim')):
            return w.validate_grant(value,hashlib.sha256(raw).hexdigest(),raw,self.root,now=self.now)

    def test_reviewed_grant_consistency_is_not_claim_or_launch_admission(self):
        self.check()
        self.assertFalse((self.root/'pending-manifest').exists())
        self.assertEqual(set(self.root.iterdir()),{Path(v['path']) for v in self.grant['reviews'].values()})

    def test_disabled_expired_foreign_host_weak_capacity_and_wrong_dependency_refused(self):
        edits=[lambda x:x.update(launch_authorized=False),lambda x:x.update(expires_utc=(self.now-dt.timedelta(seconds=1)).isoformat()),
               lambda x:x.update(expires_utc=(self.now+dt.timedelta(days=1)).isoformat()),lambda x:x.update(host={'node':'foreign','uid':0}),
               lambda x:x['capacity'].update(minimum_free_bytes=1),lambda x:x['capacity'].update(max_native_workers_on_host=True),
               lambda x:x['settings'].update(global_gui_scope_reviewed=False),lambda x:x['dependency_pins'].pop('mac_skirmish_normal_worker.py')]
        for i,edit in enumerate(edits):
            with self.subTest(case=i):
                value=copy.deepcopy(self.grant);edit(value)
                with self.assertRaises(ValueError):self.check(value)

    def test_actual_source_fit_review_bytes_bind_every_queued_job(self):
        value=copy.deepcopy(self.grant);value['jobs'].append(dict(directory=str(self.root/'another-job'),job_sha256='f'*64))
        with self.assertRaisesRegex(ValueError,'every exact queued'):self.check(value)
        p=Path(self.grant['reviews']['source_fit']['path']);review=json.loads(p.read_text());review['candidate_sha256']='0'*64
        p.write_text(json.dumps(review))
        with self.assertRaisesRegex(ValueError,'Pinned artifact bytes changed'):self.check()

    def test_exact_authorization_bytes_and_controller_hash_cannot_be_substituted(self):
        raw=json.dumps(self.grant).encode()
        with self.assertRaisesRegex(ValueError,'grant SHA'):w.validate_grant(self.grant,'0'*64,raw,self.root,now=self.now)
        value=copy.deepcopy(self.grant);value['controller_sha256']='0'*64
        with self.assertRaisesRegex(ValueError,'Worker code'):self.check(value)

    def test_local_claim_lock_does_not_replace_cross_host_slot_partition(self):
        p=Path(self.grant['reviews']['dispatch_partition']['path']);review=json.loads(p.read_text());review['nonce']='b'*32
        p.write_text(json.dumps(review));self.grant['reviews']['dispatch_partition']['sha256']=w.digest(p)
        with self.assertRaisesRegex(ValueError,'Cross-host canonical'):self.check()

    def test_old_or_substituted_python_cannot_start_or_claim(self):
        with patch.object(w.sys,'version_info',(3,9,0)),patch.object(w.queue,'claim',side_effect=AssertionError('claim forbidden')):
            with self.assertRaisesRegex(ValueError,'Python3.12'):w.main()
        value=copy.deepcopy(self.grant);value['python_runtime']['sha256']='0'*64
        with self.assertRaisesRegex(ValueError,'Python runtime'):self.check(value)


if __name__=='__main__':unittest.main(verbosity=2)
