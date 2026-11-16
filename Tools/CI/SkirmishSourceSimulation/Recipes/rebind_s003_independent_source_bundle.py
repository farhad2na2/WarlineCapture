#!/usr/bin/env python3
"""Bind current S003 source-only evidence into retained source inputs."""
import argparse
import hashlib
import json
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[4]
OLD = ROOT / 'T Logs/SkirmishProduction/s003-current-source-bundle-20261104-T1/data-bundle.json'
EVIDENCE = ROOT / 'T Logs/SkirmishProduction/s003-original-planner-world-package-20261105-T1/source-evidence-package.json'
PROJECTION = ROOT / 'T Logs/SkirmishProduction/s003-current-factory-projection-20261105-T1/projection-receipt.json'
TEMPLATE = ROOT / 'T Logs/SkirmishProduction/s003-independent-prefab-domain-20261105-T5/reference-template-descriptor.json'
MOVEMENT = ROOT / 'T Logs/SkirmishProduction/s003-current-movement-source-input-20261105-T1/unit-movement-source-input.json'
CAPTURE = ROOT / 'Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1/data/s003-native-metadata.json'


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def descriptor(path):
    path = path.resolve(strict=True)
    return {'path': str(path), 'sha256': digest(path)}


def write(path, value):
    with path.open('x') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')
    return descriptor(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--evidence', type=Path, default=EVIDENCE)
    parser.add_argument('--projection', type=Path, default=PROJECTION)
    parser.add_argument('--capture', type=Path, default=CAPTURE)
    parser.add_argument('--templates', type=Path, default=TEMPLATE)
    parser.add_argument('--movement', type=Path, default=MOVEMENT)
    args = parser.parse_args()
    out = args.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must be in the repository')
    old = json.loads(OLD.read_bytes())
    old_init_desc = old['initialization']
    if descriptor(Path(old_init_desc['path'])) != old_init_desc or old.get('mission') != 'S003' or old.get('seed') != 104732:
        raise ValueError('Exact retained S003 source bundle required')
    initial = json.loads(Path(old_init_desc['path']).read_bytes())
    evidence_path = args.evidence.resolve(strict=True)
    if not evidence_path.is_relative_to(ROOT):
        raise ValueError('Source evidence must be in the repository')
    evidence = json.loads(evidence_path.read_bytes())
    projection_path = args.projection.resolve(strict=True)
    capture_path = args.capture.resolve(strict=True)
    template_path = args.templates.resolve(strict=True)
    movement_path = args.movement.resolve(strict=True)
    if any(not p.is_relative_to(ROOT) for p in (projection_path, capture_path, template_path, movement_path)):
        raise ValueError('Source projection inputs must be repository-owned')
    projection = json.loads(projection_path.read_bytes())
    if evidence.get('kind') != 'S003IndependentCurrentSourceEvidencePackage' or any(evidence.get(k) is not False for k in ('runtime_admission', 'native_context_qualified', 'source_fit_certified', 'mission_ready')):
        raise ValueError('Independent source-only evidence required')
    # Use the actual runtime admission owner in a separate process before writing
    # any bundle. It rechecks current evidence owners and source pins and keeps
    # the process-global configuration binding isolated from this preparer.
    cli = ROOT / 'Tools/CI/run_skirmish_source_simulation.py'
    temporary_root = ROOT / 'T Logs/SkirmishProduction'
    with tempfile.TemporaryDirectory(prefix='s003-bundle-admission-', dir=temporary_root) as temporary:
        checked = subprocess.run([sys.executable, '-B', str(cli), 'resolve', '--mission', 'S003',
                                  '--seed', '104732', '--configuration-qualification', str(evidence_path),
                                  '--output', str(Path(temporary) / 'resolve')],
                                 cwd=ROOT, capture_output=True, text=True, timeout=660)
        if checked.returncode:
            raise ValueError('S003 runtime source-evidence check rejected bundle: ' + checked.stderr[-2000:])
    if projection.get('capture') != descriptor(capture_path) or projection.get('runtime_admission') is not False:
        raise ValueError('Current factory projection required')
    evidence_audit_descriptor = evidence.get('source_chain_audit')
    if not isinstance(evidence_audit_descriptor, dict) or descriptor(Path(evidence_audit_descriptor['path'])) != evidence_audit_descriptor:
        raise ValueError('Current source-chain audit required')
    evidence_audit = json.loads(Path(evidence_audit_descriptor['path']).read_bytes())
    if evidence_audit.get('native_capture') != descriptor(capture_path) or evidence_audit.get('projection_receipt') != descriptor(projection_path):
        raise ValueError('Qualification and projected capture must agree')
    artifacts = projection['artifacts']
    for name, value in artifacts.items():
        if descriptor(Path(value['path'])) != value:
            raise ValueError('Factory artifact changed: ' + name)
    templates = json.loads(template_path.read_bytes())
    if templates.get('independent_capture_proof', {}).get('metadata_capture') != descriptor(capture_path):
        raise ValueError('Template and factory metadata capture must agree')
    fixture_descriptor = evidence.get('prefab_movement_fixture')
    if not isinstance(fixture_descriptor, dict) or descriptor(Path(fixture_descriptor['path'])) != fixture_descriptor:
        raise ValueError('Checked movement fixture required')
    fixture = json.loads(Path(fixture_descriptor['path']).read_bytes())
    if fixture.get('source_pins', {}).get(str(movement_path)) != digest(movement_path):
        raise ValueError('Movement input must match the independently checked fixture')
    current = descriptor(evidence_path)
    initial['metadata_compiler_context'] = current
    initial['configuration_qualification'] = current
    initial['metadata_compatibility'] = descriptor(capture_path)
    initial['building_definitions'] = artifacts['building-definitions.json']
    initial['unit_production_metadata'] = artifacts['unit-production-metadata.json']
    initial['building_request_metadata'] = artifacts['configured-metadata.json']
    initial['reference_templates'] = templates
    initial['unit_move_authoring_source'] = descriptor(movement_path)
    out.mkdir(parents=True, exist_ok=False)
    bundle = dict(old)
    bundle['initialization'] = write(out / 'initialization.json', initial)
    bundle['unit_metadata'] = artifacts['unit-production-metadata.json']
    bundle['scope'] = 'Current source-only evidence and retained Campaign inputs; native parity and readiness unproven'
    result = write(out / 'data-bundle.json', bundle)
    print(json.dumps({'status': 'PreparedSourceOnlyInputs', 'bundle': result, 'mission_ready': False}))


if __name__ == '__main__':
    main()
