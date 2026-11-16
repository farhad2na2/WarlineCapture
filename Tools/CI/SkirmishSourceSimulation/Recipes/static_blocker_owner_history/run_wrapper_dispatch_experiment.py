#!/usr/bin/env python3
"""Compare original owner with an isolated source-host wrapper-dispatch candidate.

No game/default host sources are modified. A passing result is fixture evidence,
not gameplay parity, mission readiness, or a simulator speed claim.
"""
import argparse
import hashlib
import json
import statistics
from pathlib import Path

import run_compiled_owner_differential as owner

ROOT = Path(__file__).resolve().parents[5]
SOURCE = ROOT / 'Tools/CI/SkirmishSourceSimulation/Adapters/121_OwnedSourceQueries.cs'
ANCHOR = 'var typed=System.Linq.Expressions.Expression.Lambda<Func<EntityManager,Entity,T>>(call,manager,entity).Compile();return(m,e)=>{try{return typed(m,e);}catch(Exception error){throw new System.Reflection.TargetInvocationException(error);}};'
REPLACEMENT = ('var error=System.Linq.Expressions.Expression.Parameter(typeof(Exception),"error");'
               'var exceptionConstructor=typeof(System.Reflection.TargetInvocationException).GetConstructor(new[]{typeof(Exception)});'
               'var guarded=System.Linq.Expressions.Expression.TryCatch(call,System.Linq.Expressions.Expression.Catch(error,'
               'System.Linq.Expressions.Expression.Throw(System.Linq.Expressions.Expression.New(exceptionConstructor,error),typeof(T))));'
               'return System.Linq.Expressions.Expression.Lambda<Func<EntityManager,Entity,T>>(guarded,manager,entity).Compile();')

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def verify(pins):
    for name, expected in pins.items():
        if sha(Path(name)) != expected:
            raise ValueError('Build source/reference pin changed: ' + name)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    build_path = args.build.resolve(strict=True)
    build_raw = build_path.read_bytes()
    build = json.loads(build_raw)
    if build.get('status') != 'Passed' or build.get('mission') != 'S002':
        raise ValueError('Passed S002 source build required')
    pins = build['source_and_reference_pins']
    verify(pins)
    original = SOURCE.read_text()
    if original.count(ANCHOR) != 1:
        raise ValueError('Exact original wrapper dispatch anchor required')
    out = args.output.absolute()
    out.mkdir(parents=True, exist_ok=False)
    candidate = out / '121_OwnedSourceQueries.Candidate.cs'
    candidate.write_text(original.replace(ANCHOR, REPLACEMENT, 1))
    candidate_sha, control_sha, runner_sha = sha(candidate), sha(owner.CONTROL), sha(Path(__file__))
    response_path = Path(build['compiler_response']['path']).resolve(strict=True)
    if sha(response_path) != build['compiler_response']['sha256']:
        raise ValueError('Compiler response changed')
    response = [json.loads(line) for line in response_path.read_text().splitlines() if line.strip()]
    if response.count(str(SOURCE)) != 1:
        raise ValueError('One exact original query adapter source required')
    response[response.index(str(SOURCE))] = str(candidate)
    candidate_rsp = out / 'candidate-host.rsp'
    candidate_rsp.write_text(''.join(json.dumps(value) + '\n' for value in response))
    derived = dict(build)
    derived['compiler_response'] = {'path': str(candidate_rsp), 'sha256': sha(candidate_rsp)}
    derived_path = out / 'candidate-build-fixture.json'
    derived_path.write_text(json.dumps(derived, indent=2) + '\n')
    observations = {'original': [], 'candidate': []}
    reference = None
    for iteration in range(3):
        for label in (('original', 'candidate') if iteration % 2 == 0 else ('candidate', 'original')):
            result = owner.compile_and_run(build_path if label == 'original' else derived_path,
                                           label, out / f'{iteration + 1}-{label}', ROOT)
            observed = result['result']
            state = {'cases': observed['cases'], 'steady_blocker_rows': observed['steady_blocker_rows'],
                     'steady_cases': [{k: item[k] for k in ('kind', 'before', 'after', 'full_update_measured',
                                                            'setup_and_state_hashing_included')}
                                      for item in observed['steady_cases']]}
            if reference is None:
                reference = state
            if state != reference:
                raise ValueError('Original owner fixture state differed: ' + label)
            observations[label].append(result)
    verify(pins)
    if (build_path.read_bytes() != build_raw or SOURCE.read_text() != original or
            sha(candidate) != candidate_sha or sha(owner.CONTROL) != control_sha or sha(Path(__file__)) != runner_sha):
        raise ValueError('Original input changed during comparison')
    timing = {}
    for label, records in observations.items():
        timing[label] = {}
        for kind in ('warm_steady_state', 'forced_rebuild'):
            values = [next(x['on_update_elapsed_ms'] for x in r['result']['steady_cases'] if x['kind'] == kind)
                      for r in records]
            timing[label][kind] = {'samples_ms': values, 'median_ms': statistics.median(values)}
    receipt = {'kind': 'StaticBlockerWrapperDispatchFixtureComparison', 'status': 'Passed',
               'scope': 'Three alternating original-owner source-host fixture pairs only; not simulation speed or mission readiness',
               'source_sha256': sha(SOURCE), 'candidate_sha256': candidate_sha,
               'control_sha256': control_sha, 'runner_sha256': runner_sha,
               'build_sha256': hashlib.sha256(build_raw).hexdigest(), 'source_pin_count': len(pins),
               'source_pins_rechecked': True, 'fixture_cases': reference['cases'],
               'blocker_rows': reference['steady_blocker_rows'], 'timing': timing,
               'mission_run': False, 'native_parity': False, 'mission_ready': False}
    (out / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps(receipt, indent=2))

if __name__ == '__main__':
    main()
