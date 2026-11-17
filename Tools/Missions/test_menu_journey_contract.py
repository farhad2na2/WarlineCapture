#!/usr/bin/env python3
"""Pins the generic menu journey wiring in the Editor harness (it cannot be compiled in the code lane)."""
import re
import unittest

import check_draft as checker

DIR = checker.ROOT / 'Assets/Game/Scripts/Editor/Skirmish'


def read(name):
    return (DIR / name).read_text(encoding='utf-8')


class MenuJourneyContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.harness = read('SkirmishS002AriaRunHarness.cs')
        cls.probe = read('SkirmishS002AriaRunHarness.MenuProbe.cs')
        cls.journey = read('SkirmishS002AriaRunHarness.MenuJourney.cs')

    def test_env_routes_any_catalog_through_the_presented_controls(self):
        self.assertIn('"WARLINE_MENU_JOURNEY") == "1"', self.journey)
        self.assertIn('bool menuJourney = AnyMenuJourney;', self.harness)
        # TryQueueExpanded stays the non-journey path.
        self.assertRegex(self.harness, r'if \(menuJourney\)\s*\{\s*if \(!EnterS004ThroughPresentedControls\(\)\) return;\s*\}\s*else if \(!TryQueueExpanded\(em\)\) return;')

    def test_s004_behaviour_is_kept(self):
        self.assertIn('"WARLINE_S004_MENU_JOURNEY") == "1"', self.journey)
        self.assertIn('(catalogId == "S005" || productionCanary != null || codedSizeCameraActive) && Environment.GetEnvironmentVariable("WARLINE_S004_MENU_JOURNEY") == "1") || GenericMenuJourney', self.harness)
        self.assertIn('Debug.Log("[S004MenuJourney] result=Passed', self.probe)
        self.assertIn('FailMenuJourney(menuStep, SkirmishMenuJourneyLog.TimeoutReason(menuStep, menuScenarioCardSeen), "menuControlsTimeout")', self.probe)

    def test_no_programmatic_match_start_in_the_generic_journey(self):
        # The nudges (TryEnterMatch/TryRequestPlay/...) only run when nativeS005MenuJourney is false.
        self.assertRegex(self.harness, r'if \(stage <= 1 && !nativeS005MenuJourney\)\s*\{\s*SkirmishLaunchProjection\.TryEnterMatch')
        self.assertNotIn('SkirmishLaunchProjection.', self.journey)
        self.assertNotIn('TryEnterMatch', self.journey)

    def test_state_is_reset_per_queue_job_and_preflight_precedes_any_touch(self):
        self.assertRegex(self.harness, r'stage = 0;\s*if \(GenericMenuJourney\) ResetMenuJourneyState\(\);')
        self.assertRegex(self.probe, r'if \(menuTouch == null\)\s*\{\s*if \(!MenuJourneyPreflightPasses\(\)\) return false;')

    def test_passed_requires_the_native_session_and_setup_check_runs_first(self):
        self.assertRegex(self.probe, r'if \(!CompleteMenuJourney\(world\.EntityManager\)\) return;\s*Debug\.Log\("\[S004MenuJourney\] result=Passed')
        self.assertIn('SkirmishMenuJourneyLog.VerifySetup(catalogId, seed,', self.journey)
        self.assertIn('SkirmishMenuJourneyLog.FormatPassed(catalogId, menuPath', self.journey)

    def test_path_records_taps_and_the_scroll_only(self):
        self.assertIn('menuPath.Add(b.name);', self.probe)
        self.assertIn('menuPath.Add("BattleLibrary(scroll)")', self.probe)
        # Every tap goes through MenuTap (a real touch gesture), never onClick.Invoke.
        self.assertNotIn('onClick.Invoke', self.probe + self.journey)


if __name__ == '__main__':
    unittest.main()
