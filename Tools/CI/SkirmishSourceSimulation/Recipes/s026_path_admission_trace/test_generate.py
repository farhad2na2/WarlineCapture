import importlib.util
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
spec = importlib.util.spec_from_file_location('admission_generator', HERE / 'generate.py')
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)

class AdmissionGeneratorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.source = self.root / generator.SOURCE
        self.source.parent.mkdir(parents=True)
        self.original = (ROOT / generator.SOURCE).read_bytes()
        self.source.write_bytes(self.original)
        self.budget = self.root / generator.BUDGET_SOURCE
        self.budget_original = (ROOT / generator.BUDGET_SOURCE).read_bytes()
        self.budget.write_bytes(self.budget_original)

    def tearDown(self):
        self.temp.cleanup()

    def test_only_declared_observation_is_inserted(self):
        proof = generator.generate(self.root, self.root / 'out')
        actual = (self.root / 'out' / generator.OUTPUT).read_text()
        original = self.original.decode('utf-8').replace('\r\n', '\n')
        prefix = actual[:-len(generator.PROBE)]
        self.assertEqual(prefix.replace('\n            SourcePathAdmissionTrace.Observe(Time.frameCount, admission);', '', 1), original)
        self.assertEqual(prefix.count('_pendingPathHandle.IsCompleted'), original.count('_pendingPathHandle.IsCompleted'))
        budget = (self.root / 'out' / generator.BUDGET_OUTPUT).read_text()
        budget = budget.replace('            var sourceObservedBefore = _state;\n', '', 1)
        budget = budget.replace('\n            SourcePathAdmissionTrace.ObserveBudget(UnityEngine.Time.frameCount, sourceObservedBefore, _state, pendingFrames, pendingWallTime, requestCount, manualRequestCount, vehicleLikeCount, pendingRequestBudget);', '', 1)
        self.assertEqual(budget, self.budget_original.decode('utf-8').replace('\r\n', '\n'))
        self.assertNotIn('.IsCompleted', generator.PROBE)
        self.assertTrue(proof['experimental_only'])
        self.assertEqual(self.source.read_bytes(), self.original)
        self.assertEqual(self.budget.read_bytes(), self.budget_original)

    def test_changed_budget_anchor_is_rejected(self):
        self.budget.write_text(self.budget_original.decode('utf-8').replace(generator.BUDGET_ANCHOR, 'changed'))
        with self.assertRaisesRegex(ValueError, 'budget anchor'):
            generator.generate(self.root, self.root / 'out')

    def test_changed_admission_anchor_is_rejected(self):
        self.source.write_text(self.original.decode('utf-8').replace('AdmissionObserved?.Invoke(Time.frameCount, admission);', 'AdmissionObserved?.Invoke(0, admission);'))
        with self.assertRaisesRegex(ValueError, 'admission observation anchor'):
            generator.generate(self.root, self.root / 'out')

    def test_existing_output_is_not_overwritten(self):
        (self.root / 'out').mkdir()
        with self.assertRaises(FileExistsError):
            generator.generate(self.root, self.root / 'out')

if __name__ == '__main__':
    unittest.main()
