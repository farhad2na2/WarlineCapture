#!/usr/bin/env python3
"""Compare fixed S006 setup data and disclose policy changes in a bound candidate."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable(rows):
    geometry = {'SpawnAnchorId', 'SpawnWorldX', 'SpawnWorldZ', 'AcrossOffsetMetres', 'ForwardOffsetMetres'}
    return [{name: value for name, value in row.items() if name not in geometry} for row in rows]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = args.candidate.resolve(strict=True)
    receipt_path, result_path = root / 'receipt.json', root / 'result.json'
    before = {str(path): sha(path) for path in (receipt_path, result_path)}
    receipt, result = (json.loads(path.read_bytes()) for path in (receipt_path, result_path))
    if receipt.get('status') != 'CollectedDisposablePadCandidate' or receipt.get('result_sha256') != before[str(result_path)] or receipt.get('mission_ready') is not False:
        raise ValueError('Exact disposable candidate receipt required')
    for name, expected in receipt['source_pins'].items():
        path = Path(name)
        if not path.is_file() or sha(path) != expected:
            raise ValueError('Candidate source pin changed: ' + name)
    fields = ('CatalogId', 'DefinitionId', 'ScenarioSetupId', 'OperationMapId', 'LayoutId', 'ContentVersion',
              'Seed', 'DifficultyId', 'SizeId', 'ArmyProfileId', 'StartPackageId', 'ObjectiveKind', 'Readiness',
              'DeadlineSeconds', 'PlayerFaction', 'EnemyFaction', 'MaterialsEach', 'OilEach', 'UsableFuelEach',
              'MaterialsCapacityEach', 'OilCapacityEach', 'FuelCapacityEach', 'InfantryQueuesEach', 'VehicleQueuesEach',
              'LogisticsQueuesEach', 'RefineryModulesEach', 'InfantryCapEach', 'GroundCapEach', 'TacticalAirCapEach',
              'SupplyCapEach', 'LogisticsSupportCapEach', 'DeliveryCarrierCapEach', 'ObjectiveSupportExtraPlayer',
              'PlayerBuiltStructureCapEach', 'BarrierSegmentCapEach', 'PlayerInfantry', 'PlayerGround', 'PlayerAir',
              'PlayerCombat', 'PlayerSupply', 'PlayerLogisticsSupport', 'PlayerObjectiveTrucks', 'PlayerStartingStructures',
              'PlayerDesignatedRifles', 'EnemyInfantry', 'EnemyGround', 'EnemyAir', 'EnemyCombat', 'EnemySupply',
              'EnemyLogisticsSupport', 'EnemyObjectiveTrucks', 'EnemyStartingStructures', 'EnemyDesignatedRifles')
    rows = []
    for row in result['rows']:
        base = row['setup']
        evidence = row['disposable_pad_projection']['bound_compiler']
        bound = evidence.get('setup')
        if evidence.get('compiled') is not True or not isinstance(bound, dict):
            raise ValueError('Actual bound compiler result required')
        mismatches = [name for name in fields if name not in base or name not in bound or base[name] != bound[name]]
        force_equal = stable(base['Forces']) == stable(bound['Forces'])
        structure_equal = stable(base['Structures']) == stable(bound['Structures'])
        original_policy = base.get('EnemyPolicy') or {}
        candidate_policy = bound.get('EnemyPolicy') or {}
        if not original_policy or not candidate_policy:
            raise ValueError('Actual enemy policy results required')
        additions = row['disposable_pad_projection']['candidate_additions']
        bindings = {item['PadId']: item for item in additions['instance_bindings']}
        first_kind = {}
        for pad in additions['pads']:
            key = f"{pad['FactionId']}/{pad['Kind']}"
            if key not in first_kind:
                binding = bindings.get(pad['PadId'])
                if binding is None:
                    raise ValueError('Candidate pad is missing its instance binding')
                first_kind[key] = {'pad_id': pad['PadId'], 'structure_id': binding['StructureId'],
                                   'width_metres': pad['WidthMetres'], 'depth_metres': pad['DepthMetres']}
        rows.append({'size': row['size'], 'fixed_scalar_fields_checked': len(fields), 'fixed_scalar_mismatches': mismatches,
                     'force_identity_and_quantity_equal': force_equal, 'structure_identity_and_count_equal': structure_equal,
                     'baseline_setup_hash': base['SetupHash'], 'bound_setup_hash': bound['SetupHash'],
                     'content_hash_changed': base['ContentHash'] != bound['ContentHash'],
                     'enemy_policy_fingerprint_changed': original_policy.get('Fingerprint') != candidate_policy.get('Fingerprint'),
                     'baseline_enemy_construction_pad_count': len(original_policy.get('ConstructionPads', [])),
                     'bound_enemy_construction_pad_count': len(candidate_policy.get('ConstructionPads', [])),
                     'enemy_construction_pad_data_equal': original_policy.get('ConstructionPads') == candidate_policy.get('ConstructionPads'),
                     'first_candidate_pad_by_faction_and_kind': first_kind})
    if any(sha(Path(name)) != digest for name, digest in before.items()):
        raise ValueError('Candidate evidence changed during audit')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    before[str(Path(__file__).resolve())] = sha(Path(__file__).resolve())
    report = {'kind': 'S006BoundCandidateFixedDataAudit', 'input_sha256': before, 'sizes': rows,
              'scope': 'Disposable source-only compiler comparison; altered construction pads require separate live policy, source-fit and player validation',
              'runtime_admission': False, 'mission_ready': False}
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'sizes': rows, 'mission_ready': False}))


if __name__ == '__main__':
    main()
