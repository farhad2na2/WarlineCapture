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


if __name__ == '__main__':
    unittest.main()
