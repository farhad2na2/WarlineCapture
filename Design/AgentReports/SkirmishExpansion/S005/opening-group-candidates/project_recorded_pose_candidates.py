"""Read-only analytic candidates; no Unity invocation or camera acceptance."""
import hashlib
import itertools
import json
import math
from pathlib import Path

RAW = Path('Logs/SkirmishProduction/OpeningGroupCandidates/20261015-shared152-01/opening-group-candidates.jsonl')
SAFE = (400., 250., 1900., 845.)
WIDTH, HEIGHT = 2400., 1080.
FOCAL = HEIGHT / (2 * math.tan(math.radians(46 / 2)))


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def basis(yaw):
    y, p = math.radians(yaw), math.radians(65)
    return ((math.cos(y), 0., -math.sin(y)),
            (math.sin(y) * math.sin(p), math.cos(p), math.cos(y) * math.sin(p)),
            (math.sin(y) * math.cos(p), -math.sin(p), math.cos(y) * math.cos(p)))


def points(actor):
    result = []
    for mesh in actor['PosedMeshes']:
        b = mesh['WorldBounds']
        c, e = b['m_Center'], b['m_Extent']
        result.extend(tuple(c[k] + s * e[k] for k, s in zip('xyz', signs))
                      for signs in itertools.product((-1, 1), repeat=3))
    return result


def clip(poly, a, b, c):
    result = []
    for start, end in zip(poly, poly[1:] + poly[:1]):
        ds, de = a * start[0] + b * start[1] - c, a * end[0] + b * end[1] - c
        if ds <= 1e-9:
            result.append(start)
        if (ds <= 0) != (de <= 0):
            t = ds / (ds - de)
            result.append((start[0] + t * (end[0] - start[0]), start[1] + t * (end[1] - start[1])))
    return result


def project(ps, cam, axes):
    r, u, f = axes
    values = []
    for p in ps:
        d = tuple(x - c for x, c in zip(p, cam))
        depth = dot(f, d)
        if depth <= .3:
            return None
        values.append((WIDTH / 2 + FOCAL * dot(r, d) / depth,
                       HEIGHT / 2 + FOCAL * dot(u, d) / depth))
    return [min(x for x, y in values), min(y for x, y in values),
            max(x for x, y in values), max(y for x, y in values)]


def candidate(actors, yaw, height):
    axes = basis(yaw)
    r, u, f = axes
    groups = [points(a) for a in actors]
    all_points = sum(groups, [])
    mx = sum(a['ActorPosition']['x'] for a in actors) / len(actors)
    mz = sum(a['ActorPosition']['z'] for a in actors) / len(actors)
    poly = [(mx - 100, mz - 100), (mx + 100, mz - 100),
            (mx + 100, mz + 100), (mx - 100, mz + 100)]
    # Each projected safe-edge inequality is linear in camera X/Z at fixed Y.
    for p in all_points:
        for v, low, high, mid in ((r, SAFE[0], SAFE[2], WIDTH / 2),
                                 (u, SAFE[1], SAFE[3], HEIGHT / 2)):
            for edge, sign in ((low, -1), (high, 1)):
                t = (edge - mid) / FOCAL
                w = tuple(sign * (v[i] - t * f[i]) for i in range(3))
                # w.(P-C)<=0 => -wx*cx-wz*cz <= -w.P+wy*height.
                poly = clip(poly, -w[0], -w[2], -dot(w, p) + w[1] * height)
                if not poly:
                    return {'height': height, 'yaw': yaw, 'aabb_safe_fit': False}
        poly = clip(poly, f[0], f[2], dot(f, p) - f[1] * height - .300001)
        if not poly:
            return {'height': height, 'yaw': yaw, 'aabb_safe_fit': False}
    # A convex feasible-region centroid gives a camera with margin from boundaries.
    cam = (sum(p[0] for p in poly) / len(poly), height, sum(p[1] for p in poly) / len(poly))
    rects = [project(ps, cam, axes) for ps in groups]
    minimum = min((b[3] - b[1]) / HEIGHT for b in rects)
    return {'height': height, 'yaw': yaw, 'aabb_safe_fit': True,
            'camera_world': cam, 'member_aabb_pixel_rects': rects,
            'minimum_aabb_height_fraction': minimum,
            'aabb_minimum_03_satisfied': minimum >= .03,
            'actual_drawn_vertex_readability_and_occlusion_accepted': False}


def main():
    rows = [json.loads(s) for s in RAW.read_text(encoding='utf-8-sig').splitlines()]
    validations = []
    for row in rows[1:]:
        cam = tuple(row['actualCameraPosition'][k] for k in 'xyz')
        for actor in row['actorMeasurements']:
            bounds = project(points(actor), cam, basis(row['actualEuler']['y']))
            exact = actor['ProjectedPixels']
            contained = (bounds[0] <= exact['x'] + .1 and bounds[1] <= exact['y'] + .1 and
                         bounds[2] >= exact['x'] + exact['width'] - .1 and
                         bounds[3] >= exact['y'] + exact['height'] - .1)
            assert contained, (row['frame'], actor['StableActorId'], bounds, exact)
            validations.append({'frame': row['frame'], 'actor': actor['StableActorId'],
                                'captured_exact_vertex_rect_contained_by_reprojected_aabb': contained})
    output = {'raw': str(RAW).replace('\\', '/'), 'raw_sha256': hashlib.sha256(RAW.read_bytes()).hexdigest(),
              'safe_pixels': SAFE, 'safe_authority': 'Conservative manual PNG estimate; native HUD RectTransform readback unavailable in this JSONL',
              'pitch': 65, 'fov': 46, 'heights_are_absolute_camera_world_y': True,
              'scope': 'Recorded posed AABB corner containment only; AABB projected height is an upper bound on actual drawn-vertex height, so .03 success requires native exact vertices. No occlusion acceptance.',
              'captured_projection_crosschecks': validations,
              'qualified': False, 'groups': []}
    for index, yaws in ((1, [35, 215, 121, 301]), (3, [35, 215, 127, 307])):
        row = rows[index]
        values = [candidate(row['actorMeasurements'], yaw, height)
                  for height in (18, 20, 24, 28, 30) for yaw in yaws]
        output['groups'].append({'group': row['group'], 'source_frame': row['frame'], 'candidates': values})
    out = Path(__file__).with_name('ANALYTIC_POSED_AABB_CANDIDATES_20261015.json')
    out.write_text(json.dumps(output, indent=2) + '\n', encoding='utf-8')
    for group in output['groups']:
        print('group', group['group'], [(x['height'], x['yaw'], round(x.get('minimum_aabb_height_fraction', 0), 4))
              for x in group['candidates'] if x['aabb_safe_fit']])
    print(out, hashlib.sha256(out.read_bytes()).hexdigest())


if __name__ == '__main__':
    main()
