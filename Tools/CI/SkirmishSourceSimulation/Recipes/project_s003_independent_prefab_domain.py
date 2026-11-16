#!/usr/bin/env python3
"""Project the current captured S003 prefab domain without runtime admission."""
import argparse
import hashlib
import json
import sys
from pathlib import Path

from project_s003_refreshed_prefab_domain import FIELDS

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'Tools/CI/SkirmishSourceSimulation/Tests'))
from finalize_s003_refreshed_prefab_capture import qualify

CAPTURE = ROOT / 'Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/data/prefab-reference-data.json'
LOG = ROOT / 'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1-editor.log'
AUDIT = ROOT / 'T Logs/SkirmishProduction/s003-current-prefabs-capture-20261105-T1/independent-audit.json'
FIXTURE_DIR = ROOT / 'T Logs/SkirmishProduction/s003-actual-importer-current-fixture-20261105-T7'
FIXTURE = FIXTURE_DIR / 'receipt.json'
FIXTURE_LOG = FIXTURE_DIR / 'run.log'
METADATA = ROOT / 'Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1/data/s003-native-metadata.json'
METADATA_AUDIT = ROOT / 'T Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1/independent-audit.json'
METADATA_LOG = ROOT / 'T Logs/SkirmishProduction/s003-current-metadata-capture-20261105-T1-editor.log'


def descriptor(path):
    path = path.resolve(strict=True)
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for data in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(data)
    return {'path': str(path), 'sha256': h.hexdigest()}


def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2, ensure_ascii=False)
        stream.write('\n')
    return descriptor(path)


def linked_capture_audit(audit, checked, capture, metadata_sha, metadata_audit_sha):
    """Derive the importer's linkage fields from the checked capture itself."""
    if any(audit.get(key) != value for key, value in checked.items()):
        raise ValueError('Capture audit differs from the independently checked capture')
    if (capture.get('native_metadata_capture_sha256') != metadata_sha or
            capture.get('independent_metadata_audit_sha256') != metadata_audit_sha):
        raise ValueError('Capture does not record this independent metadata evidence')
    for key, value in (('metadata_capture_sha256', metadata_sha),
                       ('independent_metadata_audit_sha256', metadata_audit_sha)):
        if key in audit and audit[key] != value:
            raise ValueError('Capture audit contains a conflicting metadata linkage: ' + key)
    return {**checked, 'metadata_capture_sha256': metadata_sha,
            'independent_metadata_audit_sha256': metadata_audit_sha}


def main():
    global CAPTURE, LOG, AUDIT, FIXTURE, FIXTURE_LOG, METADATA, METADATA_AUDIT, METADATA_LOG
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    for name, default in (('capture', CAPTURE), ('log', LOG), ('audit', AUDIT),
                          ('fixture', FIXTURE), ('fixture-log', FIXTURE_LOG),
                          ('metadata', METADATA), ('metadata-audit', METADATA_AUDIT),
                          ('metadata-log', METADATA_LOG)):
        parser.add_argument('--' + name, type=Path, default=default)
    options = parser.parse_args()
    CAPTURE, LOG, AUDIT, FIXTURE, FIXTURE_LOG, METADATA, METADATA_AUDIT, METADATA_LOG = (
        path.resolve(strict=True) for path in (options.capture, options.log, options.audit, options.fixture,
                                                options.fixture_log, options.metadata, options.metadata_audit,
                                                options.metadata_log))
    if any(not path.is_relative_to(ROOT) for path in (CAPTURE, LOG, AUDIT, FIXTURE, FIXTURE_LOG, METADATA, METADATA_AUDIT, METADATA_LOG)):
        raise ValueError('Evidence must remain in the repository')
    out = options.output.resolve()
    if not out.is_relative_to(ROOT):
        raise ValueError('Output must remain in the repository')
    pins = {str(p): descriptor(p)['sha256'] for p in (CAPTURE, LOG, AUDIT, FIXTURE, FIXTURE_LOG, METADATA, METADATA_AUDIT, METADATA_LOG, Path(__file__))}
    audit, fixture = (json.loads(p.read_bytes()) for p in (AUDIT, FIXTURE))
    checked = qualify(ROOT, CAPTURE, LOG, 0)
    if (audit.get('status') != 'PassedDataOnlyCapture' or audit.get('capture') != descriptor(CAPTURE)
            or checked.get('capture') != audit['capture'] or audit.get('editor_log') != descriptor(LOG)
            or audit.get('mission_ready') is not False):
        raise ValueError('Current independent prefab capture audit required')
    if (fixture.get('status') != 'PassedDisposableS003ImporterFixture'
            or fixture.get('capture_sha256') != audit['capture']['sha256']
            or fixture.get('movement_source_projection_checked') is not True
            or fixture.get('world_allocated') is not False or fixture.get('mission_ready') is not False):
        raise ValueError('Current source importer and movement fixture required')
    source = json.loads(CAPTURE.read_bytes())
    metadata = json.loads(METADATA.read_bytes())
    metadata_audit = json.loads(METADATA_AUDIT.read_bytes())
    metadata_descriptor, metadata_audit_descriptor = descriptor(METADATA), descriptor(METADATA_AUDIT)
    if (metadata_audit.get('status') != 'Passed' or metadata_audit.get('capture_sha256') != metadata_descriptor['sha256']
            or metadata_audit.get('log_sha256') != descriptor(METADATA_LOG)['sha256']
            or metadata_audit.get('wrapper_exit_observed') != 0 or metadata_audit.get('marker_count') != 1
            or metadata_audit.get('source_pin_mismatches') != []
            or metadata_audit.get('native_context_qualified') is not False or metadata_audit.get('mission_ready') is not False):
        raise ValueError('Exact independent current metadata capture required')
    linked_audit = linked_capture_audit(audit, checked, source, metadata_descriptor['sha256'],
                                        metadata_audit_descriptor['sha256'])
    metadata_pins = metadata.get('source_pins')
    if (not isinstance(metadata_pins, dict) or not metadata_pins or
            metadata_audit.get('source_pin_entries') != len(metadata_pins)):
        raise ValueError('Complete audited metadata source pins required')
    def verify_metadata_pins():
        for name, expected in metadata_pins.items():
            path = Path(name)
            path = path if path.is_absolute() else ROOT / path
            if not path.is_file() or path.is_symlink() or descriptor(path)['sha256'] != expected:
                raise ValueError('Metadata capture source changed: ' + name)
    verify_metadata_pins()
    graph = metadata['source_graph']
    if descriptor(Path(graph['path'])) != graph:
        raise ValueError('Metadata source graph changed')
    domain = {name: source[name] for name in FIELDS if name in source}
    domain.update(source_reference_sha256=audit['capture']['sha256'], derived_read_domain='ExactTypedPrefabClosureOnly')
    if len(source['opening_prefabs']) != 19 or len(source['entities']) != 647 or len(source['unsupported_values']) != 32:
        raise ValueError('Current captured prefab domain changed')
    out.mkdir(parents=True, exist_ok=False)
    linked_audit['original_capture_audit'] = descriptor(AUDIT)
    linked_audit_descriptor = write(out / 'linked-capture-audit.json', linked_audit)
    sliced = write(out / 's003-focused-template-domain.json', domain)
    proof = {'schema_version': 1, 'kind': 'S003IndependentFocusedTemplateSourceProof',
             'source_graph': graph, 'capture_audit': linked_audit_descriptor, 'metadata_capture': descriptor(METADATA),
             'metadata_audit': descriptor(METADATA_AUDIT), 'metadata_log': descriptor(METADATA_LOG), 'importer_receipt': descriptor(FIXTURE),
             'importer_log': descriptor(FIXTURE_LOG)}
    domain_path = Path(sliced['path'])
    manifest = {'schema_version': 1, 'kind': 'PinnedExactTemplateDomainProjection',
                'source_path': str(CAPTURE), 'source_sha256': descriptor(CAPTURE)['sha256'],
                'source_bytes': CAPTURE.stat().st_size, 'slice_path': sliced['path'],
                'slice_sha256': sliced['sha256'], 'slice_bytes': domain_path.stat().st_size,
                'selected_fields': FIELDS, 'root_count': 19, 'entity_count': 647,
                'independent_capture_proof': proof, 'unsupported_values_preserved': 32,
                'world_created': False, 'consumer_read_domain': 'ExactTypedPrefabClosureOnly'}
    manifest_desc = write(out / 'template-slice-manifest.json', manifest)
    view_descriptor = {**sliced, 'source_reference_path': str(CAPTURE),
                       'source_reference_sha256': descriptor(CAPTURE)['sha256'],
                       'projection_manifest_path': manifest_desc['path'],
                       'projection_manifest_sha256': manifest_desc['sha256'],
                       'independent_capture_proof': proof}
    view = write(out / 'reference-template-descriptor.json', view_descriptor)
    for name, expected in pins.items():
        if descriptor(Path(name))['sha256'] != expected:
            raise ValueError('Projection input changed: ' + name)
    verify_metadata_pins()
    receipt = {'status': 'PassedSourceOnlyProjection', 'kind': 'S003IndependentPrefabDomainProjection',
               'domain': sliced, 'view_descriptor': view, 'capture': descriptor(CAPTURE), 'capture_audit': linked_audit_descriptor,
               'original_capture_audit': descriptor(AUDIT),
               'importer_movement_fixture': descriptor(FIXTURE), 'selected_fields': FIELDS,
               'root_count': 19, 'entity_count': 647, 'unsupported_values_preserved': 32,
               'runtime_admission': False, 'source_fit_certified': False, 'mission_ready': False}
    result = write(out / 'receipt.json', receipt)
    print(json.dumps({'status': receipt['status'], 'receipt': result, 'runtime_admission': False}))


if __name__ == '__main__':
    main()
