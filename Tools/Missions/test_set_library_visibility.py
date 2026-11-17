#!/usr/bin/env python3
"""Offline tests for the real-catalog visibility tool, run against a copy of the actual library and authored assets."""
import io
import re
import shutil
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

import check_draft as checker
import set_library_visibility as tool

ROOT = checker.ROOT


def copy_assets(repo, catalog):
    for relative in (tool.LIBRARY, tool.AUTHORED_CATALOG):
        (repo / relative).parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(ROOT / relative, repo / relative)
    folder = tool.SCENARIOS / catalog
    (repo / folder).mkdir(parents=True, exist_ok=True)
    name = f'SkirmishScenario_{catalog}.asset'
    for suffix in ('', '.meta'):
        shutil.copyfile(ROOT / folder / (name + suffix), repo / folder / (name + suffix))


def run_tool(repo, catalog, state, *extra):
    out = io.StringIO()
    with redirect_stdout(out):
        code = tool.main(['--catalog', catalog, '--state', state, '--repo', str(repo), *extra])
    return code, out.getvalue().strip()


class VisibilityTool(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.repo = Path(self.tmp.name)
        copy_assets(self.repo, 'S026')
        self.library = self.repo / tool.LIBRARY
        # The checkout's S026 row may currently be Visible (the lane commits that); tests start from Planned.
        tool.run(self.repo, 'S026', 'Planned')
        self.original = self.library.read_bytes()

    def tearDown(self):
        self.tmp.cleanup()

    def lines(self):
        return self.library.read_text(encoding='utf-8').splitlines(keepends=True)

    def test_visible_edits_only_the_five_fields_of_that_row_and_planned_restores_the_file_byte_for_byte(self):
        code, line = run_tool(self.repo, 'S026', 'Visible')
        self.assertEqual(0, code, line)
        self.assertIn('[SkirmishLibraryVisibility] catalog=S026 state=Visible result=Passed changed=5', line)
        self.assertIn('index=1026 definition=skirmish.s026', line)
        self.assertIn('published=0 readinessGranted=0', line)
        before = self.original.decode('utf-8').splitlines(keepends=True)
        after = self.lines()
        self.assertEqual(len(before), len(after))
        changed = [(a.strip(), b.strip()) for a, b in zip(before, after) if a != b]
        self.assertEqual([('Status: 0', 'Status: 1'), ('PlayableScenarioIndex: -1', 'PlayableScenarioIndex: 1026'),
                          ('DefinitionId:', 'DefinitionId: skirmish.s026'), ('ContentVersion: 0', 'ContentVersion: 2'),
                          ('ReadinessManifestId:', 'ReadinessManifestId: validation.visible.skirmish.s026')], changed)
        # Every changed line sits inside the S026 row.
        rows, _ = tool.split_rows(before)
        start, end = rows['S026']
        self.assertTrue(all(start < i < end for i, (a, b) in enumerate(zip(before, after)) if a != b))
        code, line = run_tool(self.repo, 'S026', 'Planned')
        self.assertEqual(0, code, line)
        self.assertEqual(self.original, self.library.read_bytes(), 'back to Planned is exactly the original asset')

    def test_repeating_a_state_is_a_noop_that_still_passes(self):
        self.assertEqual(0, run_tool(self.repo, 'S026', 'Visible')[0])
        first = self.library.read_bytes()
        code, line = run_tool(self.repo, 'S026', 'Visible')
        self.assertEqual(0, code)
        self.assertIn('changed=0 touched=none', line)
        self.assertEqual(first, self.library.read_bytes())
        code, line = run_tool(self.repo, 'S026', 'Planned')
        code, line = run_tool(self.repo, 'S026', 'Planned')
        self.assertIn('changed=0', line)

    def test_dry_run_reports_without_writing(self):
        code, line = run_tool(self.repo, 'S026', 'Visible', '--dry-run')
        self.assertEqual(0, code)
        self.assertIn('changed=5', line)
        self.assertIn('dryRun=1', line)
        self.assertEqual(self.original, self.library.read_bytes())

    def test_other_rows_are_never_touched(self):
        run_tool(self.repo, 'S026', 'Visible')
        before = self.original.decode('utf-8').splitlines(keepends=True)
        after = self.lines()
        rows, _ = tool.split_rows(before)
        for catalog, (start, end) in rows.items():
            if catalog != 'S026':
                self.assertEqual(before[start:end], after[start:end], catalog)
        self.assertEqual(120, len(rows))

    def test_coded_legacy_and_noncanonical_missions_are_refused(self):
        for catalog in ('S001', 'S002', 'S003', 'S004', 'S005', 'S025', 'S073'):
            code, line = run_tool(self.repo, catalog, 'Visible')
            self.assertEqual(2, code)
            self.assertIn('result=Failed reason=codedOrLegacyMissionKeepsItsBinding', line)
            self.assertEqual(2, run_tool(self.repo, catalog, 'Planned')[0])
        for catalog in ('S000', 'S121', 'X026', 'S26'):
            self.assertIn('reason=catalogNotCanonical', run_tool(self.repo, catalog, 'Visible')[1])
        self.assertEqual(self.original, self.library.read_bytes())

    def test_a_published_playable_row_is_never_hidden_or_remarked(self):
        text = self.original.decode('utf-8')
        # Make S026 look published: playable, no validation marker.
        edited = tool.plan_edit(self.repo, text.splitlines(keepends=True), 'S026', 'Visible')[0]
        published = ''.join(edited).replace('ReadinessManifestId: validation.visible.skirmish.s026', 'ReadinessManifestId: publication.skirmish.s026')
        self.library.write_text(published, encoding='utf-8')
        code, line = run_tool(self.repo, 'S026', 'Planned')
        self.assertEqual(2, code)
        self.assertIn('reason=refusingToHideAPublishedRow', line)
        code, line = run_tool(self.repo, 'S026', 'Visible')
        self.assertIn('reason=rowAlreadyPlayableAndNotValidationVisible', line)
        self.assertEqual(published, self.library.read_text(encoding='utf-8'))

    def test_a_mission_without_a_registered_complete_authored_definition_is_refused(self):
        code, line = run_tool(self.repo, 'S027', 'Visible')  # no authored definition copied for S027
        self.assertEqual(2, code)
        self.assertIn('reason=authoredDefinitionMissing', line)
        definition = self.repo / tool.SCENARIOS / 'S026' / 'SkirmishScenario_S026.asset'
        original = definition.read_text(encoding='utf-8')
        definition.write_text(re.sub(r'(?m)^  campaignMap: .*$', '  campaignMap: {fileID: 0}', original), encoding='utf-8')
        self.assertIn('reason=mapOrLayoutNotBound:campaignMap', run_tool(self.repo, 'S026', 'Visible')[1])
        definition.write_text(original.replace('contentVersion: 2', 'contentVersion: 0'), encoding='utf-8')
        self.assertIn('reason=contentVersionNotPositive', run_tool(self.repo, 'S026', 'Visible')[1])
        definition.write_text(original.replace('definitionId: skirmish.s026', 'definitionId: skirmish.s099'), encoding='utf-8')
        self.assertIn('reason=definitionIdMismatch', run_tool(self.repo, 'S026', 'Visible')[1])
        definition.write_text(original, encoding='utf-8')
        registry = self.repo / tool.AUTHORED_CATALOG
        registry.write_text(registry.read_text(encoding='utf-8').replace('05facca50356947ac8f87edf6c44d2cf', '0' * 32), encoding='utf-8')
        self.assertIn('reason=definitionNotRegisteredInExpansionCatalog', run_tool(self.repo, 'S026', 'Visible')[1])
        self.assertEqual(self.original, self.library.read_bytes())

    def test_a_taken_transport_index_is_refused(self):
        text = self.original.decode('utf-8')
        # Give S002's playable row S026's transport index.
        rows, _ = tool.split_rows(text.splitlines(keepends=True))
        lines = text.splitlines(keepends=True)
        start, end = rows['S002']
        for i in range(start, end):
            if lines[i].startswith('    PlayableScenarioIndex:'):
                lines[i] = '    PlayableScenarioIndex: 1026\n'
        self.library.write_text(''.join(lines), encoding='utf-8')
        self.assertIn('reason=transportIndexTakenBy:S002', run_tool(self.repo, 'S026', 'Visible')[1])

    def test_environment_fallbacks_and_missing_arguments(self):
        with unittest.mock.patch.dict(tool.os.environ, {'WARLINE_SKIRMISH_CATALOG': 'S026', 'WARLINE_LIBRARY_STATE': 'Visible'}):
            out = io.StringIO()
            with redirect_stdout(out):
                self.assertEqual(0, tool.main(['--repo', str(self.repo), '--dry-run']))
            self.assertIn('catalog=S026 state=Visible result=Passed', out.getvalue())
        with unittest.mock.patch.dict(tool.os.environ, {}, clear=True):
            out = io.StringIO()
            with redirect_stdout(out):
                self.assertEqual(2, tool.main(['--repo', str(self.repo)]))
            self.assertIn('reason=catalogAndStateRequired', out.getvalue())

    def test_the_real_catalog_parses_into_120_unique_rows_with_the_five_editable_fields(self):
        lines = (ROOT / tool.LIBRARY).read_text(encoding='utf-8').splitlines(keepends=True)
        rows, duplicates = tool.split_rows(lines)
        self.assertEqual([], duplicates)
        self.assertEqual(120, len(rows))
        for catalog, (start, end) in rows.items():
            tool.read_row(lines, start, end)  # raises Refused when a row lacks a field


import unittest.mock  # noqa: E402  (used above via unittest.mock.patch)

if __name__ == '__main__':
    unittest.main()
