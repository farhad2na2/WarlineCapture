#!/usr/bin/env python3
"""Evaluate gate 3 (ARIA used the player's unit types) from Skirmish unity.log files.

Owner decision 2026-11-19: on objective missions (FC, BT, CE) gate 3 passes when ARIA used every
unit type in the player's starting force plus any type the objective requires. Production-only
types, producer structures (building.*, the Airport) and aircraft the start does not include do
not count. Base Assault (BA) keeps the full rule: the logged unused= list must be empty.

The log does not separate starting roles, so they come from the setup matrix, the same rows
SkirmishSetupCompiler.BuildSetup turns into setup.Forces (SkirmishSetupCompiler.cs:299-331).
Support roles (support= / automatic=) count as satisfied. The radius-limited
[SkirmishEnemyTrace] event=initial_world roster is only a cross-check (roster=match|mismatch|absent).
"""
import argparse
import csv
from collections import OrderedDict, defaultdict
from pathlib import Path
import re
import sys

from run_aria_queue import GATE_AVAILABLE, GATE_USED, unit_set

REPO = Path(__file__).resolve().parents[2]
MATRIX = REPO / 'Design/Roadmap/Skirmish_Expansion/INITIAL_SETUP_MATRIX.csv'
OBJECTIVE_KINDS = ('FC', 'BT', 'CE')
LAUNCH = re.compile(r'^\[SkirmishLaunch\] queued expanded catalog=(\S+) .*\bsize=(\S+)')
MODEL = re.compile(r'^\[SkirmishGateUnitsModel\] catalog=(\S+) available=.*?\bsupport=(\S*)')
INITIAL = re.compile(r'^\[SkirmishEnemyTrace\] event=initial_world session=(\S+) .*?\bfaction1_units=\[([^\]]*)\]')
PREFIXES = ('[SkirmishGateUnits', '[SkirmishLaunch]', '[SkirmishEnemyTrace] event=initial_world')

# Matrix column -> role id, in BuildSetup's AddForce order (SkirmishSetupCompiler.cs:300-307).
COMBAT_COLUMNS = (
    ('player_rifle', 'role.rifle'),
    ('player_gunner', 'role.gunner'),
    ('player_rocketeer', 'role.rocketeer'),
    ('player_car', 'role.car'),
    ('player_apc', 'role.apc_armored'),
    ('player_tank', 'role.tank'),
    ('player_aa', 'role.aa'),
    ('player_transport_heli', 'role.transport_heli'),
)
# SkirmishRoleKind names in the initial_world trace that do not snake-case to their role id.
KIND_ALIASES = {'AntiAir': 'role.aa'}


def load_matrix(path):
    with Path(path).open(encoding='utf-8-sig', newline='') as stream:
        return {(row['catalog_id'], row['size_id']): row for row in csv.DictReader(stream)}


def count(row, column):
    value = row.get(column, '')
    if not re.fullmatch(r'\d+', value or ''):
        raise ValueError(f'{row.get("catalog_id")}/{row.get("size_id")}: invalid {column}={value!r}')
    return int(value)


def starting_roles(row):
    """Player starting role ids for one matrix row, as BuildSetup builds setup.Forces.

    Support (SkirmishSetupCompiler.cs:320-321): one Tanker, then logistics_support - 1 Oil trucks.
    CE objective trucks (:326-331, SkirmishConvoyTruckDefinition.TryAppendForces) are role.convoy_logic.
    BT evacuees are designated rifles (player_designated_rifles) and already counted as role.rifle.
    Starting structures are not roles: the gate's available list only names producer structures the
    player does NOT start with (SkirmishS002AriaRunHarness.GateUnits.cs:90-109), so none is starting.
    """
    roles = {role for column, role in COMBAT_COLUMNS if count(row, column) > 0}
    support = count(row, 'player_logistics_support')
    if support > 0:
        roles.add('role.tanker')
    if support > 1:
        roles.add('role.logistics_truck')
    if row.get('objective_id') == 'CE' and count(row, 'player_objective_trucks') > 0:
        roles.add('role.convoy_logic')
    return roles


def kind_role(kind):
    if kind in KIND_ALIASES:
        return KIND_ALIASES[kind]
    return 'role.' + re.sub(r'(?<!^)(?=[A-Z])', '_', kind).lower()


def roster_roles(value):
    roles = set()
    for item in filter(None, value.split(',')):
        kind, _, quantity = item.partition(':')
        if not re.fullmatch(r'[A-Za-z]+', kind) or not quantity.isdigit():
            raise ValueError(f'Invalid initial_world roster item: {item!r}')
        if int(quantity) > 0:
            roles.add(kind_role(kind))
    return roles


def scan(lines, source=''):
    """Return gate-3 jobs keyed by (catalog, session), in log order."""
    jobs, sizes, rosters = OrderedDict(), {}, {}
    last_job = {}
    for raw in lines:
        if not raw.startswith(PREFIXES):
            continue
        line = raw.rstrip('\r\n')
        match = LAUNCH.match(line)
        if match:
            sizes[match[1]] = match[2]
            continue
        match = INITIAL.match(line)
        if match:
            rosters[match[1]] = roster_roles(match[2])
            continue
        match = MODEL.match(line)
        if match:
            if match[1] in last_job:
                last_job[match[1]]['support'] = unit_set(match[2])
            continue
        match = GATE_AVAILABLE.match(line)
        if match:
            key = (match[1], match[2])
            available = unit_set(match[3])
            job = jobs.get(key)
            if job is None:
                job = jobs[key] = {'log': source, 'catalog': key[0], 'session': key[1], 'available': available,
                                   'size': sizes.pop(key[0], None), 'support': set(), 'used': None}
            elif job['available'] != available:
                raise ValueError(f'{source}: conflicting available lists for {key}')
            last_job[key[0]] = job
            continue
        match = GATE_USED.match(line)
        if match:
            key = (match[1], match[2])
            job = jobs.get(key)
            if job is None:
                raise ValueError(f'{source}: event=used without event=available for {key}')
            used = (unit_set(match[3]), unit_set(match[4] or ''), unit_set(match[5]))
            if job['used'] is not None and job['used'] != used:
                raise ValueError(f'{source}: conflicting used lists for {key}')
            job['used'] = used
    for job in jobs.values():
        session = job['session']
        job['roster'] = rosters.get(session, rosters.get('expanded-' + session))
    return list(jobs.values())


def evaluate(job, matrix):
    """Add objective, starting roles, unused starting roles and the gate-3 verdict to one job."""
    size = job['size'] or 'Standard'
    row = matrix.get((job['catalog'], size))
    result = dict(job, size=size, size_source='log' if job['size'] else 'default')
    if row is None:
        return dict(result, objective='unknown', starting=set(), unused_starting=set(), roster='absent',
                    gate3='UNKNOWN', reason='no matrix row')
    objective = row['objective_id']
    starting = starting_roles(row)
    roster = 'absent' if job['roster'] is None else ('match' if job['roster'] == starting else 'mismatch')
    result.update(objective=objective, starting=starting, roster=roster)
    if job['used'] is None:
        return dict(result, unused_starting=set(), gate3='INCOMPLETE', reason='no event=used line')
    used, automatic, unused = job['used']
    satisfied = used | automatic | job['support']
    result.update(used_types=used, automatic=automatic, unused_logged=unused,
                  unused_starting=starting - satisfied,
                  not_logged=starting - job['available'])
    if objective in OBJECTIVE_KINDS:
        passed = not result['unused_starting']
        result.update(rule='objective', gate3='PASS' if passed else 'FAIL',
                      reason='all starting roles used' if passed else 'starting roles unused')
    else:
        passed = not unused
        result.update(rule='full', gate3='PASS' if passed else 'FAIL',
                      reason='unused list empty' if passed else 'unused list not empty')
    return result


def joined(values):
    return ';'.join(sorted(values)) if values else '-'


def format_job(result):
    logged = 'used_types' in result
    fields = [
        ('log', result['log']), ('catalog', result['catalog']), ('session', result['session']),
        ('objective', result['objective']), ('size', result['size']), ('rule', result.get('rule', '-')),
        ('starting', joined(result['starting'])),
        ('used', joined(result['used_types']) if logged else '-'),
        ('automatic', joined(result['automatic']) if logged else '-'),
        ('support', joined(result['support'])),
        ('unused_starting', joined(result['unused_starting'])),
        ('unused_logged', joined(result['unused_logged']) if logged else '-'),
        ('roster', result['roster']),
        ('gate3', result['gate3']),
    ]
    if result.get('not_logged'):
        fields.append(('starting_not_in_available', joined(result['not_logged'])))
    if result['size_source'] != 'log':
        fields.append(('size_source', result['size_source']))
    return ' '.join(f'{key}={value}' for key, value in fields)


def summarize(results):
    missions = defaultdict(list)
    for result in results:
        missions[result['catalog']].append(result)
    rows = []
    for catalog in sorted(missions):
        entries = missions[catalog]
        passed = sum(entry['gate3'] == 'PASS' for entry in entries)
        failed = sum(entry['gate3'] == 'FAIL' for entry in entries)
        verdict = 'PASS' if passed == len(entries) else ('FAIL' if passed == 0 else 'MIXED')
        unused = defaultdict(int)
        for entry in entries:
            for role in entry['unused_starting']:
                unused[role] += 1
        rows.append(' '.join((
            f'catalog={catalog}', f'objective={entries[0]["objective"]}', f'jobs={len(entries)}',
            f'pass={passed}', f'fail={failed}', f'other={len(entries) - passed - failed}',
            f'gate3={verdict}',
            'unused_starting=' + (';'.join(f'{role}x{unused[role]}' for role in sorted(unused)) or '-'),
            'roster=' + ';'.join(sorted({entry['roster'] for entry in entries})),
        )))
    return rows


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('logs', nargs='+', type=Path, help='unity.log files')
    parser.add_argument('--matrix', type=Path, default=MATRIX, help='INITIAL_SETUP_MATRIX.csv')
    parser.add_argument('--summary', action='store_true', help='print the per-mission summary after the jobs')
    args = parser.parse_args(argv)
    try:
        matrix = load_matrix(args.matrix)
        results = []
        for path in args.logs:
            with path.open(encoding='utf-8', errors='replace') as log:
                results.extend(evaluate(job, matrix) for job in scan(log, str(path)))
    except (OSError, ValueError, KeyError) as exc:
        print(f'error: {exc}', file=sys.stderr)
        return 1
    for result in results:
        print(format_job(result))
    if args.summary:
        print()
        for row in summarize(results):
            print(row)
    return 0


if __name__ == '__main__':
    sys.exit(main())
