#!/usr/bin/env python3
"""Extract S026 waypoint hypotheses from current-pinned r41 owner queries."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
HERE = Path(__file__).resolve().parent
R41 = ROOT / 'Logs/SkirmishProduction/s026-citycrossroads-alias-source-diagnostic-20261116-r41/report.json'
R41_SHA = '9bc633854d9dbed1b8ab2247e635f48ab3bacf89ec38734f6187bd49e3a343f6'
RALLY = ROOT / 'Logs/SkirmishProduction/s026-standard-rally-pad-draft-20261116-r1/partial-layout-candidate.json'
RALLY_SHA = '72f3eb39a6914f8b5cb41d45429e5a0279405cd4030df42d5fb425efb1a9cb4a'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    if not output.is_relative_to(ROOT) or output.exists():
        raise ValueError('New repository-local output file required')
    if digest(R41) != R41_SHA or digest(RALLY) != RALLY_SHA:
        raise ValueError('Pinned r41 or Rally draft changed')
    spec = importlib.util.spec_from_file_location('s026_compare', HERE / 'compare_r38_primary_to_r2_refined_chain.py')
    c = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(c)
    if digest(ROOT / c.R38_REL) != c.R38_SHA256 or digest(ROOT / c.R2_CANDIDATE_REL) != c.R2_CANDIDATE_SHA256:
        raise ValueError('Historical comparison inputs changed')
    old, report, candidate = c.read_json(ROOT / c.R38_REL), c.read_json(R41), c.read_json(ROOT / c.R2_CANDIDATE_REL)
    pins = c.validate_r38_source_pins(ROOT, report)
    old_probe = old['sharedTankWaypointsWithCandidateFacilityBlockers']
    probe = report['sharedTankWaypointsWithCandidateFacilityBlockers']
    for key in ('candidateRoles', 'rifleAlternateFirstLegSubdivision'):
        if old_probe[key] != probe[key]:
            raise ValueError('Current r41 owner query results differ from r38: ' + key)
    if report.get('result') != 'CollectedUnqualified' or report.get('cleanupVerified') is not True:
        raise ValueError('Unqualified cleanup-verified r41 required')
    bounds = report['aliasPlayableBounds']
    result = []
    for role in c.ROLE_ORDER:
        primary = [r for r in probe['candidateRoles'] if r['roleId'] == role and r['candidate'] == 'TankPrimary']
        refined = [r for r in candidate['chainedOwnerSegmentCandidate']['roles'] if r['roleId'] == role]
        if len(primary) != 1 or len(refined) != 1 or not refined[0]['ownerQueryPasses']:
            raise ValueError('Unique successful role chains required')
        rows = [primary[0]['ownerSegmentChecks'], []]
        for summary in refined[0]['segments']:
            row = c.resolve_json_pointer(report, summary['receiptSelector'])
            actual = hashlib.sha256(json.dumps(row['pathCells'], separators=(',', ':')).encode()).hexdigest()
            if actual != summary['returnedPathCellsSha256'] or row['startCell'] != summary['startCell'] or row['goalCell'] != summary['exactGoalCell']:
                raise ValueError('Refined segment differs from pinned evidence')
            rows[1].append(row)
        for name, segments in zip(('TankPrimary', 'RefinedChain'), rows):
            _, start, goal, footprint = c.validate_path_rows(segments, label=role + '/' + name, bounds=bounds)
            cells = [segments[0]['startCell']] + [s['goalCell'] for s in segments]
            result.append({'roleId': role, 'candidate': name, 'segmentCount': len(segments),
                           'gridCells': cells, 'worldCellCentersXZ': [[x + .5, z + .5] for x, z in cells],
                           'footprintCells': list(footprint), 'start': list(start), 'goal': list(goal)})
    value = {'schema': 1, 'kind': 'S026R41OwnerWaypointCandidates', 'status': 'UnqualifiedDraft',
             'r41Sha256': R41_SHA, 'rallyDraftSha256': RALLY_SHA, 'currentSourcePins': pins,
             'r38QueryRowsExactlyMatchR41': True, 'chains': result,
             'limitations': ['Segment endpoints are hypotheses, not compiler route rows.',
                'Route widths, times, road semantics, dynamic occupancy and route independence are not established.',
                'Historical probe endpoints are not the selected typed spawn pads.'],
             'claims': {'compilerAccepted': False, 'routeProof': False, 'sourceFit': False, 'readiness': False}}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(value, indent=2, sort_keys=True) + '\n')
    print(json.dumps({'status': value['status'], 'output': str(output.relative_to(ROOT)),
                      'sha256': digest(output), 'chains': len(result)}))


if __name__ == '__main__':
    main()
