import copy
import hashlib
import json
import tempfile
import unittest
from pathlib import Path

import verify_s006_external_delta as verifier


class ExternalDeltaControls(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.path = verifier.ROOT / 'T Logs/SkirmishProduction/s006-external-config-candidate-20261105-T1/data.json'
        cls.raw = cls.path.read_bytes()
        cls.sha = hashlib.sha256(cls.raw).hexdigest()
        cls.delta = verifier.parse(cls.raw)

    def test_retained_evidence_is_source_only(self):
        result = verifier.verify(self.path, self.sha)
        self.assertEqual(result['status'], 'PassedDiagnosticEvidence')
        self.assertGreater(result['evidence_pin_count'], 0)
        self.assertFalse(result['runtime_admission'])
        self.assertFalse(result['mission_ready'])

    def test_wrong_input_digest_rejected(self):
        with self.assertRaisesRegex(ValueError, 'caller-pinned'):
            verifier.verify(self.path, '0' * 64)

    def test_nonfinite_and_duplicate_json_rejected(self):
        for raw in (b'{"rows":[],"rows":[]}', b'{"value":NaN}', b'{"value":Infinity}'):
            with self.assertRaises(ValueError):
                verifier.parse(raw)

    def test_admission_and_duplicate_sizes_rejected(self):
        for key in ('runtime_admission', 'source_fit_certified', 'mission_ready'):
            changed = copy.deepcopy(self.delta)
            changed[key] = True
            with self.assertRaises(ValueError):
                verifier.shape(changed)
        changed = copy.deepcopy(self.delta)
        changed['rows'][1]['size'] = changed['rows'][0]['size']
        with self.assertRaises(ValueError):
            verifier.shape(changed)

    def test_altered_evidence_descriptor_rejected(self):
        changed = copy.deepcopy(self.delta['candidate_result'])
        changed['sha256'] = '0' * 64
        with self.assertRaisesRegex(ValueError, 'Evidence bytes differ'):
            verifier.descriptor(changed)
        changed['path'] = '/private/tmp/unrelated.json'
        with self.assertRaises((ValueError, FileNotFoundError)):
            verifier.descriptor(changed)

    def test_rehashed_altered_candidate_values_rejected(self):
        changed = copy.deepcopy(self.delta)
        changed['rows'][0]['disposable_pad_projection']['bound_compiler']['setup_hash'] ^= 1
        with tempfile.TemporaryDirectory(dir=verifier.ROOT / 'T Logs/SkirmishProduction') as directory:
            path = Path(directory) / 'data.json'
            path.write_text(json.dumps(changed))
            changed_sha = hashlib.sha256(path.read_bytes()).hexdigest()
            with self.assertRaisesRegex(ValueError, 'original compiler candidate'):
                verifier.verify(path, changed_sha)


if __name__ == '__main__':
    unittest.main()
