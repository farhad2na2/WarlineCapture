#!/usr/bin/env python3
"""Turn a measured Airport source-fit scan into per-side Airport pads for a mission draft.

Input: a draft from Tools/Missions/drafts and ONE measured site list:
  --sites  the <trace>.airport-source-fit.json written by RunCatalogAirportSourceFitAndExit (S004-bound lookup), or
  --dump   a layout-dump JSON from RunCatalogLayoutDumpAndExit with WARLINE_LAYOUT_DUMP_KINDS=airport (any
           catalog): its airport candidates already passed the starting-placement predicates (inside logical
           playable bounds, no cached-invalid cell, no authored map building, road, water or sidewalk) and the native
           IsPlacementValid. Candidates on non-level ground are dropped (the validator enforces level ground while a
           match is Playing). A dump with no Airport definition or no airport candidate exits 1 with the reason.
Both feed the same per-side selection. For each side (player faction 1,
enemy faction 2) this picks a native-valid, inside-playable site that does not overlap the draft's
other pads or routes, nearest to that side's base anchor, with the two sites not overlapping each other.
It never invents a site: no valid site means exit 1 with the reason.

Output (stdout): the air.<side> anchor, pad, AirReturn instance pad and building.airport footprint
that would be set. With --write the draft is updated in place (or --out PATH) and the note
'airport measured_from=<sites json>' is appended to draft notes (the draft schema has no other free field).
A pick is a coarse-stride native-validator hit. It does not certify taxi, parking, launch or return capacity.
"""
import argparse
import json
import math
from pathlib import Path
import sys

# check_draft.py role prefixes -> default pad footprint (SkirmishMissionAuthoringEntry.cs:576-607).
PAD_PREFIXES = (('base.', (16, 12)), ('staging.', (20, 14)), ('spawn.infantry.', (12, 10)),
                ('spawn.vehicle.', (14, 12)), ('rally.', (10, 8)), ('economy.', (14, 12)),
                ('service.', (12, 10)), ('supply.expansion.', (14, 12)))
SIDES = (('player', 1), ('enemy', 2))


class SiteError(Exception):
    pass


def load_sites(data, catalog_id=None):
    """Return (sites, cell_size). A site is a dict with centre (x, z), size (w, h) metres, cells rect."""
    if not isinstance(data, dict) or not isinstance(data.get('sites'), list):
        raise SiteError('sites json has no sites list')
    scan_catalog = data.get('catalog')
    if catalog_id and scan_catalog and scan_catalog != catalog_id:
        raise SiteError('scan is for catalog %s, draft is %s' % (scan_catalog, catalog_id))
    cell = data.get('cellSize')
    if not isinstance(cell, (int, float)) or isinstance(cell, bool) or cell <= 0:
        raise SiteError('sites json has no cellSize (rerun the scan with the current harness)')
    sites = []
    for i, s in enumerate(data['sites']):
        try:
            rect, centre = s['effectiveRect'], s['centre']
            sites.append({'index': i, 'x': float(centre['x']), 'z': float(centre['z']),
                          'w': float(rect['width']) * cell, 'h': float(rect['height']) * cell,
                          'rotated': bool(s.get('rotated')), 'playable': bool(s.get('insideLogicalPlayable'))})
        except (KeyError, TypeError, ValueError) as exc:
            raise SiteError('site %d is malformed: %s' % (i, exc))
    return sites, float(cell)


def load_dump_sites(data, catalog_id=None):
    """Return (sites, cell_size, info) from a layout dump. info = report lines. Never invents a site."""
    if not isinstance(data, dict) or not isinstance(data.get('candidates'), list):
        raise SiteError('layout dump has no candidates list')
    scan_catalog = data.get('catalog')
    if catalog_id and scan_catalog and scan_catalog != catalog_id:
        raise SiteError('dump is for catalog %s, draft is %s' % (scan_catalog, catalog_id))
    cell = data.get('cellSize')
    if not isinstance(cell, (int, float)) or isinstance(cell, bool) or cell <= 0:
        raise SiteError('layout dump has no cellSize')
    airport_defs = [d for d in data.get('definitions') or [] if isinstance(d, dict) and 'airport' in str(d.get('name', '')).lower()]
    missing = [k for k in str(data.get('missingKinds') or '').split(',') if k]
    if 'airport' in missing or (not airport_defs and 'definitions' in data):
        fallback = str(data.get('airportAssetFallback') or '')
        raise SiteError('this session configures no Airport building definition (dump definitions: %s)%s; an airport scan '
                        'has nothing to place, so no site was measured' %
                        (', '.join(str(d.get('name')) for d in data.get('definitions') or []) or 'none',
                         ' and the asset fallback did not load one (%s)' % fallback if fallback else
                         ' (a dump from a harness without the asset fallback; rerun with the current one)'))
    if airport_defs and not any(d.get('scanned') for d in airport_defs):
        raise SiteError('the Airport definition was not scanned; rerun with WARLINE_LAYOUT_DUMP_KINDS=airport')
    origin, lo, hi = data.get('gridOrigin') or {}, data.get('playableMin'), data.get('playableMax')
    sites, not_level, outside, total, unvalidated, asset = [], 0, 0, 0, 0, 0
    for i, c in enumerate(data['candidates']):
        if not isinstance(c, dict) or c.get('kind') != 'airport':
            continue
        total += 1
        try:
            w, h = float(c['rectW']) * cell, float(c['rectH']) * cell
            x, z = float(c['centreX']), float(c['centreZ'])
            if c.get('level') is False:
                not_level += 1
                continue
            playable = True
            if lo and hi and origin:
                x0, z0 = float(origin['x']) + float(c['rectX']) * cell, float(origin['z']) + float(c['rectZ']) * cell
                playable = (x0 >= float(lo['x']) - 1e-3 and z0 >= float(lo['z']) - 1e-3 and
                            x0 + w <= float(hi['x']) + 1e-3 and z0 + h <= float(hi['z']) + 1e-3)
            if not playable:
                outside += 1
            native = c.get('nativeValidated') is not False
            from_asset = c.get('definitionSource') == 'asset'
            unvalidated += 0 if native else 1
            asset += 1 if from_asset else 0
            sites.append({'index': i, 'x': x, 'z': z, 'w': w, 'h': h, 'rotated': bool(c.get('rotated')), 'playable': playable,
                          'native': native, 'asset': from_asset})
        except (KeyError, TypeError, ValueError) as exc:
            raise SiteError('dump candidate %d is malformed: %s' % (i, exc))
    info = ['dump: %d airport candidates (stride %s), %d on non-level ground dropped, %d outside playable%s'
            % (total, data.get('stride', '?'), not_level, outside,
               '; CANDIDATE LIST TRUNCATED (scan order is low z first, picks may be biased; use a larger stride)'
               if data.get('candidatesTruncated') else '')]
    if asset:
        info.append('airport definition built from the prefab asset, not the session list (definitionSource=asset)')
    if unvalidated:
        info.append('WARNING nativeValidated=0 for %d candidate(s): the native IsPlacementValid could not take the asset '
                    'definition (%s); only the starting-placement predicates were applied'
                    % (unvalidated, data.get('nativeValidationFailure') or 'no reason recorded'))
    if not sites:
        counters = ', '.join('%s=%s' % (k, data[k]) for k in ('tested', 'rejectedCachedInvalid', 'rejectedBuilding', 'rejectedRoad',
                                                              'rejectedWater', 'rejectedSidewalk', 'rejectedNative') if k in data)
        raise SiteError('no airport candidate in the dump (%s%s)' % (info[0], '; ' + counters if counters else ''))
    return sites, float(cell), info


def rect_of(x, z, w, h):
    return (x - w / 2, z - h / 2, x + w / 2, z + h / 2)


def rects_overlap(a, b, margin=0.0):
    return a[0] < b[2] + margin and b[0] < a[2] + margin and a[1] < b[3] + margin and b[1] < a[3] + margin


def segment_hits_rect(p, q, rect, pad):
    """Liang-Barsky clip of segment p-q against rect grown by pad (metres)."""
    x0, z0, x1, z1 = rect[0] - pad, rect[1] - pad, rect[2] + pad, rect[3] + pad
    dx, dz = q[0] - p[0], q[1] - p[1]
    lo, hi = 0.0, 1.0
    for d, start, low, high in ((dx, p[0], x0, x1), (dz, p[1], z0, z1)):
        if abs(d) < 1e-12:
            if start < low or start > high:
                return False
            continue
        t0, t1 = (low - start) / d, (high - start) / d
        if t0 > t1:
            t0, t1 = t1, t0
        lo, hi = max(lo, t0), min(hi, t1)
        if lo > hi:
            return False
    return True


def draft_obstacles(draft):
    """Footprints (rects) and route corridors the Airport must not overlap; the air pads are excluded."""
    anchors = {k: tuple(v) for k, v in draft.get('anchors', {}).items()}
    pads = {}
    for role, at in anchors.items():
        for prefix, size in PAD_PREFIXES:
            if role.startswith(prefix):
                pads[role] = (at, size)
    for role, spec in (draft.get('pads') or {}).items():
        if role.startswith('air.') or not isinstance(spec, (dict, list)):
            continue
        at = spec.get('at') if isinstance(spec, dict) else spec
        size = spec.get('size') if isinstance(spec, dict) else None
        if at is None:
            at = anchors.get(role)
        if at is not None:
            pads[role] = (tuple(at), tuple(size) if size else pads.get(role, (None, (14, 12)))[1])
    footprints = [(role, rect_of(at[0], at[1], size[0], size[1])) for role, (at, size) in pads.items()]
    for item in draft.get('instance_pads') or []:
        if isinstance(item, dict) and not str(item.get('anchor', '')).startswith('air.') and item.get('at') and item.get('size'):
            footprints.append(('instance ' + str(item.get('id')),
                               rect_of(item['at'][0], item['at'][1], item['size'][0], item['size'][1])))
    corridors = []
    for n, route in enumerate(draft.get('routes') or []):
        via = [r for r in route.get('via', []) if r in anchors]
        if route.get('aircraft') is True or any(r.startswith('air.') for r in via):
            continue  # flying routes and routes that end on the air pad touch it by design
        width = route.get('width', 12 if route.get('vehicles', True) else 6)
        for a, b in zip(via, via[1:]):
            corridors.append(('route %d (%s)' % (n + 1, route.get('kind')), anchors[a], anchors[b], width / 2.0))
    return footprints, corridors


def blocked_by(site, footprints, corridors, clearance):
    rect = rect_of(site['x'], site['z'], site['w'], site['h'])
    for name, other in footprints:
        if rects_overlap(rect, other, clearance):
            return name
    for name, p, q, half in corridors:
        if segment_hits_rect(p, q, rect, half + clearance):
            return name
    return None


def pick(draft, sites, clearance=4.0):
    """Return ({side: site}, report). Raises SiteError when a side has no legal site."""
    anchors = draft.get('anchors', {})
    footprints, corridors = draft_obstacles(draft)
    report, usable = [], {}
    for side, _ in SIDES:
        base = anchors.get('base.' + side)
        if base is None:
            raise SiteError('draft has no base.%s anchor' % side)
        counts = {'total': len(sites), 'outside_playable': 0, 'blocked': 0}
        ok = []
        for s in sites:
            if not s['playable']:
                counts['outside_playable'] += 1
            elif blocked_by(s, footprints, corridors, clearance):
                counts['blocked'] += 1
            else:
                ok.append((math.hypot(s['x'] - base[0], s['z'] - base[1]), s['index'], s))
        ok.sort(key=lambda t: (t[0], t[1]))
        usable[side] = ok
        report.append('%s: %d candidate sites, %d outside playable, %d overlap draft pads/routes, %d usable'
                      % (side, counts['total'], counts['outside_playable'], counts['blocked'], len(ok)))
        if not ok:
            raise SiteError('no valid Airport site for %s side (%s)' % (side, report[-1]))
    best = None
    for dp, _, sp in usable['player']:
        if best and dp >= best[0]:
            break
        for de, _, se in usable['enemy']:
            if best and dp + de >= best[0]:
                break
            if not rects_overlap(rect_of(sp['x'], sp['z'], sp['w'], sp['h']),
                                 rect_of(se['x'], se['z'], se['w'], se['h']), clearance):
                best = (dp + de, sp, se, dp, de)
                break
    if best is None:
        raise SiteError('no non-overlapping player/enemy Airport pair (%s)' % '; '.join(report))
    report.append('player pick: site %d %.1f m from base; enemy pick: site %d %.1f m from base'
                  % (best[1]['index'], best[3], best[2]['index'], best[4]))
    return {'player': best[1], 'enemy': best[2]}, report


def round2(v):
    return round(v * 2) / 2 if abs(v * 2 - round(v * 2)) < 1e-6 else round(v, 3)


def apply_picks(draft, picks, measured_from):
    """Set air.<side> anchor, pad, AirReturn instance pad and building.airport footprint in place."""
    draft.setdefault('anchors', {})
    pads = draft.get('pads')
    if not isinstance(pads, dict):
        pads = draft['pads'] = {}
    instances = draft.get('instance_pads')
    size = None
    for side, faction in SIDES:
        s = picks[side]
        at = [round2(s['x']), round2(s['z'])]
        size = [round2(s['w']), round2(s['h'])]
        role = 'air.' + side
        draft['anchors'][role] = at
        pads[role] = {'at': at, 'anchor': role, 'size': size}
        if isinstance(instances, list):
            entry = next((i for i in instances if isinstance(i, dict) and i.get('anchor') == role), None)
            if entry is None:
                entry = {'id': draft.get('catalog_id', 'draft').lower() + '.f%d.airport' % faction,
                         'kind': 'AirReturn', 'faction': faction}
                instances.append(entry)
            entry.update({'at': at, 'size': size, 'anchor': role})
    # building.airport shares one footprint across sides; both picks come from the same validator.
    footprints = draft.setdefault('structure_footprints', {})
    footprints['building.airport'] = [round2(max(picks[s]['w'] for s, _ in SIDES)),
                                      round2(max(picks[s]['h'] for s, _ in SIDES))]
    note = 'airport measured_from=%s' % measured_from
    chosen = [picks[side] for side, _ in SIDES]
    if any(site.get('asset') for site in chosen):
        note += ' definitionSource=asset'
    if any(site.get('native') is False for site in chosen):
        note += ' nativeValidated=0'
    notes = draft.get('notes') or ''
    draft['notes'] = (notes + ' | ' if notes else '') + note
    return draft


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    source = parser.add_mutually_exclusive_group(required=True)
    source.add_argument('--sites', help='<trace>.airport-source-fit.json (or WARLINE_AIRPORT_SITES_OUT copy)')
    source.add_argument('--dump', help='layout dump JSON (RunCatalogLayoutDumpAndExit, WARLINE_LAYOUT_DUMP_KINDS=airport)')
    parser.add_argument('--draft', required=True, help='draft JSON from Tools/Missions/drafts')
    parser.add_argument('--clearance', type=float, default=4.0, help='metres kept clear of other pads/routes (default 4)')
    parser.add_argument('--measured-from', help='path recorded in the draft note (default: absolute --sites/--dump path)')
    parser.add_argument('--write', action='store_true', help='update the draft (in place unless --out)')
    parser.add_argument('--out', help='write the updated draft here instead of in place')
    args = parser.parse_args(argv)
    try:
        draft = json.loads(Path(args.draft).read_text(encoding='utf-8'))
        sites_path = Path(args.dump or args.sites)
        data = json.loads(sites_path.read_text(encoding='utf-8'))
        info = []
        if args.dump:
            sites, _, info = load_dump_sites(data, draft.get('catalog_id'))
        else:
            sites, _ = load_sites(data, draft.get('catalog_id'))
        picks, report = pick(draft, sites, args.clearance)
        report = info + report
    except (OSError, ValueError) as exc:
        print('airport_sites_to_pads: ' + str(exc), file=sys.stderr)
        return 1
    except SiteError as exc:
        print('airport_sites_to_pads: ' + str(exc), file=sys.stderr)
        return 1
    measured = args.measured_from or str(sites_path.resolve())
    updated = apply_picks(json.loads(json.dumps(draft)), picks, measured)
    for line in report:
        print(line)
    for side, _ in SIDES:
        role = 'air.' + side
        print(json.dumps({role: {'anchor': updated['anchors'][role], 'pad': updated['pads'][role]}}))
    print(json.dumps({'structure_footprints': {'building.airport': updated['structure_footprints']['building.airport']}}))
    print('measured_from=' + measured)
    if args.write or args.out:
        target = Path(args.out or args.draft)
        target.write_text(json.dumps(updated, indent=2) + '\n', encoding='utf-8')
        print('wrote ' + str(target))
    return 0


if __name__ == '__main__':
    sys.exit(main())
