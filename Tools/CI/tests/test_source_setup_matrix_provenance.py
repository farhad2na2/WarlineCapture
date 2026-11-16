"""Protocol controls for setup-matrix provenance; no gameplay is simulated."""
import hashlib
import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

ci = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ci))
spec = importlib.util.spec_from_file_location('_matrix_checked_cli_bootstrap', ci / 'run_skirmish_source_simulation.py')
bootstrap = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = bootstrap
spec.loader.exec_module(bootstrap)
source = bootstrap.owned.require('skirmish_source_build')


class SetupMatrixProvenanceTests(unittest.TestCase):
    def test_changed_and_unverified_matrices_demote_even_with_canonical_build(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            packaged = root / 'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
            packaged.parent.mkdir(parents=True)
            packaged.write_bytes(b'baseline')
            candidate = root / 'candidate.csv'
            provenance = {'trusted': True, 'experimental': False, 'canonical': True}
            for contents, descriptor_hash, expected_canonical in (
                (b'baseline', hashlib.sha256(b'baseline').hexdigest(), True),
                (b'changed', hashlib.sha256(b'changed').hexdigest(), False),
                (b'changed', hashlib.sha256(b'baseline').hexdigest(), False),
            ):
                candidate.write_bytes(contents)
                initial = {'setup_matrix': {'path': str(candidate), 'sha256': descriptor_hash}}
                matrix = source.setup_matrix_provenance(initial, root)
                self.assertEqual(matrix['canonical'], expected_canonical)
                result = source.classify_experimental_source_run(initial, provenance,
                    {'status': 'SourceOutcomeObserved', 'validation_passed': True, 'mission_ready': True}, matrix)
                self.assertEqual(result['validation_passed'], expected_canonical)
                if not expected_canonical:
                    self.assertEqual(result['status'], 'DiagnosticSourceOutcomeObserved')
                    self.assertFalse(result['mission_ready'])
                    self.assertEqual(result['source_provenance_classification']['class'],
                                     'ExternalOrUnverifiedSetupMatrix')

    def test_missing_matrix_is_unverified(self):
        self.assertFalse(source.setup_matrix_provenance({}, '/nonexistent')['canonical'])


if __name__ == '__main__':
    unittest.main()
