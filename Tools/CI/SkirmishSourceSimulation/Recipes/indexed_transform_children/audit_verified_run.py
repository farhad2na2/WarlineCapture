#!/usr/bin/env python3
"""Audit a checked same-host original-versus-indexed transform control."""
import argparse
import hashlib
import json
from pathlib import Path
import re


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run', type=Path, required=True)
    parser.add_argument('--comparison', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    run = args.run.resolve(strict=True)
    comparison = args.comparison.resolve(strict=True)
    paths = [run / name for name in ('run-receipt.json', 'result.json', 'process.log')] + [comparison]
    pins = {str(path): sha(path) for path in paths}
    receipt, result, compared = (json.loads(path.read_bytes()) for path in (paths[0], paths[1], comparison))
    if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0 or
            receipt.get('timed_out') is not False or receipt.get('validation_passed') is not False or
            receipt.get('source_result') != result):
        raise ValueError('Completed unqualified source control required')
    provenance = receipt.get('source_provenance_classification') or {}
    if provenance.get('experimental_generator_proof') is not True or provenance.get('canonical_manifest') is not False:
        raise ValueError('Experimental source generator classification required')
    log = paths[2].read_text(errors='strict')
    markers = re.findall(r'^\[SourceChildParity\] compared=(\d+)(?: originalTicks=(\d+) indexedTicks=(\d+) frequency=(\d+))? scope=ActualSourceHostOnly$', log, re.MULTILINE)
    if len(markers) != 1 or int(markers[0][0]) <= 0:
        raise ValueError('Positive unique actual source comparison marker required')
    count, original, indexed, frequency = markers[0]
    if original and (int(frequency) <= 0 or int(original) <= 0 or int(indexed) <= 0):
        raise ValueError('Positive original, indexed and frequency timing required')
    if (compared.get('kind') != 'S003IndexedTransformRunComparison' or
            compared.get('input_sha256', {}).get(str(paths[0])) != pins[str(paths[0])] or
            compared.get('input_sha256', {}).get(str(paths[1])) != pins[str(paths[1])] or
            any(not row.get('exact_after_declared_normalization') for row in compared['normalized_differences'].values())):
        raise ValueError('Exact same-run trace and validator comparison required')
    if any(sha(Path(name)) != expected for name, expected in pins.items()):
        raise ValueError('Control evidence changed during audit')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    report = {'kind': 'S003VerifiedTransformChildControl', 'input_sha256': pins,
              'actual_source_comparisons': int(count), 'tick': result['tick'],
              'trace_rows': compared['normalized_differences']['trace']['candidate_rows'],
              'validator_snapshots': compared['normalized_differences']['validator']['candidate_rows'],
              'same_input_submethod_timing': ({'original_seconds': int(original) / int(frequency),
                  'indexed_seconds': int(indexed) / int(frequency), 'frequency': int(frequency),
                  'scope': 'Alternating same-call source-host timing; excludes whole-simulator timing'} if original else None),
              'scope': 'Same-host original-versus-indexed comparison; no native parity, general performance or readiness claim',
              'mission_ready': False}
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'compared': report['actual_source_comparisons'], 'tick': report['tick'], 'mission_ready': False}))


if __name__ == '__main__':
    main()
