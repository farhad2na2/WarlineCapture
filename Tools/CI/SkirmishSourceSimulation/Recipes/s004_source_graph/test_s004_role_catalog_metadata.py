import hashlib
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import verify_s004_role_catalog_metadata as verifier
import verify_s004_current_role_catalog_metadata as current_verifier


class RoleCatalogVerificationTests(unittest.TestCase):
    def test_exact_role_projection_stays_unqualified(self):
        payload=b'{"units":[1]}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'candidate.json';path.write_bytes(payload)
            with patch.object(verifier.role_candidate,'build',return_value={'units':[1]}):
                result=verifier.verify(path,hashlib.sha256(payload).hexdigest())
            self.assertFalse(result['runtime_admission']);self.assertEqual(result['unit_count'],1)

    def test_changed_domain_rejected(self):
        payload=b'{"units":[1]}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'candidate.json';path.write_bytes(payload)
            with patch.object(verifier.role_candidate,'build',return_value={'units':[2]}):
                with self.assertRaises(ValueError):verifier.verify(path,hashlib.sha256(payload).hexdigest())

    def test_duplicate_field_rejected(self):
        payload=b'{"units":[],"units":[]}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'candidate.json';path.write_bytes(payload)
            with self.assertRaises(ValueError):verifier.verify(path,hashlib.sha256(payload).hexdigest())

    def test_explicit_inputs_are_forwarded_together(self):
        payload=b'{"units":[1]}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'candidate.json';path.write_bytes(payload)
            inputs=(Path('build'),Path('prefabs'),Path('metadata'))
            with patch.object(verifier.role_candidate,'build',return_value={'units':[1]}) as builder:
                verifier.verify(path,hashlib.sha256(payload).hexdigest(),*inputs)
                builder.assert_called_once_with(*inputs)
            with self.assertRaisesRegex(ValueError,'All three explicit source inputs'):
                verifier.verify(path,hashlib.sha256(payload).hexdigest(),inputs[0])

    def test_current_evidence_rebuild_and_tamper_guards(self):
        payload=b'{"units":[1]}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'current.json';path.write_bytes(payload)
            inputs=tuple(Path(name) for name in ('build','prefab','audit','projection','metadata'))
            with patch.object(current_verifier.roles,'build',return_value={'units':[1]}) as builder:
                result=current_verifier.verify(path,hashlib.sha256(payload).hexdigest(),*inputs)
                builder.assert_called_once_with(*inputs)
                self.assertFalse(result['runtime_admission']);self.assertFalse(result['mission_ready'])
                with self.assertRaisesRegex(ValueError,'SHA-256 mismatch'):
                    current_verifier.verify(path,'0'*64,*inputs)
            with patch.object(current_verifier.roles,'build',return_value={'units':[2]}):
                with self.assertRaisesRegex(ValueError,'differs from its exact source join'):
                    current_verifier.verify(path,hashlib.sha256(payload).hexdigest(),*inputs)


if __name__=='__main__':unittest.main()
