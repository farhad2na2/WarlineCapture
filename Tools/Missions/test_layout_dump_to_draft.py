#!/usr/bin/env python3
"""Tests for layout_dump_to_draft.py with synthetic dumps (nothing here is measured Unity data)."""
import json
import tempfile
import unittest
from pathlib import Path

import layout_dump_to_draft as tool

COLS, ROWS = 30, 20  # 4 m blocks: 120 x 80 m, cellSize 1


def encode(flat):
    runs = []
    for v in flat:
        if runs and runs[-2] == v:
            runs[-1] += 1
        else:
            runs += [v, 1]
    return runs


def walled(gaps):
    flat = []
    for r in range(ROWS):
        for c in range(COLS):
            flat.append(4 if c in (14, 15) and r not in gaps else 0)
    return flat


def cand(kind, x, z, w=8, h=8, name=None):
    return {'kind': kind, 'definition': name or kind, 'rotated': False, 'originX': int(x - w / 2), 'originZ': int(z - h / 2),
            'rectX': int(x - w / 2), 'rectZ': int(z - h / 2), 'rectW': w, 'rectH': h, 'centreX': x, 'centreZ': z}


def dump(gaps=(3, 16), candidates=None):
    flat = walled(gaps)
    return {'catalog': 'S099', 'stride': 16, 'cellSize': 1.0, 'blockCells': 4, 'bitmapColumns': COLS, 'bitmapRows': ROWS,
            'bitmapOriginCell': {'x': 0, 'y': 0}, 'gridOrigin': {'x': 0.0, 'y': 0.0, 'z': 0.0},
            'bitmapRuns': encode(flat),
            'bases': [{'role': 'base.player', 'faction': 1, 'x': 8.0, 'z': 42.0}, {'role': 'base.enemy', 'faction': 2, 'x': 112.0, 'z': 42.0}],
            'candidates': candidates if candidates is not None else [
                cand('staging', 20, 42), cand('staging', 60, 42), cand('staging', 100, 42),
                cand('supply', 12, 30), cand('supply', 104, 30),
                cand('service', 16, 56), cand('service', 100, 56),
                cand('helipad', 12, 72), cand('helipad', 104, 72)]}


class LayoutDump(unittest.TestCase):
    def test_rle_roundtrip(self):
        flat = walled((3, 16))
        grid = tool.decode_bitmap(dump())
        self.assertEqual(flat, [v for row in grid for v in row])
        bad = dump()
        bad['bitmapRuns'] = bad['bitmapRuns'][:-2]
        with self.assertRaises(tool.DumpError):
            tool.decode_bitmap(bad)

    def test_block_world_roundtrip(self):
        data = dump()
        x, z = tool.block_to_world(data, 3, 5)
        self.assertEqual((14.0, 22.0), (x, z))
        self.assertEqual((3, 5), tool.world_to_block(data, x, z))

    def test_pads_are_nearest_distinct_measured_values(self):
        draft, needs = tool.build(dump(), None, 'Assets/x.asset', '/logs/dump.json')
        self.assertEqual([20, 42], draft['anchors']['staging.player'])
        self.assertEqual([100, 42], draft['anchors']['staging.enemy'])
        self.assertEqual({'at': [12, 30], 'anchor': 'economy.player', 'size': [8.0, 8.0]}, draft['pads']['economy.player'])
        self.assertEqual([12, 30], draft['anchors']['economy.player'])
        self.assertEqual([12, 72], draft['anchors']['air.player'])  # helipad used: no airport candidate in the dump
        self.assertIn('measured_from=/logs/dump.json', draft['notes'])
        self.assertTrue(any(n.startswith('spawn.infantry.player') for n in needs))

    def test_overlapping_candidate_is_skipped_and_absent_kind_is_reported(self):
        cs = [cand('staging', 20, 42), cand('staging', 22, 42), cand('staging', 100, 42), cand('supply', 20, 42, 8, 8),
              cand('supply', 104, 30)]
        draft, needs = tool.build(dump(candidates=cs), 'S099', None, 'x', max_distance=60)
        self.assertEqual([20, 42], draft['anchors']['staging.player'])
        self.assertNotIn('economy.player', draft['anchors'])  # only supply candidate overlaps the staging pick
        self.assertIn('economy.player: no supply candidate in the dump within 60 m of the base', needs)
        self.assertIn('service.enemy: no service candidate in the dump within 60 m of the base', needs)
        self.assertNotIn('service.enemy', draft['anchors'])
        self.assertTrue(any(n.startswith('campaign_source') for n in needs))

    def test_two_routes_cross_the_wall_only_through_gaps(self):
        draft, needs = tool.build(dump(), None, 'a', 'x')
        self.assertEqual(2, len(draft['routes']))
        self.assertFalse(any(n.startswith('routes') for n in needs))
        grid = tool.decode_bitmap(dump())
        crossings = []
        for route in draft['routes']:
            pts = [draft['anchors'][r] for r in route['via']]
            self.assertEqual(draft['anchors']['base.player'], pts[0])
            self.assertEqual(draft['anchors']['base.enemy'], pts[-1])
            for (x0, z0), (x1, z1) in zip(pts, pts[1:]):  # sample each leg; no sample may sit in a wall block
                for t in range(0, 101):
                    x, z = x0 + (x1 - x0) * t / 100, z0 + (z1 - z0) * t / 100
                    c, r = tool.world_to_block(dump(), x, z)
                    if c in (14, 15):
                        crossings.append(r)
                        self.assertEqual(0, grid[r][c], 'route enters a wall block at row %d' % r)
        self.assertTrue({3, 16} <= {r for r in crossings} or len(set(crossings)) >= 2)

    def test_single_gap_gives_one_route_and_is_reported(self):
        draft, needs = tool.build(dump(gaps=(16,)), None, 'a', 'x')
        self.assertEqual(1, len(draft['routes']))
        self.assertTrue(any(n.startswith('routes: 1 route') for n in needs))

    def test_closed_wall_gives_no_route_and_nothing_invented(self):
        draft, needs = tool.build(dump(gaps=()), None, 'a', 'x')
        self.assertEqual([], draft['routes'])
        self.assertFalse(any(k.startswith('candidate.route') for k in draft['anchors']))

    def test_missing_base_or_fields_fail(self):
        bad = dump()
        bad['bases'] = bad['bases'][:1]
        with self.assertRaises(tool.DumpError):
            tool.build(bad, None, 'a', 'x')
        old = dump()
        del old['bitmapRuns']
        with self.assertRaises(tool.DumpError):
            tool.load(old)

    def test_cli(self):
        with tempfile.TemporaryDirectory() as tmp:
            src, out = Path(tmp) / 'dump.json', Path(tmp) / 'draft.json'
            src.write_text(json.dumps(dump()))
            self.assertEqual(0, tool.main(['--dump', str(src), '--campaign-source', 'a.asset', '--out', str(out)]))
            written = json.loads(out.read_text())
            self.assertEqual('S099', written['catalog_id'])
            self.assertIn('measured_from=' + str(src.resolve()), written['notes'])
            src.write_text('{}')
            self.assertEqual(1, tool.main(['--dump', str(src)]))


if __name__ == '__main__':
    unittest.main()
