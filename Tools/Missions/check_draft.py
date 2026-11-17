#!/usr/bin/env python3
"""Draft-only preflight for SkirmishMissionAuthoringEntry; PASS is not Unity validation."""
import argparse
import csv
import json
import math
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
ENTRY = 'Assets/Game/Scripts/Editor/Skirmish/SkirmishMissionAuthoringEntry.cs'
BINDER = 'Assets/Game/Scripts/Configs/Skirmish/SkirmishMeasuredLayoutBinder.cs'
NEEDS_UNITY = ('source asset/metadata, map bounds and terrain, placement and navigation, '
               'shared profiles, compilation, fingerprints, registration and native play')


def rows(path):
    with (ROOT / path).open(newline='', encoding='utf-8-sig') as f:
        return list(csv.DictReader(f))


def check(draft, catalog, matrix):
    errors = []
    def fail(rule, message):
        errors.append((rule, message))
    # SkirmishMissionAuthoringEntry.cs:119-126 (JObject and allowed top-level keys).
    if not isinstance(draft, dict):
        return [('draft', 'JSON root must be an object')]
    for key in draft.keys() - {'catalog_id', 'campaign_source', 'anchors', 'pads', 'routes', 'notes'}:
        fail('draft', 'unknown key ' + key)
    # SkirmishMissionDispatch.cs:61-72; SkirmishMissionAuthoringEntry.cs:128-135.
    cid = draft.get('catalog_id')
    canonical = isinstance(cid, str) and re.fullmatch(r'S[0-9]{3}', cid) and 1 <= int(cid[1:]) <= 120
    if not canonical:
        fail('catalog_id', 'canonical S001..S120 required')
    elif cid in {'S001', 'S002', 'S003', 'S004', 'S005', 'S025', 'S073'}:
        fail('catalog_id', 'coded or legacy binding is reserved')
    # SkirmishMissionAuthoringEntry.cs:138-143; SkirmishSetupMatrixTable.cs:76 (packaged row).
    row = next((r for r in matrix if r['catalog_id'] == cid and r['size_id'] == 'Standard'), None)
    if canonical and row is None:
        fail('matrix', 'no Standard matrix row')
    if row and row['objective_id'] != 'BA':
        fail('objective', 'authoring supports Base Assault only')
    # SkirmishMissionAuthoringEntry.cs:146-148: source loading itself needs Unity.
    if not isinstance(draft.get('campaign_source'), str) or not draft['campaign_source']:
        fail('campaign_source', 'source asset path required')
    # SkirmishMissionAuthoringEntry.cs:437-446, 608-615.
    raw = draft.get('anchors')
    if not isinstance(raw, dict):
        fail('draft.anchors', 'object of role to [x,z] required')
        raw = {}
    points = {}
    def point(value, name):
        if (not isinstance(value, list) or len(value) != 2 or
            any(isinstance(v, bool) or not isinstance(v, (int, float)) for v in value)):
            fail('draft.point', name + ' must be [x,z] numbers')
            return None
        try:
            if not all(math.isfinite(v) and abs(v) <= 3.402823466e38 for v in value):
                raise ValueError()
        except (OverflowError, ValueError):
            fail('draft.point', name + ' requires finite float values')
            return None
        return tuple(value)
    for role, value in raw.items():
        if not role or any(c.isspace() for c in role):
            fail('draft.anchors', 'invalid role ' + repr(role))
        p = point(value, 'anchors.' + role)
        if p is not None:
            points[role] = p
    # SkirmishMissionAuthoringEntry.cs:445-446; SkirmishSetupCompiler.cs:569-589, 620-646.
    required = ['base.player', 'base.enemy', 'staging.player', 'staging.enemy',
                'economy.player', 'economy.enemy', 'service.player', 'service.enemy']
    if row and row['start_profile'] == 'E':
        required += ['air.player', 'air.enemy']
    for role in required:
        if role not in points:
            fail('anchor.required', 'missing ' + role)
    # SkirmishMeasuredLayoutBinder.cs:131-134: generated IDs replace dots with underscores.
    ids = [role.replace('.', '_') for role in points]
    if len(ids) != len(set(ids)):
        fail('anchor.identity', 'roles produce duplicate anchor IDs')
    # SkirmishMissionAuthoringEntry.cs:477-480.
    if 'base.player' in points and 'base.enemy' in points and math.dist(points['base.player'], points['base.enemy']) < 1:
        fail('anchor.frame', 'bases are less than one metre apart')

    # SkirmishMissionAuthoringEntry.cs:576-607: role prefixes determine kind and footprint.
    prefixes = [('base.', 'base', (16, 12)), ('staging.', 'staging', (20, 14)),
                ('spawn.infantry.', 'infantry', (12, 10)), ('spawn.vehicle.', 'vehicle', (14, 12)),
                ('rally.', 'rally', (10, 8)), ('air.', 'air', (16, 16)),
                ('economy.', 'supply', (14, 12)), ('service.', 'service', (12, 10)),
                ('supply.expansion.', 'expansion', (14, 12))]
    def kind(role):
        return next(((k, size) for prefix, k, size in prefixes if role.startswith(prefix)), (None, None))
    pads = {role: (p, role, kind(role)[1]) for role, p in points.items() if kind(role)[0]}
    # SkirmishMissionAuthoringEntry.cs:449-474. Non-object pads are ignored by C#.
    rawpads = draft.get('pads')
    if isinstance(rawpads, dict):
        for role, spec in rawpads.items():
            k, size = kind(role)
            if not k:
                fail('pad.kind', 'unrecognized pad role ' + role)
                continue
            anchor = role if role in points else ('staging.player' if role.endswith('.player') else 'staging.enemy' if role.endswith('.enemy') else None)
            if spec is None:
                at = points.get(role)
                if at is None:
                    fail('pad.anchor', role + ' null requires same-role anchor')
            elif isinstance(spec, dict):
                at = point(spec['at'], 'pads.' + role + '.at') if 'at' in spec else points.get(role)
                if at is None and 'at' not in spec:
                    fail('pad.anchor', role + ' needs at or same-role anchor')
                if spec.get('anchor') is not None:
                    anchor = spec['anchor']
                if spec.get('size') is not None:
                    size = point(spec['size'], 'pads.' + role + '.size')
            else:
                at = point(spec, 'pads.' + role)
            if not isinstance(anchor, str) or anchor not in points:
                fail('pad.anchor', role + ' has no anchor role')
            if size is not None and (size[0] <= 0 or size[1] <= 0):
                fail('pad.size', role + ' size must be positive')
            if at is not None and size is not None and isinstance(anchor, str) and anchor in points:
                pads[role] = (at, anchor, size)
    # SkirmishMeasuredLayoutBinder.cs:155-185, 198-207; SkirmishMapLayoutBuilder.cs:367-433.
    kinds = {}
    for role in pads:
        faction = 'player' if role.endswith('.player') else 'enemy' if role.endswith('.enemy') else None
        if faction:
            key = (faction, kind(role)[0])
            if key in kinds:
                fail('pad.ambiguous', role + ' duplicates faction/kind of ' + kinds[key])
            kinds[key] = role
    for side in ('player', 'enemy'):
        needed = ['base', 'staging', 'vehicle', 'rally', 'supply', 'service']
        if row and any(int(row[side + '_' + unit]) > 0 for unit in ('rifle', 'gunner', 'rocketeer')):
            needed.append('infantry')
        if row and (int(row[side + '_transport_heli']) > 0 or row['start_profile'] == 'E'):
            needed.append('air')
        for k in needed:
            if (side, k) not in kinds:
                fail('pad.required', 'missing ' + side + ' ' + k + ' pad')
    # SkirmishMissionAuthoringEntry.cs:545-558; SkirmishMeasuredLayoutBinder.cs:255-313.
    routes = draft.get('routes')
    if not isinstance(routes, list) or len(routes) < 2:
        fail('route.count', 'at least two routes required')
        routes = routes if isinstance(routes, list) else []
    paths, mains = set(), 0
    for i, route in enumerate(routes):
        label = 'route ' + str(i + 1)
        if not isinstance(route, dict):
            fail('route.type', label + ' must be an object')
            continue
        k = route.get('kind')
        if k not in ('MainHighway', 'FlankNorthRuins', 'FlankSouthSweep'):
            fail('route.kind', label + ' has invalid kind')
        via = route.get('via')
        if not isinstance(via, list) or len(via) < 2 or any(not isinstance(x, str) or x not in points for x in via):
            fail('route.via', label + ' needs at least two known anchor roles')
            continue
        width = route.get('width', 12 if route.get('vehicles', True) else 6)
        if isinstance(width, bool) or not isinstance(width, (int, float)) or not math.isfinite(width) or width <= 0:
            fail('route.width', label + ' width must be finite and positive')
        if any(not isinstance(route[key], bool) for key in ('vehicles', 'aircraft') if key in route and route[key] is not None):
            fail('route.type', label + ' vehicle/aircraft flags must be boolean')
        if k == 'MainHighway' and route.get('vehicles', True) is not False:
            mains += 1
        coords = [points[x] for x in via]
        if len(set(coords)) != len(coords):
            fail('route.repeat', label + ' repeats a position')
            continue
        simplified = list(coords)
        for n in range(len(simplified)-2, 0, -1):
            a, b, c = simplified[n-1:n+2]
            cross = (b[0]-a[0])*(c[1]-b[1])-(b[1]-a[1])*(c[0]-b[0])
            if cross == 0:
                dot = (b[0]-a[0])*(c[0]-b[0])+(b[1]-a[1])*(c[1]-b[1])
                if dot <= 0:
                    fail('route.backtrack', label + ' backtracks')
                else:
                    simplified.pop(n)
        path = tuple(simplified)
        canonical_path = min(path, path[::-1])
        if canonical_path in paths:
            fail('route.distinct', label + ' duplicates an approach')
        paths.add(canonical_path)
    if len(paths) < 2:
        fail('route.distinct', 'two distinct approaches required')
    if mains != 1:
        fail('route.main', 'exactly one vehicle-enabled main route required')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--all', action='store_true', help='check all drafts')
    parser.add_argument('drafts', nargs='*')
    args = parser.parse_args()
    if not args.all and not args.drafts:
        parser.error('provide draft paths or --all')
    paths = list(map(Path, args.drafts))
    if args.all:
        paths += sorted((ROOT / 'Tools/Missions/drafts').glob('*.json'))
    try:
        catalog = rows('Design/Roadmap/Skirmish_Expansion/SCENARIO_CATALOG.csv')
        matrix = rows('Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv')
    except (OSError, csv.Error) as exc:
        print('data FAIL matrix: ' + str(exc), file=sys.stderr)
        return 1
    failed = False
    for path in paths:
        try:
            with path.open(encoding='utf-8') as f:
                draft = json.load(f)
            errors = check(draft, catalog, matrix)
        except (OSError, ValueError, TypeError, KeyError, OverflowError) as exc:
            errors = [('draft', str(exc))]
        failed |= bool(errors)
        try:
            label = str(path.relative_to(ROOT))
        except ValueError:
            label = str(path)
        result = 'FAIL ' + '; '.join(r + ': ' + m for r, m in errors) if errors else 'PASS'
        print(label + ' ' + result + ' | needs Unity: ' + NEEDS_UNITY)
    return int(failed)


if __name__ == '__main__':
    sys.exit(main())
