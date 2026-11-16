"""Convert one sealed passive S002 fuel boundary to two existing WorldReplay cases.

This only converts captured inputs/assertions; it runs no simulation. The unknown
last-two-store order is enumerated, never represented as a proved native order.
Aircraft no-op certificates are checked by the existing WorldReplay endpoint.
"""
import argparse
import copy
import hashlib
import itertools
import json
import math
import os
from pathlib import Path
import re
import stat
import struct

SOURCES = (
    'Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs',
    'Assets/Game/Scripts/Components/GridComponents.cs',
    'Assets/Game/Scripts/Components/GridCoordinateKernel.cs',
    'Assets/Game/Scripts/Editor/Skirmish/S002ForwardIntervalCapture.cs',
    'Assets/Game/Scripts/Systems/S002PhysicalStartupObservation.cs',
)
FUEL_COMPONENTS = ('UnitGrid', 'Faction', 'UnitMovementBehavior', 'UnitFuelConsumption', 'UnitFuelConsumptionState')


def need(ok, reason):
    if not ok:
        raise ValueError(reason)


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def read_regular(path, maximum=16 * 1024 * 1024):
    path = Path(path).absolute()
    before = path.lstat()
    need(stat.S_ISREG(before.st_mode) and not stat.S_ISLNK(before.st_mode) and
         0 < before.st_size <= maximum, 'Positive bounded non-symlink regular input required: ' + str(path))
    for parent in path.parents:
        metadata = parent.lstat()
        need(stat.S_ISDIR(metadata.st_mode) and not stat.S_ISLNK(metadata.st_mode),
             'Input ancestor symlink/special path rejected: ' + str(parent))
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, 'rb') as stream:
        opened = os.fstat(stream.fileno())
        need(stat.S_ISREG(opened.st_mode) and (opened.st_dev, opened.st_ino) == (before.st_dev, before.st_ino)
             and 0 < opened.st_size == before.st_size <= maximum, 'Input identity/type/size changed before read')
        raw = bytearray(opened.st_size)
        view = memoryview(raw)
        offset = 0
        while offset < len(raw):
            count = stream.readinto(view[offset:])
            need(count is not None and count > 0, 'Input truncated during fixed bounded read')
            offset += count
        need(stream.read(1) == b'' and os.fstat(stream.fileno()).st_size == opened.st_size,
             'Input grew/changed during fixed bounded read')
        return bytes(raw)


def unique(pairs):
    result = {}
    for key, value in pairs:
        need(key not in result, 'Duplicate JSON key: ' + key)
        result[key] = value
    return result


def parse(raw):
    return json.loads(raw, object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: ' + value)))


def integer(value, low, high, label):
    need(type(value) is int and low <= value <= high, 'Bounded integer required: ' + label)
    return value


def entity_id(entity):
    return str(integer(entity['index'], 0, 2147483647, 'entity index')) + ':' + str(integer(entity['version'], 0, 2147483647, 'entity version'))


def float_bits(value):
    need(type(value) in (int, float) and math.isfinite(value), 'Finite grid scalar required')
    try:
        return struct.pack('<f', value)
    except (OverflowError, struct.error) as error:
        raise ValueError('Finite float32 grid scalar required') from error


def matching_grid(left, right):
    for name in ('Width', 'Height'):
        need(integer(left[name], 1, 2097152, name) == integer(right[name], 1, 2097152, name), 'Before/captured grid dimensions differ')
    need(left['Width'] == 2048 and left['Height'] == 1024, 'Actual full S002 grid required')
    need(float_bits(left['CellSize']) == float_bits(right['CellSize']) and left['CellSize'] > 0, 'Positive matching actual CellSize required')
    for axis in ('x', 'y', 'z'):
        need(float_bits(left['Origin'][axis]) == float_bits(right['Origin'][axis]), 'Before/captured grid origin differs')


def source_pins(project, baseline):
    pins = baseline['pins']
    need(isinstance(pins, dict), 'Native baseline source pins required')
    result = {}
    for relative in SOURCES:
        candidates = [(key, value) for key, value in pins.items()
                      if key.replace('\\', '/') == relative or key.replace('\\', '/').endswith('/' + relative)]
        need(candidates and len({value for _, value in candidates}) == 1, 'Consistent native baseline aliases required: ' + relative)
        key, expected = candidates[0]
        need(isinstance(expected, str) and re.fullmatch('[0-9a-f]{64}', expected), 'Invalid native source digest')
        actual = digest(read_regular(project / relative, 16 * 1024 * 1024))
        need(actual == expected, 'Native baseline source differs: ' + relative)
        result[relative] = dict(sha256=actual, native_baseline_keys=sorted(key for key, _ in candidates))
    return result


def convert(trace, trace_sha, pins):
    need(trace['schema_version'] == 1 and trace['capture_complete'] is True and trace['capture_failure'] is None,
         'Complete successful passive capture required')
    need(trace['scope'] == 'one-grid-dispatch-and-ground-fuel-boundary' and trace['consumer_storage_tail_order_proven'] is False,
         'Supported passive scope with original unproved storage tail required')
    scope = trace['actual_consumer_scope']
    need(scope['steel_push'] is False and scope['expanded_session_count'] == 1 and scope['legacy_expanded_routing_active'] is False,
         'Only migrated single-session non-SteelPush physical fuel supported')
    before, after = trace['before_fuel'], trace['after_fuel_assertion']
    need(before['boundary'] == 'before-ground-fuel-consumer' and after['boundary'] == 'after-ground-fuel-consumer'
         and before['shared_supply_initialized'] is True, 'Actual migrated before-consumer boundary required')
    need(before['session_id'] and before['session_id'] == after['session_id']
         and before['engine_frame'] == after['engine_frame'], 'Single session/frame consumer interval required')
    grids = before['grids']
    need(isinstance(grids, list) and len(grids) == 1, 'Unique actual before-consumer grid required')
    captured_grid = trace['before_grid_borrowed_facts']['grid']
    matching_grid(grids[0]['config'], captured_grid)
    native_rows = trace['exact_fuel_query_rows_in_iteration_order']
    need(isinstance(native_rows, list) and len(native_rows) == 34, 'Complete actual34 global fuel query required')
    roster, seen, aircraft, metadata_absent = [], set(), 0, 0
    for row in native_rows:
        identity = entity_id(row['entity'])
        need(row['query_id'] == identity and identity not in seen, 'Unique exact fuel query identity required')
        seen.add(identity)
        need(row['entity_enabled'] is True, 'Default fuel query cannot include disabled entity')
        types, components = set(row['component_types']), row['components']
        need(not {'Unity.Entities.Prefab', 'Unity.Entities.Disabled'} & types and
             all('Game.Components.' + name in types and isinstance(components[name], dict) for name in FUEL_COMPONENTS),
             'Actual complete five-component fuel query values required')
        for name in ('UnitFootprint', 'LocalTransform'):
            need(isinstance(components[name], dict), 'Actual body prestate required: ' + name)
        role = components['SkirmishUnitRoleComponent']
        if role is None:
            role_id = category = None
            metadata_absent += 1
        else:
            role_id = integer(role['Role'], 0, 255, 'role')
            category = integer(role['Category'], 0, 255, 'category')
        air = components['UnitAirMovement']
        need((air is not None) == ('Game.Components.UnitAirMovement' in types), 'Aircraft presence/value mismatch')
        if air is not None:
            need(isinstance(air, dict) and components['UnitFuelConsumptionState']['Initialized'] == 1,
                 'Initialized aircraft prestate required; no general aircraft fallback')
            aircraft += 1
        roster.append(dict(id=identity, entity=copy.deepcopy(row['entity']), role=role_id, category=category,
                           faction=integer(components['Faction']['Id'], 0, 255, 'faction'), components=copy.deepcopy(components)))
    need(aircraft > 0, 'Global contains-aircraft fact must be retained')
    stores = trace['global_storage_census_before']
    need(isinstance(stores, list) and len(stores) == 51, 'Complete51 before-store census required')
    before_stores = {}
    for row in stores:
        need(row['id'] == entity_id(row['entity']) and row['id'] not in before_stores and row['entity_enabled'] is True
             and isinstance(row['value'], dict), 'Unique enabled before-storage identity/value required')
        before_stores[row['id']] = copy.deepcopy(row['value'])
    visits = trace['actual_storage_query_visits_in_iteration_order']
    need(isinstance(visits, list) and len(visits) == 97, 'Complete actual97 native visits required')
    per_faction, last_faction = {}, -1
    for row in visits:
        faction = integer(row['faction'], 0, 255, 'visited faction')
        need(faction >= last_faction, 'Native faction visit groups must be ascending')
        last_faction = faction
        order = per_faction.setdefault(faction, [])
        need(row['ordinal'] == len(order) and row['id'] in before_stores and row['id'] not in order,
             'Dense unique native per-faction storage prefix required')
        order.append(row['id'])
    need(len(per_faction) == 2 and sorted(map(len, per_faction.values())) == [48, 49], 'Supported two48/49 native prefixes required')
    prefix = max(per_faction.values(), key=len)
    need(all(order == prefix[:len(order)] for order in per_faction.values()), 'Native storage visit prefixes do not match')
    tail = sorted(set(before_stores) - set(prefix))
    need(len(prefix) == 49 and len(tail) == 2, 'Exactly two unvisited storage tail identities required')
    # All following native outputs are assertion payloads only. None supplies
    # query membership/order, role metadata, before component values or grid facts.
    post_states = trace['fuel_query_post_states_assertion']
    need([row['id'] for row in post_states] == [row['id'] for row in roster], 'Complete ordered34 poststate assertions required')
    post = {row['id']: row['fuel_state_assertion'] for row in post_states}
    after_stores = {}
    for row in trace['global_storage_census_after_assertion']:
        need(row['id'] == entity_id(row['entity']) and row['id'] not in after_stores and isinstance(row['value'], dict),
             'Unique after-storage assertion identity required')
        after_stores[row['id']] = row['value']
    need(set(after_stores) == set(before_stores), 'Complete51 after-store assertions required')
    requested = trace['requested_bits_assertion']
    need(isinstance(requested, list) and len(requested) == 256 and
         all(type(value) is int and -2147483648 <= value <= 2147483647 for value in requested), 'Complete256 requested-bit assertions required')
    cases = []
    for index, permutation in enumerate(itertools.permutations(tail)):
        order = prefix + list(permutation)
        authority = dict(evidence_kind='captured_stage', evidence_sha256=trace_sha, before_boundary='before-ground-fuel-consumer',
                         caller_policy_sha256=pins[SOURCES[0]]['sha256'], grounding_policy_sha256=None, collision_policy_sha256=None,
                         order_evidence_sha256=trace_sha, unit_query_complete=True, storage_query_complete=True,
                         steering_query_complete=False, shared_supply_initialized=True, expanded_session_count=scope['expanded_session_count'],
                         steel_push=scope['steel_push'], contains_aircraft=True)
        cases.append(dict(id='actual-fuel-' + trace_sha[:12] + '-tail-' + str(index), kind='body_transition', stage='ground_fuel_consumer',
            prestate_scope='complete_global_fuel_query', storage_order_proof=dict(native_full_order_proven=False,
                observed_prefix=copy.deepcopy(prefix), unordered_unvisited_tail=tail, completion_index=index, completion_count=2, evidence_sha256=trace_sha),
            authority=authority, roster=copy.deepcopy(roster), unit_query_order=[row['id'] for row in roster], storage_query_order=order,
            stores=[dict(id=identity, value=copy.deepcopy(before_stores[identity])) for identity in order], steering_steps=[],
            expected=dict(requested_bits=copy.deepcopy(requested), unit_states=[dict(id=row['id'],
                last_cell=[post[row['id']]['LastCell']['x'], post[row['id']]['LastCell']['y']], initialized=post[row['id']]['Initialized']) for row in roster],
                stores=[dict(id=identity, value=copy.deepcopy(after_stores[identity])) for identity in order]),
            fuel_grids=copy.deepcopy(grids), captured_grid_config=copy.deepcopy(captured_grid)))
    return dict(schema_version=1, cases=cases), dict(query_rows=len(roster), stores=len(stores), native_visits=len(visits),
        foreign_role_metadata_absent_rows=metadata_absent, aircraft_rows=aircraft,
        faction_visit_counts={str(key): len(value) for key, value in per_faction.items()},
        longest_observed_prefix_length=len(prefix), common_overlapping_prefix_length=min(map(len, per_faction.values())), tail_completions=2)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('project', 'trace', 'baseline', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    args = parser.parse_args()
    project = args.project.resolve()
    trace_raw, baseline_raw = read_regular(args.trace, 32 * 1024 * 1024), read_regular(args.baseline, 16 * 1024 * 1024)
    trace, baseline = parse(trace_raw), parse(baseline_raw)
    pins = source_pins(project, baseline)
    packet, facts = convert(trace, digest(trace_raw), pins)
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    packet_raw = (json.dumps(packet, indent=2, ensure_ascii=False) + '\n').encode()
    (output / 'input.json').write_bytes(packet_raw)
    need(read_regular(args.trace, 32 * 1024 * 1024) == trace_raw and read_regular(args.baseline, 16 * 1024 * 1024) == baseline_raw, 'Raw trace/baseline bytes changed during conversion')
    need(source_pins(project, baseline) == pins, 'Native source closure changed during conversion')
    receipt = dict(result='ConvertedNotSimulated', trace_sha256=digest(trace_raw), baseline_sha256=digest(baseline_raw),
        trace_bytes_unchanged=True, baseline_bytes_unchanged=True, source_pins=pins, converter_sha256=digest(read_regular(Path(__file__))),
        emitted_input_sha256=digest(packet_raw), facts=facts, original_observed_flag_value=False, native_full_storage_order_proven=False,
        per_case_universal_invariance_claim=False, paired_tail_invariance_proven_by_converter=False, aircraft_noops_certified_by_converter=False,
        expected_only_sources=['requested_bits_assertion', 'fuel_query_post_states_assertion', 'global_storage_census_after_assertion'],
        native_runs=0, complete_frame_derived=False, mission_qualified=False)
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(dict(result='ConvertedNotSimulated', cases=2, query_rows=facts['query_rows'], stores=facts['stores'], output=str(output))))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
