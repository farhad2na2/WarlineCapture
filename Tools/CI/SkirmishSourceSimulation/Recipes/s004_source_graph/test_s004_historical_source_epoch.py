import unittest
from unittest.mock import patch

import verify_s004_historical_source_epoch as verifier


class HistoricalSourceEpochControls(unittest.TestCase):
    def test_changed_historical_snapshot_is_rejected(self):
        original = verifier.sha
        def changed(path):
            return '0' * 64 if path == verifier.SNAPSHOT else original(path)
        with patch.object(verifier, 'sha', side_effect=changed), self.assertRaisesRegex(ValueError, 'historical and current'):
            verifier.verify()

    def test_changed_current_builder_is_rejected(self):
        original = verifier.sha
        def changed(path):
            return '0' * 64 if path == verifier.BUILDER else original(path)
        with patch.object(verifier, 'sha', side_effect=changed), self.assertRaisesRegex(ValueError, 'historical and current'):
            verifier.verify()
