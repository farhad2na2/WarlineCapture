import contextlib
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest

import pad_preflight as tool


def dump(flags=0):
    # 12 x 8 blocks, with a nonzero grid origin AND nonzero bitmap origin cell.
    return dict(cellSize=2, blockCells=2, bitmapColumns=12, bitmapRows=8,
                gridOrigin={'x': 92, 'z': -28}, bitmapOriginCell={'x': 4, 'y': 4},
                playableMin={'x': 100, 'z': -20}, playableMax={'x': 148, 'z': 12},
                bitmapRuns=[flags, 96])


def instance(kind='Supply', at=(118, -6), size=(4, 4), name='a'):
    return dict(id=name, kind=kind, at=at, size=size)


def draft(*pads, **overrides):
    anchors = {'base.player': [100, -20], 'base.enemy': [140, -20]}
    anchors.update(overrides.pop('anchors', {}))
    return dict(instance_pads=pads, pad_slack_m=0, anchors=anchors, **overrides)


def cli(*args):
    stdout, stderr = io.StringIO(), io.StringIO()
    with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
        status = tool.main([str(arg) for arg in args])
    return status, stdout.getvalue(), stderr.getvalue()


class PadPreflightTests(unittest.TestCase):
    def test_all_six_bits_one_block_and_edge_contact(self):
        data = dump()
        data['bitmapRuns'] = [0, 40, 63, 1, 0, 55]
        a, b = tool.preflight(draft(instance(), instance(at=(122, -6), name='b')), data)
        self.assertEqual((a.blocked, sum(a.counts.values())), (1, 1))
        for _, flag in tool.FLAGS:
            self.assertIn(flag + '=1', tool.flags_text(a.counts))
        self.assertTrue(b.passed)
        self.assertFalse(a.overlaps)

    def test_spawn_rally_forms_and_travel_predicate(self):
        d = draft(anchors={'spawn.infantry.player': [108, -10], 'rally.enemy': [138, 2],
                           'spawn.vehicle.enemy': [128, -8]},
                  pads={'rally.enemy': None, 'spawn.vehicle.enemy': [136, -8],
                        'rally.player': {'at': [116, 2], 'size': [2, 2]}})
        for flags, passable in [(1, True), (32, True), (1 | 8, True), (8, False),
                                (1 | 4, False), (2, False), (16, False)]:
            with self.subTest(flags=flags):
                rows = {r.pad.name: r for r in tool.preflight(d, dump(flags))}
                self.assertEqual(4, len(rows))
                self.assertEqual(rows['spawn.vehicle.enemy'].pad.footprint, tool.Rect(129, -14, 143, -2))
                self.assertTrue(all((r.blocked == 0) == passable for r in rows.values()))

    def test_diagonal_spawn_tests_the_footprint_inside_its_bounding_box(self):
        d = draft(instance(at=(114, 6), size=(1, 1)), anchors={'base.enemy': [132, 12]},
                  pads={'spawn.vehicle.player': {'at': [120, 0], 'size': [16, 2]}})
        data = dump()
        data['bitmapRuns'] = [0, 75, 4, 1, 0, 20]
        _, spawn = tool.preflight(d, data)
        self.assertTrue(spawn.passed)
        d['anchors']['base.player'] = [100, 12]
        d['anchors']['base.enemy'] = [132, -20]
        _, spawn = tool.preflight(d, data)
        self.assertEqual(spawn.blocked, 1)
        self.assertEqual(spawn.overlaps, ['a'])

    def test_ring_uses_only_the_surrounding_blocks_and_checks_playable_edge(self):
        data = dump()
        d = draft(instance('BaseBarracks'))
        # A diagonal scenery block immediately outside the HQ footprint.
        data['bitmapRuns'] = [0, 27, 4, 1, 0, 68]
        hq, = tool.preflight(d, data)
        self.assertEqual(hq.blocked, 0)
        self.assertEqual(hq.ring_counts, {0: 7, 4: 1})
        self.assertFalse(hq.passed)
        data = dump()
        self.assertTrue(tool.preflight(d, data)[0].ring_clear)
        # The last bitmap block is only partially playable.
        data['playableMax']['x'] = 147
        edge, = tool.preflight(draft(instance('BaseBarracks', at=(142, -6))), data)
        self.assertTrue(edge.inside)
        self.assertFalse(edge.ring_inside)
        self.assertFalse(edge.passed)

    def test_ring_occupied_by_a_neighbor_without_overlapping_footprints(self):
        hq, other = tool.preflight(draft(instance('BaseBarracks'), instance(at=(122, -6), name='b')), dump())
        self.assertFalse(hq.overlaps)
        self.assertFalse(hq.ring_clear)
        self.assertEqual(hq.ring_neighbors, ['b'])
        self.assertTrue(other.passed)

    def test_bounds_slack_and_symmetric_pair_reporting(self):
        d = draft(instance(at=(108, -4)), instance(at=(118, -4), name='b'),
                  pads={'rally.player': {'at': [144, 8], 'size': [8, 8]}})
        del d['pad_slack_m']
        a, b, rally = tool.preflight(d, dump())
        self.assertEqual(a.pad.slack, 12)
        self.assertFalse(a.inside)
        self.assertTrue(b.inside)
        self.assertEqual((a.overlaps, b.overlaps), (['b'], ['a']))
        self.assertTrue(rally.passed)
        # The two padded footprints just touch at 3 metres per side.
        d['pad_slack_m'] = 3
        self.assertTrue(all(r.passed for r in tool.preflight(d, dump())))
        self.assertFalse(tool.preflight(d, dump(), 3.01)[0].passed)

    def test_partial_blocks_and_missing_bitmap_coverage(self):
        data = dump()
        row, = tool.preflight(draft(instance(at=(100, -20), size=(1, 1))), data)
        self.assertEqual(row.counts, {tool.OUTSIDE: 3, 0: 1})
        self.assertEqual(row.blocked, 3)
        self.assertFalse(row.inside)

    def test_cli_pass_failure_and_bad_input(self):
        with tempfile.TemporaryDirectory() as tmp:
            p, m = Path(tmp) / 'draft.json', Path(tmp) / 'dump.json'
            p.write_text(json.dumps(draft(instance('BaseBarracks'))))
            m.write_text(json.dumps(dump()))
            args = ('--draft', p, '--dump', m)
            status, out, _ = cli(*args, '--pad-slack', 'auto')
            self.assertEqual(status, 0)
            self.assertIn('PREFLIGHT PASS', out.splitlines())
            for value in ('nan', '-1', '65'):
                self.assertEqual(cli(*args, '--pad-slack', value)[0], 1)
            m.write_text(json.dumps(dump(flags=1)))
            status, out, _ = cli(*args)
            self.assertEqual(status, 1)
            self.assertIn('road=1', out)
            self.assertIn('PREFLIGHT FAIL 1/1 pads', out)
            data = dump()
            data['bitmapRuns'] = [0, 95]
            m.write_text(json.dumps(data))
            self.assertIn('bitmap', cli(*args)[2])

    def test_missing_pads_and_wrong_map_cannot_pass(self):
        with self.assertRaisesRegex(ValueError, 'no authored'):
            tool.preflight(draft(), dump())
        with self.assertRaisesRegex(ValueError, 'expected dump'):
            tool.preflight(draft(instance(), campaign_source=next(iter(tool.SOURCES))), dump())


class HistoricalS026Tests(unittest.TestCase):
    """Integration against the exact saved dump and both complete historical drafts."""
    @classmethod
    def setUpClass(cls):
        cls.repo = Path('/Volumes/T7/Projects/WarlineCapture')
        cls.dump_path = tool.DUMP_ROOT / 'lane4-dump-S026/dump.json'
        if not (cls.repo / '.git').exists() or not cls.dump_path.exists():
            raise unittest.SkipTest('requires the WarlineCapture history and saved lane4 S026 dump')
        cls.data = json.loads(cls.dump_path.read_text())

    def historical(self, revision):
        path = 'Tools/Missions/drafts/s026-city-crossroads-ba-ground-established.json'
        return json.loads(subprocess.check_output(['git', '-C', self.repo, 'show', revision + ':' + path]))

    def test_parent_fails_on_both_hq_roads(self):
        rows = tool.preflight(self.historical('21345188d^'), self.data)
        hqs = [r for r in rows if r.pad.kind == 'BaseBarracks']
        self.assertEqual([(r.blocked, sum(r.counts.values())) for r in hqs], [(26, 35), (20, 35)])
        self.assertEqual([sum(n for mask, n in r.counts.items() if mask & 1) for r in hqs], [26, 20])
        self.assertTrue(all(not r.passed for r in hqs))

    def test_repair_passes_both_hq_footprints_but_full_draft_fails_ring_and_spawn(self):
        d = self.historical('21345188d')
        rows = tool.preflight(d, self.data)
        hqs = [r for r in rows if r.pad.kind == 'BaseBarracks']
        candidates = {(c['centreX'], c['centreZ'], c['rectW'], c['rectH']) for c in self.data['candidates']
                      if c['kind'] == 'barracks'}
        for p in d['instance_pads']:
            if p['kind'] == 'BaseBarracks':
                self.assertIn((*p['at'], *p['size']), candidates)
        self.assertEqual([(r.blocked, sum(r.counts.values())) for r in hqs], [(0, 28), (0, 28)])
        self.assertEqual(tool.flags_text(hqs[0].ring_counts), 'scenery=5')
        self.assertEqual(tool.flags_text(hqs[1].ring_counts), 'road=12,sidewalk=8')
        spawn = next(r for r in rows if r.pad.name == 'spawn.infantry.player')
        self.assertEqual((spawn.blocked, sum(spawn.counts.values())), (14, 16))
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 's026.json'
            path.write_text(json.dumps(d))
            status, out, _ = cli('--draft', path)  # auto-select by campaign_source
            self.assertEqual(status, 1)
            self.assertIn(str(self.dump_path), out)
            self.assertIn('PREFLIGHT FAIL', out)


if __name__ == '__main__':
    unittest.main()
