import importlib.util
import hashlib
import json
import os
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "Recipes" / "run_s003_focused_template_source_host_controls.py"
SPEC = importlib.util.spec_from_file_location("s003_focused_source_host_runner", MODULE)
RUNNER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(RUNNER)
REPO = Path(__file__).resolve().parents[4]
FRESH_SOURCE_BUILD = REPO / "Logs/SkirmishProduction/s003-current-qualified-source-build-20261103-r2"
WORKSPACE_INPUTS = (
    REPO / RUNNER.RAW_REL, REPO / RUNNER.STRICT_RECEIPT_REL, REPO / RUNNER.EDITOR_LOG_REL,
    REPO / RUNNER.KEYS_REL, REPO / RUNNER.KEY_RECEIPT_REL,
    FRESH_SOURCE_BUILD / "build.json", FRESH_SOURCE_BUILD / "compile.rsp",
    FRESH_SOURCE_BUILD / "SourceSimulation.exe",
)
WORKSPACE_INTEGRATION_AVAILABLE = all(path.is_file() for path in WORKSPACE_INPUTS)
WORKSPACE_SKIP_REASON = "Workspace integration requires exact current S003 capture/key receipts and a fresh source build"
if WORKSPACE_INTEGRATION_AVAILABLE:
    try:
        RUNNER.validate_source_build(REPO, FRESH_SOURCE_BUILD)
    except (OSError, ValueError) as error:
        WORKSPACE_INTEGRATION_AVAILABLE = False
        WORKSPACE_SKIP_REASON = "Optional workspace S003 source build is stale: " + str(error)

class S003FocusedSourceHostRunnerTests(unittest.TestCase):
    @unittest.skipUnless(WORKSPACE_INTEGRATION_AVAILABLE, WORKSPACE_SKIP_REASON)
    def test_current_capture_and_explicit_fresh_build_validate_together(self):
        evidence = RUNNER.validate_evidence(REPO, FRESH_SOURCE_BUILD)
        self.assertEqual(16720, evidence["raw_pin_count"])
        self.assertGreater(evidence["build_pin_count"], 0)
        self.assertEqual(FRESH_SOURCE_BUILD.resolve(), evidence["build_dir"])

    def _build_fixture(self, temp):
        root = Path(temp).resolve()
        build_dir = root / "fresh-build"
        build_dir.mkdir()
        runtime = root / "runtime"
        runtime.mkdir()
        importer = root / RUNNER.IMPORTER_RELS[1]
        importer.parent.mkdir(parents=True)
        importer.write_text("// fixture current importer", encoding="utf-8")
        response = build_dir / "compile.rsp"
        response.write_text('"' + str(importer.resolve()) + '"\n', encoding="utf-8")
        executable = build_dir / "SourceSimulation.exe"
        executable.write_bytes(b"isolated source-host fixture")
        pins = {str(importer.resolve()): hashlib.sha256(importer.read_bytes()).hexdigest()}
        build = {"kind": "SkirmishSourceSimulationBuild", "status": "Passed", "compile_exit": 0,
                 "native_or_player_launched": False, "mission": "S003",
                 "runtime_selection": {"contents": str(runtime)}, "source_and_reference_pins": pins,
                 "executable": {"sha256": hashlib.sha256(executable.read_bytes()).hexdigest()},
                 "compiler_response": {"sha256": hashlib.sha256(response.read_bytes()).hexdigest()}}
        (build_dir / "build.json").write_text(json.dumps(build), encoding="utf-8")
        return root, build_dir

    def test_explicit_fresh_build_path_and_importer_pin_are_bound(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, build_dir = self._build_fixture(temporary)
            validated = RUNNER.validate_source_build(root, build_dir)
            self.assertEqual(build_dir.resolve(), validated["build_dir"])
            self.assertEqual("305_GeneratedReferenceTemplateImporter.s003.cs", validated["importer_path"].name)

    def test_rejects_stale_current_importer_pin(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, build_dir = self._build_fixture(temporary)
            build_path = build_dir / "build.json"
            build = json.loads(build_path.read_text())
            importer = next(iter(build["source_and_reference_pins"]))
            build["source_and_reference_pins"][importer] = "0" * 64
            build_path.write_text(json.dumps(build), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "pin mismatch"):
                RUNNER.validate_source_build(root, build_dir)

    def test_rejects_build_response_with_duplicate_importer(self):
        with tempfile.TemporaryDirectory() as temporary:
            root, build_dir = self._build_fixture(temporary)
            response = build_dir / "compile.rsp"
            importer = str(root / RUNNER.IMPORTER_RELS[1])
            response.write_text(importer + "\n" + importer + "\n", encoding="utf-8")
            build_path = build_dir / "build.json"
            build = json.loads(build_path.read_text())
            build["compiler_response"]["sha256"] = hashlib.sha256(response.read_bytes()).hexdigest()
            build_path.write_text(json.dumps(build), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "exactly once"):
                RUNNER.validate_source_build(root, build_dir)

    def test_pin_map_rejects_stale_bytes(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            source = root / "source.txt"
            source.write_text("current", encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "pin mismatch"):
                RUNNER.verify_pin_map({str(source): "0" * 64}, "fixture", root)

    def test_pin_map_rejects_repository_escape(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary).resolve()
            outside_fd, outside_name = tempfile.mkstemp()
            os.close(outside_fd)
            outside = Path(outside_name).resolve()
            try:
                outside.write_text("external", encoding="utf-8")
                import hashlib
                pin = hashlib.sha256(outside.read_bytes()).hexdigest()
                with self.assertRaisesRegex(ValueError, "escapes"):
                    RUNNER.verify_pin_map({str(outside): pin}, "fixture", root)
            finally:
                outside.unlink(missing_ok=True)

    def test_duplicate_json_properties_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "duplicate.json"
            path.write_text('{"a":1,"a":2}', encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Duplicate"):
                RUNNER.load_json(path)

    def test_output_must_be_fresh(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "existing"
            output.mkdir()
            with self.assertRaisesRegex(ValueError, "Fresh T output"):
                RUNNER.run(output, Path(temporary), Path(temporary) / "missing-build")


if __name__ == "__main__":
    unittest.main()
