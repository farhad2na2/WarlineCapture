#!/usr/bin/env python3
"""Pins the once-per-match [MatchSceneInventory] diagnostic (Editor harness; cannot be compiled in the code lane)."""
import unittest

import check_draft as checker

DIR = checker.ROOT / 'Assets/Game/Scripts/Editor/Skirmish'


class MatchInventoryContract(unittest.TestCase):
    def test_logged_once_at_the_first_playing_frame_and_is_read_only(self):
        harness = (DIR / 'SkirmishS002AriaRunHarness.cs').read_text(encoding='utf-8')
        inventory = (DIR / 'SkirmishS002AriaRunHarness.MatchInventory.cs').read_text(encoding='utf-8')
        # Inside the playingSince<=0 block, which LaunchCatalog resets per match.
        block = harness[harness.index('if (playingSince <= 0d)'):]
        self.assertIn('LogMatchSceneInventory();', block[:400])
        self.assertIn('playingSince = 0d;', harness)
        self.assertEqual(1, harness.count('LogMatchSceneInventory();'))
        for field in ('scenes=', 'canvases=', 'cameras=', 'audioListeners=', 'eventSystems=', 'menuRoots='):
            self.assertIn(field, inventory)
        for forbidden in ('SetActive', 'Destroy(root', 'enabled =', 'AssetDatabase'):
            self.assertNotIn(forbidden, inventory)
        self.assertTrue(inventory.startswith('#if UNITY_EDITOR'))


if __name__ == '__main__':
    unittest.main()
