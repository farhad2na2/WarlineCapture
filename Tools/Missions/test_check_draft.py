#!/usr/bin/env python3
"""Mutation coverage for the offline authoring preflight."""
import copy
import json
import unittest

import check_draft as checker


class DraftChecks(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.example = json.loads((checker.ROOT / 'Tools/Missions/examples/s006-desert-base-ba-combined-established.json').read_text())
        cls.catalog = checker.rows('Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv')
        cls.matrix = checker.rows('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv')

    def test_example(self):
        self.assertEqual([], checker.check(self.example, self.catalog, self.matrix))

    def test_mutations(self):
        def missing_anchor(d): del d['anchors']['economy.player']
        def duplicate_anchor_id(d):
            d['anchors']['foo.bar'] = [20, 20]
            d['anchors']['foo_bar'] = [30, 30]
        def bad_point(d): d['anchors']['base.player'] = [1, 'bad']
        def bad_frame(d): d['anchors']['base.enemy'] = d['anchors']['base.player'][:]
        def bad_pad(d): d['pads']['unknown.player'] = [1, 2]
        def bad_pad_anchor(d): d['pads']['rally.player'] = {'at': [1, 2], 'anchor': 'missing'}
        def bad_pad_size(d): d['pads']['rally.player'] = {'at': [1, 2], 'size': [0, 2]}
        def duplicate_pad(d): d['anchors']['base.extra.player'] = [1, 2]
        def missing_pad(d): del d['pads']['rally.player']
        def bad_route_count(d): d['routes'] = []
        def bad_route_type(d): d['routes'][0] = None
        def bad_route_kind(d): d['routes'][0]['kind'] = 'bogus'
        def bad_route_via(d): d['routes'][0]['via'] = ['missing', 'staging.enemy']
        def bad_route_width(d): d['routes'][0]['width'] = 0
        def repeated_route_point(d): d['routes'][0]['via'].append('staging.player')
        def backtrack(d):
            d['anchors']['backtrack.a'] = [100, 100]
            d['anchors']['backtrack.b'] = [120, 100]
            d['anchors']['backtrack.c'] = [110, 100]
            d['routes'][0]['via'] = ['backtrack.a', 'backtrack.b', 'backtrack.c']
        def duplicate_route(d): d['routes'][1]['via'] = list(reversed(d['routes'][0]['via']))
        def no_main(d): d['routes'][0]['vehicles'] = False
        cases = [
            ('draft', lambda d: d.update({'extra': 1})),
            ('catalog_id', lambda d: d.update(catalog_id='S001')),
            ('area.required', lambda d: d.update(catalog_id='S007')),
            ('area.unexpected', lambda d: d.update(objective_areas={})),
            ('pad.objective', lambda d: d['anchors'].update({'defense.approach.a': [1, 2]})),
            ('campaign_source', lambda d: d.update(campaign_source='')),
            ('draft.anchors', lambda d: d.update(anchors=None)),
            ('draft.point', bad_point), ('anchor.required', missing_anchor),
            ('anchor.identity', duplicate_anchor_id), ('anchor.frame', bad_frame),
            ('pad.kind', bad_pad), ('pad.anchor', bad_pad_anchor), ('pad.size', bad_pad_size),
            ('pad.ambiguous', duplicate_pad), ('pad.required', missing_pad),
            ('route.count', bad_route_count), ('route.type', bad_route_type),
            ('route.kind', bad_route_kind), ('route.via', bad_route_via),
            ('route.width', bad_route_width), ('route.repeat', repeated_route_point),
            ('route.backtrack', backtrack), ('route.distinct', duplicate_route),
            ('route.main', no_main),
        ]
        for rule, mutate in cases:
            with self.subTest(rule=rule):
                draft = copy.deepcopy(self.example)
                mutate(draft)
                self.assertIn(rule, [r for r, _ in checker.check(draft, self.catalog, self.matrix)])
        self.assertIn('matrix', [r for r, _ in checker.check(self.example, self.catalog, [])])

    def with_instance_pads(self):
        draft = copy.deepcopy(self.example)
        a = draft['anchors']
        draft['instance_pads'] = [
            {'id': 'p.supply', 'kind': 'Supply', 'faction': 1, 'at': a['economy.player'], 'size': [15, 4], 'anchor': 'economy.player'},
            {'id': 'p.service', 'kind': 'Service', 'faction': 1, 'at': a['service.player'], 'size': [3, 4], 'anchor': 'service.player'},
        ]
        draft['structure_footprints'] = {'building.refinery.module': [25, 15]}
        return draft

    def test_instance_pads(self):
        base = self.with_instance_pads()
        self.assertEqual([], checker.check(base, self.catalog, self.matrix))
        def second(d, **change): d['instance_pads'][1].update(change)
        cases = [
            ('instance.type', lambda d: d.update(instance_pads={})),
            ('instance.type', lambda d: d['instance_pads'].append(None)),
            ('instance.type', lambda d: second(d, extra=1)),
            ('instance.id', lambda d: second(d, id='')),
            ('instance.id', lambda d: second(d, id='p.supply')),
            ('instance.id', lambda d: second(d, id='x' * 60)),
            ('instance.duplicate', lambda d: second(d, id='rally.player')),
            ('instance.kind', lambda d: second(d, kind='Rally')),
            ('instance.faction', lambda d: second(d, faction=3)),
            ('instance.faction', lambda d: second(d, faction=True)),
            ('draft.point', lambda d: second(d, at=[1])),
            ('instance.size', lambda d: second(d, size=[0, 4])),
            ('instance.anchor', lambda d: second(d, anchor='missing')),
            ('instance.anchor', lambda d: second(d, at=[d['anchors']['service.player'][0] + 1, d['anchors']['service.player'][1]])),
            ('instance.anchor', lambda d: second(d, at=d['anchors']['economy.player'], anchor='economy.player')),
            ('instance.bounds', lambda d: second(d, size=[100000, 4])),
            ('instance.footprint', lambda d: d['structure_footprints'].update({'building.nope': [1, 1]})),
            ('instance.footprint', lambda d: d['structure_footprints'].update({'building.refinery': [0, 1]})),
            ('instance.footprint', lambda d: d.update(structure_footprints=[])),
        ]
        for rule, mutate in cases:
            with self.subTest(rule=rule):
                draft = copy.deepcopy(base)
                mutate(draft)
                self.assertIn(rule, [r for r, _ in checker.check(draft, self.catalog, self.matrix)])


BOUNDS = (900.0, 260.0, 1300.0, 875.0)  # Desert Base playableMin/Max (read from the asset when present)


class ObjectiveDrafts(unittest.TestCase):
    """FC (S007), BT (S013) and CE (S019) drafts on the Desert Base example; geometry here is test data only."""

    @classmethod
    def setUpClass(cls):
        cls.example = json.loads((checker.ROOT / 'Tools/Missions/examples/s006-desert-base-ba-combined-established.json').read_text())
        cls.catalog = checker.rows('Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv')
        cls.matrix = checker.rows('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv')

    def rect(self, x, z):
        return {'rectangle': [[x, z], [x + 20, z + 20]]}

    def make(self, code):
        d = copy.deepcopy(self.example)
        d['catalog_id'] = {'FC': 'S007', 'BT': 'S013', 'CE': 'S019'}[code]
        d['area_measurement'] = {'artifact': 'Design/AgentReports/SkirmishExpansion/test/area-measurement.json'}
        a = d['anchors']
        if code == 'FC':
            d['objective_areas'] = {'FrontlineA': self.rect(1000, 300), 'FrontlineB': self.rect(1000, 500),
                                    'FrontlineC': {'polygon': [[1100, 300], [1140, 300], [1140, 340], [1100, 320]]}}
        if code == 'BT':
            d['objective_areas'] = {'BreakthroughCorridorA': self.rect(1000, 300), 'BreakthroughCorridorB': self.rect(1000, 500),
                                    'BreakthroughExit': self.rect(1200, 400)}
        if code == 'CE':
            d['objective_areas'] = {'ConvoyDestination': self.rect(1200, 400)}
            for i in range(3):
                a['convoy.origin.%d' % (i + 1)] = [920 + i * 20, 300]
                d['pads']['convoy.origin.%d' % (i + 1)] = {'size': [10, 8]}
        if code in ('BT', 'CE'):
            a['defense.approach.a'] = [1150, 320]
            a['defense.approach.b'] = [1150, 560]
            d['pads']['defense.approach.a'] = {'size': [12, 12]}
            d['pads']['defense.approach.b'] = {'size': [12, 12]}
        return d

    def run_check(self, d, bounds=BOUNDS):
        return [r for r, _ in checker.check(d, self.catalog, self.matrix, bounds)]

    def test_valid_drafts(self):
        for code in ('FC', 'BT', 'CE'):
            with self.subTest(code=code):
                d = self.make(code)
                self.assertEqual([], checker.check(d, self.catalog, self.matrix, BOUNDS))

    def test_mutations(self):
        def drop(role): return lambda d: d['objective_areas'].pop(role)
        def polygon(points): return lambda d: d['objective_areas'].update(FrontlineC={'polygon': points})
        cases = [
            ('FC', 'area.required', lambda d: d.pop('objective_areas')),
            ('FC', 'area.required', drop('FrontlineB')),
            ('FC', 'area.role', lambda d: d['objective_areas'].update(BreakthroughExit=self.rect(1, 1))),
            ('FC', 'area.type', lambda d: d['objective_areas'].update(FrontlineA={'circle': [1, 2, 3]})),
            ('FC', 'area.type', lambda d: d['objective_areas'].update(FrontlineA={'rectangle': 5})),
            ('FC', 'draft.point', lambda d: d['objective_areas'].update(FrontlineA={'rectangle': [[1, 'x'], [2, 3]]})),
            ('FC', 'area.geometry', lambda d: d['objective_areas'].update(FrontlineA={'rectangle': [[10, 10], [5, 20]]})),
            ('FC', 'area.geometry', polygon([[1000, 300], [1100, 300]])),
            ('FC', 'area.geometry', polygon([[1000, 300], [1100, 400], [1100, 300], [1000, 400]])),  # bow tie
            ('FC', 'area.geometry', polygon([[1000, 300], [1100, 300], [1200, 300]])),  # zero area
            ('FC', 'area.bounds', lambda d: d['objective_areas'].update(FrontlineA=self.rect(1290, 300))),
            ('FC', 'area.distinct', lambda d: d['objective_areas'].update(FrontlineB=d['objective_areas']['FrontlineA'])),
            ('FC', 'area.measurement', lambda d: d.pop('area_measurement')),
            ('FC', 'area.measurement', lambda d: d['area_measurement'].update(artifact='/etc/passwd')),
            ('FC', 'pad.objective', lambda d: d['anchors'].update({'defense.approach.a': [1150, 320]})),
            ('BT', 'area.required', drop('BreakthroughExit')),
            ('BT', 'pad.required', lambda d: d['anchors'].pop('defense.approach.b')),
            ('BT', 'pad.size_required', lambda d: d['pads'].pop('defense.approach.a')),
            ('BT', 'pad.objective', lambda d: d['anchors'].update({'convoy.origin.1': [920, 300]})),
            ('CE', 'area.required', lambda d: d.update(objective_areas={'FrontlineA': self.rect(1000, 300)})),
            ('CE', 'pad.required', lambda d: d['anchors'].pop('convoy.origin.3')),
            ('CE', 'pad.size_required', lambda d: d['pads'].pop('convoy.origin.2')),
            ('CE', 'pad.convoy_overlap', lambda d: d['anchors'].update({'convoy.origin.2': [925, 300]})),
            ('CE', 'pad.required', lambda d: d['anchors'].pop('defense.approach.a')),
            ('CE', 'route.convoy', lambda d: d.update(routes=d['routes'][:1])),
        ]
        for code, rule, mutate in cases:
            with self.subTest(code=code, rule=rule):
                d = self.make(code)
                mutate(d)
                self.assertIn(rule, self.run_check(d))

    def test_ce_routes_need_distinct_middles(self):
        d = self.make('CE')
        # Both routes keep the same middle waypoints (and one reversed): not two distinct truck routes.
        d['routes'] = [d['routes'][0], copy.deepcopy(d['routes'][0])]
        d['routes'][1]['kind'] = 'FlankSouthSweep'
        d['routes'][1]['via'] = ['staging.player', 'highway.near', 'highway.mid', 'highway.far', 'staging.enemy'][::-1]
        self.assertIn('route.convoy', self.run_check(d))
        d = self.make('CE')
        d['routes'][1]['vehicles'] = False
        d['routes'][2]['vehicles'] = False
        self.assertIn('route.convoy', self.run_check(d))

    def test_playable_bounds_from_asset(self):
        bounds = checker.playable_bounds(self.example)
        if bounds is not None:  # the asset is outside some sparse checkouts
            self.assertEqual((900.0, 260.0, 1300.0, 875.0), bounds)

    def test_ba_rejects_objective_fields(self):
        d = copy.deepcopy(self.example)
        d['objective_areas'] = {'FrontlineA': self.rect(1000, 300)}
        d['area_measurement'] = {'artifact': 'x'}
        self.assertEqual({'area.unexpected'}, set(self.run_check(d)))


if __name__ == '__main__':
    unittest.main()
