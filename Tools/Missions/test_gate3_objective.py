import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from gate3_objective import evaluate, kind_role, load_matrix, scan, starting_roles, summarize

HEADER = ('catalog_id,size_id,objective_id,player_rifle,player_gunner,player_rocketeer,player_car,player_apc,'
          'player_tank,player_aa,player_transport_heli,player_logistics_support,player_objective_trucks\n')
MATRIX = HEADER + (
    'S031,Standard,FC,8,0,4,1,1,0,0,0,2,0\n'
    'S019,Standard,CE,8,0,4,0,0,0,0,0,1,3\n'
    'S001,Standard,BA,8,0,0,0,0,0,0,0,0,0\n'
)
AVAILABLE = ('building.helipad;role.apc_armored;role.apc_fast;role.car;role.drone;role.logistics_truck;'
             'role.rifle;role.rocketeer;role.tanker')
MODEL = ('[SkirmishGateUnitsModel] catalog={} available=setup.Forces(player)+x readinessGating=0 '
         'support=role.logistics_truck;role.tanker automatic=UnitResourceHaulOrder(player,2s)')


def job_lines(catalog, session, used, unused, available=AVAILABLE, roster=None):
    lines = [f'[SkirmishLaunch] queued expanded catalog={catalog} definition=x start=FieldBase difficulty=Regular '
             f'size=Standard seed=1 mapHint=0']
    if roster is not None:
        lines.append(f'[SkirmishEnemyTrace] event=initial_world session={session} radius=90 faction1_units=[{roster}] x=1')
    lines += [f'[SkirmishGateUnits] event=available catalog={catalog} session={session} types={available}',
              MODEL.format(catalog),
              f'[SkirmishGateUnits] event=used catalog={catalog} session={session} types={used} '
              f'automatic=role.tanker unused={unused}']
    return lines


class Gate3ObjectiveTest(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.directory = Path(directory.name)
        self.matrix_path = self.directory / 'matrix.csv'
        self.matrix_path.write_text(MATRIX, encoding='utf-8')
        self.matrix = load_matrix(self.matrix_path)

    def evaluate_lines(self, lines):
        return [evaluate(job, self.matrix) for job in scan(lines, 'unity.log')]

    def test_starting_roles_from_matrix(self):
        self.assertEqual(starting_roles(self.matrix[('S031', 'Standard')]),
                         {'role.rifle', 'role.rocketeer', 'role.car', 'role.apc_armored', 'role.tanker',
                          'role.logistics_truck'})
        # CE convoy trucks are starting objective roles; one support actor is only the Tanker.
        self.assertEqual(starting_roles(self.matrix[('S019', 'Standard')]),
                         {'role.rifle', 'role.rocketeer', 'role.tanker', 'role.convoy_logic'})
        self.assertEqual(kind_role('AntiAir'), 'role.aa')
        self.assertEqual(kind_role('ApcArmored'), 'role.apc_armored')

    def test_objective_rule_ignores_production_only_types(self):
        lines = job_lines('S031', 'expanded-s031-1-a', 'role.apc_armored;role.car;role.rifle;role.rocketeer',
                          'building.helipad;role.apc_fast;role.drone',
                          roster='ApcArmored:1,Car:1,LogisticsTruck:1,Rifle:8,Rocketeer:4,Tanker:1')
        [result] = self.evaluate_lines(lines)
        self.assertEqual((result['objective'], result['rule'], result['gate3']), ('FC', 'objective', 'PASS'))
        self.assertEqual(result['unused_starting'], set())
        self.assertEqual(result['roster'], 'match')

    def test_objective_rule_fails_on_unused_starting_role(self):
        lines = job_lines('S031', 'expanded-s031-1-b', 'role.apc_armored;role.rifle;role.rocketeer',
                          'building.helipad;role.apc_fast;role.car;role.drone', roster='Rifle:8')
        [result] = self.evaluate_lines(lines)
        self.assertEqual(result['gate3'], 'FAIL')
        self.assertEqual(result['unused_starting'], {'role.car'})
        self.assertEqual(result['roster'], 'mismatch')

    def test_base_assault_keeps_full_rule(self):
        passed = job_lines('S001', 's1', 'role.rifle', '', available='role.rifle;role.tanker')
        failed = job_lines('S001', 's2', 'role.rifle', 'building.helipad',
                           available='building.helipad;role.rifle;role.tanker')
        results = self.evaluate_lines(passed + failed)
        self.assertEqual([(r['rule'], r['gate3']) for r in results], [('full', 'PASS'), ('full', 'FAIL')])
        self.assertEqual(results[1]['unused_starting'], set())
        self.assertIn('gate3=MIXED', summarize(results)[0])

    def test_used_without_available_is_rejected(self):
        with self.assertRaises(ValueError):
            scan(['[SkirmishGateUnits] event=used catalog=S031 session=x types=role.rifle unused='])

    def test_cli_prints_jobs_and_summary(self):
        log = self.directory / 'unity.log'
        log.write_text('\n'.join(job_lines('S031', 'expanded-s031-1-c', 'role.rifle', 'role.car')) + '\n',
                       encoding='utf-8')
        result = subprocess.run([sys.executable, '-B', str(Path(__file__).with_name('gate3_objective.py')),
                                 '--matrix', str(self.matrix_path), '--summary', str(log)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('catalog=S031 session=expanded-s031-1-c objective=FC', result.stdout)
        self.assertIn('unused_starting=role.apc_armored;role.car;role.rocketeer', result.stdout)
        self.assertIn('catalog=S031 objective=FC jobs=1 pass=0 fail=1 other=0 gate3=FAIL', result.stdout)


if __name__ == '__main__':
    unittest.main()
