import hashlib
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import verify_s004_airport_source_definition as verifier
import verify_s004_current_airport_source_definition as current_verifier


class AirportCandidateVerificationTests(unittest.TestCase):
    def test_exact_unqualified_source_candidate(self):
        raw=b'{"airport":{}}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'airport.json';path.write_bytes(raw)
            with patch.object(verifier.candidate,'build',return_value={'airport':{}}):
                result=verifier.verify(path,hashlib.sha256(raw).hexdigest())
            self.assertFalse(result['live_runway_registry_validated'])
            self.assertFalse(result['mission_ready'])

    def test_changed_airport_or_duplicate_property_rejected(self):
        for raw,expected in [(b'{"airport":1}',{'airport':2}), (b'{"airport":1,"airport":1}',{'airport':1})]:
            with self.subTest(raw=raw), tempfile.TemporaryDirectory() as directory:
                path=Path(directory)/'airport.json';path.write_bytes(raw)
                with patch.object(verifier.candidate,'build',return_value=expected):
                    with self.assertRaises(ValueError):verifier.verify(path,hashlib.sha256(raw).hexdigest())

    def test_explicit_current_inputs_rebuild_and_fail_closed(self):
        raw=b'{"airport":{}}'
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'airport.json';path.write_bytes(raw)
            inputs=tuple(Path('input'+str(i)) for i in range(9))
            with patch.object(current_verifier.airports,'build',return_value={'airport':{}}) as builder:
                result=current_verifier.verify(path,hashlib.sha256(raw).hexdigest(),*inputs)
                builder.assert_called_once_with(*inputs)
                self.assertFalse(result['runtime_admission']);self.assertFalse(result['mission_ready'])
                with self.assertRaisesRegex(ValueError,'SHA-256 mismatch'):
                    current_verifier.verify(path,'0'*64,*inputs)
                with self.assertRaisesRegex(ValueError,'nine explicit'):
                    current_verifier.verify(path,hashlib.sha256(raw).hexdigest(),*inputs[:-1])
            with patch.object(current_verifier.airports,'build',return_value={'airport':{'changed':True}}):
                with self.assertRaisesRegex(ValueError,'differs from its exact'):
                    current_verifier.verify(path,hashlib.sha256(raw).hexdigest(),*inputs)


if __name__=='__main__':unittest.main()
