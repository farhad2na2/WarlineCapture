"""Offline tests for the ARIA queue runner."""
import csv
from datetime import date
import io
import os
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock

from Tools.Missions import run_aria_queue as runner


FIXTURE = '''Unity starting
[SkirmishAriaQueue] job=1/6 catalog=S002 seed=1 locale=en result=Victory seconds=10.5
noise [SkirmishAriaQueue] job=2/6 catalog=BAD seed=1 locale=en result=Victory seconds=1
[SkirmishAriaQueue] job=2/6 catalog=S002 seed=1 locale=fa-IR result=Defeat seconds=20
[SkirmishAriaQueue] job=3/6 catalog=S002 seed=2 locale=en result=Abort seconds=3
[SkirmishAriaQueue] job=4/6 catalog=S003 seed=1 locale=en result=Victory seconds=11.5
[SkirmishAriaQueue] job=5/6 catalog=S003 seed=1 locale=fa-IR result=Victory seconds=14.5
[SkirmishAriaQueue] job=6/6 catalog=S003 seed=2 locale=en result=Victory seconds=12.5
[SkirmishAriaQueue] result=Completed jobs=6 recorded=6 wins=4
Shutdown noise
'''


class QueueTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name).resolve()

    def test_job_order_and_queue_contents(self):
        jobs = runner.job_list(['S002', 'S003'], ['1', '2'], ['en', 'fa-IR'])
        self.assertEqual(jobs, [
            ('S002', '1', 'en'), ('S002', '1', 'fa-IR'),
            ('S002', '2', 'en'), ('S002', '2', 'fa-IR'),
            ('S003', '1', 'en'), ('S003', '1', 'fa-IR'),
            ('S003', '2', 'en'), ('S003', '2', 'fa-IR'),
        ])
        queue = self.root / 'queue.csv'
        runner.write_queue(queue, jobs)
        self.assertEqual(queue.read_bytes(), b'S002,1,en\nS002,1,fa-IR\nS002,2,en\nS002,2,fa-IR\nS003,1,en\nS003,1,fa-IR\nS003,2,en\nS003,2,fa-IR\n')

    def test_launcher_commands_and_no_table(self):
        repo = self.root / 'repo'
        (repo / 'ProjectSettings').mkdir(parents=True)
        (repo / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.5.2f1\n')
        logs = self.root / 'logs'
        table = repo / 'missions.csv'
        table.write_text('unchanged')
        common = ['--jobs', 'S002:1:en,S002:2:en', '--repo', str(repo),
                  '--log-dir', str(logs), '--table', 'missions.csv', '--no-table', '--timeout', '123']
        fixture = ('[SkirmishAriaQueue] job=1/2 catalog=S002 seed=1 locale=en result=Victory seconds=5\n'
                   '[SkirmishAriaQueue] result=Completed jobs=2 recorded=2 wins=2\n')
        for launcher in ('macos', 'windows'):
            calls = []
            def run(command, **kwargs):
                calls.append((command, kwargs))
                if 'ResolveUnityEditor.ps1' in command[5]:
                    return subprocess.CompletedProcess(command, 0, 'C:\\Unity\\Unity.exe\n', '')
                (logs / 'unity.log').write_text(fixture)
                return subprocess.CompletedProcess(command, 0)
            with self.subTest(launcher=launcher), mock.patch.object(runner.subprocess, 'run', side_effect=run), \
                 mock.patch.object(runner, 'check_editor_and_hub'), mock.patch('sys.stdout', new_callable=io.StringIO) as out:
                self.assertEqual(runner.main(common + ['--launcher', launcher] +
                                             (['--gui-licensing'] if launcher == 'windows' else [])), 0)
                self.assertIn('catalog=S002 seed=1 locale=en result=Victory', out.getvalue())
            self.assertEqual(table.read_text(), 'unchanged')
            command, kwargs = calls[-1]
            self.assertEqual(kwargs['env']['WARLINE_ARIA_QUEUE'], str(logs / 'queue.csv'))
            self.assertEqual(kwargs['cwd'], repo)
            if launcher == 'macos':
                self.assertEqual(command, [str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--timeout', '123',
                    '--log', str(logs / 'unity.log'), '--', '-executeMethod',
                    'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'])
            else:
                self.assertEqual(calls[0][0], ['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
                    str(repo / 'Tools/CI/ResolveUnityEditor.ps1'), '-UnityVersion', '6000.5.2f1'])
                self.assertEqual(command, ['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File',
                    str(repo / 'Tools/CI/InvokeUnity.ps1'), '-UnityExe', 'C:\\Unity\\Unity.exe',
                    '-ProjectPath', str(repo), '-LogFile', str(logs / 'unity.log'), '-TimeoutSeconds', '123',
                    '-GuiLicensing', '-releaseCodeOptimization', '-executeMethod', 'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'])

    def test_menu_journey_flag_sets_the_environment_only_when_asked(self):
        repo = self.root / 'repo'
        (repo / 'ProjectSettings').mkdir(parents=True)
        logs = self.root / 'logs'
        fixture = ('[SkirmishAriaQueue] job=1/1 catalog=S002 seed=1 locale=en result=Victory seconds=5\n'
                   '[SkirmishAriaQueue] result=Completed jobs=1 recorded=1 wins=1\n')
        common = ['--jobs', 'S002:1:en', '--repo', str(repo), '--log-dir', str(logs), '--no-table', '--launcher', 'macos']
        for flag in (False, True):
            calls = []
            def run(command, **kwargs):
                calls.append(kwargs)
                (logs / 'unity.log').write_text(fixture)
                return subprocess.CompletedProcess(command, 0)
            with self.subTest(menu_journey=flag), mock.patch.object(runner.subprocess, 'run', side_effect=run), \
                 mock.patch.object(runner, 'check_editor_and_hub'), mock.patch('sys.stdout', new_callable=io.StringIO), \
                 mock.patch.dict(runner.os.environ, {}, clear=False):
                runner.os.environ.pop('WARLINE_MENU_JOURNEY', None)
                self.assertEqual(runner.main(common + (['--menu-journey'] if flag else [])), 0)
            env = calls[-1]['env']
            self.assertEqual('1' if flag else None, env.get('WARLINE_MENU_JOURNEY'))
            self.assertEqual(str(logs / 'queue.csv'), env['WARLINE_ARIA_QUEUE'])

    def test_validation_library_flag_sets_the_environment_only_when_asked(self):
        repo = self.root / 'repo'
        (repo / 'ProjectSettings').mkdir(parents=True)
        logs = self.root / 'logs'
        fixture = ('[SkirmishAriaQueue] job=1/1 catalog=S026 seed=1 locale=en result=Victory seconds=5\n'
                   '[SkirmishAriaQueue] result=Completed jobs=1 recorded=1 wins=1\n')
        common = ['--jobs', 'S026:1:en', '--repo', str(repo), '--log-dir', str(logs), '--no-table', '--launcher', 'macos',
                  '--menu-journey']
        for flag in (False, True):
            calls = []
            def run(command, **kwargs):
                calls.append(kwargs)
                (logs / 'unity.log').write_text(fixture)
                return subprocess.CompletedProcess(command, 0)
            with self.subTest(validation_library=flag), mock.patch.object(runner.subprocess, 'run', side_effect=run), \
                 mock.patch.object(runner, 'check_editor_and_hub'), mock.patch('sys.stdout', new_callable=io.StringIO), \
                 mock.patch.dict(runner.os.environ, {}, clear=False):
                runner.os.environ.pop('WARLINE_VALIDATION_LIBRARY', None)
                self.assertEqual(runner.main(common + (['--validation-library'] if flag else [])), 0)
            env = calls[-1]['env']
            self.assertEqual('1' if flag else None, env.get('WARLINE_VALIDATION_LIBRARY'))
            self.assertEqual('1', env.get('WARLINE_MENU_JOURNEY'))

    def _reuse_fixture(self):
        repo = self.root / 'repo'
        (repo / 'ProjectSettings').mkdir(parents=True)
        logs = self.root / 'logs'
        fixture = ('[SkirmishAriaQueue] job=1/1 catalog=S026 seed=1 locale=en result=Victory seconds=5\n'
                   '[SkirmishAriaQueue] result=Completed jobs=1 recorded=1 wins=1\n')
        common = ['--jobs', 'S026:1:en', '--repo', str(repo), '--log-dir', str(logs), '--no-table', '--launcher', 'macos',
                  '--reuse', '--menu-journey', '--validation-library']
        return repo, logs, fixture, common

    def test_reuse_writes_the_request_file_and_drives_the_wrapper_in_reuse_mode_without_environment_flags(self):
        repo, logs, fixture, common = self._reuse_fixture()
        seen = {}
        def run(command, **kwargs):
            seen['command'], seen['env'] = command, kwargs['env']
            seen['request'] = json.loads((repo / 'Temp/aria-queue/request.json').read_text())
            (logs / 'unity.log').write_text(fixture)
            return subprocess.CompletedProcess(command, 0)
        env = {k: v for k, v in runner.os.environ.items() if not k.startswith('WARLINE_')}
        with mock.patch.object(runner.subprocess, 'run', side_effect=run), \
             mock.patch.object(runner, 'check_editor_and_hub') as editor_check, mock.patch.object(runner, 'check_hub') as hub_check, \
             mock.patch('sys.stdout', new_callable=io.StringIO), mock.patch.dict(runner.os.environ, env, clear=True):
            self.assertEqual(runner.main(common), 0)
        editor_check.assert_not_called()  # an open Editor is the point of --reuse
        hub_check.assert_called_once()
        self.assertEqual([str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--reuse', '--timeout', '7200', '--log', str(logs / 'unity.log'),
                          '--', '-executeMethod', 'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'], seen['command'])
        request = seen['request']
        self.assertEqual(1, request['version'])
        self.assertEqual(str(repo.resolve()), request['project'])
        self.assertEqual(str(logs.resolve() / 'queue.csv'), request['queue'])
        self.assertTrue(request['menuJourney'] and request['validationLibrary'])
        self.assertRegex(request['createdUtc'], r'^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$')
        self.assertTrue(request['requestId'])
        for name in ('WARLINE_ARIA_QUEUE', 'WARLINE_MENU_JOURNEY', 'WARLINE_VALIDATION_LIBRARY'):
            self.assertNotIn(name, seen['env'])  # environment cannot reach an open Editor
        self.assertEqual('S026,1,en\n', (logs / 'queue.csv').read_text())
        # The mocked Editor never consumed the request, so the runner removed it instead of leaving it to be replayed.
        self.assertFalse((repo / 'Temp/aria-queue/request.json').exists())

    def test_reuse_flags_are_false_in_the_request_unless_asked(self):
        repo = self.root / 'repo'
        path, request_id = runner.write_reuse_request(repo, self.root / 'q.csv', False, False)
        request = json.loads(path.read_text())
        self.assertEqual(request_id, request['requestId'])
        self.assertFalse(request['menuJourney'])
        self.assertFalse(request['validationLibrary'])
        self.assertFalse(path.with_name(path.name + '.tmp').exists(), 'written atomically')

    def test_reuse_removes_only_its_own_unconsumed_request(self):
        repo = self.root / 'repo'
        path, request_id = runner.write_reuse_request(repo, self.root / 'q.csv', True, False)
        self.assertFalse(runner.drop_unconsumed_request(path, 'someone-elses-request'))
        self.assertTrue(path.exists())
        self.assertTrue(runner.drop_unconsumed_request(path, request_id))
        self.assertFalse(path.exists())
        self.assertFalse(runner.drop_unconsumed_request(path, request_id), 'already consumed by the Editor')

    def test_reuse_refuses_a_log_directory_with_prior_evidence(self):
        repo, logs, fixture, common = self._reuse_fixture()
        logs.mkdir()
        (logs / 'unity.log.exit').write_text('0')
        with mock.patch.object(runner.subprocess, 'run') as run, mock.patch.object(runner, 'check_hub'), \
             mock.patch('sys.stderr', new_callable=io.StringIO) as err:
            self.assertEqual(1, runner.main(common))
        run.assert_not_called()
        self.assertIn('prior evidence', err.getvalue())
        self.assertFalse((repo / 'Temp/aria-queue/request.json').exists(), 'no request is written for a refused run')

    def test_reuse_is_macos_only(self):
        repo, logs, fixture, common = self._reuse_fixture()
        common[common.index('macos')] = 'windows'
        with mock.patch('sys.stderr', new_callable=io.StringIO), self.assertRaises(SystemExit) as raised:
            runner.main(common)
        self.assertEqual(2, raised.exception.code)

    def test_reuse_dry_run_writes_no_request_and_launches_nothing(self):
        repo, logs, fixture, common = self._reuse_fixture()
        with mock.patch.object(runner.subprocess, 'run') as run, mock.patch('sys.stdout', new_callable=io.StringIO):
            self.assertEqual(0, runner.main(common + ['--dry-run']))
        run.assert_not_called()
        self.assertFalse((repo / 'Temp').exists())

    def test_menu_journey_flag_is_accepted_in_dry_run_and_does_not_touch_the_queue_file(self):
        logs = self.root / 'logs'
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)), '--jobs', 'S002:1:en', '--log-dir', str(logs),
                               '--dry-run', '--menu-journey'], capture_output=True, text=True)
        self.assertEqual(0, proc.returncode, proc.stderr)
        self.assertEqual('S002,1,en\n', proc.stdout)
        self.assertEqual(b'S002,1,en\n', (logs / 'queue.csv').read_bytes())

    def test_windows_failures_and_always_gui(self):
        repo = self.root / 'repo'
        (repo / 'ProjectSettings').mkdir(parents=True)
        (repo / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 6000.5.2f1\n')
        logs = self.root / 'logs'
        def run(command, **kwargs):
            if 'ResolveUnityEditor.ps1' in command[5]:
                return subprocess.CompletedProcess(command, 0, 'editor.exe\n', '')
            self.assertIn('-GuiLicensing', command)  # ARIA never runs headless
            (logs / 'unity.log').write_text('[SkirmishAriaQueue] result=Completed jobs=1 recorded=1 wins=1\n')
            return subprocess.CompletedProcess(command, 124)
        with mock.patch.object(runner.subprocess, 'run', side_effect=run):
            self.assertEqual(runner.main(['--launcher', 'windows', '--jobs', 'S002:1:en', '--repo', str(repo),
                                          '--log-dir', str(logs), '--no-table']), 1)

        def missing_marker(command, **kwargs):
            if 'ResolveUnityEditor.ps1' in command[5]:
                return subprocess.CompletedProcess(command, 0, 'editor.exe\n', '')
            (logs / 'unity.log').write_text('no completion marker\n')
            return subprocess.CompletedProcess(command, 0)
        with mock.patch.object(runner.subprocess, 'run', side_effect=missing_marker):
            self.assertEqual(runner.main(['--launcher', 'windows', '--jobs', 'S002:1:en', '--repo', str(repo),
                                          '--log-dir', str(logs), '--no-table']), 1)

    def test_log_directory_on_another_drive(self):
        with mock.patch.object(runner.os.path, 'commonpath', side_effect=ValueError('different drives')):
            self.assertEqual(runner.main(['--jobs', 'S002:1:en', '--repo', str(self.root / 'repo'),
                                          '--log-dir', str(self.root / 'logs'), '--dry-run']), 0)

    def test_explicit_jobs_and_dry_run_order(self):
        spec = 'S002:130365:en,S006:130370:en,S002:130365:fa-IR'
        jobs = [('S002', '130365', 'en'), ('S006', '130370', 'en'),
                ('S002', '130365', 'fa-IR')]
        self.assertEqual(runner.explicit_jobs(spec), jobs)
        logs = self.root / 'logs'
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                               '--jobs', spec, '--log-dir', str(logs), '--dry-run'],
                              capture_output=True, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        expected = 'S002,130365,en\nS006,130370,en\nS002,130365,fa-IR\n'
        self.assertEqual(proc.stdout, expected)
        self.assertEqual((logs / 'queue.csv').read_text(), expected)

    def test_invalid_job_forms_fail_before_creating_log_dir(self):
        cases = [('S002:1', 'fields'), ('S002:1:en:extra', 'fields'),
                 ('S002::en', 'fields'), ('S002:abc:en', 'integer seed'),
                 ('S002:1:en,S002:1:en', 'Duplicate job')]
        for spec, error in cases:
            with self.subTest(spec=spec):
                logs = self.root / 'logs'
                proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                                       '--jobs', spec, '--log-dir', str(logs), '--dry-run'],
                                      capture_output=True, text=True)
                self.assertNotEqual(proc.returncode, 0)
                self.assertIn(error, proc.stderr)
                self.assertFalse(logs.exists())

    def test_job_forms_are_mutually_exclusive_and_required(self):
        cases = [[], ['--missions', 'S002'],
                 ['--jobs', 'S002:1:en', '--missions', 'S002'],
                 ['--jobs', 'S002:1:en', '--seeds', '1'],
                 ['--jobs', 'S002:1:en', '--locales', 'en']]
        for args in cases:
            with self.subTest(args=args):
                logs = self.root / 'logs'
                proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                                       *args, '--log-dir', str(logs), '--dry-run'],
                                      capture_output=True, text=True)
                self.assertNotEqual(proc.returncode, 0)
                self.assertIn('error:', proc.stderr)
                self.assertFalse(logs.exists())

    def test_log_parser_ignores_noise(self):
        jobs, completed = runner.parse_log(FIXTURE)
        self.assertEqual(len(jobs), 6)
        self.assertEqual(jobs[0][:5], ('S002', '1', 'en', 'Victory', None))
        self.assertEqual(jobs[0].job_seconds, 10.5)
        self.assertEqual(jobs[1][3], 'Defeat')
        self.assertEqual(jobs[2][3], 'Abort')
        self.assertEqual(completed, (6, 6, 4))
        self.assertIsNone(runner.parse_log('unrelated')[1])

    def test_table_preserves_other_fields_and_rows(self):
        table = self.root / 'missions.csv'
        header = 'mission_id,note,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok,other\r\n'
        row1 = 'S002,"a, note",old,old,old,old,old,old,x,x,x,x,"keep ""quotes"""\r\n'
        row2 = 'S003,"two\nlines",old,old,old,old,old,old,x,x,x,x,untouched\r\n'
        row3 = 'S004,"untouched, row",old,old,old,old,old,old,x,x,x,x,"yes"\r\n'
        table.write_bytes((header + row1 + row2 + row3).encode())
        jobs, _ = runner.parse_log(FIXTURE)
        expected = [(j[0], j[1], j[2]) for j in jobs]
        log = self.root / 'unity.log'
        self.assertEqual(runner.update_table(table, jobs, expected, log, date(2026, 11, 16)), 2)
        actual = table.read_bytes().decode()
        self.assertTrue(actual.startswith(header))
        self.assertTrue(actual.endswith(row3))
        self.assertIn('S002,"a, note",W1 L0 A1,W0 L1 A0,unproven,abort,', actual)
        self.assertIn('S003,"two\nlines",W2 L0 A0,W1 L0 A0,unproven,win,', actual)
        self.assertIn(',"keep ""quotes"""\r\n', actual)
        rows = list(csv.DictReader(io.StringIO(actual, newline='')))
        self.assertEqual(rows[0]['last_log'], str(log.resolve()))
        self.assertEqual(rows[0]['updated'], '2026-11-16')
        self.assertEqual(rows[1]['other'], 'untouched')
        self.assertEqual(rows[2]['aria_en'], 'old')

    def test_missing_completion_exits_one_in_dry_run(self):
        repo = self.root / 'repo'
        repo.mkdir()
        table = repo / 'missions.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,old,old,old,old,old,old,x,x,x,x\n')
        logs = self.root / 'logs'
        logs.mkdir()
        (logs / 'unity.log').write_text('[SkirmishAriaQueue] job=1/2 catalog=S002 seed=1 locale=en result=Victory seconds=5\n')
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)), '--missions', 'S002',
                               '--seeds', '1', '--locales', 'en,fa-IR', '--repo', str(repo),
                               '--table', 'missions.csv', '--log-dir', str(logs), '--dry-run'],
                              capture_output=True, text=True)
        self.assertEqual(proc.returncode, 1, proc.stdout + proc.stderr)
        self.assertIn('Completed marker missing', proc.stdout)
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual(row['aria_en'], 'W1 L0 A0')
        self.assertEqual(row['aria_fa'], 'old')
        self.assertEqual(row['unity_result'], 'old')

    def test_dispatch_evidence_and_rescan(self):
        log = self.root / 'unity.log'
        log.write_text("""[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=155923 locale=en
[AriaS002Trace] event=gesture frame=505 intent=SelectSquad infantry=20
[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=MAIN MENU point=(1629.00, 153.50)
[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU completed=1 interventions=0
[SkirmishAriaQueue] dispatch job=2/2 catalog=S002 seed=130365 locale=en
[SkirmishAriaQueue] job=1/2 catalog=S002 seed=155923 locale=en result=Victory seconds=272.8 recorded=1 countedWin=1
[SkirmishAriaQueue] job=2/2 catalog=S002 seed=130365 locale=en result=Victory seconds=266.7 recorded=1 countedWin=1
[SkirmishAriaQueue] result=Completed jobs=2 recorded=2 wins=2 sourceUnchanged=1 exitCode=0
""")
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)), '--rescan', str(log)], capture_output=True, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertIn('units_available=unproven units_used_by_aria=unproven result_return_ok=yes checkpoint_ok=unproven', proc.stdout)
        self.assertIn('result_return_ok=unproven', proc.stdout.splitlines()[1])
        evidence = runner.scan_evidence(log.read_text().splitlines())
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        jobs, _ = runner.parse_log(log.read_text())
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['result_return_ok'], 'unproven')
        evidence[(2, 2)]['result_return_ok'] = 'no'
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['result_return_ok'], 'no')

    def test_rescan_write_is_opt_in_and_aggregates(self):
        log = self.root / 'unity.log'
        log.write_text("""[SkirmishAriaQueue] dispatch job=1/1 catalog=S002 seed=155923 locale=en
[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU completed=1 interventions=0
[SkirmishAriaQueue] job=1/1 catalog=S002 seed=155923 locale=en result=Victory seconds=272.8 recorded=1 countedWin=1
""")
        table = self.root / 'missions.csv'
        original = 'mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n'
        table.write_text(original)
        args = [sys.executable, '-B', str(Path(runner.__file__)), '--rescan', str(log), '--repo', str(self.root), '--table', 'missions.csv']
        self.assertEqual(subprocess.run(args, capture_output=True).returncode, 0)
        self.assertEqual(table.read_text(), original)
        self.assertEqual(subprocess.run(args + ['--write', '--no-table'], capture_output=True).returncode, 0)
        self.assertEqual(table.read_text(), original)
        self.assertEqual(subprocess.run(args + ['--write'], capture_output=True).returncode, 0)
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['result_return_ok'], row['checkpoint_ok'], row['units_available']), ('yes', 'unproven', 'unproven'))

    def test_gate_units_are_attributed_and_aggregated(self):
        lines = '''[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=1 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Infantry;Jet
[SkirmishGateUnits] event=used catalog=S002 session=one types=Airport;Jet unused=Infantry
[SkirmishAriaQueue] dispatch job=2/2 catalog=S002 seed=2 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=two types=Airport;Infantry;Tank
[SkirmishGateUnits] event=used catalog=S002 session=two types=Airport;Tank unused=Infantry
'''.splitlines()
        evidence = runner.scan_evidence(lines)
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        jobs = [('S002', '1', 'en', 'Victory', 1), ('S002', '2', 'en', 'Victory', 2)]
        values = runner.aggregate_evidence(jobs, lookup)
        self.assertEqual(values['units_available'], 'Airport;Infantry;Jet;Tank')
        self.assertEqual(values['units_used_by_aria'], 'Airport')
        del lookup[('S002', '2', 'en')]
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['units_used_by_aria'], 'unproven')

    def test_gate_units_reject_mismatched_or_inconsistent_evidence(self):
        dispatch = '[SkirmishAriaQueue] dispatch job=1/1 catalog=S002 seed=1 locale=en'
        available = '[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Jet'
        for line in (
            '[SkirmishGateUnits] event=used catalog=S002 session=other types=Jet unused=Airport',
            '[SkirmishGateUnits] event=used catalog=S002 session=one types=Jet unused=',
            '[SkirmishGateUnits] event=used catalog=S003 session=one types=Jet unused=Airport',
            '[SkirmishGateUnits] event=available catalog=S002 session=one types=Jet;Airport',
        ):
            with self.subTest(line=line), self.assertRaises(ValueError):
                runner.scan_evidence([dispatch, available, line])

    def test_gate_units_empty_used_and_incomplete_queue(self):
        lines = '''[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=1 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Jet
[SkirmishGateUnits] event=used catalog=S002 session=one types= unused=Airport;Jet
'''.splitlines()
        entry = next(iter(runner.scan_evidence(lines).values()))
        self.assertEqual(entry['units_used_by_aria'], '')
        table = self.root / 'table.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n')
        runner.update_table(table, [('S002', '1', 'en', 'Victory', 1)],
                            [('S002', '1', 'en'), ('S002', '2', 'en')], self.root / 'log',
                            evidence={('S002', '1', 'en'): entry})
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['units_available'], row['units_used_by_aria']), ('unproven', 'unproven'))

    def test_frozen_clocks_match_attempt_and_dispatch(self):
        jobs, _ = runner.parse_log("""[SkirmishAriaQueue] dispatch job=1/2 catalog=S026 seed=13 locale=en
[SkirmishTerminalFrozen] result=Passed initial={attempt=expanded-s026-130-x elapsed=777}
[SkirmishTerminalFrozen] result=Passed initial={attempt=expanded-s002-13-x elapsed=888}
[SkirmishTerminalFrozen] result=Passed initial={found=1 attempt=expanded-s026-13-abc elapsed=147.868} delayed={attempt=expanded-s026-13-abc elapsed=900}
[SkirmishTerminalFrozen] result=Passed initial={attempt=expanded-s026-13-abc elapsed=456}
[SkirmishAriaQueue] dispatch job=2/2 catalog=S026 seed=13 locale=fa-IR
[SkirmishTerminalFrozen] result=Failed attempt=expanded-s026-13-def elapsed=13.25
[SkirmishAriaQueue] job=1/2 catalog=S026 seed=13 locale=en result=Victory seconds=218.3
[SkirmishAriaQueue] job=2/2 catalog=S026 seed=13 locale=fa-IR result=Defeat seconds=210
""")
        self.assertEqual([(j.match_seconds, j.line, j.job_seconds) for j in jobs],
                         [(147.868, 4, 218.3), (13.25, 7, 210.0)])

    def test_objective_missions_clock_return_and_gate_units(self):
        """FC/BT/CE use the shared terminal markers; the objective line and an ARIA-stop used list parse cleanly."""
        log = """[SkirmishAriaQueue] dispatch job=1/3 catalog=S007 seed=130365 locale=en
[SkirmishGateUnits] event=available catalog=S007 session=expanded-s007-130365-a types=role.apc_armored;role.rifle;role.tank
[SkirmishGateUnits] event=used catalog=S007 session=expanded-s007-130365-a types=role.rifle;role.tank automatic= unused=role.apc_armored
[SkirmishTerminalObjective] catalog=S007 type=FC attempt=expanded-s007-130365-a outcome=Victory reason=TicketsDepleted elapsed=612.500 deadline=1200.0 deadlinePct=51.04 ownZones=2 opposingZones=1 tickets=88/0
[SkirmishTerminalFrozen] result=Passed violations=none clockFrozen=1 initial={found=1 session=1:1 phase=Finished outcome=Victory attempt=expanded-s007-130365-a elapsed=612.500 playerBaseHp=800 enemyBaseHp=800 simActive=0 clockPlaying=0 clockPaused=1} delayed={found=1 session=1:1 phase=Finished outcome=Victory attempt=expanded-s007-130365-a elapsed=612.500 playerBaseHp=800 enemyBaseHp=800 simActive=0 clockPlaying=0 clockPaused=1}
[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=MAIN MENU point=(1,1)
[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU completed=1 interventions=0
[SkirmishAriaQueue] dispatch job=2/3 catalog=S013 seed=130365 locale=en
[SkirmishGateUnits] event=available catalog=S013 session=expanded-s013-130365-b types=role.rifle;role.tanker
[SkirmishGateUnits] event=used catalog=S013 session=expanded-s013-130365-b types=role.rifle automatic=role.tanker unused=
[SkirmishTerminalObjective] catalog=S013 type=BT attempt=expanded-s013-130365-b outcome=Defeat reason=BreakthroughImpossible elapsed=400.250 deadline=1080.0 deadlinePct=37.06 completed=3/8 lost=6 exitOpen=1
[SkirmishTerminalFrozen] result=Passed violations=none clockFrozen=1 initial={found=1 session=2:1 phase=Finished outcome=Defeat attempt=expanded-s013-130365-b elapsed=400.250 playerBaseHp=-1 enemyBaseHp=-1 simActive=0 clockPlaying=0 clockPaused=1} delayed={found=1 session=2:1 phase=Finished outcome=Defeat attempt=expanded-s013-130365-b elapsed=400.250 playerBaseHp=-1 enemyBaseHp=-1 simActive=0 clockPlaying=0 clockPaused=1}
[SkirmishTerminalProbe] result=Failed reason=resultNotLocalized completed=0 interventions=0
[SkirmishAriaQueue] dispatch job=3/3 catalog=S019 seed=130365 locale=en
[SkirmishGateUnits] event=available catalog=S019 session=expanded-s019-130365-c types=role.convoy_logic;role.rifle
[SkirmishGateUnits] event=used catalog=S019 session=expanded-s019-130365-c types=role.rifle automatic= unused=role.convoy_logic
[SkirmishTerminalObjective] catalog=S019 type=CE attempt=expanded-s019-130365-c outcome=None reason=None elapsed=95.000 deadline=1080.0 deadlinePct=8.80 completed=0/2 lost=0
[SkirmishAriaQueue] job=1/3 catalog=S007 seed=130365 locale=en result=Victory seconds=700.1 recorded=1 countedWin=1
[SkirmishAriaQueue] job=2/3 catalog=S013 seed=130365 locale=en result=Defeat seconds=480 recorded=1 countedWin=0
[SkirmishAriaQueue] job=3/3 catalog=S019 seed=130365 locale=en result=Abort seconds=120 recorded=0 countedWin=0
"""
        jobs, _ = runner.parse_log(log)
        self.assertEqual([(j.mission, j.match_seconds, j.clock_source) for j in jobs],
                         [('S007', 612.5, 'TerminalFrozen'), ('S013', 400.25, 'TerminalFrozen'), ('S019', None, 'unproven')])
        lookup = {e['mission']: e for e in runner.scan_evidence(log.splitlines()).values()}
        self.assertEqual(lookup['S007']['result_return_ok'], 'yes')
        self.assertEqual(lookup['S013']['result_return_ok'], 'no')
        self.assertEqual(lookup['S019']['result_return_ok'], 'unproven')
        self.assertEqual([lookup[m]['unused'] for m in ('S007', 'S013', 'S019')], ['role.apc_armored', '', 'role.convoy_logic'])

    def test_dispatchless_repeated_seed_needs_one_attempt_per_locale(self):
        frozen = '[SkirmishTerminalFrozen] attempt=expanded-s002-1-'
        summaries = ('[SkirmishAriaQueue] job=1/2 catalog=S002 seed=1 locale=en result=Victory seconds=20\n'
                     '[SkirmishAriaQueue] job=2/2 catalog=S002 seed=1 locale=fa-IR result=Victory seconds=21')
        first = frozen + 'abc elapsed=2\n' + frozen + 'abc elapsed=3\n'
        jobs, _ = runner.parse_log(first + frozen + 'def elapsed=4\n' + summaries)
        self.assertEqual([job.match_seconds for job in jobs], [2, 4])
        jobs, _ = runner.parse_log(first + summaries)
        self.assertEqual([job.match_seconds for job in jobs], [None, None])

    def test_missing_elapsed_uses_matching_candidate_row_only(self):
        sha = 'a' * 64
        base = self.root / 'Design/AgentReports/SkirmishExpansion/S026/_Evidence/candidates'
        path = base / sha / 'runs.csv'
        path.parent.mkdir(parents=True)
        header = 'run_id,catalog_id,seed,locale,result,duration_seconds\n'
        rows = ('S026-aria-rs-1-en-1,S026,1,en,Victory,300\n'
                'S026-aria-rs-1-en-2,S026,1,en,Victory,147.868\n')
        path.write_text(header + rows)
        log = f"""[SkirmishAriaQueue] result=Started jobs=1 sha256={sha}
[SkirmishAriaQueue] dispatch job=1/1 catalog=S026 seed=1 locale=en
[SkirmishTerminalFrozen] result=Passed initial={{attempt=expanded-s026-1-abc}} delayed={{attempt=expanded-s026-1-abc elapsed=999}}
[SkirmishTerminalFrozen] result=Passed initial={{attempt=expanded-s026-1-abc elapsed=888}}
S026-aria-rs-1-en-2,S026,more fields
[SkirmishAriaQueue] job=1/1 catalog=S026 seed=1 locale=en result=Victory seconds=218.3 recorded=1 countedWin=1
"""
        def score(snippet):
            return runner.scan_queue(snippet.splitlines(), self.root)[0][0]
        job = score(log)
        self.assertEqual((job.match_seconds, job.line, job.clock_source), (147.868, 6, f'{path}:3'))
        self.assertEqual(score(log.replace('abc}', 'abc elapsed=12.34}')).match_seconds, 12.34)
        for changed in (log.replace(sha, 'b' * 64), log.replace('recorded=1', 'recorded=0'),
                        log.replace('locale=en', 'locale=fa-IR'), log.replace('result=Victory', 'result=Abort'),
                        log.replace('S026-aria-rs-1-en-2,', 'no echoed id,')):
            with self.subTest(changed=changed):
                self.assertIsNone(score(changed).match_seconds)
        path.write_text(header + rows.replace('147.868', 'NaN'))
        self.assertIsNone(score(log).match_seconds)
        path.write_text(header + rows.splitlines()[0] + '\n')
        self.assertEqual(score(log.replace('S026-aria-rs-1-en-2,', 'no echoed id,')).match_seconds, 300)

    def test_warmup_requires_flag_unique_catalog_and_first_job(self):
        log = self.root / 'unity.log'
        snippet = """[SkirmishAriaQueue] job=1/2 catalog=S004 seed=1 locale=en result=Victory seconds=200
[SkirmishAriaQueue] job=2/2 catalog=S002 seed=2 locale=en result=Abort seconds=150
"""
        log.write_text(snippet)
        table = self.root / 'table.csv'
        header = ','.join(('mission_id', *runner.COLUMNS))
        untouched = 'S004' + ',old' * len(runner.COLUMNS)
        table.write_text(header + '\n' + untouched + '\nS002' + ',old' * len(runner.COLUMNS) + '\n')
        args = ['--rescan', str(log), '--table', str(table)]
        with mock.patch('sys.stdout', new_callable=io.StringIO) as out:
            self.assertEqual(runner.main(args), 0)
            self.assertEqual(out.getvalue().count('class=counted'), 2)
        with mock.patch('sys.stdout', new_callable=io.StringIO) as out:
            self.assertEqual(runner.main(args + ['--warmup-first', '--write']), 0)
            self.assertIn('class=warmup job=1/2 catalog=S004', out.getvalue())
            self.assertIn('class=counted job=2/2 catalog=S002', out.getvalue())
        self.assertEqual(table.read_text().splitlines()[1], untouched)
        self.assertIn('W0 L0 A1', table.read_text().splitlines()[2])
        log.write_text(snippet.replace('S004', 'S002'))
        with mock.patch('sys.stdout', new_callable=io.StringIO) as out:
            self.assertEqual(runner.main(args + ['--warmup-first']), 0)
            self.assertEqual(out.getvalue().count('class=counted'), 2)

    def test_rescan_all_orders_by_mtime_and_replaces_only_latest_mission_sets(self):
        date_dir = self.root / 'date'
        old = date_dir / 'z' / 'unity.log'
        latest = date_dir / 'a' / 'queue' / 'unity.log'
        probe = date_dir / 'resume' / 'unity.log'
        deep = date_dir / 'a' / 'b' / 'c' / 'unity.log'
        for path in (old, latest, probe, deep):
            path.parent.mkdir(parents=True)
        old.write_text("""[SkirmishTerminalFrozen] attempt=expanded-s002-1-abc elapsed=12.5
[SkirmishAriaQueue] job=1/2 catalog=S002 seed=1 locale=fa-IR result=Victory seconds=90
[SkirmishAriaQueue] job=2/2 catalog=S004 seed=1 locale=en result=Draw seconds=40
""")
        latest.write_text("""[SkirmishTerminalFrozen] result=Passed initial={attempt=expanded-s002-2-abc elapsed=11.75}
[SkirmishTerminalFrozen] result=Passed initial={attempt=expanded-s002-3-def elapsed=14.75}
[SkirmishAriaQueue] job=1/3 catalog=S002 seed=2 locale=en result=Victory seconds=50
[SkirmishAriaQueue] job=2/3 catalog=S002 seed=3 locale=en result=Victory seconds=75
[SkirmishAriaQueue] job=3/3 catalog=S002 seed=4 locale=en result=Abort seconds=30
""")
        probe.write_text('[SkirmishTerminalFrozen] attempt=expanded-s002-1-abc elapsed=1000\n')
        deep.write_text(old.read_text())
        for index, path in enumerate((old, latest, probe, deep), 1):
            os.utime(path, (index, index))
        table = self.root / 'table.csv'
        header = ','.join(('mission_id', 'deadline_s', *runner.COLUMNS))
        original = header + '\n' + ''.join(mission + ',100' + ',old' * len(runner.COLUMNS) + '\n'
                                             for mission in ('S002', 'S004'))
        table.write_text(original)
        args = ['--rescan-all', str(date_dir), '--table', str(table)]
        with mock.patch('sys.stdout', new_callable=io.StringIO) as out:
            self.assertEqual(runner.main(args), 0)
        lines = out.getvalue().splitlines()
        self.assertEqual(len(lines), 5)
        self.assertIn(f'log={old}:1', lines[0])
        self.assertIn(f'log={latest}:1', lines[2])
        self.assertIn('match_seconds=11.75 job_seconds=50.0 deadline_pct=11.75% unused=unproven', lines[2])
        self.assertEqual(table.read_text(), original)
        with mock.patch('sys.stdout', new_callable=io.StringIO):
            self.assertEqual(runner.main(args + ['--write']), 0)
        with table.open() as stream:
            updated, older = csv.DictReader(stream)
        self.assertEqual((updated['aria_en'], updated['aria_fa'], updated['aria_time_s'], updated['unity_result'],
                          updated['last_log']), ('W2 L0 A1', '', '13', 'abort', str(latest)))
        self.assertEqual((older['aria_en'], older['aria_time_s'], older['last_log']), ('W0 L1 A0', '', str(old)))

    def test_draw_and_no_victory(self):
        table = self.root / 'table.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n')
        jobs = [('S002', '1', 'en', 'Draw', 20.0)]
        runner.update_table(table, jobs, [('S002', '1', 'en')], self.root / 'unity.log')
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['aria_en'], row['aria_time_s'], row['unity_result']), ('W0 L1 A0', '', 'loss'))


class LongSessionTests(unittest.TestCase):
    PASSED = ('[SkirmishLongSessionRegression] catalog=S026 seed=104729 result=Passed allocatorErrors=0 exceptions=0 errors=2 '
              'resultScreen=1 returnToMenu=1 expiry=1 outcome=Defeat endReason=TimeLimit elapsed=1080.2 purchases=21 '
              'purchaseFailures=3 archetypes=4100 queryShapes=1720 queryBlocks=301/2048 abort=none firstError=none')

    def test_passed_marker(self):
        markers = runner.read_long_session(['noise', self.PASSED + '\n'])
        self.assertEqual(1, len(markers))
        self.assertTrue(markers[0]['passed'])
        self.assertEqual('1080.2', markers[0]['elapsed'])
        self.assertEqual(0, runner.long_session_exit(markers))

    def test_allocator_error_or_missing_return_fails(self):
        bad = self.PASSED.replace('allocatorErrors=0', 'allocatorErrors=3')
        self.assertFalse(runner.read_long_session([bad])[0]['passed'], 'a Passed claim is re-checked')
        bad = self.PASSED.replace('result=Passed', 'result=Failed').replace('returnToMenu=1', 'returnToMenu=0')
        self.assertEqual(1, runner.long_session_exit(runner.read_long_session([bad])))

    def test_inconclusive_missing_and_duplicate_fail(self):
        early = self.PASSED.replace('result=Passed', 'result=Inconclusive').replace('expiry=1', 'expiry=0')
        self.assertEqual(1, runner.long_session_exit(runner.read_long_session([early])))
        self.assertEqual(1, runner.long_session_exit([]))
        self.assertEqual(1, runner.long_session_exit(runner.read_long_session([self.PASSED, self.PASSED])))
        truncated = '[SkirmishLongSessionRegression] catalog=S026 result=Passed'
        self.assertEqual([], runner.read_long_session([truncated]))

    def test_long_session_rejects_reuse_and_multiple_jobs(self):
        with self.assertRaises(SystemExit):
            runner.main(['--long-session', '--reuse', '--jobs', 'S026:104729:en', '--log-dir', '/tmp/x-long'])
        with tempfile.TemporaryDirectory() as directory:
            code = runner.main(['--long-session', '--jobs', 'S026:104729:en,S026:104730:en',
                                '--log-dir', directory, '--dry-run'])
            self.assertEqual(1, code)


if __name__ == '__main__':
    unittest.main()
