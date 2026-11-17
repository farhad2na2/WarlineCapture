"""Offline tests for the ARIA queue runner."""
import csv
from datetime import date
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from Tools.Missions import run_aria_queue as runner


FIXTURE = '''Unity starting
[SkirmishAriaQueue] job=1/6 catalog=S002 seed=1 locale=en result=Victory seconds=10.5
noise [SkirmishAriaQueue] job=2/6 catalog=BAD seed=1 locale=en result=Victory seconds=1
[SkirmishAriaQueue] job=2/6 catalog=S002 seed=1 locale=fa-IR result=Defeat seconds=20
[SkirmishAriaQueue] job=3/6 catalog=S002 seed=2 locale=en result=Abort seconds=3
[SkirmishAriaQueue] job=4/6 catalog=S003 seed=1 locale=en result=Victory seconds=11.5
[SkirmishAriaQueue] job=5/6 catalog=S003 seed=1 locale=fa-IR result=Victory seconds=14.5
[SkirmishAriaQueue] job=6/6 catalog=S003 seed=2 locale=en result=Victory seconds=12.5
[SkirmishAriaQueue] result=Completed jobs=6 recorded=6 wins=4
Shutdown noise
'''


class QueueTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)

    def test_job_order_and_queue_contents(self):
        jobs = runner.job_list(['S002', 'S003'], ['1', '2'], ['en', 'fa-IR'])
        self.assertEqual(jobs, [
            ('S002', '1', 'en'), ('S002', '1', 'fa-IR'),
            ('S002', '2', 'en'), ('S002', '2', 'fa-IR'),
            ('S003', '1', 'en'), ('S003', '1', 'fa-IR'),
            ('S003', '2', 'en'), ('S003', '2', 'fa-IR'),
        ])
        queue = self.root / 'queue.csv'
        runner.write_queue(queue, jobs)
        self.assertEqual(queue.read_bytes(), b'S002,1,en\nS002,1,fa-IR\nS002,2,en\nS002,2,fa-IR\nS003,1,en\nS003,1,fa-IR\nS003,2,en\nS003,2,fa-IR\n')

    def test_explicit_jobs_and_dry_run_order(self):
        spec = 'S002:130365:en,S006:130370:en,S002:130365:fa-IR'
        jobs = [('S002', '130365', 'en'), ('S006', '130370', 'en'),
                ('S002', '130365', 'fa-IR')]
        self.assertEqual(runner.explicit_jobs(spec), jobs)
        logs = self.root / 'logs'
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                               '--jobs', spec, '--log-dir', str(logs), '--dry-run'],
                              capture_output=True, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        expected = 'S002,130365,en\nS006,130370,en\nS002,130365,fa-IR\n'
        self.assertEqual(proc.stdout, expected)
        self.assertEqual((logs / 'queue.csv').read_text(), expected)

    def test_invalid_job_forms_fail_before_creating_log_dir(self):
        cases = [('S002:1', 'fields'), ('S002:1:en:extra', 'fields'),
                 ('S002::en', 'fields'), ('S002:abc:en', 'integer seed'),
                 ('S002:1:en,S002:1:en', 'Duplicate job')]
        for spec, error in cases:
            with self.subTest(spec=spec):
                logs = self.root / 'logs'
                proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                                       '--jobs', spec, '--log-dir', str(logs), '--dry-run'],
                                      capture_output=True, text=True)
                self.assertNotEqual(proc.returncode, 0)
                self.assertIn(error, proc.stderr)
                self.assertFalse(logs.exists())

    def test_job_forms_are_mutually_exclusive_and_required(self):
        cases = [[], ['--missions', 'S002'],
                 ['--jobs', 'S002:1:en', '--missions', 'S002'],
                 ['--jobs', 'S002:1:en', '--seeds', '1'],
                 ['--jobs', 'S002:1:en', '--locales', 'en']]
        for args in cases:
            with self.subTest(args=args):
                logs = self.root / 'logs'
                proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)),
                                       *args, '--log-dir', str(logs), '--dry-run'],
                                      capture_output=True, text=True)
                self.assertNotEqual(proc.returncode, 0)
                self.assertIn('error:', proc.stderr)
                self.assertFalse(logs.exists())

    def test_log_parser_ignores_noise(self):
        jobs, completed = runner.parse_log(FIXTURE)
        self.assertEqual(len(jobs), 6)
        self.assertEqual(jobs[0], ('S002', '1', 'en', 'Victory', 10.5))
        self.assertEqual(jobs[1][3], 'Defeat')
        self.assertEqual(jobs[2][3], 'Abort')
        self.assertEqual(completed, (6, 6, 4))
        self.assertIsNone(runner.parse_log('unrelated')[1])

    def test_table_preserves_other_fields_and_rows(self):
        table = self.root / 'missions.csv'
        header = 'mission_id,note,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok,other\r\n'
        row1 = 'S002,"a, note",old,old,old,old,old,old,x,x,x,x,"keep ""quotes"""\r\n'
        row2 = 'S003,"two\nlines",old,old,old,old,old,old,x,x,x,x,untouched\r\n'
        row3 = 'S004,"untouched, row",old,old,old,old,old,old,x,x,x,x,"yes"\r\n'
        table.write_bytes((header + row1 + row2 + row3).encode())
        jobs, _ = runner.parse_log(FIXTURE)
        expected = [(j[0], j[1], j[2]) for j in jobs]
        log = self.root / 'unity.log'
        self.assertEqual(runner.update_table(table, jobs, expected, log, date(2026, 11, 16)), 2)
        actual = table.read_bytes().decode()
        self.assertTrue(actual.startswith(header))
        self.assertTrue(actual.endswith(row3))
        self.assertIn('S002,"a, note",W1 L0 A1,W0 L1 A0,10,abort,', actual)
        self.assertIn('S003,"two\nlines",W2 L0 A0,W1 L0 A0,12,win,', actual)
        self.assertIn(',"keep ""quotes"""\r\n', actual)
        rows = list(csv.DictReader(io.StringIO(actual, newline='')))
        self.assertEqual(rows[0]['last_log'], str(log.resolve()))
        self.assertEqual(rows[0]['updated'], '2026-11-16')
        self.assertEqual(rows[1]['other'], 'untouched')
        self.assertEqual(rows[2]['aria_en'], 'old')

    def test_missing_completion_exits_one_in_dry_run(self):
        repo = self.root / 'repo'
        repo.mkdir()
        table = repo / 'missions.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,old,old,old,old,old,old,x,x,x,x\n')
        logs = self.root / 'logs'
        logs.mkdir()
        (logs / 'unity.log').write_text('[SkirmishAriaQueue] job=1/2 catalog=S002 seed=1 locale=en result=Victory seconds=5\n')
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)), '--missions', 'S002',
                               '--seeds', '1', '--locales', 'en,fa-IR', '--repo', str(repo),
                               '--table', 'missions.csv', '--log-dir', str(logs), '--dry-run'],
                              capture_output=True, text=True)
        self.assertEqual(proc.returncode, 1, proc.stdout + proc.stderr)
        self.assertIn('Completed marker missing', proc.stdout)
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual(row['aria_en'], 'W1 L0 A0')
        self.assertEqual(row['aria_fa'], 'old')
        self.assertEqual(row['unity_result'], 'old')

    def test_dispatch_evidence_and_rescan(self):
        log = self.root / 'unity.log'
        log.write_text("""[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=155923 locale=en
[AriaS002Trace] event=gesture frame=505 intent=SelectSquad infantry=20
[SkirmishTerminalProbe] frozen=1 returnInput=Touch target=MAIN MENU point=(1629.00, 153.50)
[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU completed=1 interventions=0
[SkirmishAriaQueue] dispatch job=2/2 catalog=S002 seed=130365 locale=en
[SkirmishAriaQueue] job=1/2 catalog=S002 seed=155923 locale=en result=Victory seconds=272.8 recorded=1 countedWin=1
[SkirmishAriaQueue] job=2/2 catalog=S002 seed=130365 locale=en result=Victory seconds=266.7 recorded=1 countedWin=1
[SkirmishAriaQueue] result=Completed jobs=2 recorded=2 wins=2 sourceUnchanged=1 exitCode=0
""")
        proc = subprocess.run([sys.executable, '-B', str(Path(runner.__file__)), '--rescan', str(log)], capture_output=True, text=True)
        self.assertEqual(proc.returncode, 0, proc.stderr)
        self.assertIn('units_available=unproven units_used_by_aria=unproven result_return_ok=yes checkpoint_ok=unproven', proc.stdout)
        self.assertIn('locale=en units_available=unproven units_used_by_aria=unproven result_return_ok=unproven', proc.stdout)
        evidence = runner.scan_evidence(log.read_text().splitlines())
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        jobs, _ = runner.parse_log(log.read_text())
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['result_return_ok'], 'unproven')
        evidence[(2, 2)]['result_return_ok'] = 'no'
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['result_return_ok'], 'no')

    def test_rescan_write_is_opt_in_and_aggregates(self):
        log = self.root / 'unity.log'
        log.write_text("""[SkirmishAriaQueue] dispatch job=1/1 catalog=S002 seed=155923 locale=en
[SkirmishTerminalProbe] result=Passed reason=frozenResultAnd:MAIN MENU completed=1 interventions=0
[SkirmishAriaQueue] job=1/1 catalog=S002 seed=155923 locale=en result=Victory seconds=272.8 recorded=1 countedWin=1
""")
        table = self.root / 'missions.csv'
        original = 'mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n'
        table.write_text(original)
        args = [sys.executable, '-B', str(Path(runner.__file__)), '--rescan', str(log), '--repo', str(self.root), '--table', 'missions.csv']
        self.assertEqual(subprocess.run(args, capture_output=True).returncode, 0)
        self.assertEqual(table.read_text(), original)
        self.assertEqual(subprocess.run(args + ['--write'], capture_output=True).returncode, 0)
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['result_return_ok'], row['checkpoint_ok'], row['units_available']), ('yes', 'unproven', 'unproven'))

    def test_gate_units_are_attributed_and_aggregated(self):
        lines = '''[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=1 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Infantry;Jet
[SkirmishGateUnits] event=used catalog=S002 session=one types=Airport;Jet unused=Infantry
[SkirmishAriaQueue] dispatch job=2/2 catalog=S002 seed=2 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=two types=Airport;Infantry;Tank
[SkirmishGateUnits] event=used catalog=S002 session=two types=Airport;Tank unused=Infantry
'''.splitlines()
        evidence = runner.scan_evidence(lines)
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        jobs = [('S002', '1', 'en', 'Victory', 1), ('S002', '2', 'en', 'Victory', 2)]
        values = runner.aggregate_evidence(jobs, lookup)
        self.assertEqual(values['units_available'], 'Airport;Infantry;Jet;Tank')
        self.assertEqual(values['units_used_by_aria'], 'Airport')
        del lookup[('S002', '2', 'en')]
        self.assertEqual(runner.aggregate_evidence(jobs, lookup)['units_used_by_aria'], 'unproven')

    def test_gate_units_reject_mismatched_or_inconsistent_evidence(self):
        dispatch = '[SkirmishAriaQueue] dispatch job=1/1 catalog=S002 seed=1 locale=en'
        available = '[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Jet'
        for line in (
            '[SkirmishGateUnits] event=used catalog=S002 session=other types=Jet unused=Airport',
            '[SkirmishGateUnits] event=used catalog=S002 session=one types=Jet unused=',
            '[SkirmishGateUnits] event=used catalog=S003 session=one types=Jet unused=Airport',
            '[SkirmishGateUnits] event=available catalog=S002 session=one types=Jet;Airport',
        ):
            with self.subTest(line=line), self.assertRaises(ValueError):
                runner.scan_evidence([dispatch, available, line])

    def test_gate_units_empty_used_and_incomplete_queue(self):
        lines = '''[SkirmishAriaQueue] dispatch job=1/2 catalog=S002 seed=1 locale=en
[SkirmishGateUnits] event=available catalog=S002 session=one types=Airport;Jet
[SkirmishGateUnits] event=used catalog=S002 session=one types= unused=Airport;Jet
'''.splitlines()
        entry = next(iter(runner.scan_evidence(lines).values()))
        self.assertEqual(entry['units_used_by_aria'], '')
        table = self.root / 'table.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n')
        runner.update_table(table, [('S002', '1', 'en', 'Victory', 1)],
                            [('S002', '1', 'en'), ('S002', '2', 'en')], self.root / 'log',
                            evidence={('S002', '1', 'en'): entry})
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['units_available'], row['units_used_by_aria']), ('unproven', 'unproven'))

    def test_draw_and_no_victory(self):
        table = self.root / 'table.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,units_available,units_used_by_aria,result_return_ok,checkpoint_ok\nS002,x,x,x,x,x,x,x,x,x,x\n')
        jobs = [('S002', '1', 'en', 'Draw', 20.0)]
        runner.update_table(table, jobs, [('S002', '1', 'en')], self.root / 'unity.log')
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['aria_en'], row['aria_time_s'], row['unity_result']), ('W0 L1 A0', '', 'loss'))


if __name__ == '__main__':
    unittest.main()
