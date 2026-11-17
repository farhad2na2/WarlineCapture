#!/usr/bin/env python3
"""Tests for airport_sites_to_pads.py with synthetic scans (no Unity data is invented)."""
import copy
import json
import tempfile
import unittest
from pathlib import Path

import airport_sites_to_pads as tool
import check_draft as checker

DRAFT = checker.ROOT / 'Tools/Missions/drafts/s027-city-crossroads-ba-air-field.json'


def site(x, z, w=20, h=10, playable=True, rotated=False):
    # cellSize 1: centre is the middle of the effective rect, as the harness writes it.
    return {'origin': {'x': int(x - w / 2), 'y': int(z - h / 2)}, 'rotated': rotated, 'insideLogicalPlayable': playable,
            'effectiveRect': {'x': int(x - w / 2), 'y': int(z - h / 2), 'width': w, 'height': h},
            'centre': {'x': x, 'y': 0.0, 'z': z}}


def scan(sites, catalog='S027', cell=1.0):
    return {'catalog': catalog, 'cellSize': cell, 'sites': sites}


class AirportSites(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.draft = json.loads(DRAFT.read_text())

    def pick(self, sites, draft=None):
        parsed, _ = tool.load_sites(scan(sites), 'S027')
        return tool.pick(draft or self.draft, parsed)

    def test_nearest_valid_site_per_side(self):
        # base.player (1020,750), base.enemy (1100,400); sites placed in open ground well away from pads.
        near_p, far_p = site(1200, 760), site(1400, 760)
        near_e, far_e = site(1240, 400), site(1400, 300)
        picks, report = self.pick([far_p, near_p, far_e, near_e])
        self.assertEqual((1200, 760), (picks['player']['x'], picks['player']['z']))
        self.assertEqual((1240, 400), (picks['enemy']['x'], picks['enemy']['z']))
        self.assertTrue(any('usable' in line for line in report))

    def test_outside_playable_is_never_used(self):
        with self.assertRaises(tool.SiteError) as ctx:
            self.pick([site(1200, 760, playable=False), site(1240, 400, playable=False)])
        self.assertIn('no valid Airport site', str(ctx.exception))

    def test_site_on_a_base_pad_or_route_is_rejected(self):
        on_base = site(1020, 750)  # sits on base.player and the rifle route start
        with self.assertRaises(tool.SiteError):
            self.pick([on_base, site(1240, 400)])

    def test_pair_must_not_overlap(self):
        # One legal site only: both sides would need it.
        with self.assertRaises(tool.SiteError) as ctx:
            self.pick([site(1200, 600)])
        self.assertIn('non-overlapping', str(ctx.exception))

    def test_catalog_mismatch_and_missing_cell_size(self):
        with self.assertRaises(tool.SiteError):
            tool.load_sites(scan([site(1200, 760)], catalog='S051'), 'S027')
        data = scan([site(1200, 760)])
        del data['cellSize']
        with self.assertRaises(tool.SiteError):
            tool.load_sites(data, 'S027')

    def test_cell_size_scales_footprint(self):
        parsed, cell = tool.load_sites(scan([site(1200, 760, 20, 10)], cell=2.0), 'S027')
        self.assertEqual((40.0, 20.0, 2.0), (parsed[0]['w'], parsed[0]['h'], cell))

    def test_applied_draft_keeps_checker_clean_and_marks_measurement(self):
        sites, _ = tool.load_sites(scan([site(1200, 760), site(1240, 400)]), 'S027')
        picks, _ = tool.pick(self.draft, sites)
        updated = tool.apply_picks(copy.deepcopy(self.draft), picks, '/logs/sites.json')
        catalog = checker.rows('Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv')
        matrix = checker.rows('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv')
        before = {e for e in checker.check(self.draft, catalog, matrix)}
        after = {e for e in checker.check(updated, catalog, matrix)}
        self.assertEqual(set(), after - before)
        self.assertEqual([1200, 760], updated['anchors']['air.player'])
        self.assertEqual({'at': [1240, 400], 'anchor': 'air.enemy', 'size': [20, 10]}, updated['pads']['air.enemy'])
        self.assertEqual([20, 10], updated['structure_footprints']['building.airport'])
        self.assertIn('airport measured_from=/logs/sites.json', updated['notes'])
        air_returns = [i for i in updated['instance_pads'] if i['kind'] == 'AirReturn']
        self.assertEqual([[1200, 760], [1240, 400]], [i['at'] for i in air_returns])

    def test_cli_exit_codes_and_write(self):
        with tempfile.TemporaryDirectory() as tmp:
            sites = Path(tmp) / 'sites.json'
            out = Path(tmp) / 'draft.json'
            sites.write_text(json.dumps(scan([site(1200, 760), site(1240, 400)])))
            self.assertEqual(0, tool.main(['--sites', str(sites), '--draft', str(DRAFT), '--out', str(out)]))
            written = json.loads(out.read_text())
            self.assertEqual([1240, 400], written['anchors']['air.enemy'])
            self.assertIn('measured_from=' + str(sites.resolve()), written['notes'])
            sites.write_text(json.dumps(scan([site(1200, 760, playable=False)])))
            self.assertEqual(1, tool.main(['--sites', str(sites), '--draft', str(DRAFT)]))


def cand(x, z, w=20, h=10, level=True, rotated=False, kind='airport'):
    # cellSize 1, gridOrigin 0: rect is cell coordinates, centre is the footprint centre (harness shape).
    return {'kind': kind, 'definition': 'Airport', 'rotated': rotated, 'level': level, 'originX': int(x - w / 2), 'originZ': int(z - h / 2),
            'rectX': int(x - w / 2), 'rectZ': int(z - h / 2), 'rectW': w, 'rectH': h, 'centreX': float(x), 'centreZ': float(z)}


def dump(candidates, catalog='S027', cell=1.0, definitions=None, **extra):
    data = {'catalog': catalog, 'cellSize': cell, 'stride': 8, 'gridOrigin': {'x': 0.0, 'y': 0.0, 'z': 0.0},
            'playableMin': {'x': 0.0, 'y': 0.0, 'z': 0.0}, 'playableMax': {'x': 2048.0, 'y': 40.0, 'z': 1024.0},
            'definitions': definitions if definitions is not None else [
                {'name': 'Barracks', 'kind': 'barracks', 'scanned': False},
                {'name': 'Airport', 'kind': 'airport', 'footprintX': 20, 'footprintY': 10, 'scanned': True}],
            'candidates': candidates}
    data.update(extra)
    return data


class AirportDump(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.draft = json.loads(DRAFT.read_text())

    def test_picks_nearest_per_side_from_dump_candidates(self):
        data = dump([cand(1400, 760), cand(1200, 760), cand(1400, 300), cand(1240, 400), cand(1210, 770, kind='helipad')])
        sites, cell, info = tool.load_dump_sites(data, 'S027')
        picks, report = tool.pick(self.draft, sites)
        self.assertEqual((1200, 760), (picks['player']['x'], picks['player']['z']))
        self.assertEqual((1240, 400), (picks['enemy']['x'], picks['enemy']['z']))
        self.assertEqual(1.0, cell)
        self.assertIn('4 airport candidates (stride 8)', info[0])  # the helipad candidate is not an airport site

    def test_non_level_candidates_are_dropped_never_used(self):
        data = dump([cand(1200, 760, level=False), cand(1400, 760), cand(1240, 400)])
        sites, _, info = tool.load_dump_sites(data, 'S027')
        self.assertEqual(2, len(sites))
        self.assertIn('1 on non-level ground dropped', info[0])
        picks, _ = tool.pick(self.draft, sites)
        self.assertEqual(1400, picks['player']['x'])

    def test_candidate_outside_playable_bounds_is_never_used(self):
        data = dump([cand(1200, 760), cand(1240, 400)], playableMin={'x': 1230.0, 'y': 0.0, 'z': 0.0})
        sites, _, info = tool.load_dump_sites(data, 'S027')
        self.assertIn('1 outside playable', info[0])  # the 1200 rect starts at x=1190 < 1230; the 1240 rect starts at 1230
        with self.assertRaises(tool.SiteError) as ctx:
            tool.pick(self.draft, sites)
        self.assertIn('1 outside playable', str(ctx.exception))  # one usable site left, both sides need a distinct one

    def test_no_airport_definition_is_a_clear_error_not_a_guess(self):
        data = dump([], definitions=[{'name': 'Barracks', 'kind': 'barracks', 'scanned': False},
                                     {'name': 'Helipad', 'kind': 'helipad', 'scanned': True}])
        with self.assertRaises(tool.SiteError) as ctx:
            tool.load_dump_sites(data, 'S027')
        self.assertIn('no Airport building definition', str(ctx.exception))
        self.assertIn('Barracks, Helipad', str(ctx.exception))
        with self.assertRaises(tool.SiteError) as ctx:
            tool.load_dump_sites(dump([], missingKinds='airport'), 'S027')
        self.assertIn('no Airport building definition', str(ctx.exception))

    def test_unscanned_airport_definition_says_how_to_rerun(self):
        data = dump([cand(1200, 760)], definitions=[{'name': 'Airport', 'kind': 'airport', 'scanned': False}])
        with self.assertRaises(tool.SiteError) as ctx:
            tool.load_dump_sites(data, 'S027')
        self.assertIn('WARLINE_LAYOUT_DUMP_KINDS=airport', str(ctx.exception))

    def test_no_airport_candidate_and_catalog_mismatch(self):
        with self.assertRaises(tool.SiteError) as ctx:
            tool.load_dump_sites(dump([cand(1200, 760, kind='helipad')]), 'S027')
        self.assertIn('no airport candidate', str(ctx.exception))
        with self.assertRaises(tool.SiteError):
            tool.load_dump_sites(dump([cand(1200, 760)], catalog='S051'), 'S027')
        with self.assertRaises(tool.SiteError):
            tool.load_dump_sites({'catalog': 'S027', 'cellSize': 1.0}, 'S027')

    def test_truncated_dump_is_flagged_in_the_report(self):
        _, _, info = tool.load_dump_sites(dump([cand(1200, 760)], candidatesTruncated=True), 'S027')
        self.assertIn('TRUNCATED', info[0])

    def test_cell_size_scales_a_rotated_210_by_55_footprint(self):
        data = dump([cand(1200, 760, w=55, h=210, rotated=True)], cell=1.0)
        sites, _, _ = tool.load_dump_sites(data, 'S027')
        self.assertEqual((55.0, 210.0, True), (sites[0]['w'], sites[0]['h'], sites[0]['rotated']))

    def test_cli_dump_writes_draft_and_rejects_bad_arguments(self):
        with tempfile.TemporaryDirectory() as tmp:
            dump_path, out = Path(tmp) / 'dump.json', Path(tmp) / 'draft.json'
            dump_path.write_text(json.dumps(dump([cand(1200, 760), cand(1240, 400)])))
            self.assertEqual(0, tool.main(['--dump', str(dump_path), '--draft', str(DRAFT), '--out', str(out)]))
            written = json.loads(out.read_text())
            self.assertEqual([1200, 760], written['anchors']['air.player'])
            self.assertEqual([1240, 400], written['anchors']['air.enemy'])
            self.assertIn('airport measured_from=' + str(dump_path.resolve()), written['notes'])
            dump_path.write_text(json.dumps(dump([], missingKinds='airport')))
            self.assertEqual(1, tool.main(['--dump', str(dump_path), '--draft', str(DRAFT)]))
            with self.assertRaises(SystemExit):  # --sites and --dump are exclusive
                tool.main(['--dump', str(dump_path), '--sites', str(dump_path), '--draft', str(DRAFT)])

    def test_dump_picks_keep_the_checker_clean(self):
        sites, _, _ = tool.load_dump_sites(dump([cand(1200, 760), cand(1240, 400)]), 'S027')
        picks, _ = tool.pick(self.draft, sites)
        updated = tool.apply_picks(copy.deepcopy(self.draft), picks, '/logs/dump.json')
        catalog = checker.rows('Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv')
        matrix = checker.rows('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv')
        self.assertEqual(set(), set(checker.check(updated, catalog, matrix)) - set(checker.check(self.draft, catalog, matrix)))


class DumpHarnessContract(unittest.TestCase):
    """Pins the C# fields the --dump path reads (the harness cannot be compiled in this lane)."""

    def test_layout_dump_exposes_missing_kinds_and_airport_keyword(self):
        text = (checker.ROOT / 'Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.LayoutDump.cs').read_text(encoding='utf-8')
        self.assertIn('public string missingKinds', text)
        self.assertIn('("airport", new[] { "airport" })', text)
        self.assertIn('" missingKinds="', text)


if __name__ == '__main__':
    unittest.main()
