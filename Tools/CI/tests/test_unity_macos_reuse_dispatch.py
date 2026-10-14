"""Exercise the staged wrapper against CLI stubs and its unchanged real monitor."""
import json
import os
import pathlib
import subprocess
import tempfile
import unittest

BASE = pathlib.Path(__file__).resolve().parents[1]
CLI = r'''#!/usr/bin/env python3
import json,os,pathlib,re,sys
a=sys.argv[1:]; pathlib.Path(os.environ['ARGS_CAPTURE']).write_text(json.dumps(a))
def arg(k): return a[a.index(k)+1]
if a[:2]!=['command','run_script'] or '--timeout_ms' not in a or '--code' in a:
 print('unsupported dispatch',file=sys.stderr);sys.exit(2)
source=pathlib.Path(arg('--file')).read_text();pathlib.Path(os.environ['SOURCE_CAPTURE']).write_text(source)
if arg('--entry')!='WarlineExistingValidationDispatch.Run':sys.exit(2)
match=re.search(r'ExistingEditorValidation.Run\(("(?:\\.|[^"\\])*")\s*,\s*("(?:\\.|[^"\\])*")\)',source)
if not match:sys.exit(2)
method,log=(json.loads(x) for x in match.groups());mode=os.environ['STUB_MODE']
if mode=='unsupported': print('unsupported dispatch',file=sys.stderr);sys.exit(2)
project=str(pathlib.Path(arg('--project-path')).resolve()/'Assets')
if mode=='foreign':project='/other/project/Assets'
pathlib.Path(log).write_text('[ExistingEditorValidation] project='+project+' executeMethod='+method+'\n[TestCase] result=Passed\n')
if mode!='missing_receipt':pathlib.Path(log+'.exit').write_text('1' if mode=='native_failure' else '0')
print(json.dumps({'success':mode!='lost_ack','data':{'success':mode!='lost_ack','result':{'success':mode!='lost_ack','result':'scheduled'}}}))
sys.exit(1 if mode=='lost_ack' else 0)
'''


class ReuseDispatchTests(unittest.TestCase):
    def invoke(self, mode='pass', timeout=4, collision=None, escaped=False):
        with tempfile.TemporaryDirectory(prefix='wrapper-stub-') as temp:
            work = pathlib.Path(temp)
            project = work / ('project "quoted"' if escaped else 'project')
            project.mkdir()
            bin_path = work/'bin'
            bin_path.mkdir()
            for name, text in [('unity', CLI), ('pgrep', '#!/bin/sh\necho "1 Unity Hub"\nexit 0\n')]:
                p=bin_path/name;p.write_text(text);p.chmod(0o755)
            editor=work/'unused-editor';editor.write_text('#!/bin/sh\nexit 99\n');editor.chmod(0o755)
            log=project/'owned.log'
            if collision is not None:pathlib.Path(str(log)+collision).write_text('preserve exact existing evidence')
            env=dict(os.environ, PATH=str(bin_path)+os.pathsep+os.environ['PATH'], STUB_MODE=mode,
                     ARGS_CAPTURE=str(work/'args.json'), SOURCE_CAPTURE=str(work/'source.txt'))
            result=subprocess.run(['bash',str(BASE/'invoke_unity_macos.sh'),'--reuse','--project',str(project),
                                   '--unity',str(editor),'--timeout',str(timeout),'--log',str(log),
                                   '--','-executeMethod','Game.Tests.Editor.RealCase.Run'],env=env,
                                  capture_output=True,text=True,timeout=10)
            args=json.loads((work/'args.json').read_text()) if (work/'args.json').exists() else None
            source=(work/'source.txt').read_text() if (work/'source.txt').exists() else None
            existing=pathlib.Path(str(log)+(collision if collision is not None else '.dispatch.cs'))
            evidence=existing.read_text() if existing.exists() else None
            return result,args,source,evidence

    def test_supported_run_script_and_native_budget_are_distinct(self):
        r,args,source,evidence=self.invoke(timeout=400)
        self.assertEqual(r.returncode,0,r.stdout+r.stderr)
        self.assertEqual(args[:2],['command','run_script'])
        self.assertEqual(args[args.index('--timeout_ms')+1],'30000')
        self.assertEqual(args[args.index('--timeout')+1],'400')
        self.assertIn('EditorApplication.timeSinceStartup+10',source)
        self.assertIn('EditorApplication.update -= callback',source)
        self.assertIn('Game.Tests.Editor.RealCase.Run',source)
        self.assertEqual(source,evidence)

    def test_short_timeout_caps_native_dispatch_budget(self):
        r,args,_,_=self.invoke(timeout=4)
        self.assertEqual(r.returncode,0,r.stdout+r.stderr)
        self.assertEqual(args[args.index('--timeout_ms')+1],'4000')

    def test_acknowledgement_failure_requires_exact_native_ownership(self):
        r,_,_,_=self.invoke('lost_ack')
        self.assertEqual(r.returncode,0,r.stdout+r.stderr)
        self.assertIn('exact validation log confirms ownership',r.stdout)

    def test_unsupported_dispatch_without_native_evidence_fails(self):
        r,_,_,_=self.invoke('unsupported',timeout=1)
        self.assertEqual(r.returncode,124,r.stdout+r.stderr)
        self.assertIn('existing Editor preserved',r.stderr)

    def test_foreign_owner_cannot_pass_using_success_receipt(self):
        r,_,_,_=self.invoke('foreign')
        self.assertNotEqual(r.returncode,0)
        self.assertIn('ownership mismatch',r.stderr)

    def test_native_failure_receipt_fails(self):
        r,_,_,_=self.invoke('native_failure')
        self.assertNotEqual(r.returncode,0)
        self.assertIn('failed with status',r.stderr)

    def test_missing_native_receipt_times_out(self):
        r,_,_,_=self.invoke('missing_receipt',timeout=1)
        # Dispatch may consume the one-second budget before monitor entry.
        # Both timeout branches must fail closed and preserve the Editor.
        self.assertEqual(r.returncode,124,r.stdout+r.stderr)
        self.assertIn('existing Editor preserved',r.stderr)

    def test_sidecar_is_exclusive_and_existing_bytes_are_preserved(self):
        r,args,_,existing=self.invoke(collision='.dispatch.cs')
        self.assertEqual(r.returncode,64)
        self.assertIsNone(args)
        self.assertEqual(existing,'preserve exact existing evidence')

    def test_prior_log_is_preserved_before_cli(self):
        r,args,_,existing=self.invoke(collision='')
        self.assertEqual(r.returncode,64)
        self.assertIsNone(args)
        self.assertEqual(existing,'preserve exact existing evidence')

    def test_quoted_project_and_log_are_exact_owned_arguments(self):
        r,_,source,_=self.invoke(escaped=True)
        self.assertEqual(r.returncode,0,r.stdout+r.stderr)
        self.assertIn('\\"quoted\\"',source)


if __name__=='__main__':unittest.main(verbosity=2)
