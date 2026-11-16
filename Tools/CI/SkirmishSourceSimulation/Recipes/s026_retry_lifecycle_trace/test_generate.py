import importlib.util
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]
spec = importlib.util.spec_from_file_location('retry_trace_generator', HERE / 'generate.py')
generator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(generator)

class RetryTraceGeneratorTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)
        self.sources = [generator.RETRY, generator.SYSTEM]
        self.originals = []
        for source in self.sources:
            destination = self.root / source
            destination.parent.mkdir(parents=True, exist_ok=True)
            original = (ROOT / source).read_bytes()
            destination.write_bytes(original)
            self.originals.append(original)

    def tearDown(self):
        self.temp.cleanup()

    def test_overlay_is_gated_and_leaves_inputs_unchanged(self):
        proof = generator.generate(self.root, self.root / 'out')
        retry = (self.root / 'out' / generator.RETRY_OUT).read_text()
        system = (self.root / 'out' / generator.SYSTEM_OUT).read_text()
        self.assertEqual((self.root / self.sources[0]).read_bytes(), self.originals[0])
        self.assertEqual((self.root / self.sources[1]).read_bytes(), self.originals[1])
        self.assertIn('WARLINE_S026_RETRY_TRACE', system)
        self.assertNotIn('BurstDiscard', system)
        self.assertIn('role.rifle.2.6.5', system)
        self.assertIn('role.rifle.2.6.8', system)
        for event in ('retry_create', 'cooldown_clear', 'manual_request_restore', 'long_distance_request_restore'):
            self.assertIn(event, system)
        self.assertEqual(retry.count('RetryResumeFrame(Time.frameCount)'), 1)
        self.assertEqual(system.count('SourceRetryLifecycleTrace.Observe('), 3)
        self.assertIn('OwnerLookup = SourceRetryLifecycleTrace.Enabled ? SystemAPI.GetComponentLookup<SkirmishAttemptOwnedComponent>(true) : default', system)
        self.assertIn('CurrentFrame = Time.frameCount', system)
        self.assertEqual(proof['experimental_only'], True)
        self.assertTrue(any(path.endswith(generator.RETRY) for path in proof['source_pins']))
        self.assertTrue(any(path.endswith(generator.SYSTEM) for path in proof['source_pins']))

    def test_changed_retry_anchor_is_rejected(self):
        path = self.root / generator.RETRY
        path.write_text(path.read_text().replace(generator.RETRY_ANCHOR, 'changed'))
        with self.assertRaisesRegex(ValueError, 'retry creation anchor'):
            generator.generate(self.root, self.root / 'out')

    def test_changed_restore_anchor_is_rejected(self):
        path = self.root / generator.SYSTEM
        original = path.read_text()
        path.write_text(original.replace('if (intent.RestoreRequest)', 'if (false)', 1))
        with self.assertRaisesRegex(ValueError, 'manual request restore anchor'):
            generator.generate(self.root, self.root / 'out')

    def test_existing_output_is_not_overwritten(self):
        (self.root / 'out').mkdir()
        with self.assertRaises(FileExistsError):
            generator.generate(self.root, self.root / 'out')

if __name__ == '__main__':
    unittest.main()
