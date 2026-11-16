import copy
import json
import unittest
from pathlib import Path

from generate_s003_refreshed_prefab_keys import extract


class RefreshedS003PrefabKeysTests(unittest.TestCase):
    def test_exact_native_identities_and_bad_rows(self):
        root = Path(__file__).resolve().parents[4]
        source = root / "Logs/SkirmishProduction/s003-native-metadata-refresh-20261104-r3/data/s003-native-metadata.json"
        capture = json.loads(source.read_bytes())
        self.assertEqual(19, len(extract(capture)))
        changed = copy.deepcopy(capture)
        changed["context"]["metadata"]["units"][0]["prefab"]["local_id"] = 0
        with self.assertRaises(ValueError):
            extract(changed)
        changed = copy.deepcopy(capture)
        rows = changed["context"]["metadata"]["units"]
        rows[1]["prefab"]["key"] = rows[0]["prefab"]["key"]
        with self.assertRaises(ValueError):
            extract(changed)


if __name__ == "__main__":
    unittest.main()
