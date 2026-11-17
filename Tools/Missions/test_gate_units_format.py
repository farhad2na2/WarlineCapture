"""Format contract: the C# [SkirmishGateUnits] lines (SkirmishS002AriaRunHarness.GateUnits.cs) must parse in run_aria_queue."""
import unittest

from Tools.Missions import run_aria_queue as runner


class GateUnitsFormat(unittest.TestCase):
    def test_lines_as_emitted_by_the_harness_parse(self):
        # Same shapes as: " event=available catalog=" + c + " session=" + s + " types=" + string.Join(";", ordinal-sorted set)
        available = ['building.airport', 'building.helipad', 'role.attack_heli_light', 'role.rifle', 'role.tank']
        used = ['role.rifle', 'role.tank']
        unused = ['building.airport', 'building.helipad', 'role.attack_heli_light']
        log = [
            '[SkirmishAriaQueue] dispatch job=1/1 catalog=S027 seed=104731 locale=en',
            '[SkirmishGateUnits] event=available catalog=S027 session=s027-104731-ab12 types=' + ';'.join(available),
            '[SkirmishGateUnitsModel] catalog=S027 available=setup.Forces(player)+SkirmishProductionCatalog.TryRole readinessGating=0',
            '[SkirmishGateUnits] event=used catalog=S027 session=s027-104731-ab12 types=' + ';'.join(used) +
            ' unused=' + ';'.join(unused),
        ]
        evidence = runner.scan_evidence(log)
        row = evidence[(1, 1)]
        self.assertEqual(';'.join(available), row['units_available'])
        self.assertEqual(';'.join(used), row['units_used_by_aria'])

    def test_empty_used_and_unused_parse(self):
        log = ['[SkirmishAriaQueue] dispatch job=1/1 catalog=S027 seed=1 locale=en',
               '[SkirmishGateUnits] event=available catalog=S027 session=x types=role.rifle',
               '[SkirmishGateUnits] event=used catalog=S027 session=x types= unused=role.rifle']
        self.assertEqual('', runner.scan_evidence(log)[(1, 1)]['units_used_by_aria'])


if __name__ == '__main__':
    unittest.main()
