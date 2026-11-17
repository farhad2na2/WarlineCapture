#!/usr/bin/env python3
"""Pins the ARIA queue request-file wiring (run_aria_queue.py --reuse -> Temp/aria-queue/request.json -> the Editor harness).
The Editor code cannot be compiled in the code lane, so the wiring that matters is asserted from the sources."""
import json
import re
import unittest

import check_draft as checker
import run_aria_queue as runner

ROOT = checker.ROOT
EDITOR = ROOT / 'Assets/Game/Scripts/Editor/Skirmish'
CONFIGS = ROOT / 'Assets/Game/Scripts/Configs/Skirmish'


def read(path):
    return path.read_text(encoding='utf-8')


class QueueRequestContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.request = read(CONFIGS / 'SkirmishAriaQueueRequest.cs')
        cls.partial = read(EDITOR / 'SkirmishS002AriaRunHarness.QueueRequest.cs')
        cls.queue = read(EDITOR / 'SkirmishS002AriaRunHarness.AriaQueue.cs')
        cls.wrapper = read(ROOT / 'Tools/CI/invoke_unity_macos.sh')

    def test_python_and_csharp_agree_on_the_path_and_the_json_fields(self):
        self.assertEqual('Temp/aria-queue/request.json', str(runner.REQUEST_RELATIVE_PATH))
        self.assertIn('RelativePath = "Temp/aria-queue/request.json"', self.request)
        import tempfile, pathlib
        with tempfile.TemporaryDirectory() as tmp:
            path, request_id = runner.write_reuse_request(pathlib.Path(tmp), pathlib.Path(tmp) / 'q.csv', True, True)
            written = json.loads(path.read_text())
        self.assertEqual({'version', 'requestId', 'project', 'queue', 'menuJourney', 'validationLibrary', 'createdUtc'}, set(written))
        for field, kind in (('version', 'int'), ('requestId', 'string'), ('project', 'string'), ('queue', 'string'),
                            ('menuJourney', 'bool'), ('validationLibrary', 'bool'), ('createdUtc', 'string')):
            self.assertRegex(self.request, r'public ' + kind + r' ' + field + r';', field)

    def test_the_environment_path_wins_and_the_file_is_the_fallback(self):
        resolve = self.partial[self.partial.index('private static string ResolveAriaQueuePath()'):]
        env_return = resolve.index('if (!string.IsNullOrWhiteSpace(environmentPath)) return environmentPath;')
        self.assertLess(env_return, resolve.index('SkirmishAriaQueueRequest.PathFor(root)'))
        self.assertIn('string path = ResolveAriaQueuePath();', self.queue)
        self.assertNotIn('string path = Environment.GetEnvironmentVariable("WARLINE_ARIA_QUEUE");', self.queue)

    def test_a_request_is_validated_consumed_and_never_replayed(self):
        resolve = self.partial[self.partial.index('private static string ResolveAriaQueuePath()'):]
        self.assertLess(resolve.index('SkirmishAriaQueueRequest.Validate(request, root, DateTime.UtcNow, File.Exists)'),
                        resolve.index('SkirmishAriaQueueRequest.ConsumedPath('))
        self.assertIn('MoveAside(requestPath, requestPath + ".refused");', resolve)
        self.assertIn('throw new InvalidDataException("ARIA queue request refused: " + error);', resolve)

    def test_flags_are_set_explicitly_and_cleared_when_the_queue_ends(self):
        self.assertIn('Environment.SetEnvironmentVariable("WARLINE_MENU_JOURNEY", request.menuJourney ? "1" : null);', self.partial)
        self.assertIn('Environment.SetEnvironmentVariable(SkirmishValidationLibraryPlan.EnvironmentVariable, request.validationLibrary ? "1" : null);', self.partial)
        complete = self.queue[self.queue.index('private static void CompleteAriaQueue()'):]
        self.assertIn('ClearAriaQueueRequestEnvironment();', complete)
        catch = self.queue[self.queue.index('Debug.LogError(AriaQueueLog + " result=Failed reason=startup error="'):]
        self.assertIn('ClearAriaQueueRequestEnvironment();', catch.split('private static List<string> ParseAriaQueue')[0])

    def test_a_reused_editor_is_never_closed_and_never_reloads_its_domain(self):
        startup_catch = self.queue[self.queue.index('Debug.LogError(AriaQueueLog + " result=Failed reason=startup error="'):]
        startup_catch = startup_catch.split('private static List<string> ParseAriaQueue')[0]
        self.assertIn('if (ExistingEditorValidation.IsRunning) MissionEditorValidationExit.Complete(false);', startup_catch)
        self.assertIn('else EditorApplication.Exit(2);', startup_catch)
        # The queue's final exit already routes a reused Editor to Complete, not Exit.
        self.assertIn('if (ExistingEditorValidation.IsRunning || Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") < 0)\n                MissionEditorValidationExit.Complete(exitCode == 0);', self.queue)
        self.assertIn('if (!ExistingEditorValidation.IsRunning) return false;', self.partial)
        self.assertIn('EnterPlayModeOptions.DisableDomainReload', self.partial)
        # Options are re-applied at every dispatch (each job's end restores them) and restored when the queue ends.
        dispatch = self.queue[self.queue.index('private static void DispatchAriaQueueJob()'):]
        self.assertIn('EnsureReusedQueueEditorOptions();', dispatch)
        self.assertIn('RestoreReusedQueueEditorOptions();', self.queue[self.queue.index('private static void CompleteAriaQueue()'):])
        self.assertIn('SuspendRefreshForReusedQueue();', self.queue)

    def test_the_wrapper_reuse_contract_matches_what_the_runner_passes(self):
        # --reuse requires a positive timeout and exactly "-executeMethod <method>"; it refuses any existing log artifact.
        self.assertIn('"$#" -ne 2 || "$1" != "-executeMethod"', self.wrapper)
        for suffix in runner.REUSE_EVIDENCE_SUFFIXES:
            self.assertIn('"$LOG_FILE' + suffix + '"', self.wrapper) if suffix else None
        command = runner.reuse_command(ROOT, ROOT / 'x.log', 99)
        self.assertEqual(['--reuse', '--timeout', '99', '--log', str(ROOT / 'x.log'), '--', '-executeMethod',
                          'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'], command[1:])


if __name__ == '__main__':
    unittest.main()
