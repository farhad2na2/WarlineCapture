#!/usr/bin/env python3
"""Verify that two source builds differ only in the indexed child scan and its readset seal."""
import argparse
import hashlib
import json
from pathlib import Path
import generate


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--baseline', type=Path, required=True)
    parser.add_argument('--candidate', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    roots = [args.baseline.resolve(strict=True), args.candidate.resolve(strict=True)]
    receipts = [json.loads((root / 'build.json').read_bytes()) for root in roots]
    for receipt in receipts:
        if receipt.get('status') != 'Passed' or receipt.get('compile_exit') != 0:
            raise ValueError('Successful actual source builds required')
        for name, expected in receipt['source_and_reference_pins'].items():
            path = Path(name)
            if not path.is_file() or sha(path) != expected:
                raise ValueError('Source build pin changed: ' + name)
    responses = [[json.loads(line) for line in (root / 'compile.rsp').read_text().splitlines() if line.strip()] for root in roots]
    if len(responses[0]) != len(responses[1]):
        raise ValueError('Compiler response lengths differ')
    placement_receipts = [json.loads((root / 'placement/build.json').read_bytes()) for root in roots]
    placement_pins = []
    placement_responses = []
    for root, receipt in zip(roots, placement_receipts):
        if receipt.get('status') != 'Passed' or receipt.get('compile_exit') != 0 or receipt.get('input_drift'):
            raise ValueError('Successful placement-domain source compilation required')
        normalized = {}
        for name, digest in receipt['source_and_reference_pins'].items():
            path = Path(name)
            if not path.is_file() or sha(path) != digest:
                raise ValueError('Placement-domain source pin changed: ' + name)
            normalized[str(path.relative_to(root)) if path.is_relative_to(root) else name] = digest
        placement_pins.append(normalized)
        placement_responses.append([json.loads(line).replace(str(root), '<build>')
                                    for line in (root / 'placement/compile.rsp').read_text().splitlines() if line.strip()])
    if placement_pins[0] != placement_pins[1] or placement_responses[0] != placement_responses[1]:
        raise ValueError('Placement-domain inputs or compiler options differ')
    transform_source = (Path(__file__).resolve().parents[5] / generate.SOURCE).resolve(strict=True)
    original = transform_source.read_bytes()
    _, expected = generate.transform(original)
    candidate_source = roots[1] / 'source/generated/SourceMissionTransformBridgeIndexed.cs'
    if candidate_source.read_bytes() != expected.encode('utf-8'):
        raise ValueError('Candidate is not the exact declared source transformation')
    differences = []
    for left, right in zip(*responses):
        if left.startswith('-out:') and right.startswith('-out:'):
            continue
        if left == '-r:' + str(roots[0] / 'placement/SourcePlacementDomain.dll') and right == '-r:' + str(roots[1] / 'placement/SourcePlacementDomain.dll'):
            differences.append('placement_domain_recompiled')
            continue
        if left.endswith('.cs') and right.endswith('.cs'):
            a, b = Path(left), Path(right)
            if a.resolve() == transform_source and b.resolve() == candidate_source.resolve():
                differences.append('indexed_child_scan')
            elif a.name == 'SourceTransformConsumedReadset.cs' and b.name == a.name:
                differences.append('build_specific_readset_seal')
            elif a.name == 'SourceNavigationFactsIdentity.cs' and b.name == a.name:
                if a.read_text().replace(str(roots[0]), '<build>') != b.read_text().replace(str(roots[1]), '<build>'):
                    raise ValueError('Navigation identity differs beyond the build path')
                differences.append('navigation_identity_build_path')
            elif sha(a) != sha(b):
                raise ValueError('Unexpected compiler source difference: ' + left + ' / ' + right)
        elif left != right:
            raise ValueError('Unexpected compiler option/reference difference: ' + left + ' / ' + right)
    if sorted(differences) != ['build_specific_readset_seal', 'indexed_child_scan', 'navigation_identity_build_path', 'placement_domain_recompiled']:
        raise ValueError('Exact declared build-dependent differences required')
    readsets = [json.loads((root / 'SourceSimulation.exe.transform-readset.json').read_bytes()) for root in roots]
    maps = []
    for root, readset in zip(roots, readsets):
        maps.append({str(Path(name).relative_to(root)) if Path(name).is_relative_to(root) else name: digest
                     for name, digest in readset['source_pins'].items()})
    left, right = maps
    candidate_key = 'source/generated/SourceMissionTransformBridgeIndexed.cs'
    if (left.get(str(transform_source)) != hashlib.sha256(original).hexdigest() or
            right.get(candidate_key) != sha(candidate_source)):
        raise ValueError('Transform readset source identity mismatch')
    del left[str(transform_source)]
    del right[candidate_key]
    if left != right or any({k: v for k, v in item.items() if k != 'source_pins'} !=
                            {k: v for k, v in readsets[0].items() if k != 'source_pins'} for item in readsets):
        raise ValueError('Unexpected transform readset difference')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    report = {'kind': 'S003IndexedTransformBuildComparison', 'baseline_build_sha256': sha(roots[0] / 'build.json'),
              'candidate_build_sha256': sha(roots[1] / 'build.json'), 'compiler_source_differences': differences,
              'placement_source_and_reference_pins_equal': len(placement_pins[0]),
              'placement_dll_byte_identical': sha(roots[0] / 'placement/SourcePlacementDomain.dll') == sha(roots[1] / 'placement/SourcePlacementDomain.dll'),
              'identical_other_transform_readset_pins': len(left), 'baseline_source_sha256': hashlib.sha256(original).hexdigest(),
              'candidate_source_sha256': sha(candidate_source),
              'scope': 'Source build comparison only; runtime parity and performance require run evidence', 'mission_ready': False}
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report))


if __name__ == '__main__':
    main()
