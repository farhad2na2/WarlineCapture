from __future__ import annotations

import importlib.util
from pathlib import Path
import tempfile
import unittest

RUNNER_PATH = Path(__file__).with_name("run_s002_building_scalar_parity.py")
SPEC = importlib.util.spec_from_file_location("s002_building_scalar_parity_runner", RUNNER_PATH)
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)


class S002BuildingScalarParityRunnerTests(unittest.TestCase):
    def test_exact_wrapper_command_uses_repo_wrapper_and_editor_method(self):
        command = RUNNER.build_wrapper_command(Path("/tmp/scalar.log"), 321)
        self.assertEqual(command[:2], [RUNNER.RTK, "proxy"])
        self.assertEqual(command[2], str(RUNNER.WRAPPER))
        self.assertEqual(command[3:8], ["--timeout", "321", "--log", "/tmp/scalar.log", "--"])
        self.assertEqual(command[-3:], ["-quit", "-executeMethod", RUNNER.EXECUTE_METHOD])
        self.assertNotIn("-batchmode", command)

    def test_exact_marker_must_occur_once_as_a_complete_log_line(self):
        with tempfile.TemporaryDirectory() as temp:
            log = Path(temp) / "unity.log"
            log.write_text("prefix " + RUNNER.PASS_MARKER + "\n")
            self.assertEqual(RUNNER.exact_marker_count(log), 0)
            log.write_text(RUNNER.PASS_MARKER + "\n")
            self.assertEqual(RUNNER.exact_marker_count(log), 1)
            log.write_text(RUNNER.PASS_MARKER + "\n" + RUNNER.PASS_MARKER + "\n")
            self.assertEqual(RUNNER.exact_marker_count(log), 2)

    def test_success_requires_zero_exit_stable_sources_and_complete_assembly_snapshots(self):
        before = {"source.cs": "source-hash"}
        assemblies = {path: "sha" for path in RUNNER.ASSEMBLIES}
        RUNNER.validate_result(0, 1, before, dict(before), assemblies, dict(assemblies))

    def test_changed_source_or_incomplete_assemblies_reject(self):
        before = {"source.cs": "before"}
        after = {"source.cs": "after"}
        assemblies = {path: "sha" for path in RUNNER.ASSEMBLIES}
        with self.assertRaisesRegex(ValueError, "source changed"):
            RUNNER.validate_result(0, 1, before, after, assemblies, dict(assemblies))
        partial = dict(assemblies)
        partial.pop(RUNNER.ASSEMBLIES[-1])
        with self.assertRaisesRegex(ValueError, "All six compiled assemblies"):
            RUNNER.validate_result(0, 1, before, dict(before), partial, dict(assemblies))

    def test_wrapper_exit_and_marker_failures_reject(self):
        source = {"source.cs": "sha"}
        assemblies = {path: "sha" for path in RUNNER.ASSEMBLIES}
        with self.assertRaisesRegex(ValueError, "exit zero"):
            RUNNER.validate_result(1, 1, source, dict(source), assemblies, dict(assemblies))
        with self.assertRaisesRegex(ValueError, "exactly once"):
            RUNNER.validate_result(0, 0, source, dict(source), assemblies, dict(assemblies))

    def test_recompiled_assembly_during_run_rejects_epoch(self):
        source = {"source.cs": "sha"}
        before = {path: "before" for path in RUNNER.ASSEMBLIES}
        after = dict(before)
        after[RUNNER.ASSEMBLIES[0]] = "recompiled"
        with self.assertRaisesRegex(ValueError, "assembly bytes changed"):
            RUNNER.validate_result(0, 1, source, dict(source), before, after)

    def test_source_and_assembly_pin_sets_cover_all_owner_assemblies(self):
        snapshot = RUNNER.snapshot(RUNNER.ROOT, RUNNER.SOURCE_PATHS)
        self.assertEqual(set(snapshot), set(RUNNER.SOURCE_PATHS))
        self.assertIn("Assets/Game/Scripts/Composition/BuildingDefinitionAuthoringMetadataPrefabSystemHelper.cs",
                      RUNNER.SOURCE_PATHS)
        self.assertIn("Assets/Game/Scripts/Components/BuildingDefinitionAuthoringScalarProjection.cs",
                      RUNNER.SOURCE_PATHS)
        self.assertEqual({Path(path).name for path in RUNNER.ASSEMBLIES}, {
            "Game.Components.dll", "Game.Authoring.dll", "Game.Configs.dll",
            "Game.Composition.dll", "Game.Runtime.dll", "Game.Editor.dll"})


if __name__ == "__main__":
    unittest.main()
