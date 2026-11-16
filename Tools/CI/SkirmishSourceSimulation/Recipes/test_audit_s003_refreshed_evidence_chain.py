import hashlib
import tempfile
import unittest
from pathlib import Path

from audit_s003_refreshed_evidence_chain import verify_pins


class RefreshedS003AuditTests(unittest.TestCase):
    def test_pin_normalization_and_changed_input_rejection(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / "asset"
            source.write_bytes(b"current")
            digest = hashlib.sha256(b"current").hexdigest()
            normalized = verify_pins(root, {"asset": digest, str(source): digest})
            self.assertEqual({str(source.resolve()): digest}, normalized)
            source.write_bytes(b"changed")
            with self.assertRaises(ValueError):
                verify_pins(root, {"asset": digest})
            with self.assertRaises(ValueError):
                verify_pins(root, {"missing": digest})


if __name__ == "__main__":
    unittest.main()
