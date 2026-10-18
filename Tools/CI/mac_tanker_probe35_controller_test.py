"""Pure controller corruption controls. No Unity, cache copy or native execution."""
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import mac_tanker_probe35_controller as c


class ControllerTests(unittest.TestCase):
    def row(self,pid,ppid,executable,cpu=0,project=None,usec=1):
        command=executable+(' -projectPath '+project if project else '')
        return dict(pid=pid,ppid=ppid,start_sec=1700000000,start_usec=usec,
            started=c.display_start(1700000000),cpu=cpu,rss_kib=1,executable=executable,comm=executable,
            identity_verified=True,arguments_observed=True,command=command)
    def hub(self):return self.row(1,0,'/Applications/Unity Hub.app/Contents/MacOS/Unity Hub')
    def editor(self,pid=2,ppid=1,project='/tmp/other',cpu=0):
        return self.row(pid,ppid,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity',cpu,project)
    def initial_tree(self,worker_exe=None):
        baseline=[self.hub(),self.editor(20),self.row(21,20,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler')]
        rows=baseline+[self.row(100,99,'/opt/homebrew/bin/rtk'),self.row(101,100,'/bin/bash'),self.editor(102,101,c.ROOT),
            self.row(103,102,worker_exe or '/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler')]
        tree=c.OwnedProcessTree(100,baseline);tree.observe(rows,Path(c.ROOT));return tree,baseline,rows
    def authorization(self):
        return dict(schema_version=1,launch_authorized=True,project=c.ROOT,commit=c.COMMIT,
            controller_sha256='a'*64,review_receipt_sha256='b'*64,scope='Synthetic35Only_NoMissionCredit',timeout_seconds=3600)
    def text(self):return '\n'.join(c.NEGATIVES+[c.MARKER])+'\n'
    def state(self):return dict(source_manifest=[{'path':'a','sha256':'a'*64}],project_settings=[],user_settings=[],
        build_manifest=[{'path':'Library/ScriptAssemblies/Game.Editor.dll','sha256':'b'*64}],response_file_pins=[])
    def test_default_is_disabled_without_native_command(self):
        with patch('sys.argv',['controller']),patch.object(c,'execute') as execute,patch('sys.stdout',new_callable=io.StringIO) as out:
            self.assertEqual(c.main(),0);execute.assert_not_called();self.assertFalse(json.loads(out.getvalue())['launch_performed'])
    def test_execute_requires_separate_authorization(self):
        with patch('sys.argv',['controller','--execute']),self.assertRaises(ValueError),patch.object(c.subprocess,'Popen') as p:
            c.main();p.assert_not_called()
    def test_authorization_exact_scope_and_hash(self):
        c.authorization(self.authorization(),'a'*64)
        for k,v in [('launch_authorized',False),('commit','f'*40),('project','/tmp/other'),('controller_sha256','c'*64),
                    ('review_receipt_sha256','partial'),('scope','NormalMission'),('timeout_seconds',600),('schema_version',True)]:
            a=self.authorization();a[k]=v
            with self.subTest(k=k),self.assertRaises(ValueError):c.authorization(a,'a'*64)
        a=self.authorization();a['extra']=1
        with self.assertRaises(ValueError):c.authorization(a,'a'*64)
    def test_command_has_only_checked_gui_wrapper_and_explicit_android(self):
        cmd=c.command(Path(c.ROOT),Path('/tmp/new.log'),3600)
        self.assertEqual(cmd[:3],['rtk','proxy',str(Path(c.ROOT)/c.WRAPPER)])
        self.assertIn('Android',cmd);self.assertIn(c.ENTRY,cmd);self.assertIn('-quit',cmd)
        for forbidden in ('-batchmode','--reuse','--reset-ipc','--quit-hub'):self.assertNotIn(forbidden,cmd)
    def test_exact_final_marker_and_two_expected_negative_errors(self):c.log_checks(self.text(),0)
    def test_missing_duplicate_or_early_marker_nonzero_and_timeout(self):
        for text,code in [(self.text(),1),(self.text(),124),('',0),(self.text()+c.MARKER+'\n',0),
                          (c.MARKER+'\n'+'\n'.join(c.NEGATIVES),0),('\n'.join(c.NEGATIVES[:1]+[c.MARKER]),0)]:
            with self.subTest(code=code,text=text),self.assertRaises(ValueError):c.log_checks(text,code)
    def test_unexpected_failure_compile_exception_or_import_error(self):
        for error in ('[Other] result=Failed reason=x','error CS0123: bad','Unhandled Exception: x','AssertionException: bad',
                      'Unexpected log received','Compilation failed','Failed to import a','[Error] unexpected'):
            with self.subTest(error=error),self.assertRaises(ValueError):c.log_checks(self.text()+error+'\n',0)
    def test_scoped_controls_can_pass_with_explicit_pending_closure(self):
        r=c.assess(self.state(),self.state(),self.text(),0,[],True,True)
        self.assertTrue(r['operationalControlPassed']);self.assertEqual(r['proofCompleteness']['native_compiler_input_consumption_closure'],'Pending')
        self.assertTrue(all(v is False for v in r['readiness'].values()))
    def test_source_settings_output_shutdown_env_hub_failures(self):
        for key in ('source_manifest','project_settings','user_settings','build_manifest'):
            a=self.state();b=self.state();b[key]=[] if a[key] else [{'changed':True}]
            self.assertFalse(c.assess(a,b,self.text(),0,[],True,True)['operationalControlPassed'])
        for remaining,env,hub in [([{'pid':123}],True,True),([],False,True),([],True,False)]:
            self.assertFalse(c.assess(self.state(),self.state(),self.text(),0,remaining,env,hub)['operationalControlPassed'])
    def test_actual_current_android_rsp_separate_from_compiler_consumption(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'Game.Editor.dll.rsp';p.write_text('-define:UNITY_EDITOR;UNITY_ANDROID\n')
            a=self.state();a['response_file_pins']=[c.checked_file(p)]
            r=c.assess(self.state(),a,self.text(),0,[],True,True)
            self.assertEqual(len(r['proofCompleteness']['actual_android_define_response_files']),1)
            p.write_text('changed')
            self.assertEqual(c.assess(self.state(),a,self.text(),0,[],True,True)['proofCompleteness']['actual_android_define_response_files'],[])
    def test_busy_or_owned_editor_never_terminated(self):
        for project,cpu in [(c.ROOT,0),('/tmp/other',100)]:
            with patch.object(c,'processes',return_value=[self.hub(),self.editor(project=project,cpu=cpu)]),patch.object(c.time,'sleep'),self.assertRaises(ValueError):c.capacity(Path(c.ROOT))
    def test_worker_load_blocks_even_with_idle_parent_editor(self):
        worker=self.row(3,2,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler',100)
        with patch.object(c,'processes',return_value=[self.hub(),self.editor(),worker]),patch.object(c.time,'sleep'),self.assertRaisesRegex(ValueError,'CPU workload'):c.capacity(Path(c.ROOT))
    def test_active_orphan_shader_bee_and_unity_dotnet_roots_refused(self):
        for exe in ['/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler',
                    '/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/bee_backend',
                    '/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/NetCoreRuntime/dotnet']:
            rows=[self.hub(),self.row(8,1,exe,100)]
            with self.subTest(exe=exe),patch.object(c,'processes',return_value=rows),self.assertRaisesRegex(ValueError,'CPU workload'):c.capacity(Path(c.ROOT))
    def test_uncertain_or_idle_unattributed_worker_conservatively_deferred(self):
        worker=self.row(8,1,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler')
        for verified in (True,False):
            worker['identity_verified']=verified
            with self.subTest(verified=verified),self.assertRaisesRegex(ValueError,'unresolved'):
                c.check_capacity_sample([self.hub(),worker],Path(c.ROOT),{})
    def test_known_idle_other_editor_and_attached_workers_remain_allowed(self):
        worker=self.row(3,2,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler')
        c.check_capacity_sample([self.hub(),self.editor(),worker],Path(c.ROOT),{})
    def test_generic_non_unity_busy_descendant_is_counted(self):
        worker=self.row(3,2,'/usr/bin/python3',100)
        with self.assertRaisesRegex(ValueError,'CPU workload'):c.check_capacity_sample([self.hub(),self.editor(),worker],Path(c.ROOT),{})
    def test_unresolved_native_descendant_identity_cannot_be_called_idle(self):
        worker=self.row(3,2,'/usr/bin/python3');worker['identity_verified']=False
        with self.assertRaisesRegex(ValueError,'descendant identity'):c.check_capacity_sample([self.hub(),self.editor(),worker],Path(c.ROOT),{})
    def test_surviving_owned_children_refuse_shutdown_for_all_names(self):
        for exe in ['/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler',
                    '/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/AssetImportWorker',
                    '/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/bee_backend','/usr/bin/python3']:
            tree,baseline,rows=self.initial_tree(exe);child=dict(rows[-1],ppid=1)
            with self.subTest(exe=exe),patch.object(c,'processes',return_value=baseline+[child]),patch.object(c.time,'monotonic',return_value=0),patch.object(c.os,'kill') as signal:
                status,_=c.owned_shutdown_barrier(tree,Path(c.ROOT),timeout=0)
                self.assertFalse(status['closed']);self.assertEqual(status['survivors'],[child]);signal.assert_not_called()
                self.assertFalse(c.assess(self.state(),self.state(),self.text(),0,status['survivors'],True,True)['operationalControlPassed'])
    def test_reparented_owned_child_and_new_child_retain_full_tree_ownership(self):
        tree,baseline,rows=self.initial_tree();child=dict(rows[-1],ppid=1)
        grandchild=self.row(104,103,'/bin/sleep')
        live=tree.observe(baseline+[child,grandchild],Path(c.ROOT))
        self.assertEqual({r['pid'] for r in live},{103,104});self.assertEqual(len(tree.records),5)
    def test_pid_reuse_does_not_adopt_or_terminate_unrelated_generation(self):
        tree,baseline,rows=self.initial_tree();new=self.row(103,1,'/Applications/TextEdit.app/Contents/MacOS/TextEdit',usec=2)
        self.assertEqual(tree.observe(baseline+[new],Path(c.ROOT)),[])
        self.assertNotIn(c.process_key(new),tree.records)
        self.assertEqual(tree.identity_transitions[0]['kind'],'PIDReused_NotAdopted')
    def test_new_owned_birth_may_reuse_a_departed_baseline_pid(self):
        baseline=[self.hub(),self.row(103,1,'/bin/old')]
        rows=[self.hub(),self.row(100,99,'/opt/homebrew/bin/rtk'),self.editor(102,100,c.ROOT),self.row(103,102,'/bin/sleep',usec=2)]
        tree=c.OwnedProcessTree(100,baseline);tree.observe(rows,Path(c.ROOT))
        self.assertIn(c.process_key(rows[-1]),tree.records);self.assertNotIn(c.process_key(baseline[-1]),tree.records)

    def test_same_birth_exec_transition_remains_owned_and_visible(self):
        tree,baseline,rows=self.initial_tree();changed=dict(rows[-1],ppid=1,executable='/usr/bin/python3',command='/usr/bin/python3')
        self.assertEqual(tree.observe(baseline+[changed],Path(c.ROOT)),[changed])
        self.assertEqual(len(tree.records[c.process_key(changed)]['executable_history']),2)
    def test_owned_identity_unreadable_does_not_prove_absence(self):
        tree,baseline,rows=self.initial_tree();child=dict(rows[-1],ppid=1,identity_verified=False)
        with self.assertRaisesRegex(ValueError,'Unverified process'):tree.observe(baseline+[child],Path(c.ROOT))
    def test_baseline_other_process_cannot_be_borrowed(self):
        tree,baseline,rows=self.initial_tree();borrowed=dict(baseline[-1],ppid=100)
        with self.assertRaisesRegex(ValueError,'Pre-existing'):tree.observe([borrowed if r['pid']==borrowed['pid'] else r for r in rows],Path(c.ROOT))
    def test_unrelated_new_target_editor_is_refused_without_cleanup(self):
        tree,baseline,rows=self.initial_tree();foreign=self.editor(200,1,c.ROOT)
        with patch.object(c.os,'kill') as signal:
            with self.assertRaisesRegex(ValueError,'not wrapper-owned'):tree.observe(rows+[foreign],Path(c.ROOT))
            signal.assert_not_called()
    def test_new_unattributed_orphan_is_not_silently_assumed_unrelated(self):
        tree,baseline,rows=self.initial_tree();orphan=self.row(200,1,'/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/bee_backend')
        with self.assertRaisesRegex(ValueError,'unresolved ownership'):tree.observe(baseline+[orphan],Path(c.ROOT))
    def test_shutdown_requires_two_complete_observed_absence_samples(self):
        tree,baseline,rows=self.initial_tree()
        with patch.object(c,'processes',side_effect=[baseline,baseline]) as snapshots,patch.object(c.time,'sleep'),patch.object(c.time,'monotonic',return_value=0):
            status,_=c.owned_shutdown_barrier(tree,Path(c.ROOT));self.assertTrue(status['closed']);self.assertEqual(snapshots.call_count,2)
        self.assertEqual(status['observed_absence_samples'],2)
    def test_known_owned_editor_missing_argv_stays_live_until_kernel_absence(self):
        tree,baseline,rows=self.initial_tree();current=[dict(r,arguments_observed=False,command=r['executable']) if r['pid']==102 else r for r in rows]
        live=tree.observe(current,Path(c.ROOT));self.assertIn(102,{r['pid'] for r in live});self.assertTrue(tree.saw_target_editor)
    def test_known_protected_editor_missing_argv_is_not_adopted(self):
        tree,baseline,rows=self.initial_tree();current=[dict(r,arguments_observed=False,command=r['executable']) if r['pid']==20 else r for r in rows]
        live=tree.observe(current,Path(c.ROOT));self.assertNotIn(20,{r['pid'] for r in live})
    def test_reused_editor_pid_cannot_reuse_old_project_binding(self):
        tree,baseline,rows=self.initial_tree();fresh=dict(self.editor(102,1,c.ROOT),start_usec=2,arguments_observed=False)
        with self.assertRaisesRegex(ValueError,'arguments unresolved'):tree.observe(baseline+[fresh],Path(c.ROOT))
    def test_new_owned_editor_needs_later_argv_before_target_lifecycle_credit(self):
        baseline=[self.hub()];first=[self.hub(),self.row(100,99,'/opt/homebrew/bin/rtk'),dict(self.editor(102,100,c.ROOT),arguments_observed=False)]
        tree=c.OwnedProcessTree(100,baseline);tree.observe(first,Path(c.ROOT));self.assertFalse(tree.saw_target_editor)
        tree.observe([dict(r,arguments_observed=True) for r in first],Path(c.ROOT));self.assertTrue(tree.saw_target_editor)

    def test_kernel_birth_tuple_distinguishes_microsecond_pid_reuse(self):
        row=self.row(3,1,'/bin/sleep');other=dict(row,start_usec=2)
        self.assertNotEqual(c.process_key(row),c.process_key(other))
        with self.assertRaises(ValueError):c.process_key(dict(row,identity_verified=False))

    def test_process_evidence_redacts_license_and_auth_credentials(self):
        command='/Applications/Unity.app/Contents/MacOS/Unity -projectPath /tmp/owned -cbmEditorLicense secret1 -cbmMachineId secret2 --token=secret3 -parentPid 2'
        clean=c.redact_command(command)
        for value in ('secret1','secret2','secret3'):self.assertNotIn(value,clean)
        self.assertIn('-projectPath /tmp/owned',clean);self.assertIn('-parentPid 2',clean)

    def test_process_inventory_parses_spaceful_commands(self):
        sec=1700000000;started=c.display_start(sec);exe='/Applications/Unity Hub.app/Contents/MacOS/Unity Hub'
        text='2 1 '+started+' 0.0 1024 '+exe+'\n'
        identity=dict(status='Verified',pid=2,ppid=1,start_sec=sec,start_usec=1,executable=exe)
        with patch.object(c,'run',return_value=text),patch.object(c,'kernel_process_identity',return_value=identity):
            rows=c.processes();self.assertEqual(rows[0]['pid'],2);self.assertEqual(rows[0]['started'],started);self.assertTrue(rows[0]['identity_verified'])
    def test_collector_keeps_full_parent_graph_without_unrelated_arguments(self):
        from types import SimpleNamespace
        sec=1700000000;started=c.display_start(sec)
        exe='/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity'
        generic='/usr/bin/python3'
        text='2 1 '+started+' 0.0 1024 '+exe+'\n3 2 '+started+' 0.0 1024 '+generic+'\n'
        def kernel(pid):return dict(status='Verified',pid=pid,ppid=1 if pid==2 else 2,start_sec=sec,start_usec=1,executable=exe if pid==2 else generic)
        observed=SimpleNamespace(returncode=0,stdout='2 '+started+' '+exe+' -projectPath '+c.ROOT+' -cbmEditorLicense secret\n')
        with patch.object(c,'run',return_value=text),patch.object(c,'kernel_process_identity',side_effect=kernel),patch.object(c.subprocess,'run',return_value=observed) as query:
            rows=c.processes();self.assertEqual({r['pid'] for r in rows},{2,3});self.assertEqual(rows[1]['command'],generic)
            self.assertNotIn('secret',rows[0]['command']);self.assertEqual(query.call_args.args[0][3],'2')
    def test_kernel_and_ps_birth_mismatch_is_unverified(self):
        sec=1700000000;exe='/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/Tools/UnityShaderCompiler'
        text='2 1 '+c.display_start(sec)+' 0.0 1024 '+exe+'\n'
        identity=dict(status='Verified',pid=2,ppid=1,start_sec=sec+1,start_usec=1,executable=exe)
        from types import SimpleNamespace
        with patch.object(c,'run',return_value=text),patch.object(c,'kernel_process_identity',return_value=identity),patch.object(c.subprocess,'run',return_value=SimpleNamespace(returncode=1,stdout='')):
            rows=c.processes();self.assertFalse(rows[0]['identity_verified']);self.assertEqual(c.native_kind(rows[0]),'uncertain_worker')

    def test_missing_executable_path_is_not_kernel_pid_absence(self):
        import ctypes,errno
        from types import SimpleNamespace
        calls=[]
        def info(pid,flavor,arg,buffer,size):
            calls.append(pid);value=ctypes.cast(buffer,ctypes.POINTER(c._BsdInfo)).contents
            value.pid=pid;value.start_sec=1700000000;value.start_usec=1;return ctypes.sizeof(value)
        def path(*args):ctypes.set_errno(errno.ESRCH);return 0
        with patch.object(c.sys,'platform','darwin'),patch.object(c,'_LIBPROC',SimpleNamespace(proc_pidinfo=info,proc_pidpath=path)):
            self.assertEqual(c.kernel_process_identity(42)['status'],'Unverified');self.assertEqual(len(calls),2)
    def test_only_kernel_pid_absence_reports_missing(self):
        import ctypes,errno
        from types import SimpleNamespace
        def info(*args):ctypes.set_errno(errno.ESRCH);return 0
        with patch.object(c.sys,'platform','darwin'),patch.object(c,'_LIBPROC',SimpleNamespace(proc_pidinfo=info)):
            self.assertEqual(c.kernel_process_identity(42)['status'],'Missing')

    def test_actual_unity_cli_auth_envelope_uses_data_logged_in(self):
        value=dict(command='auth',data={'loggedIn':True},errors=[],success=True,warnings=[])
        self.assertTrue(c.auth_logged_in(value))
    def test_auth_rejects_wrong_top_level_login_errors_or_untyped_success(self):
        value=dict(command='auth',data={'loggedIn':True},errors=[],success=True,warnings=[])
        for key,replacement in [('command','other'),('success',False),('success',1),('errors',['failed']),('warnings',None),('data',{'loggedIn':False}),('data',{'loggedIn':'true'}),('data',{})]:
            bad=copy.deepcopy(value);bad[key]=replacement
            with self.subTest(key=key,replacement=replacement),self.assertRaises(ValueError):c.auth_logged_in(bad)
        with self.assertRaises(ValueError):c.auth_logged_in({'loggedIn':True})

    def test_mac_hub_android_module_is_sibling_of_unity_app(self):
        root=Path('/Applications/Unity/Hub/Editor/6000.5.8f1')
        self.assertEqual(c.android_module(root),root/'PlaybackEngines/AndroidPlayer')
        self.assertNotIn('Unity.app',c.android_module(root).relative_to(root).parts)

    def test_sdk_alias_only_resolves_inside_explicit_android_root(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d);(p/'actual').write_text('tool');(p/'alias').symlink_to(p/'actual')
            with self.assertRaises(ValueError):c.checked_file(p/'alias')
            self.assertEqual(c.checked_file(p/'alias',p)['resolved_path'],str((p/'actual').resolve()))


if __name__=='__main__':unittest.main(verbosity=2)
