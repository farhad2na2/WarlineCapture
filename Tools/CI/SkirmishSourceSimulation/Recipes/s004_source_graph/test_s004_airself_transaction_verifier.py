import json
import shutil
import tempfile
import unittest
from pathlib import Path
import verify_s004_airself_transaction as verifier


ROOT = Path(__file__).resolve().parents[5]
REFERENCE = ROOT / "T Logs/SkirmishProduction/s004-airself-paid-delivery-20261105-T4"


class TransactionControls(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.path = Path(self.tmp.name)
        for name in ("receipt.json", "requests.json"):
            shutil.copyfile(REFERENCE / name, self.path / name)

    def tearDown(self): self.tmp.cleanup()

    def mutate(self, func):
        path = self.path / "requests.json"
        data = json.loads(path.read_bytes())
        func(data)
        path.write_text(json.dumps(data))
        receipt_path = self.path / "receipt.json"
        receipt = json.loads(receipt_path.read_bytes())
        receipt["requests_sha256"] = verifier.sha(path)
        receipt_path.write_text(json.dumps(receipt))

    def test_real_reference(self):
        self.assertEqual(verifier.verify(self.path)["status"], "PassedNarrowSourceTransaction")

    def test_payment_tamper_with_rehashed_payload(self):
        def change(x):
            p = verifier.find(x, "source_semantic_aircraft_probe")[0]
            p["source_transaction_after_request"]["materials"] += 1
        self.mutate(change)
        with self.assertRaises(ValueError): verifier.verify(self.path)

    def test_wrong_spawn_cell_with_rehashed_payload(self):
        def change(x):
            ticks = [v for v in verifier.find(x, "source_production_ticks") if isinstance(v, dict) and "delivery_events" in v][0]
            ticks["delivery_events"][-1]["cell_x"] += 1
        self.mutate(change)
        with self.assertRaises(ValueError): verifier.verify(self.path)

    def test_wrong_entity_with_rehashed_payload(self):
        def change(x):
            p = verifier.find(x, "source_semantic_aircraft_probe")[0]
            p["source_transaction_after_owner_ticks"]["owned_members"][0]["entity_index"] += 1
        self.mutate(change)
        with self.assertRaises(ValueError): verifier.verify(self.path)

    def test_claimed_readiness_rejected(self):
        path = self.path / "receipt.json"
        data = json.loads(path.read_bytes()); data["mission_ready"] = True
        path.write_text(json.dumps(data))
        with self.assertRaises(ValueError): verifier.verify(self.path)


if __name__ == "__main__": unittest.main()
