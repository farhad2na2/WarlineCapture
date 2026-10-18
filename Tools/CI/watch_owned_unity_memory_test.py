"""Synthetic cancellation tests. Never query/stop a real process or load kernel APIs."""
import copy
import json
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[2]))
from Tools.CI import watch_owned_unity_memory as watcher

class OwnedCancellationTests(unittest.TestCase):
    def setUp(self):
        self.argv_patch = patch.object(watcher, 'argv', side_effect=json.loads)
        self.argv_patch.start()
        self.addCleanup(self.argv_patch.stop)
        root = str(watcher.ROOT)
        self.row = dict(ProcessId=10, ParentProcessId=1, Name='Unity.exe',
                        ExecutablePath=str(watcher.ROOT / 'test-Unity.exe'),
                        CreationDate='/Date(1000)/', CommandLine=json.dumps([
                            '-projectPath', root, '-logFile', root + '/Logs/owned.log',
                            '-executeMethod', 'Owned.Run']))
        self.owner = dict(process=copy.deepcopy(self.row), editor=self.row['ExecutablePath'],
                          project=root, log=root + '/Logs/owned.log', executeMethod='Owned.Run')

    def child(self, name, parent=10):
        return dict(ProcessId=11, ParentProcessId=parent, Name=name,
                    ExecutablePath='observed-child', CommandLine='observed-child',
                    CreationDate='/Date(2000)/')

    def execute(self, samples, *, authorized=True, override_descendants=None, exit_code=0):
        sequence = iter(samples)
        commands = []
        api = dict(vars(watcher), census=lambda: next(sequence), authorization=lambda: authorized)
        if override_descendants:
            api['descendants'] = override_descendants
        def run(command, **kwargs):
            commands.append(command)
            return SimpleNamespace(returncode=exit_code, stdout='synthetic', stderr='')
        test_root = watcher.ROOT / 'Logs/SkirmishProduction/WatcherSyntheticControls'
        test_root.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=test_root) as directory:
            output = Path(directory) / 'events.jsonl'
            try:
                result = watcher.stop_owned(api, self.owner, output, 'test floor',
                                           run=run, sleep=lambda _: None)
            except Exception as error:
                result = error
            events = [json.loads(line) for line in output.read_text().splitlines()] if output.exists() else []
        return result, commands, events

    def test_each_actual_protected_descendant_selects_root_only(self):
        for name in ('UnityPackageManager.exe', 'Unity.Licensing.Client.exe', 'Unity Hub.exe',
                     'UnityHub.exe', 'ChatGPT.exe', 'Codex.exe', 'explorer.exe'):
            rows = [self.row, self.child(name)]
            with self.subTest(name=name):
                with self.assertRaises(ValueError):
                    watcher.descendants(rows, self.row)
                result, commands, events = self.execute([rows, rows, [], []])
                self.assertEqual(result, 3)
                self.assertEqual(commands, [['rtk', 'proxy', 'taskkill.exe', '/PID', '10', '/F']])
                self.assertEqual(len(events[0]['observedTree']), 2)
                self.assertFalse(events[-1]['descendantShutdownClaim'])

    def test_unrelated_protected_process_not_in_tree(self):
        rows = [self.row, self.child('UnityPackageManager.exe', parent=999)]
        self.assertEqual(watcher.descendants(rows, self.row), [self.row])
        result, commands, _ = self.execute([rows, rows, [], []])
        self.assertEqual(result, 3)
        self.assertIn('/T', commands[0])

    def test_protected_child_appearing_between_checks(self):
        result, commands, _ = self.execute([[self.row], [self.row, self.child('UnityPackageManager.exe')], [], []])
        self.assertEqual(result, 3)
        self.assertNotIn('/T', commands[0])

    def test_initial_absence(self):
        result, commands, _ = self.execute([[]])
        self.assertEqual((result, commands), (0, []))

    def test_root_disappears_before_second_check(self):
        result, commands, _ = self.execute([[self.row], []])
        self.assertEqual((result, commands), (0, []))

    def test_single_absence_then_present_requires_two_new_absences(self):
        result, commands, events = self.execute([[self.row], [self.row], [], [self.row], [], []])
        self.assertEqual(result, 3)
        self.assertEqual(len(commands), 1)
        self.assertEqual(events[-1]['kind'], 'OwnedRootShutdownVerified')

    def test_transient_null_after_command_is_unknown(self):
        null = dict(self.row, ExecutablePath=None)
        result, commands, events = self.execute([[self.row], [self.row], [null], [], []])
        self.assertEqual(result, 3)
        self.assertEqual(len(commands), 1)
        self.assertEqual(events[-1]['kind'], 'OwnedRootShutdownVerified')

    def test_persistent_null_after_command_is_pending(self):
        null = dict(self.row, ExecutablePath=None)
        result, commands, events = self.execute([[self.row], [self.row]] + [[null]] * 20)
        self.assertIsInstance(result, RuntimeError)
        self.assertEqual(len(commands), 1)
        self.assertEqual(events[-1]['kind'], 'OwnedRootShutdownPending')

    def test_null_before_command_and_reused_pid_refuse(self):
        for field, value in (('ExecutablePath', None), ('CommandLine', None),
                             ('CreationDate', '/Date(3000)/')):
            with self.subTest(field=field):
                result, commands, _ = self.execute([[self.row], [dict(self.row, **{field: value})]])
                self.assertIsInstance(result, ValueError)
                self.assertEqual(commands, [])

    def test_foreign_project_log_method_and_executable_refuse(self):
        for key in ('project', 'log', 'executeMethod', 'editor'):
            with self.subTest(key=key), self.assertRaises(ValueError):
                watcher.verify([self.row], dict(self.owner, **{key: 'foreign'}))

    def test_revoked_authorization_never_kills(self):
        result, commands, _ = self.execute([[self.row], [self.row]], authorized=False)
        self.assertIsInstance(result, ValueError)
        self.assertEqual(commands, [])

    def test_malformed_child_and_refused_tree_choose_root_only(self):
        rows = [self.row, dict(self.child('helper.exe'), CreationDate=None)]
        result, commands, _ = self.execute([rows, rows, [], []])
        self.assertEqual(result, 3)
        self.assertNotIn('/T', commands[0])
        for error in (ValueError, TypeError, KeyError, OSError, AttributeError):
            def refused(*args, error=error):
                raise error('synthetic child observation refusal')
            result, commands, _ = self.execute([[self.row], [self.row], [], []], override_descendants=refused)
            self.assertEqual(result, 3)
            self.assertNotIn('/T', commands[0])

    def test_nonzero_taskkill_with_verified_absence_still_failed(self):
        result, _, events = self.execute([[self.row], [self.row], [], []], exit_code=128)
        self.assertEqual(result, 3)
        self.assertEqual(events[-1]['commandExit'], 128)

if __name__ == '__main__':
    unittest.main()
