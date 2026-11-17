#!/usr/bin/env python3
"""Pins the Editor validation library wiring (WARLINE_VALIDATION_LIBRARY=1); the Editor code cannot be compiled in the code lane."""
import re
import unittest

import check_draft as checker

ROOT = checker.ROOT
EDITOR = ROOT / 'Assets/Game/Scripts/Editor/Skirmish'
CONFIGS = ROOT / 'Assets/Game/Scripts/Configs/Skirmish'


def read(path):
    return path.read_text(encoding='utf-8')


class ValidationLibraryContract(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.plan = read(CONFIGS / 'SkirmishValidationLibraryPlan.cs')
        cls.scope = read(EDITOR / 'SkirmishValidationLibrary.cs')
        cls.journey = read(EDITOR / 'SkirmishS002AriaRunHarness.MenuJourney.cs')
        cls.harness = read(EDITOR / 'SkirmishS002AriaRunHarness.cs')
        cls.queue = read(EDITOR / 'SkirmishS002AriaRunHarness.AriaQueue.cs')

    def test_editor_only_and_never_touches_the_shipped_catalog(self):
        self.assertTrue(self.scope.startswith('#if UNITY_EDITOR'))
        self.assertTrue(self.scope.rstrip().endswith('#endif'))
        # Only the Editor-memory catalog slot is used; no asset or Resources write.
        self.assertIn('SkirmishBattleCatalogConfig.UseEditorValidationCatalog(created)', self.scope)
        for forbidden in ('AssetDatabase', 'SetDirty', 'CreateAsset', 'File.Write', 'SaveAssets'):
            self.assertNotIn(forbidden, self.scope)
            self.assertNotIn(forbidden, self.plan)
        self.assertIn('shippedCatalogModified=0 published=0 readinessGranted=0', self.plan)

    def test_the_switch_is_one_environment_variable_read_only_by_the_editor_harness(self):
        self.assertIn('"WARLINE_VALIDATION_LIBRARY"', self.plan)
        self.assertIn('Environment.GetEnvironmentVariable(SkirmishValidationLibraryPlan.EnvironmentVariable) == "1"', self.scope)
        # The runtime library code (UI, configs) never reads it.
        ui = (ROOT / 'Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.Scenarios.cs').read_text(encoding='utf-8')
        self.assertNotIn('WARLINE_VALIDATION_LIBRARY', ui)
        self.assertNotIn('GetEnvironmentVariable', self.plan)

    def test_it_is_ensured_before_dispatch_before_launch_and_before_the_menu_preflight(self):
        self.assertIn('EnsureValidationLibrary("queueDispatch");', self.queue)
        self.assertIn('EnsureValidationLibrary("launchCatalog");', self.harness)
        self.assertRegex(self.journey, r'menuPreflightDone = true;\s*EnsureValidationLibrary\("menuPreflight"\);\s*var catalog')
        # Dispatch ensures it before the payload check that may open the single-mission harness library.
        dispatch = self.queue.index('EnsureValidationLibrary("queueDispatch");')
        self.assertLess(dispatch, self.queue.index('TryCreateSelectedPayload(fields[0]', dispatch))

    def test_the_catalog_slot_is_released_when_the_queue_ends(self):
        self.assertIn('SkirmishValidationLibrary.Disable();', self.queue)
        complete = self.queue[self.queue.index('private static void CompleteAriaQueue()'):]
        self.assertRegex(complete, r'DisposeHarnessGenericLibrary\(\);\s*SkirmishValidationLibrary\.Disable\(\);')

    def test_a_scope_already_owned_by_an_editor_journey_is_never_replaced(self):
        self.assertRegex(self.journey, r'if \(candidateCatalogScope != null\)\s*\{\s*Debug\.LogWarning\(SkirmishValidationLibraryPlan\.FormatDisabled\("editorJourneyCatalogScopeActive"\)\);\s*return;')
        self.assertIn('anotherEditorValidationCatalogActive', self.scope)

    def test_passed_line_marks_validation_candidates(self):
        self.assertIn('SkirmishValidationLibrary.IsCandidate(catalogId)', self.journey)
        self.assertRegex(self.plan, r'TitleBadge = "\[VALIDATION\] "')


if __name__ == '__main__':
    unittest.main()
