"""Offline negative controls; synthetic fixtures are never native mission evidence."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import re
from concurrent.futures import ThreadPoolExecutor

import skirmish_production_queue as q


class ProductionQueueTests(unittest.TestCase):
    def setUp(self):
        # Keep fixtures under the writable checkout in restricted Windows sessions.
        fixture_root = Path(__file__).resolve().parents[2] / "Temp" / "SkirmishProductionOfflineTests"
        fixture_root.mkdir(parents=True, exist_ok=True)
        self.temp = tempfile.TemporaryDirectory(prefix="case-", dir=fixture_root)
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / "source").write_text("unchanged", encoding="utf-8")
        (self.root / "packet.md").write_text("## S026\nRegular seeds `104755`, `130389`, `155947` in both EN and FA at Standard. Recruit `196639`, Veteran `262173`, Commander `327699`. War seed `393267` and Large War seed `458905`.\n## S027\n", encoding="utf-8")
        self.job = dict(schema_version=1, job_id="S026-EN-seed104755", catalog_id="S026", definition_id="skirmish.s026", definition_version=1, candidate_sha256="a" * 64, build_sha256="b" * 64, setup_sha256="c" * 64, content_sha256="d" * 64, source_manifest=[{"path": "source", "sha256": q.digest(self.root / "source")}], packet_path="packet.md", packet_sha256=q.digest(self.root / "packet.md"), seed=104755, locale="EN", size="Standard", difficulty="Regular", control_mode="aria", required_stages=sorted(q.STAGES), timeout_seconds=1800)
        self.directory = self.root / "queue" / self.job["job_id"]

    def start(self):
        q.enqueue(self.root / "queue", self.job, self.root)
        return q.claim(self.directory, "worker1", self.root)

    def completion(self):
        record = self.start()
        artifacts = Path(record["artifact_directory"])
        common = {"attempt_id": record["attempt_id"], "job_sha256": q.fingerprint(self.job), "catalog_id": self.job["catalog_id"]}
        values = {
            "wrapper_log": "[SyntheticCanary] result=Passed\n",
            "terminal": {**common, "outcome": "Victory", "rendered": True},
            "return": {**common, "returned_to_menu": True, "profile_restored": True, "session_clean": True},
            "journey": {**common, **{k: self.job[k] for k in ("seed", "locale", "size", "difficulty", "control_mode")}, "normal_menu_launch": True, "read_only_observers": True, "paid_resources": True, "forced_outcome": False},
        }
        rows = []
        for i, phase in enumerate(("Menu", "Runtime", "Terminal"), 1):
            for kind in ("Dispatched", "Began", "Ended"):
                rows.append(dict(device="synthetic", generation=1, dispatched=i, kind=kind, phase=phase, completed=i - (kind != "Ended"), commandAuditActive=True))
        values["contacts"] = "\n".join(json.dumps(row) for row in rows)
        references = {}
        for kind, value in values.items():
            path = artifacts / (kind + ".json")
            path.write_text(value if isinstance(value, str) else json.dumps(value), encoding="utf-8")
            references[kind] = {"path": path.name, "sha256": q.digest(path)}
        evidence = {"schema_version": 1, "job_sha256": q.fingerprint(self.job), "attempt_id": record["attempt_id"], "worker": record["worker"], "source_start": self.job["source_manifest"], "source_end": self.job["source_manifest"], **{k: self.job[k] for k in ("candidate_sha256", "build_sha256", "setup_sha256", "content_sha256")}, "stages": {s: "passed" for s in q.STAGES}, "outcome": "Victory", "simulation_speed": 1, "human_interventions": 0, "artifacts": references, "wrapper": {"attempt_id": record["attempt_id"], "exit_code": 0, "timed_out": False, "required_pass_marker": "[SyntheticCanary] result=Passed", "execute_method": "Synthetic.Canary"}}
        return record, evidence, artifacts

    def test_valid_job_is_only_preflight(self):
        self.assertEqual(q.preflight(self.job, self.root)["native_qualification"], "pending")

    def test_strict_schema_negative_controls(self):
        for key, value in (("seed", True), ("seed", "104755"), ("seed", 1), ("definition_id", "skirmish.s001"), ("catalog_id", "S121"), ("timeout_seconds", 0), ("locale", "en"), ("required_stages", ["compile"]), ("schema_version", True)):
            with self.subTest(key=key, value=value):
                job = {**self.job, key: value}
                with self.assertRaises(q.ContractError):
                    q.preflight(job, self.root)
        with self.assertRaises(q.ContractError):
            q.preflight({**self.job, "playable": True}, self.root)

    def test_packet_config_seed_selection(self):
        for size, difficulty, seed in (("War", "Regular", 393267), ("LargeWar", "Regular", 458905), ("Standard", "Veteran", 262173)):
            q.preflight({**self.job, "size": size, "difficulty": difficulty, "seed": seed}, self.root)
        with self.assertRaises(q.ContractError):
            q.preflight({**self.job, "difficulty": "Veteran", "locale": "FA", "seed": 262173}, self.root)

    def test_source_or_packet_mutation_fails(self):
        for name in ("source", "packet.md"):
            original = (self.root / name).read_bytes()
            (self.root / name).write_bytes(b"changed")
            with self.assertRaises(q.ContractError):
                q.preflight(self.job, self.root)
            (self.root / name).write_bytes(original)

    def test_paths_cannot_escape_or_import_artifacts(self):
        for path in ("../outside", "C:/outside", "/outside", "a\\b"):
            with self.assertRaises(q.ContractError):
                q.relative_file(self.root, path)
        with self.assertRaises(q.ContractError):
            q.enqueue(self.root / "Assets" / "queue", self.job, self.root)

    def test_claim_exclusive_under_race(self):
        q.enqueue(self.root / "queue", self.job, self.root)
        def contender(_):
            try:
                return q.claim(self.directory, "worker", self.root)
            except (FileExistsError, q.ContractError):
                return None
        with ThreadPoolExecutor(max_workers=8) as pool:
            records = list(pool.map(contender, range(8)))
        self.assertEqual(sum(r is not None for r in records), 1)

    def test_invalid_worker_leaves_no_empty_attempt(self):
        q.enqueue(self.root / "queue", self.job, self.root)
        for worker in (None, "../bad", "", "a" * 81):
            with self.subTest(worker=worker), self.assertRaises(q.ContractError):
                q.claim(self.directory, worker, self.root)
        self.assertEqual(list(self.directory.glob("attempt-*")), [])
        self.assertFalse((self.directory / ".metadata-lock").exists())

    def test_interruption_preserves_attempt_and_ownership(self):
        first = self.start()
        with self.assertRaises(q.ContractError):
            q.finish(self.directory, "wrong", "interrupted", "infrastructure", "host-interrupted")
        q.finish(self.directory, first["token"], "interrupted", "infrastructure", "host-interrupted")
        second = q.claim(self.directory, "worker2", self.root)
        self.assertNotEqual(first["attempt_id"], second["attempt_id"])
        self.assertTrue((Path(first["artifact_directory"]) / "result.json").exists())

    def test_two_failures_stop_repeating_matches(self):
        first = self.start()
        q.finish(self.directory, first["token"], "failed", "driver", "response-shape")
        second = q.claim(self.directory, "worker", self.root)
        q.finish(self.directory, second["token"], "failed", "driver", "response-shape")
        with self.assertRaisesRegex(q.ContractError, "two identical"):
            q.claim(self.directory, "worker", self.root)

    def test_cannot_claim_native_success(self):
        record = self.start()
        with self.assertRaises(q.ContractError):
            q.finish(self.directory, record["token"], "qualified")

    def test_mutated_job_prevents_finish(self):
        record = self.start()
        path = self.directory / "job.json"
        path.write_text(json.dumps({**self.job, "locale": "FA"}), encoding="utf-8")
        with self.assertRaises(q.ContractError):
            q.finish(self.directory, record["token"], "failed", "schema", "changed")

    def test_crashed_finish_reuses_original_result(self):
        record = self.start()
        result = {**record, "finished_utc": "original", "status": "interrupted", "failure_category": "infrastructure", "failure_signature": "host", "native_qualification": "pending"}
        q.write_new(Path(record["artifact_directory"]) / "result.json", result)
        self.assertEqual(q.finish(self.directory, record["token"], "interrupted", "infrastructure", "host"), result)
        self.assertFalse((self.directory / "claim.json").exists())

    def test_stale_claim_and_transaction_lock_never_reclaimed_by_age(self):
        record = self.start()
        with self.assertRaises(q.ContractError):
            q.claim(self.directory, "worker2", self.root)
        lock = self.directory / ".metadata-lock"
        lock.mkdir()
        with self.assertRaises(FileExistsError):
            q.finish(self.directory, record["token"], "interrupted", "infrastructure", "host")
        self.assertTrue(lock.exists())
        self.assertTrue((self.directory / "claim.json").exists())

    def test_complete_synthetic_contract_retains_canary_gate(self):
        record, evidence, artifacts = self.completion()
        result = q.validate_completion(self.job, record, evidence, artifacts, self.root)
        self.assertEqual(result["native_qualification"], "pending_adapter_canary")

    def test_completion_negative_controls(self):
        record, evidence, artifacts = self.completion()
        changes = [("outcome", None), ("source_end", []), ("stages", {"compile": "passed"}), ("worker", "other"), ("candidate_sha256", "f" * 64), ("simulation_speed", 2), ("human_interventions", 1)]
        for field, value in changes:
            with self.subTest(field=field):
                with self.assertRaises(q.ContractError):
                    q.validate_completion(self.job, record, {**evidence, field: value}, artifacts, self.root)
        for kind in evidence["artifacts"]:
            bad = copy.deepcopy(evidence)
            del bad["artifacts"][kind]
            with self.subTest(missing=kind), self.assertRaises(q.ContractError):
                q.validate_completion(self.job, record, bad, artifacts, self.root)

    def test_wrapper_negative_controls(self):
        record, evidence, artifacts = self.completion()
        for field, value in (("exit_code", 1), ("exit_code", False), ("timed_out", True), ("attempt_id", "old"), ("required_pass_marker", "missing")):
            bad = copy.deepcopy(evidence)
            bad["wrapper"][field] = value
            with self.subTest(field=field), self.assertRaises(q.ContractError):
                q.validate_completion(self.job, record, bad, artifacts, self.root)

    def test_artifact_bytes_and_receipts_fail_closed(self):
        record, evidence, artifacts = self.completion()
        for kind, change in (("terminal", {"rendered": False}), ("return", {"profile_restored": False}), ("journey", {"seed": "104755"}), ("journey", {"forced_outcome": True})):
            path = artifacts / evidence["artifacts"][kind]["path"]
            original = path.read_bytes()
            receipt = json.loads(original)
            path.write_text(json.dumps({**receipt, **change}), encoding="utf-8")
            bad = copy.deepcopy(evidence)
            with self.assertRaises(q.ContractError):
                q.validate_completion(self.job, record, bad, artifacts, self.root)
            bad["artifacts"][kind]["sha256"] = q.digest(path)
            with self.subTest(receipt=change), self.assertRaises(q.ContractError):
                q.validate_completion(self.job, record, bad, artifacts, self.root)
            path.write_bytes(original)

    def test_input_audit_reused_without_permissive_parser(self):
        record, evidence, artifacts = self.completion()
        path = artifacts / evidence["artifacts"]["contacts"]["path"]
        rows = [json.loads(line) for line in path.read_text().splitlines()]
        rows[4]["interventions"] = 1
        path.write_text("\n".join(json.dumps(row) for row in rows), encoding="utf-8")
        evidence["artifacts"]["contacts"]["sha256"] = q.digest(path)
        with self.assertRaisesRegex(q.ContractError, "input contact"):
            q.validate_completion(self.job, record, evidence, artifacts, self.root)

    def test_all_120_real_packet_baseline_sections(self):
        """Audit 720 packet slots; protected counterparts need separate authority."""
        project = Path(__file__).resolve().parents[2]
        packet_root = project / "Design" / "Roadmap" / "Skirmish_Expansion" / "Scenarios"
        packets = [p for p in packet_root.glob("*.md") if p.name != "README.md"]
        self.assertEqual(len(packets), 20, "restore the 20 governing packet files before running this check")
        found = set()
        audited = admitted = protected_refusals = 0
        for packet in sorted(packets):
            relative = packet.relative_to(project).as_posix()
            packet_hash = q.digest(packet)
            for match in re.finditer(r"^## (S\d{3})\s*$([\s\S]*?)(?=^## |\Z)", packet.read_text(encoding="utf-8"), re.MULTILINE):
                catalog, section = match.groups()
                self.assertNotIn(catalog, found)
                found.add(catalog)
                line = re.search(r"Regular seeds (.+?) in both EN and FA at Standard", section)
                self.assertIsNotNone(line, catalog)
                seeds = [int(seed) for seed in re.findall(r"`(\d+)`", line.group(1))]
                self.assertEqual(len(set(seeds)), 3, catalog)
                for seed in seeds:
                    for locale in ("EN", "FA"):
                        job = {**self.job, "job_id": catalog + "-" + locale + "-" + str(seed), "catalog_id": catalog, "definition_id": "skirmish." + catalog.lower(), "seed": seed, "locale": locale, "packet_path": relative, "packet_sha256": packet_hash, "source_manifest": [{"path": relative, "sha256": packet_hash}]}
                        with self.subTest(catalog=catalog, locale=locale, seed=seed):
                            if catalog in ("S001", "S025", "S073"):
                                with self.assertRaisesRegex(q.ContractError, "protected preset cannot be represented|schema3 requires exact protected counterpart"):
                                    q.preflight(job, project)
                                protected_refusals += 1
                            else:
                                self.assertEqual(q.preflight(job, project)["native_qualification"], "pending")
                                admitted += 1
                        audited += 1
        self.assertEqual(found, {"S" + str(i).zfill(3) for i in range(1, 121)})
        self.assertEqual(audited, 720)
        self.assertEqual(admitted, 702)
        self.assertEqual(protected_refusals, 18)


if __name__ == "__main__":
    unittest.main(verbosity=2)
