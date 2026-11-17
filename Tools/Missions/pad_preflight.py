#!/usr/bin/env python3
"""Check authored pad rectangles against a saved Unity layout bitmap.

Every block touched by a footprint counts, including partial blocks. Construction
and its HQ clearance ring require unflagged ground. Spawn and rally terrain use
the dump reader's travel predicate. Bounds and pairwise overlaps use the requested
instance relocation slack before runtime capping or relocation. Structures are
world-aligned; spawn/rally sizes follow the draft's base-to-base layout frame.
Edge contact is OK.
"""
import argparse
from collections import Counter
from dataclasses import dataclass, field
from itertools import combinations
import json
import math
from pathlib import Path
import sys
from typing import NamedTuple, Optional

import layout_dump_to_draft as layout

DUMP_ROOT = Path('/Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-17')
# Match the physical source, including the shared citywide_alert Definition.asset.
SOURCES = {
    'Assets/Game/Configs/OperationMaps/Candidates/OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset':
        ('S002', 'opmap.skirmish.desert_base_01'),
    'Assets/Game/Configs/Skirmish/CityCrossroads/OperationMap_CityCrossroads.asset':
        ('S026', 'opmap.skirmish.city_crossroads'),
    'Assets/Game/GeneratedOperationMaps/CH05M01CitywideAlert/Definition.asset':
        ('S050', 'opmap.ch05.citywide_alert'),
}
FLAGS = ((layout.FLAG_ROAD, 'road'), (layout.FLAG_WATER, 'water'), (layout.FLAG_SCENERY, 'scenery'),
         (layout.FLAG_INVALID, 'cached-invalid'), (layout.FLAG_NOT_LEVEL, 'not-level'),
         (layout.FLAG_SIDEWALK, 'sidewalk'))
# SkirmishMissionAuthoringEntry.DefaultSize
TRAVEL_SIZES = {'spawn.infantry.': (12, 10), 'spawn.vehicle.': (14, 12), 'rally.': (10, 8)}
OUTSIDE = -1


class Rect(NamedTuple):
    x0: float
    z0: float
    x1: float
    z1: float

    def grow(self, margin):
        return Rect(self.x0 - margin, self.z0 - margin, self.x1 + margin, self.z1 + margin)

    def contains(self, other):
        return self.x0 <= other.x0 and self.z0 <= other.z0 and self.x1 >= other.x1 and self.z1 >= other.z1

    def overlaps(self, other):
        return self.x0 < other.x1 and other.x0 < self.x1 and self.z0 < other.z1 and other.z0 < self.z1


class Footprint(NamedTuple):
    # Rect before rotation about its centre, plus its unit width axis in world X/Z.
    rect: Rect
    axis: tuple = (1, 0)

    def radius(self, x, z):
        r = self.rect
        fx, fz = self.axis
        return (abs(x * fx + z * fz) * (r.x1 - r.x0) + abs(z * fx - x * fz) * (r.z1 - r.z0)) / 2

    def envelope(self):
        r = self.rect
        x, z = (r.x0 + r.x1) / 2, (r.z0 + r.z1) / 2
        w, h = self.radius(1, 0), self.radius(0, 1)
        return Rect(x - w, z - h, x + w, z + h)

    def overlaps(self, other):
        # Separating-axis test. Positive area on both axes of both rectangles is required.
        if 0 in self.axis and 0 in other.axis:
            return self.envelope().overlaps(other.envelope())
        a, b = self.rect, other.rect
        dx = (a.x0 + a.x1 - b.x0 - b.x1) / 2
        dz = (a.z0 + a.z1 - b.z0 - b.z1) / 2
        for fx, fz in (self.axis, other.axis):
            for x, z in ((fx, fz), (-fz, fx)):
                if abs(dx * x + dz * z) >= self.radius(x, z) + other.radius(x, z) - 1e-9:
                    return False
        return True


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def rectangle(at, size, name):
    if not all(isinstance(pair, (list, tuple)) and len(pair) == 2 and all(map(number, pair)) for pair in (at, size)):
        raise ValueError(f'{name} needs finite [x,z] and [width,depth]')
    x, z = at
    w, h = size
    if min(w, h) <= 0:
        raise ValueError(f'{name} needs a positive size')
    return Rect(x - w / 2, z - h / 2, x + w / 2, z + h / 2)


@dataclass
class Pad:
    name: str
    kind: str
    footprint: Rect
    slack: float
    axis: tuple = (1, 0)

    @property
    def travel(self):
        return self.kind in ('spawn', 'rally')

    def shape(self, padded=False):
        return Footprint(self.footprint.grow(self.slack) if padded else self.footprint, self.axis)


def draft_pads(draft, slack):
    for spec in draft.get('instance_pads', []):
        name = spec['id']
        yield Pad(name, spec['kind'], rectangle(spec['at'], spec['size'], name), slack)
    anchors, overrides = draft.get('anchors', {}), draft.get('pads', {})
    roles = [role for role in sorted(anchors.keys() | overrides.keys()) if role.startswith(tuple(TRAVEL_SIZES))]
    if roles:
        if 'base.player' not in anchors or 'base.enemy' not in anchors:
            raise ValueError('spawn and rally footprints need both base anchors for orientation')
        px, pz = anchors['base.player']
        ex, ez = anchors['base.enemy']
        distance = math.hypot(ex - px, ez - pz)
        if distance < 1 or not math.isfinite(distance):
            raise ValueError('invalid base-to-base frame')
        axis = ((ex - px) / distance, (ez - pz) / distance)
    for role in roles:
        default = next(size for prefix, size in TRAVEL_SIZES.items() if role.startswith(prefix))
        spec = overrides.get(role)
        at, size = anchors.get(role), default
        if isinstance(spec, dict):
            at, size = spec.get('at', at), spec.get('size', default)
        elif spec is not None:
            at = spec
        yield Pad(role, role.split('.')[0], rectangle(at, size, role), 0, axis)


class Bitmap:
    def __init__(self, data):
        layout.load(data)
        self.step = data['blockCells'] * data['cellSize']
        if not number(self.step) or self.step <= 0 or min(data['bitmapRows'], data['bitmapColumns']) <= 0:
            raise ValueError('invalid bitmap dimensions')
        runs = data['bitmapRuns']
        if any(type(n) != int or n <= 0 for n in runs[1::2]) or any(
                type(mask) != int or not 0 <= mask <= 63 for mask in runs[::2]):
            raise ValueError('invalid bitmap run count or flags')
        if sum(runs[1::2]) != data['bitmapRows'] * data['bitmapColumns']:
            raise ValueError('bitmap run counts do not cover the bitmap')
        self.rows = layout.decode_bitmap(data)
        self.x, self.z = layout.block_to_world(data, -.5, -.5)
        low, high = data['playableMin'], data['playableMax']
        self.bounds = Rect(low['x'], low['z'], high['x'], high['z'])
        if not all(map(number, self.bounds)) or low['x'] >= high['x'] or low['z'] >= high['z']:
            raise ValueError('invalid playable bounds')
        for key in ('authoredBuildingsReady', 'roadSurfacesReady', 'waterMaskKnown'):
            if data.get(key) is False:
                raise ValueError(f'dump is incomplete ({key}=false)')

    def blocks(self, rect):
        return Rect(math.floor((rect.x0 - self.x) / self.step), math.floor((rect.z0 - self.z) / self.step),
                    math.ceil((rect.x1 - self.x) / self.step), math.ceil((rect.z1 - self.z) / self.step))

    def world(self, blocks):
        return Rect(self.x + self.step * blocks.x0, self.z + self.step * blocks.z0,
                    self.x + self.step * blocks.x1, self.z + self.step * blocks.z1)

    def counts(self, blocks):
        # Clip both axes and account for missing coverage without indexing negative rows.
        x0, x1 = max(0, blocks.x0), min(len(self.rows[0]), blocks.x1)
        z0, z1 = max(0, blocks.z0), min(len(self.rows), blocks.z1)
        counts = Counter()
        if x0 < x1:
            for z in range(z0, z1):
                counts.update(self.rows[z][x0:x1])
        area = (blocks.x1 - blocks.x0) * (blocks.z1 - blocks.z0)
        counts[OUTSIDE] = area - sum(counts.values())
        return counts

    def footprint_counts(self, shape, blocks):
        if 0 in shape.axis:
            return self.counts(blocks)
        counts = Counter()
        for z in range(blocks.z0, blocks.z1):
            for x in range(blocks.x0, blocks.x1):
                cell = self.world(Rect(x, z, x + 1, z + 1))
                if shape.overlaps(Footprint(cell)):
                    inside = 0 <= z < len(self.rows) and 0 <= x < len(self.rows[0])
                    counts[self.rows[z][x] if inside else OUTSIDE] += 1
        return counts


@dataclass
class Result:
    pad: Pad
    counts: Counter
    blocked: int
    inside: bool
    overlaps: list = field(default_factory=list)
    ring_counts: Optional[Counter] = None
    ring_inside: bool = True
    ring_neighbors: list = field(default_factory=list)

    @property
    def ring_clear(self):
        if self.ring_counts is None:
            return True
        return self.ring_inside and not self.ring_neighbors and all(mask == 0 for mask in self.ring_counts)

    @property
    def passed(self):
        return not self.blocked and self.inside and not self.overlaps and self.ring_clear


def preflight(draft, data, slack='auto'):
    source = SOURCES.get(draft.get('campaign_source'))
    if source and data.get('sourceOperationMapId') != source[1]:
        raise ValueError(f"expected dump for {source[1]}, got {data.get('sourceOperationMapId')}")
    if slack == 'auto':
        slack = draft.get('pad_slack_m')
        if slack is None:
            slack = 12
    if not number(slack) or not 0 <= slack <= 64:
        raise ValueError('pad slack must be finite and between 0 and 64 metres')
    bitmap = Bitmap(data)
    pads = list(draft_pads(draft, slack))
    if not pads:
        raise ValueError('no authored instance, spawn or rally pads')
    results = []
    for pad in pads:
        shape = pad.shape()
        blocks = bitmap.blocks(shape.envelope())
        counts = bitmap.footprint_counts(shape, blocks)
        blocked = sum(n for mask, n in counts.items()
                      if mask == OUTSIDE or (layout.is_blocked(mask) if pad.travel else mask != 0))
        result = Result(pad, counts, blocked, bitmap.bounds.contains(pad.shape(padded=True).envelope()))
        if pad.kind == 'BaseBarracks':
            outer = blocks.grow(1)
            result.ring_counts = bitmap.counts(outer) - counts
            apron = bitmap.world(outer)
            result.ring_inside = bitmap.bounds.contains(apron)
            # A neighbour wholly inside the core is already a footprint overlap.
            core = bitmap.world(blocks)
            result.ring_neighbors = [other.name for other in pads if other is not pad and
                                     other.shape().overlaps(Footprint(apron)) and not core.contains(other.shape().envelope())]
        results.append(result)
    for a, b in combinations(results, 2):
        if a.pad.shape(padded=True).overlaps(b.pad.shape(padded=True)):
            a.overlaps.append(b.pad.name)
            b.overlaps.append(a.pad.name)
    return results


def flags_text(counts):
    parts = []
    for flag, label in FLAGS:
        n = sum(count for mask, count in counts.items() if mask != OUTSIDE and mask & flag)
        if n:
            parts.append(f'{label}={n}')
    if counts[OUTSIDE]:
        parts.append(f'outside-bitmap={counts[OUTSIDE]}')
    return ','.join(parts) or '-'


def print_table(results):
    print('pad | kind | blocked/blocks | footprint flags | bounds+slack | overlaps | HQ ring | terrain passable | result')
    for row in results:
        ring = '-'
        if row.ring_counts is not None:
            details = [flags_text(row.ring_counts)]
            if not row.ring_inside:
                details.append('outside-playable')
            details.extend(row.ring_neighbors)
            ring = 'clear' if row.ring_clear else 'blocked ' + ' '.join(d for d in details if d != '-')
        passable = ('no' if row.blocked else 'yes') if row.pad.travel else '-'
        print(' | '.join((row.pad.name, row.pad.kind, f'{row.blocked}/{sum(row.counts.values())}',
                          flags_text(row.counts), 'inside' if row.inside else 'outside',
                          ','.join(row.overlaps) or '-', ring, passable, 'PASS' if row.passed else 'FAIL')))


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--draft', required=True, type=Path)
    parser.add_argument('--dump', type=Path, help='defaults to the saved dump for campaign_source')
    parser.add_argument('--pad-slack', default='auto', help='auto uses draft pad_slack_m (default 12 m), or supply 0..64')
    args = parser.parse_args(argv)
    try:
        draft = json.loads(args.draft.read_text(encoding='utf-8'))
        path = args.dump
        if path is None:
            source = SOURCES.get(draft['campaign_source'])
            if source is None:
                raise ValueError(f"no saved dump mapped for {draft['campaign_source']}; supply --dump")
            path = DUMP_ROOT / ('lane4-dump-' + source[0]) / 'dump.json'
        data = json.loads(path.read_text(encoding='utf-8'))
        slack = args.pad_slack if args.pad_slack == 'auto' else float(args.pad_slack)
        results = preflight(draft, data, slack)
    except (OSError, ValueError, KeyError, TypeError, AttributeError, OverflowError, layout.DumpError) as exc:
        print(f'PREFLIGHT ERROR {args.draft.name}: {exc}', file=sys.stderr)
        return 1
    print(f"{draft.get('catalog_id', args.draft.name)} | dump={path} | instance slack={max(r.pad.slack for r in results):g} m")
    print_table(results)
    failed = sum(not r.passed for r in results)
    print(f'PREFLIGHT FAIL {failed}/{len(results)} pads' if failed else 'PREFLIGHT PASS')
    return int(bool(failed))


if __name__ == '__main__':
    sys.exit(main())
