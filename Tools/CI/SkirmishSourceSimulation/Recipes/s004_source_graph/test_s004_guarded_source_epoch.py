import unittest
from unittest.mock import patch

import build_s004_guarded_source_epoch as guarded_epoch
import verify_s004_guarded_source_epoch as guarded
import verify_s004_merged_source_epoch as merged


class GuardedEpochIsolationTests(unittest.TestCase):
    def test_verifier_restores_original_epoch_after_success(self):
        original = merged.epoch
        with patch.object(merged, "verify", side_effect=lambda *_: merged.epoch) as verify:
            self.assertIs(guarded.verify("bundle", "hash", "output"), guarded_epoch)
            verify.assert_called_once()
        self.assertIs(merged.epoch, original)

    def test_verifier_restores_original_epoch_after_failure(self):
        original = merged.epoch
        with patch.object(merged, "verify", side_effect=ValueError("failed verification")):
            with self.assertRaisesRegex(ValueError, "failed verification"):
                guarded.verify("bundle", "hash", "output")
        self.assertIs(merged.epoch, original)
