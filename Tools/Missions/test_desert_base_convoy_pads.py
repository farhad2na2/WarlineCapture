"""Keep established Desert Base convoy bays clear of opening vehicles."""
import json
from pathlib import Path
import unittest

from check_draft import CONVOY_ROLES
from pad_preflight import Footprint, draft_pads, rectangle


class DesertBaseConvoyPads(unittest.TestCase):
    def test_established_vehicle_pad_does_not_overlap_convoy_bays(self):
        drafts = Path(__file__).resolve().parent / 'drafts'
        for catalog_id in ('s020', 's022', 's024'):
            path, = drafts.glob(f'{catalog_id}-desert-base-ce-*-established-candidate.json')
            draft = json.loads(path.read_text(encoding='utf-8'))
            vehicle = next(pad for pad in draft_pads(draft, draft['pad_slack_m'])
                           if pad.name == 'spawn.vehicle.player')
            # Opening combat and logistics vehicles use the whole vehicle pad before
            # each objective truck is confined to its own bay in the same frame.
            for role in CONVOY_ROLES:
                with self.subTest(catalog_id=catalog_id, bay=role):
                    spec = draft['pads'][role]
                    bay = Footprint(rectangle(spec.get('at', draft['anchors'][role]),
                                              spec['size'], role), vehicle.axis)
                    self.assertFalse(vehicle.shape().overlaps(bay),
                                     f'{vehicle.name} overlaps {role}')


if __name__ == '__main__':
    unittest.main()
