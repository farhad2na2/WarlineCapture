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

JOB = re.compile(r'^\[SkirmishAriaQueue\] job=(\d+)/(\d+) catalog=(\S+) seed=(\S+) locale=(\S+) result=(Victory|Defeat|Draw|Abort) seconds=((?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)(?: recorded=\d+ countedWin=\d+)?$')
COMPLETED = re.compile(r'^\[SkirmishAriaQueue\] result=Completed jobs=(\d+) recorded=(\d+) wins=(\d+)(?: .*)?$')
COLUMNS = ('aria_en', 'aria_fa', 'aria_time_s', 'unity_result', 'last_log', 'updated',
           'units_available', 'units_used_by_aria', 'result_return_ok', 'checkpoint_ok')
DISPATCH = re.compile(r'^\[SkirmishAriaQueue\] dispatch job=(\d+)/(\d+) catalog=(\S+) seed=(\S+) locale=(\S+)$')
TERMINAL = re.compile(r'^\[SkirmishTerminalProbe\] result=(Passed|Failed) reason=(.*?) completed=\d+ interventions=\d+$')
GATE_AVAILABLE = re.compile(r'^\[SkirmishGateUnits\] event=available catalog=(\S+) session=(\S+) types=(\S*)$')
# automatic= (support roles that hauled on their own) is optional for older logs.
GATE_USED = re.compile(r'^\[SkirmishGateUnits\] event=used catalog=(\S+) session=(\S+) types=(\S*)(?: automatic=(\S*))? unused=(\S*)$')


def unit_set(value):
    items = value.split(';') if value else []
    if any(not item for item in items) or items != sorted(set(items)):
        raise ValueError('Invalid gate unit list')
    return set(items)


def job_list(missions, seeds, locales):
    return [(mission, seed, locale) for mission in missions for seed in seeds for locale in locales]


def write_queue(path, jobs):
    with path.open('w', encoding='utf-8', newline='') as stream:
        writer = csv.writer(stream, lineterminator='\n')
        writer.writerows(jobs)


def parse_log(log):
    return parse_log_stream(log.splitlines())


def parse_log_stream(lines):
    jobs, completed = [], None
    for line in lines:
        line = line.rstrip('\r\n')
        match = JOB.fullmatch(line)
        if match:
            number, total, catalog, seed, locale, result, seconds = match.groups()
            jobs.append((catalog, seed, locale, result, float(seconds)))
        match = COMPLETED.fullmatch(line)
        if match:
            completed = tuple(map(int, match.groups()))
    return jobs, completed



def scan_evidence(lines):
    """Attribute probe output to dispatch boundaries; summaries are emitted later."""
    evidence, current = {}, None
    for raw in lines:
        line = raw.rstrip('\r\n')
        dispatch = DISPATCH.fullmatch(line)
        if dispatch:
            number, total, mission, seed, locale = dispatch.groups()
            key = (int(number), int(total))
            if key in evidence:
                raise ValueError('Duplicate queue dispatch')
            current = {'mission': mission, 'seed': seed, 'locale': locale,
                       'units_available': 'unproven', 'units_used_by_aria': 'unproven',
                       'result_return_ok': 'unproven', 'checkpoint_ok': 'unproven'}
            evidence[key] = current
        available = GATE_AVAILABLE.fullmatch(line)
        used = GATE_USED.fullmatch(line)
        if current is not None and (available or used):
            match = available or used
            catalog, session, types = match.groups()[:3]
            if catalog != current['mission']:
                raise ValueError('Gate units catalog does not match dispatch')
            parsed = unit_set(types)
            if available:
                if '_unit_session' in current:
                    raise ValueError('Duplicate gate units available')
                current['_unit_session'] = session
                current['units_available'] = types
            else:
                if current.get('_unit_session') != session or '_units_used_seen' in current:
                    raise ValueError('Gate units session mismatch or duplicate used')
                automatic = unit_set(match.group(4) or '')
                unused = unit_set(match.group(5))
                if unused != unit_set(current['units_available']) - parsed - automatic:
                    raise ValueError('Gate units unused list is inconsistent')
                current['_units_used_seen'] = True
                current['units_used_by_aria'] = types
        terminal = TERMINAL.fullmatch(line)
        if current is not None and terminal:
            result, reason = terminal.groups()
            if result == 'Failed':
                current['result_return_ok'] = 'no'
            elif reason == 'frozenResultAnd:MAIN MENU' and current['result_return_ok'] != 'no':
                current['result_return_ok'] = 'yes'
    return evidence


def aggregate_evidence(observed, evidence):
    """A mission passes only when every recorded job has the corresponding proof."""
    entries = [evidence.get((job[0], job[1], job[2])) for job in observed]
    values = {'units_available': 'unproven', 'units_used_by_aria': 'unproven'}
    for column in ('units_available', 'units_used_by_aria'):
        if entries and all(entry and entry[column] != 'unproven' for entry in entries):
            sets = [unit_set(entry[column]) for entry in entries]
            # Available is the union; used is restricted to types observed in every run.
            combined = set.union(*sets) if column == 'units_available' else set.intersection(*sets)
            values[column] = ';'.join(sorted(combined))
    for column in ('result_return_ok', 'checkpoint_ok'):
        results = [entry[column] if entry else 'unproven' for entry in entries]
        values[column] = 'no' if 'no' in results else 'yes' if results and all(x == 'yes' for x in results) else 'unproven'
    return values

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


def update_table(path, jobs, expected, log_path, today=None, evidence=None):
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
        values = aggregate_evidence(observed, evidence or {})
        if Counter((job[1], job[2]) for job in observed) != expected_jobs[mission]:
            values['units_available'] = values['units_used_by_aria'] = 'unproven'
            for column in ('result_return_ok', 'checkpoint_ok'):
                if values[column] == 'yes':
                    values[column] = 'unproven'
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


def windows_command(repo, log, timeout, gui_licensing):
    version_file = repo / 'ProjectSettings/ProjectVersion.txt'
    match = re.search(r'^m_EditorVersion:\s*(\S+)', version_file.read_text(encoding='utf-8'), re.MULTILINE)
    if not match:
        raise ValueError('ProjectVersion.txt has no m_EditorVersion')
    powershell = ['powershell.exe', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File']
    resolved = subprocess.run(powershell + [str(repo / 'Tools/CI/ResolveUnityEditor.ps1'),
                              '-UnityVersion', match.group(1)], capture_output=True, text=True)
    if resolved.returncode or not resolved.stdout.strip():
        raise RuntimeError(f'Unity Editor resolution failed: {resolved.stderr.strip()}')
    command = powershell + [str(repo / 'Tools/CI/InvokeUnity.ps1'), '-UnityExe', resolved.stdout.strip(),
                            '-ProjectPath', str(repo), '-LogFile', str(log),
                            '-TimeoutSeconds', str(timeout)]
    # ARIA plays through the real UI and cannot run headless; InvokeUnity.ps1 adds -batchmode unless
    # -GuiLicensing is passed, so ARIA queues always use GUI mode (gui_licensing kept for compatibility).
    command.append('-GuiLicensing')
    return command + ['-executeMethod', 'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit']


def comma_list(value):
    items = [item.strip() for item in value.split(',')]
    if not all(items):
        raise argparse.ArgumentTypeError('Comma-separated values must not be empty')
    return items


def explicit_jobs(value):
    jobs, seen = [], set()
    for item in value.split(','):
        fields = [field.strip() for field in item.split(':')]
        if len(fields) != 3 or not all(fields):
            raise argparse.ArgumentTypeError('Each job must have nonempty CATALOG:SEED:LOCALE fields')
        mission, seed, locale = fields
        try:
            numeric_seed = int(seed)
        except ValueError:
            raise argparse.ArgumentTypeError(f'Invalid integer seed in job: {item}')
        key = (mission, numeric_seed, locale)
        if key in seen:
            raise argparse.ArgumentTypeError(f'Duplicate job: {item}')
        seen.add(key)
        jobs.append((mission, seed, locale))
    return jobs


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--jobs', type=explicit_jobs)
    parser.add_argument('--missions', type=comma_list)
    parser.add_argument('--seeds', type=comma_list)
    parser.add_argument('--locales', type=comma_list)
    parser.add_argument('--log-dir', type=Path)
    parser.add_argument('--rescan', type=Path)
    parser.add_argument('--write', action='store_true', help='Write rescan results to the table')
    parser.add_argument('--timeout', type=int, default=7200)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--table', type=Path, default=Path('Design/Orchestration/missions.csv'))
    parser.add_argument('--dry-run', action='store_true')
    parser.add_argument('--launcher', choices=('macos', 'windows'),
                        default='windows' if sys.platform == 'win32' else 'macos')
    parser.add_argument('--gui-licensing', action='store_true',
                        help='kept for compatibility; Windows ARIA queues always pass -GuiLicensing')
    parser.add_argument('--no-table', action='store_true', help='Print results without writing the table')
    args = parser.parse_args(argv)
    repo = args.repo.resolve()
    if args.rescan:
        if args.jobs or args.missions or args.seeds or args.locales or args.log_dir or args.dry_run:
            parser.error('--rescan cannot be combined with queue or dry-run options')
        try:
            with args.rescan.open(encoding='utf-8', errors='replace') as stream:
                evidence = scan_evidence(stream)
            # Result summaries occur after all dispatches; retain the existing parser's semantics.
            with args.rescan.open(encoding='utf-8', errors='replace') as stream:
                parsed, completed = parse_log_stream(stream)
            for (number, total), entry in evidence.items():
                print(f"job={number}/{total} catalog={entry['mission']} seed={entry['seed']} locale={entry['locale']} " +
                      ' '.join(f"{key}={entry[key]}" for key in COLUMNS[-4:]))
            if args.write and not args.no_table:
                expected = [(e['mission'], e['seed'], e['locale']) for e in evidence.values()]
                lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
                update_table(repo / args.table, parsed, expected, args.rescan, evidence=lookup)
            return 0 if evidence else 1
        except (OSError, ValueError, csv.Error) as exc:
            print(f'Error: {exc}', file=sys.stderr)
            return 1
    if args.write:
        parser.error('--write requires --rescan')
    if args.log_dir is None:
        parser.error('--log-dir is required for a queue run')
    trio = (args.missions, args.seeds, args.locales)
    if args.jobs is not None and any(value is not None for value in trio):
        parser.error('--jobs cannot be combined with --missions, --seeds or --locales')
    if args.jobs is None and not all(value is not None for value in trio):
        parser.error('Specify --jobs or all of --missions, --seeds and --locales')
    repo, log_dir = args.repo.resolve(), args.log_dir.resolve()
    try:
        try:
            common = os.path.commonpath((str(repo), str(log_dir)))
        except ValueError:  # Different Windows drives cannot share a directory.
            common = None
        if common is not None and os.path.normcase(common) == os.path.normcase(str(repo)):
            raise ValueError('Log directory must be outside the repository')
        if args.timeout <= 0:
            raise ValueError('Timeout must be positive')
        log_dir.mkdir(parents=True, exist_ok=True)
        queue, log = log_dir / 'queue.csv', log_dir / 'unity.log'
        jobs = args.jobs if args.jobs is not None else job_list(args.missions, args.seeds, args.locales)
        write_queue(queue, jobs)
        if args.jobs is not None and args.dry_run:
            csv.writer(sys.stdout, lineterminator='\n').writerows(jobs)
            return 0
        launch_failed = False
        if not args.dry_run:
            if args.launcher == 'windows':
                print('Windows Editor ownership check skipped; ensure this project is not open in Unity.', file=sys.stderr)
            else:
                check_editor_and_hub(repo)
            log.unlink(missing_ok=True)
            env = os.environ.copy()
            env['WARLINE_ARIA_QUEUE'] = str(queue)
            if args.launcher == 'windows':
                result = subprocess.run(windows_command(repo, log, args.timeout, args.gui_licensing), env=env, cwd=repo)
                launch_failed = result.returncode != 0
                if launch_failed:
                    print(f'Windows Unity wrapper failed with exit code {result.returncode}', file=sys.stderr)
            else:
                subprocess.run([str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--timeout', str(args.timeout),
                            '--log', str(log), '--', '-executeMethod',
                            'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'], env=env, cwd=repo)
        with log.open(encoding='utf-8', errors='replace') as stream:
            evidence = scan_evidence(stream)
        with log.open(encoding='utf-8', errors='replace') as stream:
            parsed, completed = parse_log_stream(stream)
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        if args.no_table:
            for mission, seed, locale, result, seconds in parsed:
                print(f'catalog={mission} seed={seed} locale={locale} result={result} seconds={seconds}')
            changed = 0
        else:
            changed = update_table(repo / args.table, parsed, jobs, log, evidence=lookup)
        print(f'Queue: {len(jobs)} jobs; parsed: {len(parsed)}; missions updated: {changed}')
        if completed:
            total, recorded, wins = completed
            print(f'Completed: jobs={total} recorded={recorded} wins={wins}')
        else:
            print('Completed marker missing')
        return 0 if not launch_failed and completed and completed[0] == completed[1] else 1
    except (OSError, ValueError, csv.Error, RuntimeError) as exc:
        print(f'Error: {exc}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
