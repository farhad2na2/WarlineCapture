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
        hub=dict(pid=1,ppid=0,started='now',cpu=0,rss_kib=1,command='/Applications/Unity Hub.app/Contents/MacOS/Unity Hub')
        for project,cpu in [(c.ROOT,0),('/tmp/other',100)]:
            e=dict(hub,pid=2,cpu=cpu,command='/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity -projectPath '+project)
            with patch.object(c,'processes',return_value=[hub,e]),patch.object(c.time,'sleep'),self.assertRaises(ValueError):c.capacity(Path(c.ROOT))
    def test_worker_load_blocks_even_with_idle_parent_editor(self):
        hub=dict(pid=1,ppid=0,started='now',cpu=0,rss_kib=1,command='/Applications/Unity Hub.app/Contents/MacOS/Unity Hub')
        e=dict(hub,pid=2,command='/Applications/Unity/Hub/Editor/6000.5.8f1/Unity.app/Contents/MacOS/Unity -projectPath /tmp/other')
        worker=dict(hub,pid=3,ppid=2,cpu=100,command='UnityShaderCompiler')
        with patch.object(c,'processes',return_value=[hub,e,worker]),patch.object(c.time,'sleep'),self.assertRaises(ValueError):c.capacity(Path(c.ROOT))

    def test_process_evidence_redacts_license_and_auth_credentials(self):
        command='/Applications/Unity.app/Contents/MacOS/Unity -projectPath /tmp/owned -cbmEditorLicense secret1 -cbmMachineId secret2 --token=secret3 -parentPid 2'
        clean=c.redact_command(command)
        for value in ('secret1','secret2','secret3'):self.assertNotIn(value,clean)
        self.assertIn('-projectPath /tmp/owned',clean);self.assertIn('-parentPid 2',clean)

    def test_process_inventory_parses_spaceful_commands(self):
        text=' 2 1 Sun Oct 18 14:00:00 2026 0.0 1024 /Applications/Unity Hub.app/Contents/MacOS/Unity Hub\n'
        with patch.object(c,'run',return_value=text):
            rows=c.processes();self.assertEqual(rows[0]['pid'],2);self.assertEqual(rows[0]['started'],'Sun Oct 18 14:00:00 2026')
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
