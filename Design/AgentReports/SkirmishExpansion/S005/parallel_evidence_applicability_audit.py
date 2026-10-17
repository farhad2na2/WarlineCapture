"""Read-only source/evidence comparison; does not grant native acceptance.

Run from any directory. Writes only the adjacent uniquely owned audit JSON.
"""
import hashlib
import json
from pathlib import Path

REPORTS = Path(__file__).resolve().parent
ROOT = REPORTS.parents[3]
TARGET = 'final-1351-post-campaign-merge-postcompile-source-manifest.json'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.is_file() else None


def read(name):
    return json.loads((REPORTS / name).read_text())


def compare(name):
    old, new = read(name), read(TARGET)
    result = {}
    for section in ('files', 'materials'):
        a, b = old.get(section, {}), new.get(section, {})
        result[section] = [
            {'path': path, 'before': a.get(path), 'after': b.get(path)}
            for path in sorted(a.keys() | b.keys()) if a.get(path) != b.get(path)
        ]
    result['runtimeCodeChanges'] = [x['path'] for x in result['files']
                                    if x['path'].endswith('.cs') and '/Editor/' not in x['path']]
    result['manifestSha256'] = digest(REPORTS / name)
    return result


def main():
    target = read(TARGET)['files']
    baseline = 'final-1258-ground-carrier-post-source-manifest.json'
    cargo = 'final-1338-baker-version-repair-source-manifest.json'
    closure = read('delegation-native1258-ground-carrier-closure.json')
    evidence = {name: {'expected': sha, 'actual': digest(REPORTS / name)}
                for name, sha in closure['evidence'].items()}
    unchanged = []
    old = read(baseline)['files']
    for path, sha in sorted(old.items()):
        if not path.endswith('.cs'):
            continue
        if any(token in path for token in (
            'TransportBoarding', 'UnitTransport', 'Checkpoint', 'UnitDeath',
            'GridUtility', 'GridPath', 'GridNavigation', 'SkirmishGroundCarrierDeathTests'
        )) and not path.endswith('UnitTransportPlaneDoorAuthoringTests.cs'):
            unchanged.append({'path': path, 'baseline': sha, 'target': target.get(path),
                              'workingFile': digest(ROOT / path)})
    report = {
        'scope': 'Independent offline applicability audit; no new Unity execution or acceptance waiver.',
        'targetManifest': TARGET,
        'targetManifestSha256': digest(REPORTS / TARGET),
        'comparisons': {baseline: compare(baseline), cargo: compare(cargo)},
        'carrierEvidenceIntegrity': evidence,
        'carrierEvidenceIntegrityPassed': all(x['expected'] == x['actual'] for x in evidence.values()),
        'carrierAndCheckpointSourceInventory': unchanged,
        'carrierAndCheckpointInventoryUnchanged': bool(unchanged) and all(
            x['baseline'] == x['target'] == x['workingFile'] for x in unchanged),
        'decisions': [
            {'area': 'Nine isolated carrier-death cases1258',
             'decision': 'Retain original scoped pass; audited source inventory and immutable evidence are compared below.',
             'limit': 'Inventory is not a complete transitive dependency proof. Do not promote to current full mission, current compilation, map placement or Save/Continue acceptance.'},
            {'area': 'Paid cargo lifecycle1344',
             'decision': 'Retain historical scoped pass; aircraft/transport code can be compared independently of merged visuals.',
             'limit': 'Merged map presentation, initialization and shared AI planner changed. Reuse for current native cargo certification needs producer/map/initialization applicability, preferably observed in an already scheduled air run; no automatic full replay or automatic current pass.'},
            {'area': 'Six ARIA wins, English recovery1262, lifecycle1273',
             'decision': 'Preserve original passes, but do not relabel as exact current-source journeys.',
             'limit': 'Since1258, AttackMove, world movement, blockers, air movement, authoring and production composition changed. Current scheduled ground/manual/Stop journeys should cover affected paths. Original ten-cycle evidence need not be erased solely because visual assets changed.'},
            {'area': 'Campaign AI planner merge',
             'decision': 'Changed shared planner now reads CampaignAIBuildEligibility; its no-Campaign-runtime/None/empty-ID branches disable filtering.',
             'limit': 'Code branch inspection is conditional, not proof of S005 runtime state. Record this condition in a scheduled S005 run or focused existing regression before declaring behavioral equivalence.'},
            {'area': 'External acceptance',
             'decision': 'Keep S006-dependent family coverage and real-device/owner acceptance separate from remaining S005 implementation work.',
             'limit': 'No shipping-ready flag change or authored gate removal.'},
        ],
        'recommendedNextChecks': [
            'Use already running1352 plus queued1353/1354/1355 for affected current-source integration evidence.',
            'Update current status with cargo1344 scoped pass and latest Fighter failure;1270 is stale as latest air status.',
            'After1352 closes, resolve each failure to gameplay, driver, observer or infrastructure before another attempt.',
            'Do not rerun the entire baseline matrix merely because the whole-source hash changed; first map changed behavior to acceptance obligations.',
        ],
        'playerReady': False,
    }
    output = REPORTS / 'parallel-evidence-applicability-20261012.json'
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'report': str(output), 'carrierEvidenceIntegrityPassed': report['carrierEvidenceIntegrityPassed'],
                      'sourceFilesCompared': len(unchanged),
                      'carrierAndCheckpointInventoryUnchanged': report['carrierAndCheckpointInventoryUnchanged'],
                      'deltaCounts': {k: {s: len(v[s]) for s in ('files', 'materials', 'runtimeCodeChanges')}
                                      for k, v in report['comparisons'].items()}}, indent=2))


if __name__ == '__main__':
    main()
