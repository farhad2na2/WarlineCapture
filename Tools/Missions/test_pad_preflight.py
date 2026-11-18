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


def runway_surface():
    # Finer than the layout grid. One runway cell allows ground and air movement.
    return dict(gridOrigin={'x': 116, 'z': -8}, cellSize=1, width=4, height=4,
                movementMaskRuns=[31, 1, 15, 1, 31, 14])


def instance(kind='Supply', at=(118, -6), size=(4, 4), name='a', faction=1):
    return dict(id=name, kind=kind, at=at, size=size, faction=faction)


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
                self.assertTrue(all(r.passed == passable for r in rows.values()))

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

    def test_ring_warns_about_surrounding_blocks_and_playable_edge(self):
        data = dump()
        d = draft(instance('BaseBarracks'))
        # A diagonal scenery block immediately outside the HQ footprint.
        data['bitmapRuns'] = [0, 27, 4, 1, 0, 68]
        hq, = tool.preflight(d, data)
        self.assertEqual(hq.blocked, 0)
        self.assertEqual(hq.ring_counts, {0: 7, 4: 1})
        self.assertFalse(hq.ring_clear)
        self.assertTrue(hq.passed)
        data = dump()
        self.assertTrue(tool.preflight(d, data)[0].ring_clear)
        # The last bitmap block is only partially playable.
        data['playableMax']['x'] = 147
        edge, = tool.preflight(draft(instance('BaseBarracks', at=(142, -6))), data)
        self.assertTrue(edge.inside)
        self.assertFalse(edge.ring_inside)
        self.assertTrue(edge.passed)

    def test_ring_occupied_by_a_neighbor_without_overlapping_footprints(self):
        hq, other = tool.preflight(draft(instance('BaseBarracks'), instance(at=(122, -6), name='b')), dump())
        self.assertFalse(hq.overlaps)
        self.assertFalse(hq.ring_clear)
        self.assertEqual(hq.ring_neighbors, ['b'])
        self.assertTrue(hq.passed)
        self.assertTrue(other.passed)

    def test_only_authored_footprint_overlap_with_same_faction_fails(self):
        d = draft(instance(at=(108, -4)), instance(at=(118, -4), name='b'),
                  instance(at=(118, -4), name='enemy', faction=2),
                  pads={'rally.player': {'at': [118, -4], 'size': [2, 2]}})
        del d['pad_slack_m']
        a, b, enemy, rally = tool.preflight(d, dump())
        self.assertEqual(a.pad.slack, 12)
        self.assertTrue(all(row.passed for row in (a, b, enemy, rally)))
        d['instance_pads'][1]['at'] = (111, -4)
        a, b, enemy, rally = tool.preflight(d, dump(), 0)
        self.assertEqual((a.overlaps, b.overlaps), (['b'], ['a']))
        self.assertFalse(a.passed)
        self.assertFalse(b.passed)
        self.assertTrue(enemy.passed)
        self.assertTrue(rally.passed)

    def test_construction_search_stays_within_slack_and_reports_best_origin(self):
        data = dump()
        data['bitmapRuns'] = [0, 40, tool.layout.FLAG_ROAD, 1, 0, 55]
        d = draft(instance())
        tight, = tool.preflight(d, data, .5)
        self.assertFalse(tight.passed)
        self.assertEqual((tight.footprint, tight.blocked), (tool.Rect(116, -8, 120, -4), 1))
        moved, = tool.preflight(d, data, 4)
        self.assertEqual((moved.footprint, moved.blocked), (tool.Rect(116, -12, 120, -8), 0))
        self.assertTrue(moved.passed)

    def test_building_mask_rejects_preferred_footprint_and_searches_within_slack(self):
        data, surface = dump(), runway_surface()
        d = draft(instance())
        preferred, = tool.preflight(d, data, surface=surface)
        self.assertEqual((preferred.blocked, preferred.surface_blocked), (0, 1))
        self.assertFalse(preferred.passed)

        moved, = tool.preflight(d, data, 2, surface)
        self.assertEqual(moved.footprint, tool.Rect(118, -8, 122, -4))
        self.assertEqual((moved.preferred_surface_blocked, moved.surface_blocked), (1, 0))
        self.assertTrue(moved.passed)

        surface['movementMaskRuns'] = [31, 16]
        clear, = tool.preflight(d, data, 2, surface)
        self.assertEqual(clear.footprint, clear.pad.footprint)
        self.assertTrue(clear.passed)

    def test_building_mask_does_not_block_travel_or_footprints_outside_surface(self):
        d = draft(instance(at=(122, -6)), pads={'spawn.infantry.enemy': {'at': [118, -6], 'size': [4, 4]}})
        construction, spawn = tool.preflight(d, dump(), surface=runway_surface())
        self.assertTrue(construction.passed)
        self.assertEqual(construction.surface_blocked, 0)
        self.assertTrue(spawn.passed)

    def test_construction_ignores_level_flag_during_startup(self):
        level, = tool.preflight(draft(instance()), dump(tool.layout.FLAG_NOT_LEVEL))
        self.assertEqual(level.blocked, 0)
        self.assertEqual(tool.flags_text(level.counts), 'not-level=1')
        self.assertTrue(level.passed)

    def test_spawn_needs_a_passable_block_centred_inside_its_footprint(self):
        data = dump(tool.layout.FLAG_SCENERY)
        data['bitmapRuns'] = [tool.layout.FLAG_SCENERY, 39, 0, 1, tool.layout.FLAG_SCENERY, 56]
        d = draft(pads={'spawn.infantry.player': {'at': [120, -6], 'size': [10, 4]}})
        edge, = tool.preflight(d, data)
        self.assertEqual(edge.counts[0], 1)
        self.assertEqual(edge.passable, 0)
        self.assertFalse(edge.passed)
        d['pads']['spawn.infantry.player']['at'][0] = 119
        inside, = tool.preflight(d, data)
        self.assertEqual(inside.passable, 1)
        self.assertTrue(inside.passed)

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
            self.assertIn('BuildingPlacement bit unchecked', out)
            self.assertIn('PREFLIGHT PASS', out.splitlines())
            data = dump()
            data['bitmapRuns'] = [0, 27, 4, 1, 0, 68]
            m.write_text(json.dumps(data))
            status, out, _ = cli(*args)
            self.assertEqual(status, 0)
            self.assertIn('WARN scenery=1', out)
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

    def test_cli_reports_surface_exclusions_and_rejects_invalid_coverage(self):
        with tempfile.TemporaryDirectory() as tmp:
            p, m, s = (Path(tmp) / name for name in ('draft.json', 'dump.json', 'surface.json'))
            p.write_text(json.dumps(draft(instance())))
            m.write_text(json.dumps(dump()))
            surface = runway_surface()
            s.write_text(json.dumps(surface))
            args = ('--draft', p, '--dump', m, '--surface', s)
            status, out, _ = cli(*args)
            self.assertEqual(status, 1)
            self.assertIn('no BuildingPlacement cells (preferred/best)', out)
            self.assertIn('| 1/1 | FAIL', out)
            self.assertIn('PREFLIGHT FAIL 1/1 pads', out)

            status, out, _ = cli(*args, '--pad-slack', 2)
            self.assertEqual(status, 0)
            self.assertIn('| 1/0 | PASS', out)
            self.assertIn('PREFLIGHT PASS', out.splitlines())

            surface['movementMaskRuns'] = [31, 15]
            s.write_text(json.dumps(surface))
            status, _, error = cli(*args)
            self.assertEqual(status, 1)
            self.assertIn('invalid decoded surface movementMaskRuns', error)

    def test_missing_pads_and_wrong_map_cannot_pass(self):
        with self.assertRaisesRegex(ValueError, 'no authored'):
            tool.preflight(draft(), dump())
        with self.assertRaisesRegex(ValueError, 'expected dump'):
            tool.preflight(draft(instance(), campaign_source=next(iter(tool.SOURCES))), dump())


class UnityCalibrationTests(unittest.TestCase):
    paths = {
        'S006': 'examples/s006-desert-base-ba-combined-established.json',
        'S026': 'drafts/s026-city-crossroads-ba-ground-established.json',
        'S049': 'drafts/s049-mountain-pass-ba-ground-field.json',
        'S050': 'drafts/s050-mountain-pass-ba-ground-established.json',
    }

    @classmethod
    def setUpClass(cls):
        cls.repo = Path('/Volumes/T7/Projects/WarlineCapture')
        if not (cls.repo / '.git').exists():
            raise unittest.SkipTest('requires the WarlineCapture history')

    def fixture(self, catalog_id, revision='3b910318a'):
        path = 'Tools/Missions/' + self.paths[catalog_id]
        d = json.loads(subprocess.check_output(['git', '-C', self.repo, 'show', f'{revision}:{path}']))
        source = tool.SOURCES[d['campaign_source']]
        dump_path = tool.DUMP_ROOT / f'lane4-dump-{source[0]}' / 'dump.json'
        if not dump_path.exists():
            self.skipTest(f'requires {dump_path}')
        return d, json.loads(dump_path.read_text()), dump_path

    def test_current_s026_starts_with_hq_warnings_and_partial_spawn(self):
        d, data, dump_path = self.fixture('S026')
        rows = tool.preflight(d, data)
        self.assertEqual(len(rows), 27)
        self.assertEqual([r.pad.name for r in rows if not r.passed], [])
        self.assertTrue(all(not r.ring_clear for r in rows if r.pad.kind == 'BaseBarracks'))
        spawn = next(r for r in rows if r.pad.name == 'spawn.infantry.player')
        self.assertEqual((spawn.blocked, spawn.passable), (14, 2))
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / 's026.json'
            path.write_text(json.dumps(d))
            status, out, _ = cli('--draft', path)  # auto-select by campaign_source
            self.assertEqual(status, 0)
            self.assertIn(str(dump_path), out)
            self.assertIn('PREFLIGHT PASS', out.splitlines())

    def test_current_s049_starts(self):
        d, data, _ = self.fixture('S049')
        self.assertEqual([r.pad.name for r in tool.preflight(d, data) if not r.passed], [])

    def test_current_s050_starts(self):
        d, data, _ = self.fixture('S050')
        self.assertEqual([r.pad.name for r in tool.preflight(d, data) if not r.passed], [])

    def test_s026_parent_fails_even_with_relocation(self):
        d, data, _ = self.fixture('S026', '21345188d^')
        preferred = [r for r in tool.preflight(d, data, 0) if r.pad.kind == 'BaseBarracks']
        self.assertEqual([tool.flags_text(row.counts) for row in preferred], ['road=26', 'road=20,scenery=4'])
        hqs = [r for r in tool.preflight(d, data) if r.pad.kind == 'BaseBarracks']
        self.assertEqual([(r.blocked, sum(r.counts.values())) for r in hqs], [(12, 28), (0, 28)])
        self.assertFalse(hqs[0].passed)  # Unity logged the player HQ rejection.
        self.assertTrue(hqs[1].passed)  # The dump admits a relocated enemy HQ.

    def test_s006_blocked_enemy_barracks_matches_unity_rejection(self):
        d, data, _ = self.fixture('S006', '81c4fa26e')
        rows = tool.preflight(d, data)
        hq = next(r for r in rows if r.pad.name == 's006.f2.barracks')
        self.assertEqual(hq.blocked, 0)  # The bitmap misses the native rejection.
        self.assertEqual(tool.flags_text(hq.ring_counts), 'scenery=3')
        self.assertIn('sourceBuildings', hq.ring_failure)
        self.assertFalse(hq.passed)

    def test_s006_blocked_player_infantry_spawn_has_no_passable_centres(self):
        d, data, _ = self.fixture('S006', '1f7da50b4')
        rows = tool.preflight(d, data)
        spawn = next(r for r in rows if r.pad.name == 'spawn.infantry.player')
        self.assertEqual(spawn.passable, 0)
        self.assertFalse(spawn.passed)


if __name__ == '__main__':
    unittest.main()
