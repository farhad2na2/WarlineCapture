import copy
import json
import unittest

import build_air_combined80_coherent_authority as subject


class CoherentAuthorityTests(unittest.TestCase):
    def setUp(self):
        raw = (subject.ROOT / subject.ORIGINAL).read_bytes()
        self.assertEqual(subject.digest(raw), subject.ORIGINAL_SHA)
        self.original = json.loads(raw)
        self.bytes = {target: ("projected:" + target).encode() for target in subject.ALLOWED}
        self.rows = [{"target": pin["path"], "source": pin["path"],
                      "before_sha256": pin["sha256"],
                      "sha256": subject.digest(self.bytes[pin["path"]])}
                     for pin in self.original["requirement_source_pins"]
                     if pin["path"] in subject.ALLOWED]

    def test_exact_eighty_missions_and_all_other_content_preserved(self):
        after, changes = subject.successor(self.original, self.rows, self.bytes.__getitem__)
        self.assertEqual(after["missions"], self.original["missions"])
        self.assertEqual(after["source_candidates"], self.original["source_candidates"])
        self.assertEqual(len(changes), 2)
        self.assertEqual(sum(len(row["canonical_size_rows"]) for row in after["missions"]), 240)
        subject.verify_semantics(self.original, after)

    def test_mission_budget_source_and_undeclared_pin_changes_reject(self):
        for mutate in (
            lambda d: d["missions"][0].update(catalog_id="S999"),
            lambda d: d["missions"][0]["canonical_size_rows"][0].update(player_rifle="999"),
            lambda d: d["source_candidates"].clear(),
            lambda d: d["profile_asset_pins"]["A"].update(sha256="0" * 64),
        ):
            altered = copy.deepcopy(self.original)
            mutate(altered)
            with self.assertRaises(ValueError):
                subject.verify_semantics(self.original, altered)

    def test_projection_missing_duplicate_stale_parent_or_changed_bytes_reject(self):
        for rows in (self.rows[:1], self.rows + self.rows[:1]):
            with self.assertRaises(ValueError):
                subject.successor(self.original, rows, self.bytes.__getitem__)
        stale = copy.deepcopy(self.rows)
        stale[0]["before_sha256"] = "0" * 64
        with self.assertRaises(ValueError):
            subject.successor(self.original, stale, self.bytes.__getitem__)
        with self.assertRaises(ValueError):
            subject.successor(self.original, self.rows, lambda _: b"changed")


if __name__ == "__main__":
    unittest.main()
