import hashlib
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import verify_s004_unit_metadata_candidate as verifier


class CandidateVerifierTests(unittest.TestCase):
    def check(self, payload, rebuilt):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "candidate.json"
            path.write_bytes(payload)
            digest = hashlib.sha256(payload).hexdigest()
            with patch.object(verifier.candidate, "build", return_value=rebuilt):
                return verifier.verify(path, digest)

    def test_exact_source_projection_is_unqualified(self):
        result = self.check(b'{"units":[],"runtime_admission":false}',
                            {"runtime_admission": False, "units": []})
        self.assertFalse(result["runtime_admission"])
        self.assertFalse(result["native_prefab_resolution"])

    def test_type_change_cannot_pass_as_equal(self):
        with self.assertRaises(ValueError):
            self.check(b'{"units":[],"runtime_admission":0}',
                       {"units": [], "runtime_admission": False})

    def test_duplicate_property_rejected(self):
        with self.assertRaises(ValueError):
            self.check(b'{"units":[],"units":[]}', {"units": []})

    def test_changed_projection_rejected(self):
        with self.assertRaises(ValueError):
            self.check(b'{"units":[1]}', {"units": [2]})

    def test_hash_mismatch_rejected_before_rebuild(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "candidate.json"
            path.write_text('{}')
            with patch.object(verifier.candidate, "build") as build:
                with self.assertRaises(ValueError):
                    verifier.verify(path, "0" * 64)
                build.assert_not_called()

    def test_candidate_changed_during_rebuild_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "candidate.json"
            payload = b'{"units":[]}'
            path.write_bytes(payload)
            def rebuild():
                path.write_bytes(b'{"units":[1]}')
                return {"units": []}
            with patch.object(verifier.candidate, "build", side_effect=rebuild):
                with self.assertRaises(ValueError):
                    verifier.verify(path, hashlib.sha256(payload).hexdigest())


if __name__ == "__main__":
    unittest.main()
