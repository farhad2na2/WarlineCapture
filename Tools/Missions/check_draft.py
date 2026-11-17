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
# SkirmishInstancePadPlanner.IsInstancePadKind and SkirmishMapLayoutBuilder.PadKindForStructure.
INSTANCE_KINDS = ('BaseBarracks', 'GroundStaging', 'AirReturn', 'Supply', 'Service')
# SkirmishStructureIds (SkirmishRosterContracts.cs:270-284), the structures that use a per-building pad.
STRUCTURES = ('building.barracks', 'building.ground_staging', 'building.helipad', 'building.airport',
              'building.oil_pump', 'building.refinery', 'building.refinery.module', 'building.fuel_bladder',
              'building.fabrication_depot', 'building.watchtower', 'building.watchtower.approach',
              'building.satellite_dish')
FRAME_MARGIN = 32.0  # SkirmishMissionAuthoringEntry.FrameMarginMetres
OBJECTIVES = ('BA', 'FC', 'BT', 'CE')
# SkirmishObjectiveAreaCompiler.RequiredRoles: exact role set per objective (BA takes none).
AREA_ROLES = {
    'FC': ('FrontlineA', 'FrontlineB', 'FrontlineC'),
    'BT': ('BreakthroughCorridorA', 'BreakthroughCorridorB', 'BreakthroughExit'),
    'CE': ('ConvoyDestination',),
}
ALL_AREA_ROLES = tuple(r for roles in AREA_ROLES.values() for r in roles)
# SkirmishMeasuredLayoutBinder.cs:155-170: exact mode-owned pads (faction 1 truck bays, faction 2 towers).
CONVOY_ROLES = ('convoy.origin.1', 'convoy.origin.2', 'convoy.origin.3')
DEFENSE_ROLES = ('defense.approach.a', 'defense.approach.b')
NEEDS_UNITY = ('source asset/metadata, map bounds and terrain, placement and navigation, '
               'shared profiles, compilation, fingerprints, registration and native play')


def rows(path):
    with (ROOT / path).open(newline='', encoding='utf-8-sig') as f:
        return list(csv.DictReader(f))


def playable_bounds(draft):
    """(minx, minz, maxx, maxz) of the campaign source's playableMin/Max, or None when the asset is not readable."""
    source = draft.get('campaign_source') if isinstance(draft, dict) else None
    if not isinstance(source, str) or not source:
        return None
    try:
        text = (ROOT / source).read_text(encoding='utf-8')
    except (OSError, ValueError):
        return None
    number = r'(-?[0-9.]+(?:[eE][-+]?[0-9]+)?)'
    low = re.search(r'playableMin: \{x: ' + number + r', y: \S+, z: ' + number + r'\}', text)
    high = re.search(r'playableMax: \{x: ' + number + r', y: \S+, z: ' + number + r'\}', text)
    if not low or not high:
        return None
    return (float(low.group(1)), float(low.group(2)), float(high.group(1)), float(high.group(2)))


def cross(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def on_segment(a, b, p):
    return (cross(a, b, p) == 0 and min(a[0], b[0]) <= p[0] <= max(a[0], b[0]) and
            min(a[1], b[1]) <= p[1] <= max(a[1], b[1]))


def segments_touch(a, b, c, d):
    """SkirmishObjectiveAreaCompiler.Intersects (touching counts)."""
    ab_c, ab_d, cd_a, cd_b = cross(a, b, c), cross(a, b, d), cross(c, d, a), cross(c, d, b)
    return ((ab_c == 0 and on_segment(a, b, c)) or (ab_d == 0 and on_segment(a, b, d)) or
            (cd_a == 0 and on_segment(c, d, a)) or (cd_b == 0 and on_segment(c, d, b)) or
            ((ab_c > 0) != (ab_d > 0) and (cd_a > 0) != (cd_b > 0)))


def perimeter(shape, pts):
    """Canonical-comparable perimeter of an area (SkirmishObjectiveAreaCompiler.Perimeter/SamePerimeter)."""
    if shape == 'rectangle':
        (x0, z0), (x1, z1) = pts
        ring = [(x0, z0), (x1, z0), (x1, z1), (x0, z1)]
    else:
        ring = list(pts)
    for i in range(len(ring) - 1, -1, -1):
        if len(ring) > 3 and cross(ring[i - 1], ring[i], ring[(i + 1) % len(ring)]) == 0:
            ring.pop(i)
    forms = []
    for seq in (ring, ring[::-1]):
        for start in range(len(seq)):
            forms.append(tuple(seq[start:] + seq[:start]))
    return min(forms)


def check_areas(draft, objective, point, fail, bounds):
    """Typed objective areas (SkirmishObjectiveAreaConfig) and their measured-evidence pointer."""
    raw = draft.get('objective_areas')
    required = AREA_ROLES.get(objective, ())
    if not required:
        if raw is not None:
            fail('area.unexpected', 'objective_areas applies to FC/BT/CE drafts only')
        if draft.get('area_measurement') is not None:
            fail('area.unexpected', 'area_measurement applies to FC/BT/CE drafts only')
        return
    if not isinstance(raw, dict):
        fail('area.required', objective + ' needs objective_areas{} with ' + ', '.join(required))
        raw = {}
    for role in raw.keys() - set(required):
        fail('area.role', 'objective_areas role ' + repr(role) + ' is not one of ' + ', '.join(required))
    for role in required:
        if role not in raw:
            fail('area.required', 'missing objective area ' + role)
    perimeters = {}
    for role in required:
        spec = raw.get(role)
        if spec is None:
            continue
        label = 'objective_areas.' + role
        if not isinstance(spec, dict) or len(spec) != 1 or next(iter(spec)) not in ('rectangle', 'polygon'):
            fail('area.type', label + ' needs exactly one of rectangle:[[minX,minZ],[maxX,maxZ]] or polygon:[[x,z],...]')
            continue
        shape, value = next(iter(spec.items()))
        if not isinstance(value, list):
            fail('area.type', label + '.' + shape + ' must be an array of [x,z] points')
            continue
        pts = [point(v, label + '.' + shape + '[' + str(i) + ']') for i, v in enumerate(value)]
        if any(p is None for p in pts):
            continue
        if shape == 'rectangle':
            if len(pts) != 2 or not (pts[0][0] < pts[1][0] and pts[0][1] < pts[1][1]):
                fail('area.geometry', label + ' rectangle needs [minimum XZ, maximum XZ] with min < max on both axes')
                continue
        else:
            if len(pts) < 3 or not polygon_ok(pts):
                fail('area.geometry', label + ' polygon must be simple (no repeats, zero-area, backtracks or self-intersections), 3+ points')
                continue
        if bounds is not None and any(not (bounds[0] <= p[0] <= bounds[2] and bounds[1] <= p[1] <= bounds[3]) for p in pts):
            fail('area.bounds', label + ' leaves the source playable bounds ' + str(bounds))
        key = perimeter(shape, pts)
        if key in perimeters:
            fail('area.distinct', label + ' has the same perimeter as ' + perimeters[key])
        perimeters[key] = role
    evidence = draft.get('area_measurement')
    if (not isinstance(evidence, dict) or evidence.keys() != {'artifact'} or not isinstance(evidence.get('artifact'), str) or
            not evidence['artifact'] or evidence['artifact'].startswith('/') or '..' in evidence['artifact'].split('/') or
            any(c.isspace() for c in evidence['artifact'])):
        fail('area.measurement', 'area_measurement {"artifact": "<repo-relative path of the native measurement file>"} required; '
             'the entry binds its SHA-256 (SkirmishObjectiveAreaCompiler needs measured proof)')


def polygon_ok(pts):
    n = len(pts)
    twice = 0.0
    for i in range(n):
        nxt = (i + 1) % n
        if pts[i] == pts[nxt]:
            return False
        prev = pts[i - 1]
        if cross(prev, pts[i], pts[nxt]) == 0 and ((pts[i][0] - prev[0]) * (pts[nxt][0] - pts[i][0]) +
                                                     (pts[i][1] - prev[1]) * (pts[nxt][1] - pts[i][1])) <= 0:
            return False
        twice += pts[i][0] * pts[nxt][1] - pts[nxt][0] * pts[i][1]
        for j in range(i + 1, n):
            other = (j + 1) % n
            if nxt == j or other == i:
                continue
            if segments_touch(pts[i], pts[nxt], pts[j], pts[other]):
                return False
    return twice != 0


def check_objective_pads(draft, objective, points, pads, explicit, local, fail):
    """Objective-owned pads: faction-2 defense towers (BT/CE) and faction-1 truck bays (CE)."""
    anchored = lambda roles: [r for r in roles if r in points]
    if objective in ('BT', 'CE'):
        for role in DEFENSE_ROLES:
            if role not in points:
                fail('pad.required', 'missing ' + role + ' anchor (enemy approach tower)')
    else:
        for role in anchored(DEFENSE_ROLES):
            fail('pad.objective', role + ' is only valid for BT/CE (the binder rejects defense approaches elsewhere)')
    if objective == 'CE':
        for role in CONVOY_ROLES:
            if role not in points:
                fail('pad.required', 'missing ' + role + ' anchor (objective truck bay)')
    else:
        for role in anchored(CONVOY_ROLES):
            fail('pad.objective', role + ' is only valid for CE')
    for role in anchored(DEFENSE_ROLES + CONVOY_ROLES):
        if role not in explicit:
            fail('pad.size_required', role + ' needs pads{} with an explicit measured size; no default is guessed')
    # SkirmishConvoyTruckDefinition.TryAppendForces: no two bays may overlap in the layout frame.
    bays = [(r, pads[r]) for r in CONVOY_ROLES if r in pads and r in explicit and local]
    for i in range(len(bays)):
        for j in range(i):
            (ra, (at_a, _, sa)), (rb, (at_b, _, sb)) = bays[i], bays[j]
            ca, cb = local(*at_a), local(*at_b)
            if abs(ca[0] - cb[0]) < (sa[0] + sb[0]) / 2 and abs(ca[1] - cb[1]) < (sa[1] + sb[1]) / 2:
                fail('pad.convoy_overlap', ra + ' overlaps ' + rb)


def check_convoy_routes(routes, points, fail):
    """CE needs two vehicle routes whose middle segments differ (distinct truck holding pockets)."""
    middles = set()
    for route in routes if isinstance(routes, list) else []:
        if not isinstance(route, dict) or route.get('vehicles', True) is False:
            continue
        via = route.get('via')
        if not isinstance(via, list) or len(via) < 3 or any(not isinstance(x, str) or x not in points for x in via):
            continue
        middle = tuple(points[x] for x in via[1:-1])
        middles.add(min(middle, middle[::-1]))
    if len(middles) < 2:
        fail('route.convoy', 'CE needs two vehicle routes with different non-empty middle waypoints (via[1:-1])')


def check_instance_pads(draft, points, pads, point, fail):
    """Optional measured per-building pads (LayoutGeometry.ReadMeasured) and derived-pad footprints."""
    table = draft.get('structure_footprints')
    if table is not None:
        if not isinstance(table, dict):
            fail('instance.footprint', 'structure_footprints must be an object')
        else:
            for structure, value in table.items():
                if structure not in STRUCTURES:
                    fail('instance.footprint', 'unknown structure ' + repr(structure))
                size = point(value, 'structure_footprints.' + structure)
                if size is not None and (size[0] <= 0 or size[1] <= 0):
                    fail('instance.footprint', structure + ' footprint must be positive')
    raw = draft.get('instance_pads')
    if raw is None:
        return
    if not isinstance(raw, list):
        fail('instance.type', 'instance_pads must be an array')
        return
    seen, anchor_use, entries = set(), {}, []
    for i, item in enumerate(raw):
        label = 'instance_pads[' + str(i) + ']'
        if not isinstance(item, dict):
            fail('instance.type', label + ' must be an object')
            continue
        for key in item.keys() - {'id', 'kind', 'faction', 'at', 'size', 'anchor'}:
            fail('instance.type', label + ' unknown key ' + key)
        pad_id = item.get('id')
        if not isinstance(pad_id, str) or not pad_id or any(c.isspace() for c in pad_id):
            fail('instance.id', label + ' needs a nonempty id without spaces')
            continue
        if pad_id in seen:
            fail('instance.id', 'duplicate id ' + pad_id)
        seen.add(pad_id)
        # Generated pad id is pad.skirmish.<sNNN>.i_<id with dots as underscores> (cap 61, enemy policy).
        if len('pad.skirmish.sNNN.i_' + pad_id.replace('.', '_')) > 61:
            fail('instance.id', pad_id + ' makes a pad id longer than 61 characters')
        # No collision with a kind pad's role (hence its generated id) or an instance anchor role.
        if pad_id in pads or 'instance.' + pad_id in points:
            fail('instance.duplicate', pad_id + ' duplicates a kind pad role or an instance anchor role')
        if item.get('kind') not in INSTANCE_KINDS:
            fail('instance.kind', label + ' kind must be one of ' + ', '.join(INSTANCE_KINDS))
        faction = item.get('faction')
        if isinstance(faction, bool) or faction not in (1, 2):
            fail('instance.faction', label + ' faction must be 1 or 2')
        at = point(item.get('at'), label + '.at')
        size = point(item.get('size'), label + '.size')
        if size is not None and (size[0] <= 0 or size[1] <= 0):
            fail('instance.size', label + ' size must be positive')
        anchor = item.get('anchor')
        if not isinstance(anchor, str) or anchor not in points:
            fail('instance.anchor', label + ' anchor must name a role in anchors{}')
        elif at is not None:
            if abs(points[anchor][0] - at[0]) > .001 or abs(points[anchor][1] - at[1]) > .001:
                fail('instance.anchor', label + ' anchor must sit exactly on the pad centre')
            if anchor in anchor_use:
                fail('instance.anchor', label + ' shares anchor ' + anchor + ' with ' + anchor_use[anchor])
            anchor_use[anchor] = pad_id
        if at is not None and size is not None:
            entries.append((label, at, size))
    # SkirmishMissionAuthoringEntry.cs frame: axis player base -> enemy base, anchors and pads plus a 32 m margin.
    if entries and 'base.player' in points and 'base.enemy' in points:
        px, pz = points['base.player']
        dx, dz = points['base.enemy'][0] - px, points['base.enemy'][1] - pz
        length = math.hypot(dx, dz)
        if length < 1:
            return
        fx, fz = dx / length, dz / length
        ax, az = -fz, fx
        def local(x, z):
            return ((x - px) * fx + (z - pz) * fz, (x - px) * ax + (z - pz) * az)
        every = [local(*p) for p in points.values()] + [local(*p[0]) for p in pads.values()]
        low = [min(v[k] for v in every) - FRAME_MARGIN for k in (0, 1)]
        high = [max(v[k] for v in every) + FRAME_MARGIN for k in (0, 1)]
        for label, at, size in entries:
            c = local(*at)
            hx, hz = size[0] / 2, size[1] / 2
            if c[0] - hx < low[0] or c[0] + hx > high[0] or c[1] - hz < low[1] or c[1] + hz > high[1]:
                fail('instance.bounds', label + ' leaves the draft frame')


def check(draft, catalog, matrix, bounds='auto'):
    errors = []
    if bounds == 'auto':
        bounds = playable_bounds(draft)
    def fail(rule, message):
        errors.append((rule, message))
    # SkirmishMissionAuthoringEntry.cs:119-126 (JObject and allowed top-level keys).
    if not isinstance(draft, dict):
        return [('draft', 'JSON root must be an object')]
    for key in draft.keys() - {'catalog_id', 'campaign_source', 'anchors', 'pads', 'routes', 'notes',
                               'instance_pads', 'structure_footprints',
                               'objective_areas', 'area_measurement'}:
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
    objective = row['objective_id'] if row else None
    if row and objective not in OBJECTIVES:
        fail('objective', 'authoring supports BA, FC, BT and CE; got ' + repr(objective))
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
        if role in CONVOY_ROLES:
            return ('convoy', (14, 12))
        if role in DEFENSE_ROLES:
            return ('defense', (14, 12))
        return next(((k, size) for prefix, k, size in prefixes if role.startswith(prefix)), (None, None))
    pads = {role: (p, role, kind(role)[1]) for role, p in points.items() if kind(role)[0]}
    # SkirmishMissionAuthoringEntry.cs:449-474. Non-object pads are ignored by C#.
    rawpads = draft.get('pads')
    explicit = set()  # pads whose size the draft states itself
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
                    explicit.add(role)
            else:
                at = point(spec, 'pads.' + role)
            if not isinstance(anchor, str) or anchor not in points:
                fail('pad.anchor', role + ' has no anchor role')
            if size is not None and (size[0] <= 0 or size[1] <= 0):
                fail('pad.size', role + ' size must be positive')
            if at is not None and size is not None and isinstance(anchor, str) and anchor in points:
                pads[role] = (at, anchor, size)
    check_instance_pads(draft, points, pads, point, fail)
    # Typed objective areas and objective-owned pads (FC/BT/CE; a BA draft is untouched unless it adds them).
    check_areas(draft, objective, point, fail, bounds)
    local = None
    if 'base.player' in points and 'base.enemy' in points:
        px, pz = points['base.player']
        length = math.dist(points['base.player'], points['base.enemy'])
        if length >= 1:
            fx, fz = (points['base.enemy'][0] - px) / length, (points['base.enemy'][1] - pz) / length
            local = lambda x, z: ((x - px) * fx + (z - pz) * fz, (x - px) * -fz + (z - pz) * fx)
    check_objective_pads(draft, objective, points, pads, explicit, local, fail)
    # SkirmishMeasuredLayoutBinder.cs:155-185, 198-207; SkirmishMapLayoutBuilder.cs:367-433.
    kinds = {}
    for role in pads:
        if role in CONVOY_ROLES or role in DEFENSE_ROLES:
            continue  # exact mode-owned roles, resolved by role rather than faction/kind
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
    if objective == 'CE':
        check_convoy_routes(routes, points, fail)
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
