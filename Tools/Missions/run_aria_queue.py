#!/usr/bin/env python3
"""Run an ARIA mission queue in one Editor session and record observed results."""
import argparse
import csv
from collections import Counter, defaultdict
from datetime import date
import io
import os
from pathlib import Path
import re
import shlex
import statistics
import subprocess
import sys

JOB = re.compile(r'^\[SkirmishAriaQueue\] job=(\d+)/(\d+) catalog=(\S+) seed=(\S+) locale=(\S+) result=(Victory|Defeat|Draw|Abort) seconds=((?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)$')
COMPLETED = re.compile(r'^\[SkirmishAriaQueue\] result=Completed jobs=(\d+) recorded=(\d+) wins=(\d+)$')
COLUMNS = ('aria_en', 'aria_fa', 'aria_time_s', 'unity_result', 'last_log', 'updated')


def job_list(missions, seeds, locales):
    return [(mission, seed, locale) for mission in missions for seed in seeds for locale in locales]


def write_queue(path, jobs):
    with path.open('w', encoding='utf-8', newline='') as stream:
        writer = csv.writer(stream, lineterminator='\n')
        writer.writerows(jobs)


def parse_log(log):
    jobs, completed = [], None
    for line in log.splitlines():
        match = JOB.fullmatch(line)
        if match:
            number, total, catalog, seed, locale, result, seconds = match.groups()
            jobs.append((catalog, seed, locale, result, float(seconds)))
        match = COMPLETED.fullmatch(line)
        if match:
            completed = tuple(map(int, match.groups()))
    return jobs, completed


def field_spans(record):
    """Find CSV field spans while retaining original quoting and line endings."""
    end = len(record.rstrip('\r\n'))
    spans, start, quoted, i = [], 0, False, 0
    while i < end:
        char = record[i]
        if char == '"':
            if quoted and i + 1 < end and record[i + 1] == '"':
                i += 2
                continue
            quoted = not quoted
        elif char == ',' and not quoted:
            spans.append((start, i))
            start = i + 1
        i += 1
    spans.append((start, end))
    return spans


def encode_field(value):
    buffer = io.StringIO(newline='')
    csv.writer(buffer, lineterminator='\n').writerow([value])
    return buffer.getvalue()[:-1]


def update_table(path, jobs, expected, log_path, today=None):
    by_mission = defaultdict(list)
    for job in jobs:
        by_mission[job[0]].append(job)
    expected_jobs = defaultdict(Counter)
    for mission, seed, locale in expected:
        expected_jobs[mission][(seed, locale)] += 1
    with path.open('r', encoding='utf-8', newline='') as stream:
        original = stream.read()
    lines = original.splitlines(keepends=True)
    reader = csv.reader(io.StringIO(original, newline=''), strict=True)
    try:
        header = next(reader)
    except StopIteration:
        raise ValueError('Results table has no header')
    header[0] = header[0].lstrip('\ufeff')
    indices = {name: header.index(name) for name in ('mission_id', *COLUMNS)}
    previous_line = reader.line_num
    output = [''.join(lines[:previous_line])]
    changed = 0
    for row in reader:
        raw = ''.join(lines[previous_line:reader.line_num])
        previous_line = reader.line_num
        if len(row) != len(header):
            raise ValueError('Results table has a row with the wrong number of fields')
        mission = row[indices['mission_id']]
        observed = by_mission.get(mission, [])
        if not observed:
            output.append(raw)
            continue
        values = {}
        for locale, column in (('en', 'aria_en'), ('fa-IR', 'aria_fa')):
            results = [job[3] for job in observed if job[2] == locale]
            if results:
                values[column] = f"W{results.count('Victory')} L{results.count('Defeat') + results.count('Draw')} A{results.count('Abort')}"
        wins = [job[4] for job in observed if job[3] == 'Victory']
        values['aria_time_s'] = str(int(statistics.median(wins))) if wins else ''
        outcomes = [job[3] for job in observed]
        if 'Abort' in outcomes:
            values['unity_result'] = 'abort'
        elif any(result in ('Defeat', 'Draw') for result in outcomes):
            values['unity_result'] = 'loss'
        elif expected_jobs[mission] and Counter((job[1], job[2]) for job in observed) == expected_jobs[mission]:
            values['unity_result'] = 'win'
        values['last_log'] = str(log_path.resolve())
        values['updated'] = (today or date.today()).isoformat()
        spans = field_spans(raw)
        if len(spans) != len(header):
            raise ValueError('Could not preserve results table fields')
        for index, value in sorted(((indices[key], value) for key, value in values.items()), reverse=True):
            start, end = spans[index]
            raw = raw[:start] + encode_field(value) + raw[end:]
        output.append(raw)
        changed += 1
    with path.open('w', encoding='utf-8', newline='') as stream:
        stream.write(''.join(output))
    return changed


def matching_processes(pattern):
    result = subprocess.run(['pgrep', '-f', pattern], capture_output=True, text=True)
    if result.returncode not in (0, 1):
        raise RuntimeError('Unable to inspect running processes')
    return result.stdout.split()


def check_editor_and_hub(repo):
    for pid in matching_processes('[U]nity.app/Contents/MacOS/Unity'):
        proc = subprocess.run(['ps', '-p', pid, '-o', 'command='], capture_output=True, text=True)
        if proc.returncode:
            continue
        project_arg = re.escape(str(repo))
        if re.search(r'(?:^|\s)-projectPath\s+(?:"' + project_arg + r'"|\'' + project_arg + r'\'|' + project_arg + r')(?=\s|$)', proc.stdout):
            raise RuntimeError(f'Unity Editor already has this project open (PID {pid})')
        try:
            args = shlex.split(proc.stdout)
        except ValueError:
            raise RuntimeError(f'Unable to inspect Unity Editor process {pid}')
        for i, arg in enumerate(args[:-1]):
            if arg == '-projectPath' and Path(args[i + 1]).resolve() == repo:
                raise RuntimeError(f'Unity Editor already has this project open (PID {pid})')
    if not matching_processes('[U]nity Hub.app/Contents/MacOS/Unity Hub'):
        raise RuntimeError('Unity Hub is not running')


def comma_list(value):
    items = [item.strip() for item in value.split(',')]
    if not all(items):
        raise argparse.ArgumentTypeError('Comma-separated values must not be empty')
    return items


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--missions', type=comma_list, required=True)
    parser.add_argument('--seeds', type=comma_list, required=True)
    parser.add_argument('--locales', type=comma_list, required=True)
    parser.add_argument('--log-dir', type=Path, required=True)
    parser.add_argument('--timeout', type=int, default=7200)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--table', type=Path, default=Path('Design/Orchestration/missions.csv'))
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args(argv)
    repo, log_dir = args.repo.resolve(), args.log_dir.resolve()
    try:
        if os.path.commonpath((str(repo), str(log_dir))) == str(repo):
            raise ValueError('Log directory must be outside the repository')
        if args.timeout <= 0:
            raise ValueError('Timeout must be positive')
        log_dir.mkdir(parents=True, exist_ok=True)
        queue, log = log_dir / 'queue.csv', log_dir / 'unity.log'
        jobs = job_list(args.missions, args.seeds, args.locales)
        write_queue(queue, jobs)
        if not args.dry_run:
            check_editor_and_hub(repo)
            log.unlink(missing_ok=True)
            env = os.environ.copy()
            env['WARLINE_ARIA_QUEUE'] = str(queue)
            subprocess.run([str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--timeout', str(args.timeout),
                            '--log', str(log), '--', '-executeMethod',
                            'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'], env=env, cwd=repo)
        parsed, completed = parse_log(log.read_text(encoding='utf-8', errors='replace'))
        changed = update_table(repo / args.table, parsed, jobs, log)
        print(f'Queue: {len(jobs)} jobs; parsed: {len(parsed)}; missions updated: {changed}')
        if completed:
            total, recorded, wins = completed
            print(f'Completed: jobs={total} recorded={recorded} wins={wins}')
        else:
            print('Completed marker missing')
        return 0 if completed and completed[0] == completed[1] else 1
    except (OSError, ValueError, csv.Error, RuntimeError) as exc:
        print(f'Error: {exc}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
