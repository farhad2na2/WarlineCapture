"""Constructed contract tests only. No real compiler/mission admission is proved."""
import ast
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import skirmish_compile_attempt_adapter as adapter
import skirmish_operational_compile_connector as connector
import skirmish_native_compile_admission as native
from skirmish_production_queue import fingerprint
from skirmish_production_operational_inspector import Reader


class AttemptAdapterTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="warline-attempt-contract-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        self.items = []
        self.actual = []
        self.backend_calls = []
        self.initial = self.write("Logs/operational-proof.json", {"fixture": "synthetic-not-native"})
        self.proof_ref = self.write("Logs/compile-proof.json", {"schema_version": 3, "fixture_only": True})
        for i in range(2):
            job = dict(job_id="job-"+str(i), catalog_id="S002" if i == 0 else "S005",
                       candidate_sha256="a"*64, build_sha256="b"*64)
            folder = self.root / "Logs" / job["job_id"]
            attempt = folder / ("attempt-"+str(i))
            job_ref = self.write(str(folder / "job.json"), job)
            claim = dict(attempt_id=attempt.name, worker="fixture-worker", token="private-claim-token",
                         started_utc="fixture", job_sha256=fingerprint(job), artifact_directory=str(attempt))
            claim_ref = self.write(str(folder / "claim.json"), claim)
            immutable = self.write(str(attempt / "claim.json"), claim)
            self.items.append(dict(job=job_ref, claim=claim_ref, immutable_claim=immutable))
            self.actual.append(dict(job_sha256=fingerprint(job), native_config_sha256=str(i+1)*64,
                                    session="fixture-session-"+str(i), run_id="fixture-run-"+str(i), outcome="Victory" if i == 0 else "Defeat"))
        self.op_spec = {"attempts": self.items}
        self.request = dict(schema_version=1, operational=self.op_spec, compile_proof=self.proof_ref)
        self.op_patch = patch.object(adapter, "inspect_operational", side_effect=lambda *a, **k: self.operational())
        self.op_patch.start(); self.addCleanup(self.op_patch.stop)
        self.native_patch = patch.object(adapter.backend, "admit", side_effect=self.backend)
        self.native_patch.start(); self.addCleanup(self.native_patch.stop)

    def write(self, path, value):
        p = Path(path)
        if not p.is_absolute(): p = self.root / p
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(adapter.canonical(value)+b"\n")
        return dict(path=str(p), sha256=hashlib.sha256(p.read_bytes()).hexdigest())

    def operational(self):
        return dict(result="NormalCompletedUnqualified", wins=1, attempts=copy.deepcopy(self.actual),
                    admission=dict(missing=["compile", "live_api", "source_fit", "paid_resources"],
                                   profile_receipts=[{"existing": True}], observer_receipts=[{"existing": True}]),
                    proof_pins=[copy.deepcopy(self.initial)], qualified=False, queue_success_enabled=False,
                    mission_ready=False, publication_changed=False)

    def backend(self, project, job, proof):
        self.backend_calls.append(fingerprint(job))
        actual = next(a for a in self.actual if a["job_sha256"] == fingerprint(job))
        return dict(schema_version=1, admission="compile", result="MeasuredNativeEditorCompileForExactCandidate",
                    job_sha256=fingerprint(job), candidate_sha256=job["candidate_sha256"], build_sha256=job["build_sha256"],
                    native_config_sha256=actual["native_config_sha256"], validated_code_commit="c"*40,
                    coverage=dict(owningInputClosure={"fixture": True}, nativeInputClosure={"fixture": True},
                                  nativeVsIsolatedDifferences=[], isolatedWholeCurrentNativeSourceCertified=False, androidPlayerBuilt=False),
                    proof_pins=[copy.deepcopy(self.initial)], qualified=False, queue_success_enabled=False,
                    mission_ready=False, android_player_build_proved=False, device_accepted=False, errors=[])

    def produced(self):
        return adapter.produce(self.root, self.request)["connector_spec"]

    def assert_missing(self, spec):
        result = connector.inspect(self.root, spec)
        self.assertIn("compile", result["admission"]["missing"])
        self.assertTrue(result["admission"]["compile_errors"])
        self.assertEqual(result["wins"], 1)
        self.assertEqual(result["attempts"], self.actual)
        for f in ["qualified", "queue_success_enabled", "mission_ready", "publication_changed"]:
            self.assertIs(result[f], False)
        return result

    def mutate_receipt(self, spec, change):
        spec = copy.deepcopy(spec)
        ref = spec["compile_receipts"][0]
        value = json.loads(Path(ref["path"]).read_bytes())
        change(value)
        # Even rehashing and renaming a forged receipt is insufficient: the
        # consumer recomputes native proof and exact subject independently.
        digest = hashlib.sha256(adapter.canonical(value)).hexdigest()
        target = Path(ref["path"]).parent / ("compile-admission-"+digest+".json")
        spec["compile_receipts"][0] = self.write(target, value)
        return spec

    def test_attempts_are_distinct_immutable_references(self):
        result = adapter.produce(self.root, self.request)
        self.assertEqual(result["admitted_attempts"], 2)
        spec = result["connector_spec"]
        refs = spec["compile_receipts"]
        self.assertNotEqual(refs[0]["path"], refs[1]["path"])
        for i, ref in enumerate(refs):
            value = json.loads(Path(ref["path"]).read_bytes())
            self.assertEqual(value["subject"]["attempt_id"], "attempt-"+str(i))
            self.assertEqual(value["subject"]["worker"], "fixture-worker")
            self.assertNotIn("private-claim-token", Path(ref["path"]).read_text())
            self.assertEqual(value["sampling_coverage_credit"], 0)
        self.assertEqual(adapter.produce(self.root, self.request)["connector_spec"], spec)

    def test_connector_recomputes_and_clears_only_compile(self):
        spec = self.produced(); count = len(self.backend_calls)
        result = connector.inspect(self.root, spec)
        self.assertEqual(len(self.backend_calls)-count, 2)
        self.assertEqual(result["admission"]["missing"], ["live_api", "source_fit", "paid_resources"])
        self.assertEqual(result["admission"]["profile_receipts"], [{"existing": True}])
        self.assertEqual(result["admission"]["observer_receipts"], [{"existing": True}])
        self.assertEqual(result["wins"], 1)
        for f in ["qualified", "queue_success_enabled", "mission_ready", "publication_changed"]: self.assertIs(result[f], False)

    def test_all_subject_fields_resist_rehashed_receipt_splicing(self):
        spec = self.produced()
        for field in ["attempt_id", "worker", "job_sha256", "catalog_id", "candidate_sha256", "build_sha256", "native_config_sha256", "native_session_id", "run_id"]:
            with self.subTest(field=field):
                self.assert_missing(self.mutate_receipt(spec, lambda r, f=field: r["subject"].__setitem__(f, "foreign")))

    def test_readiness_flags_and_sampling_credit_resist_forgery(self):
        spec = self.produced()
        for field in adapter.FLAGS:
            with self.subTest(field=field):
                self.assert_missing(self.mutate_receipt(spec, lambda r, f=field: r.__setitem__(f, 1)))

    def test_receipt_reference_digest_tamper(self):
        spec = self.produced(); Path(spec["compile_receipts"][0]["path"]).write_text("{}")
        self.assert_missing(spec)

    def test_duplicate_or_reordered_or_missing_attempt_receipts(self):
        original = self.produced()
        for refs in [[original["compile_receipts"][0]]*2, list(reversed(original["compile_receipts"])), original["compile_receipts"][:1], []]:
            with self.subTest(refs=refs):
                spec = copy.deepcopy(original);spec["compile_receipts"] = refs;self.assert_missing(spec)

    def test_legacy_inmemory_connector_schema_is_refused(self):
        with self.assertRaises(ValueError): connector.inspect(self.root, dict(schema_version=2, operational=self.op_spec, compile_proof={}))

    def test_caller_truth_fields_and_bool_versions_are_refused(self):
        for change in [lambda s:s.update(accepted=True), lambda s:s.update(schema_version=True)]:
            s = copy.deepcopy(self.request);change(s)
            with self.assertRaises(ValueError): adapter.produce(self.root, s)

    def test_cross_attempt_receipt_copy_is_not_canonical(self):
        spec = self.produced();ref = spec["compile_receipts"][0]
        target = Path(spec["compile_receipts"][1]["path"]).parent / Path(ref["path"]).name
        spec["compile_receipts"][0] = self.write(target, json.loads(Path(ref["path"]).read_bytes()))
        self.assert_missing(spec)

    def test_backend_source_hash_is_pinned(self):
        with patch.object(adapter, "BACKEND_SHA256", "0"*64):
            with self.assertRaisesRegex(ValueError, "backend changed"): adapter.produce(self.root, self.request)

    def test_actual_native_backend_pending_closure_stays_refused(self):
        self.native_patch.stop()
        try:
            # No private receipt fabricated or transferred: exact schema3 refs
            # intentionally unbound exercise the real backend's refusal path.
            ref = self.write("Logs/unbound-proof.json", dict(schema_version=3, **{k:None for k in native.REFS}))
            request = copy.deepcopy(self.request);request["compile_proof"] = ref
            result = adapter.produce(self.root, request)
            self.assertEqual(result["admitted_attempts"], 0)
            self.assertEqual(result["missing_or_failed_attempts"], 2)
            rejected = connector.inspect(self.root, result["connector_spec"])
            self.assertIn("compile", rejected["admission"]["missing"])
        finally:
            self.native_patch.start()

    def test_changed_proof_document_rejects_old_receipt(self):
        spec = self.produced();Path(self.proof_ref["path"]).write_text('{"changed":true}')
        self.assert_missing(spec)

    def test_forged_backend_success_payload_cannot_substitute_actual_evidence(self):
        spec = self.produced()
        self.assert_missing(self.mutate_receipt(spec, lambda r:r["evidence"].update(validated_code_commit="f"*40)))

    def test_backend_current_subject_and_coverage_mismatches_reject(self):
        original = self.backend
        for field, value in [("candidate_sha256", "e"*64), ("build_sha256", "e"*64), ("native_config_sha256", "e"*64), ("qualified", True)]:
            def changed(project, job, proof, f=field, v=value):
                r=original(project, job, proof);r[f]=v;return r
            with self.subTest(field=field), patch.object(adapter.backend, "admit", side_effect=changed):
                with self.assertRaises(ValueError): adapter.produce(self.root, self.request)
        def expanded(project, job, proof):
            r=original(project, job, proof);r["coverage"]["isolatedWholeCurrentNativeSourceCertified"]=True;return r
        with patch.object(adapter.backend, "admit", side_effect=expanded):
            with self.assertRaises(ValueError): adapter.produce(self.root, self.request)

    def test_second_attempt_failure_never_clears_compile_for_batch(self):
        spec = self.produced();original = self.backend
        def failed(project, job, proof):
            if job["catalog_id"] == "S005":
                return dict(schema_version=1, admission="compile", result="MissingOrFailedAdmission", errors=["Synthetic failed wrapper/nonzero terminal"],
                            qualified=False, queue_success_enabled=False, mission_ready=False, android_player_build_proved=False, device_accepted=False)
            return original(project, job, proof)
        with patch.object(adapter.backend, "admit", side_effect=failed): self.assert_missing(spec)

    def test_proof_leaf_change_after_production_is_refused(self):
        spec = self.produced();Path(self.initial["path"]).write_text('{"leaf":"changed"}')
        with self.assertRaises(ValueError): connector.inspect(self.root, spec)

    def test_mutation_during_backend_invalidates_full_pin_union(self):
        original = self.backend
        def mutated(project, job, proof):
            result=original(project, job, proof);Path(self.initial["path"]).write_text('{"late":true}');return result
        with patch.object(adapter.backend, "admit", side_effect=mutated):
            with self.assertRaises(ValueError): adapter.produce(self.root, self.request)

    def test_operational_failure_never_calls_backend_or_writes_receipt(self):
        with patch.object(adapter, "inspect_operational", return_value={"result":"Incomplete"}):
            with self.assertRaises(ValueError): adapter.produce(self.root, self.request)
        self.assertEqual(self.backend_calls, [])
        self.assertEqual(list(self.root.rglob("compile-admission-*.json")), [])

    def test_cross_namespace_traversal_and_symlink_references_refused(self):
        for path in ["../outside.json", str(self.root/"Logs"/".."/"compile-proof.json")]:
            request=copy.deepcopy(self.request);request["compile_proof"]["path"]=path
            with self.assertRaises(ValueError): adapter.produce(self.root, request)
        alias=self.root/"Logs"/"alias.json";alias.symlink_to(Path(self.proof_ref["path"]))
        request=copy.deepcopy(self.request);request["compile_proof"]["path"]=str(alias)
        with self.assertRaises(ValueError): adapter.produce(self.root, request)

    def test_existing_corrupt_or_symlink_receipt_not_overwritten(self):
        read=Reader(self.root);receipt,folder=adapter.evaluate_attempt(read,self.items[0],self.actual[0],self.proof_ref)
        path=adapter.receipt_path(folder,receipt);path.write_bytes(b"incomplete preserved")
        with self.assertRaises(ValueError): adapter.write_immutable(read,folder,receipt)
        self.assertEqual(path.read_bytes(),b"incomplete preserved")
        path.unlink();path.symlink_to(Path(self.initial["path"]))
        with self.assertRaises(ValueError): adapter.write_immutable(read,folder,receipt)

    def test_duplicate_json_and_nonfinite_proof_are_refused(self):
        for raw in [b'{"schema_version":3,"schema_version":3}',b'{"number":NaN}']:
            path=Path(self.proof_ref["path"]);path.write_bytes(raw)
            request=copy.deepcopy(self.request);request["compile_proof"]["sha256"]=hashlib.sha256(raw).hexdigest()
            with self.assertRaises(ValueError): adapter.produce(self.root,request)


if __name__ == "__main__":
    unittest.main(verbosity=2)
