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
        header = 'mission_id,note,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated,other\r\n'
        row1 = 'S002,"a, note",old,old,old,old,old,old,"keep ""quotes"""\r\n'
        row2 = 'S003,"two\nlines",old,old,old,old,old,old,untouched\r\n'
        row3 = 'S004,"untouched, row",old,old,old,old,old,old,"yes"\r\n'
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
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated\nS002,old,old,old,old,old,old\n')
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

    def test_draw_and_no_victory(self):
        table = self.root / 'table.csv'
        table.write_text('mission_id,aria_en,aria_fa,aria_time_s,unity_result,last_log,updated\nS002,x,x,x,x,x,x\n')
        jobs = [('S002', '1', 'en', 'Draw', 20.0)]
        runner.update_table(table, jobs, [('S002', '1', 'en')], self.root / 'unity.log')
        with table.open() as stream:
            row = next(csv.DictReader(stream))
        self.assertEqual((row['aria_en'], row['aria_time_s'], row['unity_result']), ('W0 L1 A0', '', 'loss'))


if __name__ == '__main__':
    unittest.main()
