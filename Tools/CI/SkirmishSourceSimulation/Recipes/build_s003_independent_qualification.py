#!/usr/bin/env python3
"""Reconstruct current S003 source evidence; this does not admit a runtime."""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
AUDITOR = ROOT / 'Tools/CI/SkirmishSourceSimulation/Recipes/audit_s003_independent_source_chain.py'
PREVIOUS = ROOT / 'T Logs/SkirmishProduction/s003-independent-source-chain-audit-20261105-T1/audit.json'
FIXTURE = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7/receipt.json'


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def descriptor(path):
    path = path.resolve(strict=True)
    return {'path': str(path), 'sha256': digest(path)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--compiler', type=Path, required=True)
    parser.add_argument('--source-build', type=Path, required=True)
    parser.add_argument('--prior-audit', type=Path, required=True)
    parser.add_argument('--fixture', type=Path, default=FIXTURE)
    args = parser.parse_args()
    out = args.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be in the repository')
    out.mkdir(parents=True, exist_ok=False)
    prior = args.prior_audit.resolve(strict=True)
    if not prior.is_relative_to(ROOT):
        raise ValueError('Prior audit must be in the repository')
    fixture_path = args.fixture.resolve(strict=True)
    if not fixture_path.is_relative_to(ROOT):
        raise ValueError('Importer fixture must be inside the repository')
    pins = {str(p): digest(p) for p in (AUDITOR, prior, fixture_path, Path(__file__).resolve())}
    audit_path = out / 'source-chain-audit.json'
    run = subprocess.run([sys.executable, '-B', str(AUDITOR), '--output', str(audit_path), '--compiler', str(args.compiler), '--source-build', str(args.source_build)],
                         cwd=ROOT, capture_output=True, text=True, timeout=600)
    (out / 'audit.log').write_text(run.stdout + run.stderr)
    if run.returncode:
        raise RuntimeError('Fresh independent source chain audit failed')
    audit = json.loads(audit_path.read_bytes())
    if audit != json.loads(prior.read_bytes()):
        raise ValueError('Fresh source chain differs from the prior independent audit')
    fixture = json.loads(fixture_path.read_bytes())
    expected_marker = ('[S003RefreshedPrefabImportControl] result=Passed roots=19 entities=647 '
                       'unsupported=32 omitted=26 negatives=3 sourceProjection=Passed world=0 readiness=0')
    if (fixture.get('status') != 'PassedDisposableS003ImporterFixture' or
            fixture.get('marker') != expected_marker or
            not fixture.get('compiled_importer', '').endswith('/305_GeneratedReferenceTemplateImporter.s003.cs') or
            fixture.get('movement_source_projection_checked') is not True or
            fixture.get('world_allocated') is not False or fixture.get('mission_ready') is not False):
        raise ValueError('Current isolated source importer/movement fixture required')
    for path, sha in pins.items():
        if digest(Path(path)) != sha:
            raise ValueError('Qualification input changed: ' + path)
    result = {'schema_version': 1, 'kind': 'S003IndependentCurrentSourceEvidencePackage',
              'scope': 'SourceEvidenceOnly_NoRuntimeAdmission', 'source_chain_audit': descriptor(audit_path),
              'independent_prior_audit': descriptor(prior), 'source_chain_auditor': descriptor(AUDITOR),
              'prefab_movement_fixture': descriptor(fixture_path), 'builder': descriptor(Path(__file__)),
              'target': {'catalog_id': 'S003', 'size': 'Standard', 'difficulty': 'Regular',
                         'seed': 104732, 'setup_hash': 2394754492},
              'runtime_admission': False, 'native_context_qualified': False,
              'source_fit_certified': False, 'mission_ready': False}
    path = out / 'source-evidence-package.json'
    path.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'status': 'PassedSourceEvidenceOnly', 'package': descriptor(path),
                      'runtime_admission': False, 'mission_ready': False}))


if __name__ == '__main__':
    main()
