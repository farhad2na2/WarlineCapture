#!/usr/bin/env python3
"""Print a mission-draft skeleton from a measured layout dump.

Input: the JSON written by Game.Editor.SkirmishS002AriaRunHarness.RunCatalogLayoutDumpAndExit (or
RunExistingCampaignLayoutDumpAndExit): base anchors, grid and playable bounds, a 4 m flag bitmap
(run-length encoded) and native IsPlacementValid candidates per pad kind.

Output: a draft-shaped JSON (anchors, pads, two candidate routes, notes with measured_from=<dump>).
Every coordinate comes from the dump (base positions, candidate centres and sizes, bitmap cell centres).
Roles the dump does not measure (spawn.*, rally.*) and routes that cannot be found through unblocked
cells are NOT invented: they are listed under 'needs' (stderr) and left out. The skeleton is a starting
point for Tools/Missions/check_draft.py and Unity authoring; it certifies nothing.
"""
import argparse
import heapq
import json
import math
from pathlib import Path
import sys

FLAG_ROAD, FLAG_WATER, FLAG_SCENERY, FLAG_INVALID, FLAG_NOT_LEVEL = 1, 2, 4, 8, 16
SIDES = ('player', 'enemy')
# dump kind -> draft role prefix (check_draft.py role prefixes); air picks airport else helipad.
ROLE_KINDS = (('staging', 'staging'), ('economy', 'supply'), ('service', 'service'))
ROUTE_KINDS = ('MainHighway', 'FlankNorthRuins')
NEIGHBOURS = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1))


class DumpError(Exception):
    pass


def load(data):
    for key in ('cellSize', 'blockCells', 'bitmapColumns', 'bitmapRows', 'bitmapRuns', 'bitmapOriginCell', 'gridOrigin'):
        if key not in data:
            raise DumpError('dump has no ' + key + ' (rerun the layout dump)')
    return data


def decode_bitmap(data):
    cols, rows = data['bitmapColumns'], data['bitmapRows']
    runs = data['bitmapRuns']
    if len(runs) % 2:
        raise DumpError('bitmapRuns has an odd length')
    flat = []
    for value, count in zip(runs[0::2], runs[1::2]):
        flat.extend([value] * count)
    if len(flat) != cols * rows:
        raise DumpError('bitmap has %d blocks, expected %d' % (len(flat), cols * rows))
    return [flat[r * cols:(r + 1) * cols] for r in range(rows)]


def block_to_world(data, col, row):
    """World (x, z) of a bitmap block centre."""
    step = data['blockCells'] * data['cellSize']
    ox, oz = data['bitmapOriginCell']['x'], data['bitmapOriginCell']['y']
    return (data['gridOrigin']['x'] + ox * data['cellSize'] + (col + .5) * step,
            data['gridOrigin']['z'] + oz * data['cellSize'] + (row + .5) * step)


def world_to_block(data, x, z):
    step = data['blockCells'] * data['cellSize']
    ox, oz = data['bitmapOriginCell']['x'], data['bitmapOriginCell']['y']
    col = int(math.floor((x - data['gridOrigin']['x'] - ox * data['cellSize']) / step))
    row = int(math.floor((z - data['gridOrigin']['z'] - oz * data['cellSize']) / step))
    return col, row


def is_blocked(flags):
    # Roads are placement-invalid but travelable; a cached-invalid cell that is not a road stays blocked.
    return bool(flags & (FLAG_WATER | FLAG_SCENERY | FLAG_NOT_LEVEL)) or (bool(flags & FLAG_INVALID) and not flags & FLAG_ROAD)


def rect_overlap(a, b, margin=1):
    return (a['rectX'] < b['rectX'] + b['rectW'] + margin and b['rectX'] < a['rectX'] + a['rectW'] + margin and
            a['rectZ'] < b['rectZ'] + b['rectH'] + margin and b['rectZ'] < a['rectZ'] + a['rectH'] + margin)


def pick_pads(data, bases, max_distance=150.0):
    """Nearest non-overlapping candidate within max_distance (m) of the side's base. Returns (picks, needs)."""
    candidates = data.get('candidates', [])
    kinds = {c['kind'] for c in candidates}
    air_kind = 'airport' if 'airport' in kinds else 'helipad'
    wanted = [(prefix, kind) for prefix, kind in ROLE_KINDS] + [('air', air_kind)]
    picks, taken, needs = {}, [], []
    for prefix, kind in wanted:
        for side in SIDES:
            base = bases[side]
            options = sorted((math.hypot(c['centreX'] - base[0], c['centreZ'] - base[1]), i, c)
                             for i, c in enumerate(candidates) if c['kind'] == kind)
            choice = next((c for d, _, c in options if d <= max_distance and not any(rect_overlap(c, t) for t in taken)), None)
            if choice is None:
                needs.append('%s.%s: no %s candidate in the dump within %g m of the base' % (prefix, side, kind, max_distance))
                continue
            taken.append(choice)
            picks[prefix + '.' + side] = choice
    return picks, needs


def astar(grid, start, goal, passable, extra_cost=None):
    cols, rows = len(grid[0]), len(grid)
    best, came = {start: 0.0}, {}
    heap = [(0.0, start)]
    while heap:
        _, cur = heapq.heappop(heap)
        if cur == goal:
            path = [cur]
            while cur in came:
                cur = came[cur]
                path.append(cur)
            return path[::-1]
        for dc, dr in NEIGHBOURS:
            n = (cur[0] + dc, cur[1] + dr)
            if not (0 <= n[0] < cols and 0 <= n[1] < rows) or not passable(n):
                continue
            if dc and dr and not (passable((cur[0] + dc, cur[1])) and passable((cur[0], cur[1] + dr))):
                continue  # no corner cutting
            cost = best[cur] + (math.sqrt(2) if dc and dr else 1.0) * (1.0 if grid[n[1]][n[0]] & FLAG_ROAD else 1.4)
            cost += extra_cost(n) if extra_cost else 0
            if cost < best.get(n, 1e18):
                best[n], came[n] = cost, cur
                h = math.hypot(goal[0] - n[0], goal[1] - n[1])
                heapq.heappush(heap, (cost + h, n))
    return None


def simplify(points, tolerance, clear):
    """Ramer-Douglas-Peucker on block coordinates; a leg is only kept if clear(p, q) says it crosses no blocked block."""
    if len(points) < 3:
        return points
    (x0, y0), (x1, y1) = points[0], points[-1]
    length = math.hypot(x1 - x0, y1 - y0)
    far, index = -1.0, 0
    for i in range(1, len(points) - 1):
        px, py = points[i]
        d = (math.hypot(px - x0, py - y0) if length == 0 else abs((x1 - x0) * (y0 - py) - (x0 - px) * (y1 - y0)) / length)
        if d > far:
            far, index = d, i
    if far <= tolerance and clear(points[0], points[-1]):
        return [points[0], points[-1]]
    return simplify(points[:index + 1], tolerance, clear)[:-1] + simplify(points[index:], tolerance, clear)


def leg_is_clear(p, q, passable):
    """Sample a straight leg between block centres every quarter block."""
    steps = max(1, int(math.hypot(q[0] - p[0], q[1] - p[1]) * 4))
    for t in range(steps + 1):
        x, y = p[0] + (q[0] - p[0]) * t / steps, p[1] + (q[1] - p[1]) * t / steps
        if not passable((int(math.floor(x + .5)), int(math.floor(y + .5)))):
            return False
    return True


def find_routes(data, grid, bases, apron=4, separation=5):
    """Up to two distinct routes of block cells from the player base to the enemy base."""
    start, goal = world_to_block(data, *bases['player']), world_to_block(data, *bases['enemy'])
    cols, rows = len(grid[0]), len(grid)
    for name, cell in (('player', start), ('enemy', goal)):
        if not (0 <= cell[0] < cols and 0 <= cell[1] < rows):
            raise DumpError('%s base is outside the bitmap' % name)

    def near(cell, centre):
        return abs(cell[0] - centre[0]) <= apron and abs(cell[1] - centre[1]) <= apron

    def passable(cell):  # the base compound apron is relaxed: units start inside it
        return near(cell, start) or near(cell, goal) or not is_blocked(grid[cell[1]][cell[0]])

    first = astar(grid, start, goal, passable)
    if not first:
        return [], passable
    near_first = {(c + dc, r + dr) for c, r in first if not (near((c, r), start) or near((c, r), goal))
                  for dc in range(-separation, separation + 1) for dr in range(-separation, separation + 1)}
    second = astar(grid, start, goal,
                   lambda c: passable(c) and (near(c, start) or near(c, goal) or c not in near_first))
    return [first] + ([second] if second else []), passable


def route_waypoints(data, path, bases, passable, tolerance=1.5):
    cells = simplify(path, tolerance, lambda p, q: leg_is_clear(p, q, passable))
    points = [bases['player']] + [block_to_world(data, c, r) for c, r in cells[1:-1]] + [bases['enemy']]
    return [[round(x, 1), round(z, 1)] for x, z in points]


def build(data, catalog_id, campaign_source, measured_from, apron=4, separation=5, max_distance=150.0):
    """Return (draft skeleton, needs)."""
    bases = {}
    for point in data.get('bases', []):
        if point.get('role') in ('base.player', 'base.enemy'):
            bases[point['role'].split('.')[1]] = (point['x'], point['z'])
    if set(bases) != set(SIDES):
        raise DumpError('dump has no base anchors for both sides')
    grid = decode_bitmap(data)
    cell = data['cellSize']
    draft = {'catalog_id': catalog_id or data.get('catalog'), 'anchors': {}, 'pads': {}, 'routes': []}
    if campaign_source:
        draft['campaign_source'] = campaign_source
    needs = [] if campaign_source else ['campaign_source: pass --campaign-source (the dump does not know the asset path)']
    for side in SIDES:
        draft['anchors']['base.' + side] = [round(bases[side][0], 1), round(bases[side][1], 1)]
    picks, pick_needs = pick_pads(data, bases, max_distance)
    needs += pick_needs
    for role, c in picks.items():
        at = [round(c['centreX'], 1), round(c['centreZ'], 1)]
        draft['anchors'][role] = at
        draft['pads'][role] = {'at': at, 'anchor': role, 'size': [round(c['rectW'] * cell, 1), round(c['rectH'] * cell, 1)]}
    needs += ['%s.%s: not measured by the dump (spawn/rally pads)' % (p, s)
              for p in ('spawn.infantry', 'spawn.vehicle', 'rally') for s in SIDES]
    paths, passable = find_routes(data, grid, bases, apron, separation)
    for n, path in enumerate(paths):
        roles = []
        for i, point in enumerate(route_waypoints(data, path, bases, passable)):
            role = 'candidate.route.%d.waypoint.%02d' % (n + 1, i)
            draft['anchors'][role] = point
            roles.append(role)
        draft['routes'].append({'kind': ROUTE_KINDS[n], 'via': roles, 'width': 12.0 if n == 0 else 6.0,
                                'vehicles': True, 'aircraft': False})
    if len(paths) < 2:
        needs.append('routes: %d route(s) through unblocked bitmap cells, two distinct approaches required' % len(paths))
    draft['notes'] = ('layout skeleton measured_from=%s; catalog=%s stride=%s; coarse native-validator candidates, '
                      'not a capacity or route certificate' % (measured_from, data.get('catalog'), data.get('stride')))
    return draft, needs


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--dump', required=True, help='layout dump JSON (WARLINE_LAYOUT_DUMP_OUT copy)')
    parser.add_argument('--catalog', help='draft catalog_id (default: the dump catalog)')
    parser.add_argument('--campaign-source', help='campaign_source asset path for the draft')
    parser.add_argument('--apron', type=int, default=4, help='bitmap blocks around each base treated as passable (default 4)')
    parser.add_argument('--separation', type=int, default=5, help='blocks the second route keeps from the first (default 5)')
    parser.add_argument('--max-pad-distance', type=float, default=150.0, help='max metres from the base to a pad candidate (default 150)')
    parser.add_argument('--measured-from', help='path recorded in the notes (default: absolute --dump path)')
    parser.add_argument('--out', help='write the skeleton here instead of stdout')
    args = parser.parse_args(argv)
    try:
        path = Path(args.dump)
        data = load(json.loads(path.read_text(encoding='utf-8')))
        draft, needs = build(data, args.catalog, args.campaign_source, args.measured_from or str(path.resolve()),
                             args.apron, args.separation, args.max_pad_distance)
    except (OSError, ValueError, KeyError, TypeError, DumpError) as exc:
        print('layout_dump_to_draft: ' + str(exc), file=sys.stderr)
        return 1
    text = json.dumps(draft, indent=2) + '\n'
    if args.out:
        Path(args.out).write_text(text, encoding='utf-8')
    else:
        sys.stdout.write(text)
    for line in needs:
        print('needs: ' + line, file=sys.stderr)
    return 0


if __name__ == '__main__':
    sys.exit(main())
