"""Synthetic offline controls; no native mission evidence."""
import copy
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.dont_write_bytecode = True
import skirmish_preflight_doctor as doctor
import skirmish_production_queue as queue


class PreflightDoctorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="skirmish-doctor-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        for name, value in {
            "ProjectSettings/ProjectVersion.txt": "m_EditorVersion: 6000.5.2f1",
            "Packages/manifest.json": "{}", "Packages/packages-lock.json": "{}",
            "Tools/CI/ResolveUnityEditor.ps1": "# fixture",
            "Tools/CI/InvokeUnity.ps1": "# fixture",
            "Tools/CI/InvokeUnityExecuteMethodValidation.ps1": "# fixture",
            "Tools/CI/invoke_unity_macos.sh": "# fixture",
            "source.txt": "original",
            "packet.md": "## S026\nRegular seeds `104755`, `130389`, `155947` in both EN and FA at Standard.\n",
        }.items():
            p = self.root / name
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(value, encoding="utf-8")
        self.job = dict(schema_version=1, job_id="S026-EN-104755", catalog_id="S026",
                        definition_id="skirmish.s026", definition_version=1,
                        candidate_sha256="a"*64, build_sha256="b"*64,
                        setup_sha256="c"*64, content_sha256="d"*64,
                        source_manifest=[dict(path="source.txt", sha256=queue.digest(self.root/"source.txt"))],
                        packet_path="packet.md", packet_sha256=queue.digest(self.root/"packet.md"),
                        seed=104755, locale="EN", size="Standard", difficulty="Regular",
                        control_mode="aria", required_stages=sorted(queue.STAGES), timeout_seconds=1800)

    def write_json(self, name, value):
        p = self.root / name
        p.write_text(json.dumps(value), encoding="utf-8")
        return p

    def manifest(self, files):
        return self.write_json("launch-files.json", dict(schema_version=1, files=files))

    def codes(self, report):
        return {row["code"] for row in report["findings"]}

    def snapshot(self):
        return {str(p.relative_to(self.root)): p.read_bytes()
                for p in self.root.rglob("*") if p.is_file()}

    def test_clean_project_and_job_remain_unqualified_and_read_only(self):
        job = self.write_json("job.json", self.job)
        before = self.snapshot()
        with patch.object(queue, "preflight", wraps=queue.preflight) as original:
            result = doctor.inspect(self.root, jobs=[job])
        original.assert_called_once()
        self.assertEqual(result["errors"], 0)
        self.assertEqual(result["jobs"][0]["preflight"], "passed")
        self.assertFalse(result["player_ready"])
        self.assertFalse(result["launch_authorized"])
        self.assertEqual(result["native_qualification"], "pending")
        self.assertEqual(before, self.snapshot())

    def test_all_independent_launch_failures_reported_without_executing_python(self):
        (self.root/"ProjectSettings/ProjectVersion.txt").unlink()
        (self.root/"broken.py").write_text("def broken(:\n", encoding="utf-8")
        (self.root/"never_execute.py").write_text("raise RuntimeError('must not execute')", encoding="utf-8")
        (self.root/"duplicate.json").write_text('{"x":1,"x":2}', encoding="utf-8")
        manifest = self.manifest([
            dict(path="missing.ps1"), dict(path="broken.py", kind="python"),
            dict(path="never_execute.py", kind="python"),
            dict(path="duplicate.json", kind="json"),
            dict(path="source.txt", sha256="0"*64)])
        before = self.snapshot()
        report = doctor.inspect(self.root, manifest)
        self.assertEqual(report["errors"], 5)
        self.assertIn("hash_mismatch", self.codes(report))
        self.assertTrue(any("Duplicate JSON key" in row["message"] for row in report["findings"]))
        self.assertEqual(before, self.snapshot())

    def test_newline_hash_hint_never_accepts_changed_bytes(self):
        (self.root/"copied.py").write_bytes(b"value = 1\r\n")
        expected = hashlib.sha256(b"value = 1\n").hexdigest()
        report = doctor.inspect(self.root, self.manifest([dict(path="copied.py", sha256=expected, kind="python")]))
        self.assertEqual(report["errors"], 1)
        self.assertIn("newline-normalized", report["findings"][0]["message"])
        self.assertEqual((self.root/"copied.py").read_bytes(), b"value = 1\r\n")

    def test_lfs_pointer_rejected_even_with_matching_hash(self):
        p = self.root/"asset.bin"
        p.write_bytes(doctor.LFS_HEADER + b"\noid sha256:" + b"a"*64 + b"\nsize 50\n")
        report = doctor.inspect(self.root, self.manifest([dict(path="asset.bin", sha256=queue.digest(p))]))
        self.assertEqual(self.codes(report), {"lfs_pointer"})

    def test_malformed_job_does_not_hide_later_jobs_or_file_problems(self):
        broken = self.root/"broken-job.json"
        broken.write_text('{"seed":1,"seed":2}', encoding="utf-8")
        valid = self.write_json("valid-job.json", self.job)
        changed = copy.deepcopy(self.job)
        changed["source_manifest"] += [dict(path="missing-a", sha256="0"*64), dict(path="missing-b", sha256="0"*64)]
        invalid = self.write_json("invalid-job.json", changed)
        report = doctor.inspect(self.root, jobs=[broken, valid, invalid])
        self.assertEqual(report["jobs_checked"], 3)
        self.assertEqual(report["jobs"][1]["preflight"], "passed")
        self.assertEqual(report["errors"], 4)

    def test_group_shared_failures_with_affected_jobs(self):
        a = self.write_json("a.json", self.job)
        b = self.write_json("b.json", {**self.job, "job_id": "S026-FA-104755", "locale": "FA"})
        (self.root/"source.txt").write_text("changed", encoding="utf-8")
        report = doctor.inspect(self.root, jobs=[a, b])
        errors = [row for row in report["findings"] if row["code"] == "hash_mismatch"]
        self.assertEqual(len(errors), 1)
        self.assertEqual(set(errors[0]["affected_inputs"]), {str(a), str(b)})

    def test_each_call_reobserves_bytes_no_stale_cache(self):
        job = self.write_json("job.json", self.job)
        self.assertEqual(doctor.inspect(self.root, jobs=[job])["errors"], 0)
        (self.root/"source.txt").write_text("changed", encoding="utf-8")
        self.assertGreater(doctor.inspect(self.root, jobs=[job])["errors"], 0)

    def test_manifest_paths_escape_and_duplicate_resolved_paths_rejected(self):
        report = doctor.inspect(self.root, self.manifest([
            dict(path="../escape"), dict(path="source.txt"), dict(path="./source.txt")]))
        self.assertEqual(report["errors"], 2)
        self.assertEqual(self.codes(report), {"manifest_entry"})

    def test_real_symlink_escape_rejected(self):
        with tempfile.TemporaryDirectory(prefix="skirmish-doctor-outside-") as directory:
            outside = Path(directory)/"outside.txt"
            outside.write_text("outside", encoding="utf-8")
            try:
                (self.root/"link.txt").symlink_to(outside)
            except OSError as exc:
                self.skipTest("Host does not permit symlink creation: " + str(exc))
            report = doctor.inspect(self.root, self.manifest([dict(path="link.txt")]))
            self.assertEqual(self.codes(report), {"manifest_entry"})

    def test_windows_projection_warning_does_not_grant_launch(self):
        report = doctor.inspect(self.root, windows_root="D:/" + "long"*65)
        self.assertEqual(report["errors"], 0)
        self.assertGreater(report["warnings"], 0)
        self.assertFalse(report["launch_authorized"])

    def test_bad_sampling_authority_cannot_fall_back_to_unprotected_job_check(self):
        job = self.write_json("job.json", self.job)
        with patch.object(queue, "preflight", wraps=queue.preflight) as original:
            report = doctor.inspect(self.root, jobs=[job], sampling_plan=self.root/"missing.json", sampling_sha="a"*64)
        original.assert_not_called()
        self.assertEqual(report["errors"], 2)
        self.assertEqual(report["jobs"][0]["preflight"], "failed")

    def test_strict_json_nonfinite_rejected(self):
        (self.root/"Packages/manifest.json").write_text('{"value":NaN}', encoding="utf-8")
        self.assertEqual(doctor.inspect(self.root)["errors"], 1)

    def test_json_overflow_and_excessive_nesting_reported(self):
        for content in ('{"value":1e999}', '['*2000 + '0' + ']'*2000):
            with self.subTest(content=content[:30]):
                (self.root/"Packages/manifest.json").write_text(content, encoding="utf-8")
                self.assertEqual(doctor.inspect(self.root)["errors"], 1)

    def test_manifest_infers_python_and_json_checks(self):
        (self.root/"bad.py").write_text("def broken(:", encoding="utf-8")
        (self.root/"bad.json").write_text("{", encoding="utf-8")
        report = doctor.inspect(self.root, self.manifest([dict(path="bad.py"), dict(path="bad.json")]))
        self.assertEqual(report["errors"], 2)

    def test_file_mutation_during_inspection_reported(self):
        runner = doctor.Doctor(self.root)
        with patch.object(doctor, "stamp", side_effect=[(1, 2, 3, 4, 5), (1, 2, 3, 4, 6)]):
            runner.check_file(self.root/"source.txt", "fixture")
        self.assertEqual(self.codes(runner.report(0)), {"changed_during_read"})

    def test_oversized_json_is_reported_and_later_files_still_checked(self):
        (self.root/"big.json").write_bytes(b" "*(doctor.MAX_PARSE_BYTES+1))
        report = doctor.inspect(self.root, self.manifest([
            dict(path="big.json", kind="json"), dict(path="missing")]))
        self.assertEqual(report["errors"], 2)

    def test_cli_machine_report_and_exit_status(self):
        command = [sys.executable, "-B", str(Path(doctor.__file__).resolve()), "--project", str(self.root), "--json"]
        before = self.snapshot()
        passed = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(passed.returncode, 0, passed.stderr)
        self.assertEqual(json.loads(passed.stdout)["result"], "diagnostics_clear")
        self.assertEqual(before, self.snapshot())
        (self.root/"ProjectSettings/ProjectVersion.txt").unlink()
        failed = subprocess.run(command, capture_output=True, text=True)
        self.assertEqual(failed.returncode, 1, failed.stderr)
        self.assertEqual(json.loads(failed.stdout)["errors"], 1)

    def test_macos_checks_correct_wrapper(self):
        (self.root/"Tools/CI/InvokeUnity.ps1").unlink()
        self.assertEqual(doctor.inspect(self.root, platform="macos")["errors"], 0)


if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(PreflightDoctorTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    print(f"[SkirmishPreflightDoctorTests] result={'Passed' if result.wasSuccessful() else 'Failed'} "
          f"tests={result.testsRun} skipped={len(result.skipped)} native_runs=0")
    raise SystemExit(0 if result.wasSuccessful() else 1)
