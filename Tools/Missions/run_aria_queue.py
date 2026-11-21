#!/usr/bin/env python3
"""Run an ARIA mission queue in one Editor session and record observed results."""
import argparse
import csv
from collections import Counter, defaultdict
from datetime import date, datetime, timezone
import io
import math
import json
import os
from pathlib import Path
import re
import shlex
import statistics
import subprocess
import sys
from typing import NamedTuple, Optional

NUMBER = r'(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?'
JOB = re.compile(rf'^\[SkirmishAriaQueue\] job=(\d+)/(\d+) catalog=(\S+) seed=(\S+) locale=(\S+) result=(Victory|Defeat|Draw|Abort) seconds=({NUMBER})(?: recorded=(\d+) countedWin=\d+)?$')
COMPLETED = re.compile(r'^\[SkirmishAriaQueue\] result=Completed jobs=(\d+) recorded=(\d+) wins=(\d+)(?: .*)?$')
COLUMNS = ('aria_en', 'aria_fa', 'aria_time_s', 'unity_result', 'last_log', 'updated',
           'units_available', 'units_used_by_aria', 'result_return_ok', 'checkpoint_ok')
DISPATCH = re.compile(r'^\[SkirmishAriaQueue\] dispatch job=(\d+)/(\d+) catalog=(\S+) seed=(\S+) locale=(\S+)$')
TERMINAL = re.compile(r'^\[SkirmishTerminalProbe\] result=(Passed|Failed) reason=(.*?) completed=\d+ interventions=\d+$')
GATE_AVAILABLE = re.compile(r'^\[SkirmishGateUnits\] event=available catalog=(\S+) session=(\S+) types=(\S*)$')
# automatic= (support roles that hauled on their own) is optional for older logs.
GATE_USED = re.compile(r'^\[SkirmishGateUnits\] event=used catalog=(\S+) session=(\S+) types=(\S*)(?: automatic=(\S*))? unused=(\S*)$')

ATTEMPT = re.compile(r'\battempt=expanded-([a-z0-9]+)-(-?\d+)-([^\s}]+)')
ELAPSED = re.compile(rf'\belapsed=({NUMBER})(?=\s|$)')
CANDIDATE = re.compile(r'\bsha256=([0-9a-fA-F]{64})(?=\s|$)')
RUN_ID = re.compile(r'^([A-Za-z0-9]+-aria-[^,\s]+),')
LONG_SESSION = re.compile(r'^\[SkirmishLongSessionRegression\] (.*)$')
LONG_SESSION_REQUIRED = ('catalog', 'result', 'allocatorErrors', 'exceptions', 'resultScreen', 'returnToMenu',
                         'expiry', 'elapsed')


class Job(NamedTuple):
    mission: str
    seed: str
    locale: str
    result: str
    match_seconds: Optional[float]
    job_seconds: float
    number: int
    total: int
    line: int
    clock_source: str


def seconds(value):
    if value and re.fullmatch(NUMBER, value):
        result = float(value)
        if math.isfinite(result):
            return result
    return None


def candidate_clock(repo, candidate, key, result, run_id):
    mission, seed, locale = key
    if not repo or not candidate or not re.fullmatch(r'[A-Za-z0-9]+', mission):
        return None, 'unproven'
    path = (repo / 'Design/AgentReports/SkirmishExpansion' / mission /
            '_Evidence/candidates' / candidate / 'runs.csv')
    try:
        stream = path.open(encoding='utf-8-sig', newline='')
    except FileNotFoundError:
        return None, 'unproven'
    with stream:
        reader = csv.DictReader(stream)
        matches = [(seconds(row.get('duration_seconds')), f'{path}:{reader.line_num}')
                   for row in reader
                   if (row.get('catalog_id'), row.get('seed'), row.get('locale')) == key
                   and row.get('result') == result and row.get('executor', 'aria') == 'aria'
                   and (not run_id or row.get('run_id') == run_id)]
    if len(matches) == 1 and matches[0][0] is not None:
        return matches[0]
    return None, 'unproven'


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
    jobs, completed, _ = scan_queue(lines)
    return jobs, completed


def scan_evidence(lines):
    return scan_queue(lines)[2]


def scan_queue(lines, repo=None):
    """Keep evidence within its dispatch even when all summaries arrive at shutdown."""
    evidence, current = {}, None
    summaries, unscoped = [], defaultdict(dict)
    completed = candidate = None
    for line_number, raw in enumerate(lines, 1):
        # The harness echoes the appended CSV row, including its exact run ID.
        if current is not None and raw.startswith(current['mission'] + '-aria-'):
            run = RUN_ID.match(raw)
            if run:
                current['run_id'] = run[1]
        if not raw.startswith(('[SkirmishAriaQueue]', '[SkirmishGateUnits]',
                               '[SkirmishTerminalFrozen]', '[SkirmishTerminalProbe]')):
            continue
        line = raw.rstrip('\r\n')
        if line.startswith('[SkirmishTerminalFrozen]'):
            # Older flat samples and braced initial/delayed samples use the same fields.
            initial = line.partition(' delayed=')[0]
            if 'initial={' in initial:
                initial = initial.partition('initial={')[2].partition('}')[0]
            attempt = ATTEMPT.search(initial)
            if attempt:
                key = (attempt[1], attempt[2])
                elapsed = ELAPSED.search(initial)
                clock = (seconds(elapsed[1]) if elapsed else None, line_number)
                if current is None:
                    unscoped[key].setdefault(attempt[3], clock)
                elif key == (current['mission'].lower(), current['seed']):
                    current.setdefault('clock', clock)
            continue
        if line.startswith('[SkirmishAriaQueue] result=Started '):
            fingerprint = CANDIDATE.search(line)
            candidate = fingerprint[1] if fingerprint else None
        summary = JOB.fullmatch(line)
        if summary:
            summaries.append((summary, line_number))
            continue
        done = COMPLETED.fullmatch(line)
        if done:
            completed = tuple(map(int, done.groups()))
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
                current['unused'] = match.group(5)
                current['units_used_by_aria'] = types
        terminal = TERMINAL.fullmatch(line)
        if current is not None and terminal:
            result, reason = terminal.groups()
            if result == 'Failed':
                current['result_return_ok'] = 'no'
            elif reason == 'frozenResultAnd:MAIN MENU' and current['result_return_ok'] != 'no':
                current['result_return_ok'] = 'yes'
    jobs = []
    # Without dispatch markers, distinct attempts and queue positions must agree
    # before assigning two locales with the same catalog and seed.
    positions = defaultdict(list)
    for summary, _ in summaries:
        index = int(summary[1]), int(summary[2])
        if index not in evidence:
            positions[(summary[3].lower(), summary[4])].append(index)
    unscoped_clocks = {}
    for key, indices in positions.items():
        clocks = unscoped[key].values()
        if len(indices) == 1 or len(indices) == len(clocks):
            unscoped_clocks.update(zip(sorted(indices), clocks))
    for summary, line in summaries:
        number, total, mission, seed, locale, result, wall, recorded = summary.groups()
        key = (mission, seed, locale)
        entry = evidence.get((int(number), int(total)))
        clock = None
        if entry is not None:
            if (entry['mission'], entry['seed'], entry['locale']) != key:
                raise ValueError('Queue summary does not match dispatch')
            clock = entry.get('clock')
        else:
            clock = unscoped_clocks.get((int(number), int(total)))
        elapsed, source = None, 'unproven'
        if clock and clock[0] is not None:
            elapsed, line = clock
            source = 'TerminalFrozen'
        elif recorded != '0':
            elapsed, source = candidate_clock(repo, candidate, key, result, (entry or {}).get('run_id'))
        jobs.append(Job(*key, result, elapsed, float(wall), int(number), int(total), line, source))
    return jobs, completed, evidence


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


def score_missions(jobs, expected, log_path, today=None, evidence=None, replace_set=False):
    by_mission = defaultdict(list)
    for job in jobs:
        by_mission[job[0]].append(job)
    expected_jobs = defaultdict(Counter)
    for mission, seed, locale in expected:
        expected_jobs[mission][(seed, locale)] += 1
    scores = {}
    for mission, observed in by_mission.items():
        values = aggregate_evidence(observed, evidence or {})
        complete = Counter((job[1], job[2]) for job in observed) == expected_jobs[mission]
        if not complete:
            values['units_available'] = values['units_used_by_aria'] = 'unproven'
            for column in ('result_return_ok', 'checkpoint_ok'):
                if values[column] == 'yes':
                    values[column] = 'unproven'
        if replace_set:
            values.update(aria_en='', aria_fa='', unity_result='unproven')
        for locale, column in (('en', 'aria_en'), ('fa-IR', 'aria_fa')):
            results = [job[3] for job in observed if job[2] == locale]
            if results:
                values[column] = f"W{results.count('Victory')} L{results.count('Defeat') + results.count('Draw')} A{results.count('Abort')}"
        wins = [job[4] for job in observed if job[3] == 'Victory']
        values['aria_time_s'] = ''
        if wins:
            values['aria_time_s'] = 'unproven' if None in wins else str(int(statistics.median(wins)))
        outcomes = [job[3] for job in observed]
        if 'Abort' in outcomes:
            values['unity_result'] = 'abort'
        elif any(result in ('Defeat', 'Draw') for result in outcomes):
            values['unity_result'] = 'loss'
        elif complete:
            values['unity_result'] = 'win'
        values['last_log'] = str(log_path.resolve())
        values['updated'] = (today or date.today()).isoformat()
        scores[mission] = values
    return scores


def update_table(path, jobs, expected, log_path, today=None, evidence=None):
    return write_scores(path, score_missions(jobs, expected, log_path, today, evidence))


def write_scores(path, scores):
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
        if mission not in scores:
            output.append(raw)
            continue
        values = scores[mission]
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
        # pgrep -f also matches shells whose command text merely mentions the Unity path (lane 50 launch refusal);
        # only a process whose executable is the Unity binary can own the project.
        exe = subprocess.run(['ps', '-p', pid, '-o', 'comm='], capture_output=True, text=True)
        if exe.returncode or not exe.stdout.strip().endswith('Unity.app/Contents/MacOS/Unity'):
            continue
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


REQUEST_RELATIVE_PATH = Path('Temp/aria-queue/request.json')  # mirrors SkirmishAriaQueueRequest.RelativePath
REUSE_EVIDENCE_SUFFIXES = ('', '.exit', '.dispatch.json', '.dispatch.cs')  # the wrapper's --reuse refuses any existing one


def check_hub():
    if not matching_processes('[U]nity Hub.app/Contents/MacOS/Unity Hub'):
        raise RuntimeError('Unity Hub is not running')


def check_reuse_log_is_new(log):
    existing = [str(log) + suffix for suffix in REUSE_EVIDENCE_SUFFIXES if Path(str(log) + suffix).exists()]
    if existing:
        raise ValueError('--reuse needs a new log directory; refusing to touch prior evidence: ' + ', '.join(existing))


def write_reuse_request(repo, queue, menu_journey, validation_library, now=None, request_id=None):
    """Write the request an open Editor's RunAriaQueueAndExit reads (environment variables cannot reach it).

    Atomic (temp file + replace) so the Editor never reads a half-written file. Returns (path, request_id)."""
    import uuid
    now = now or datetime.now(timezone.utc)
    request_id = request_id or now.strftime('%Y%m%dT%H%M%SZ') + '-' + uuid.uuid4().hex[:8]
    request = {
        'version': 1,
        'requestId': request_id,
        'project': str(repo),
        'queue': str(queue),
        'menuJourney': bool(menu_journey),
        'validationLibrary': bool(validation_library),
        'createdUtc': now.strftime('%Y-%m-%dT%H:%M:%SZ'),
    }
    path = repo / REQUEST_RELATIVE_PATH
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_name(path.name + '.tmp')
    temp.write_text(json.dumps(request, indent=2) + '\n', encoding='utf-8')
    os.replace(temp, path)
    return path, request_id


def drop_unconsumed_request(path, request_id):
    """After the run: a request the Editor never consumed must not linger to be replayed. Returns True when removed."""
    try:
        if json.loads(path.read_text(encoding='utf-8')).get('requestId') != request_id:
            return False
        path.unlink()
        return True
    except (OSError, ValueError):
        return False


def reuse_command(repo, log, timeout):
    return [str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--reuse', '--timeout', str(timeout), '--log', str(log),
            '--', '-executeMethod', 'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit']


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
    # Match the Mac lane: it compiles in Release code optimization (Library/Bee/200b0aE.dag, /optimize+); a
    # Windows Editor left in Debug ran ARIA matches at 3-4 fps vs ~30 fps (2026-11-17). Session-only flag.
    command.append('-releaseCodeOptimization')
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


def read_long_session(lines):
    """Long-session regression markers ([SkirmishLongSessionRegression] key=value ...), in log order.

    A marker is accepted only with every required key; result=Passed is re-checked against its own fields."""
    markers = []
    for line in lines:
        match = LONG_SESSION.match(line.rstrip('\r\n'))
        if not match:
            continue
        fields = dict(part.split('=', 1) for part in match.group(1).split() if '=' in part)
        if any(key not in fields for key in LONG_SESSION_REQUIRED):
            continue
        consistent = (fields['allocatorErrors'] == '0' and fields['exceptions'] == '0' and fields['resultScreen'] == '1'
                      and fields['returnToMenu'] == '1' and fields['expiry'] == '1')
        fields['passed'] = fields['result'] == 'Passed' and consistent
        markers.append(fields)
    return markers


def long_session_exit(markers):
    """0 only when exactly one marker was logged and it passed."""
    return 0 if len(markers) == 1 and markers[0]['passed'] else 1


def read_queue_log(path, repo):
    with path.open(encoding='utf-8', errors='replace') as stream:
        return scan_queue(stream, repo)


def read_deadlines(table):
    if not table.exists():
        return {}
    with table.open(encoding='utf-8-sig', newline='') as stream:
        return {row['mission_id']: seconds(row.get('deadline_s'))
                for row in csv.DictReader(stream) if row.get('mission_id')}


def print_job(job, log, deadlines, entry, classification='counted'):
    elapsed = job.match_seconds
    deadline = deadlines.get(job.mission)
    percent = f'{100 * elapsed / deadline:.2f}%' if elapsed is not None and deadline else 'unproven'
    print(f'class={classification} job={job.number}/{job.total} catalog={job.mission} seed={job.seed} '
          f'locale={job.locale} result={job.result} match_seconds={elapsed if elapsed is not None else "unproven"} '
          f'job_seconds={job.job_seconds} deadline_pct={percent} unused={entry.get("unused", "unproven")} '
          f'log={log}:{job.line} clock_source={job.clock_source} ' +
          ' '.join(f'{key}={entry.get(key, "unproven")}' for key in COLUMNS[-4:]))


def rescan(args, repo):
    if args.rescan_all:
        if not args.rescan_all.is_dir():
            raise ValueError(f'Missing date folder: {args.rescan_all}')
        logs = sorted((*args.rescan_all.glob('*/unity.log'), *args.rescan_all.glob('*/*/unity.log')),
                      key=lambda path: (path.stat().st_mtime_ns, path))
    else:
        logs = [args.rescan]
    table = repo / args.table
    deadlines = read_deadlines(table)
    latest = {}
    for log in logs:
        jobs, _, evidence = read_queue_log(log, repo)
        # A summary can survive even when its dispatch is absent from the log.
        planned = {(j.number, j.total): j[:3] for j in jobs}
        planned.update({index: (e['mission'], e['seed'], e['locale']) for index, e in evidence.items()})
        catalogs = Counter(key[0] for key in planned.values())
        counted = []
        for job in jobs:
            warmup = (args.warmup_first and job.number == 1 and len(planned) == job.total
                      and catalogs[job.mission] == 1)
            entry = evidence.get((job.number, job.total), {})
            print_job(job, log, deadlines, entry, 'warmup' if warmup else 'counted')
            if not warmup:
                counted.append(job)
        if args.write and not args.no_table and counted:
            lookup = {planned[index]: entry for index, entry in evidence.items()}
            latest.update(score_missions(counted, planned.values(), log, evidence=lookup,
                                         replace_set=bool(args.rescan_all)))
    if latest:
        write_scores(table, latest)
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--jobs', type=explicit_jobs)
    parser.add_argument('--missions', type=comma_list)
    parser.add_argument('--seeds', type=comma_list)
    parser.add_argument('--locales', type=comma_list)
    parser.add_argument('--log-dir', type=Path)
    scans = parser.add_mutually_exclusive_group()
    scans.add_argument('--rescan', type=Path)
    scans.add_argument('--rescan-all', type=Path, metavar='DATE_DIR',
                       help='Scan queue logs one or two directories below DATE_DIR, oldest first')
    parser.add_argument('--warmup-first', action='store_true',
                        help='With --rescan, exclude job 1 when its catalog occurs only once in the queue')
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
    parser.add_argument('--reuse', action='store_true',
                        help='drive the project\'s already open Editor through the checked wrapper (invoke_unity_macos.sh --reuse) '
                             'instead of launching one: the queue and --menu-journey/--validation-library travel in '
                             'Temp/aria-queue/request.json, which RunAriaQueueAndExit reads. macOS only; needs a new --log-dir')
    parser.add_argument('--validation-library', action='store_true',
                        help='Editor-only: present Planned authored missions as launchable [VALIDATION] cards so --menu-journey '
                             'can start them before publication (sets WARLINE_VALIDATION_LIBRARY=1; nothing is published; '
                             'log line [SkirmishValidationLibrary] enabled=1 planned=<ids>)')
    parser.add_argument('--long-session', action='store_true',
                        help='long-session runtime regression (once per runtime candidate): no ARIA; periodic infantry '
                             'purchases through presented touch controls until the deadline, then result screen and MAIN MENU '
                             '(sets WARLINE_LONG_SESSION=1). Exactly one job; implies --no-table; exit 0 only when '
                             '[SkirmishLongSessionRegression] reports result=Passed')
    parser.add_argument('--menu-journey', action='store_true',
                        help='start every job from the main menu through presented touch controls (sets WARLINE_MENU_JOURNEY=1); '
                             'the log shows [SkirmishMenuJourney] catalog=<id> result=Passed|Failed. Without the flag the '
                             'caller environment is left unchanged')
    args = parser.parse_args(argv)
    repo = args.repo.resolve()
    if args.warmup_first and not args.rescan:
        parser.error('--warmup-first requires --rescan')
    if args.rescan or args.rescan_all:
        if args.jobs or args.missions or args.seeds or args.locales or args.log_dir or args.dry_run:
            parser.error('Rescanning cannot be combined with queue or dry-run options')
        try:
            return rescan(args, repo)
        except (OSError, ValueError, csv.Error) as exc:
            print(f'Error: {exc}', file=sys.stderr)
            return 1
    if args.write:
        parser.error('--write requires --rescan or --rescan-all')
    if args.log_dir is None:
        parser.error('--log-dir is required for a queue run')
    trio = (args.missions, args.seeds, args.locales)
    if args.jobs is not None and any(value is not None for value in trio):
        parser.error('--jobs cannot be combined with --missions, --seeds or --locales')
    if args.jobs is None and not all(value is not None for value in trio):
        parser.error('Specify --jobs or all of --missions, --seeds and --locales')
    if args.long_session:
        if args.reuse:
            parser.error('--long-session uses a launched Editor (environment); --reuse is not supported')
        args.no_table = True
    if args.reuse and args.launcher != 'macos':
        parser.error('--reuse is macOS only (it drives invoke_unity_macos.sh --reuse)')
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
        if args.long_session and len(jobs) != 1:
            raise ValueError('--long-session runs exactly one job')
        write_queue(queue, jobs)
        if args.jobs is not None and args.dry_run:
            csv.writer(sys.stdout, lineterminator='\n').writerows(jobs)
            return 0
        launch_failed = False
        if not args.dry_run:
            if args.launcher == 'windows':
                print('Windows Editor ownership check skipped; ensure this project is not open in Unity.', file=sys.stderr)
            elif args.reuse:
                check_hub()  # the open Editor itself is checked by the wrapper's dispatch; it never launches one
                check_reuse_log_is_new(log)
            else:
                check_editor_and_hub(repo)
            if not args.reuse:  # --reuse must keep any prior evidence; the wrapper itself refuses an existing log
                log.unlink(missing_ok=True)
            env = os.environ.copy()
            if not args.reuse:  # environment cannot reach an open Editor; --reuse sends these in the request file
                env['WARLINE_ARIA_QUEUE'] = str(queue)
                if args.menu_journey:
                    env['WARLINE_MENU_JOURNEY'] = '1'
                if args.validation_library:
                    env['WARLINE_VALIDATION_LIBRARY'] = '1'
                if args.long_session:
                    env['WARLINE_LONG_SESSION'] = '1'
            if args.reuse:
                request_path, request_id = write_reuse_request(repo, queue, args.menu_journey, args.validation_library)
                try:
                    result = subprocess.run(reuse_command(repo, log, args.timeout), env=env, cwd=repo)
                finally:
                    if drop_unconsumed_request(request_path, request_id):
                        print(f'Editor never consumed {REQUEST_RELATIVE_PATH}; removed it', file=sys.stderr)
                if result.returncode != 0:
                    print(f'Reuse wrapper exited with code {result.returncode}', file=sys.stderr)
            elif args.launcher == 'windows':
                result = subprocess.run(windows_command(repo, log, args.timeout, args.gui_licensing), env=env, cwd=repo)
                launch_failed = result.returncode != 0
                if launch_failed:
                    print(f'Windows Unity wrapper failed with exit code {result.returncode}', file=sys.stderr)
            else:
                subprocess.run([str(repo / 'Tools/CI/invoke_unity_macos.sh'), '--timeout', str(args.timeout),
                            '--log', str(log), '--', '-executeMethod',
                            'Game.Editor.SkirmishS002AriaRunHarness.RunAriaQueueAndExit'], env=env, cwd=repo)
        parsed, completed, evidence = read_queue_log(log, repo)
        lookup = {(e['mission'], e['seed'], e['locale']): e for e in evidence.values()}
        table = repo / args.table
        deadlines = read_deadlines(table)
        for job in parsed:
            print_job(job, log, deadlines, lookup.get(job[:3], {}))
        changed = 0 if args.no_table else update_table(table, parsed, jobs, log, evidence=lookup)
        print(f'Queue: {len(jobs)} jobs; parsed: {len(parsed)}; missions updated: {changed}')
        if completed:
            total, recorded, wins = completed
            print(f'Completed: jobs={total} recorded={recorded} wins={wins}')
        else:
            print('Completed marker missing')
        if args.long_session:
            with open(log, encoding='utf-8', errors='replace') as handle:
                markers = read_long_session(handle)
            for marker in markers:
                print('Long session: ' + ' '.join(f'{key}={value}' for key, value in marker.items()))
            if not markers:
                print('Long session marker missing')
            return 1 if launch_failed else long_session_exit(markers)
        return 0 if not launch_failed and completed and completed[0] == completed[1] else 1
    except (OSError, ValueError, csv.Error, RuntimeError) as exc:
        print(f'Error: {exc}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
