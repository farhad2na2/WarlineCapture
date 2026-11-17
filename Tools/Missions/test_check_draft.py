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
            ('objective', lambda d: d.update(catalog_id='S007')),
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


if __name__ == '__main__':
    unittest.main()
