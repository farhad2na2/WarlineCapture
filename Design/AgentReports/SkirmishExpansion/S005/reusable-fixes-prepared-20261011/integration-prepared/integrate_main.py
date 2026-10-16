#!/usr/bin/env python3
"""Exact reviewed MAIN integration. Default is read-only; no Unity/Git actions.

Run --apply only after root establishes native idle/source freeze. All five
layers are validated in memory and each final path is written once. A failed
partial apply is recorded, never automatically rolled back over other work.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import stat
import tempfile
from datetime import datetime, timezone

MAIN = Path('/Users/farhad/Projects/WarlineCapture')
INDEX_REL = 'Design/AgentReports/SkirmishExpansion/S005/reusable-fixes-prepared-20261011/index.json'
INDEX_SHA = 'a5b48a28315ed4f03cd39ae370985098b1a3936f52ac389b600094e9096a7a30'
ORDER = ['foundation', 'compile-followup', 'production-v4', 'commander-v2', 'ground-rollout-v1']
PLAN = Path(__file__).with_name('integration-plan.json')
# Pinned by the preparation step. No main bytes are written by that step.
PLAN_SHA = '02f226f15136d5afd9c9110817b6f7c5f2b85b19acc6c774a851b036db7ff1b2'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def utc():
    return datetime.now(timezone.utc).isoformat()


def checked_path(root, relative, source=False):
    logical = PurePosixPath(relative)
    require(not logical.is_absolute() and '..' not in logical.parts,
            'Unsafe relative path: ' + relative)
    if source:
        require(logical.parts[0] == 'Assets' and
                (relative.endswith('.cs') or relative.endswith('.cs.meta')),
                'Out-of-scope source: ' + relative)
    target = root.joinpath(*logical.parts)
    cursor = root
    require(not root.is_symlink(), 'Root must not be a symlink')
    for segment in logical.parts:
        cursor = cursor / segment
        require(not cursor.is_symlink(), 'Symlink refused: ' + str(cursor))
    require(target.resolve().is_relative_to(root.resolve()), 'Path escaped root')
    return target


def current_bytes(path):
    if not path.exists():
        return None
    require(path.is_file() and not path.is_symlink(), 'Not a regular file: ' + str(path))
    return path.read_bytes()


def expected_file(path, expected):
    data = current_bytes(path)
    actual = None if data is None else sha(data)
    require(actual == expected,
            'Base/absence mismatch for %s: expected %s; actual %s' % (path, expected, actual))
    return data


def read_object(root, row):
    require(row['object'] == 'objects/' + row['sha256'] + '.blob', 'Object path/hash mismatch')
    data = checked_path(root, row['object']).read_bytes()
    require(sha(data) == row['sha256'] and len(data) == row['bytes'],
            'Corrupt object: ' + row['object'])
    return data


def load_reviewed_plan():
    raw = PLAN.read_bytes()
    require(sha(raw) == PLAN_SHA, 'Prepared plan pin mismatch')
    plan = json.loads(raw)
    require(plan['project'] == str(MAIN), 'Wrong prepared project')
    index_file = MAIN / INDEX_REL
    index_raw = index_file.read_bytes()
    require(sha(index_raw) == INDEX_SHA, 'Durable backup index pin mismatch')
    index = json.loads(index_raw)
    require([bundle['bundle'] for bundle in index['bundles']] == ORDER,
            'Layer order changed')
    source_root = index_file.parent
    first, state, final, steps = {}, {}, {}, []
    for bundle in index['bundles']:
        manifest_raw = read_object(source_root, bundle['manifest'])
        manifest = json.loads(manifest_raw)
        manifest_paths = {row['path']: row for row in manifest['files']}
        require(len(manifest_paths) == len(bundle['sources']), 'Manifest source count differs')
        if 'patch' in bundle:
            read_object(source_root, bundle['patch'])
        for row in bundle['sources']:
            path = row['path']
            checked_path(MAIN, path, source=True)
            manifest_row = manifest_paths[path]
            manifest_base = manifest_row.get('baseSha256', manifest_row.get('base_sha256'))
            manifest_staged = manifest_row.get('stagedSha256', manifest_row.get('staged_sha256'))
            require(manifest_base == row['baseSha256'] and
                    manifest_staged == row['stagedSha256'] == row['sha256'],
                    'Index differs from reviewed manifest: ' + path)
            data = read_object(source_root, row)
            if path in state:
                require(state[path] == row['baseSha256'], 'Layer dependency mismatch: ' + path)
            else:
                first[path] = row['baseSha256']
            state[path] = row['sha256']
            final[path] = data
            steps.append(dict(bundle=bundle['bundle'], path=path,
                              baseSha256=row['baseSha256'], stagedSha256=row['sha256']))
    require(steps == plan['orderedSteps'], 'Prepared ordered replay differs')
    require(first == plan['initialBaseHashes'], 'Prepared initial bases differ')
    require({p: sha(data) for p, data in final.items()} == plan['finalHashes'],
            'Prepared final bytes differ')
    overlay = {row['path']: row['sha256'] for row in index['finalOverlay']}
    require(overlay == plan['finalHashes'], 'Durable final overlay differs')
    guards = dict(first)
    for path, digest in plan['adjacentMetaHashes'].items():
        require(path not in first and path.endswith('.cs.meta'), 'Bad adjacent meta guard')
        guards[path] = digest
    originals = {path: expected_file(checked_path(MAIN, path, source=True), digest)
                 for path, digest in sorted(guards.items())}
    return plan, index_raw, guards, originals, final


def exclusive_json(path, value):
    with path.open('x', encoding='utf-8') as stream:
        stream.write(json.dumps(value, indent=2, sort_keys=True) + '\n')
        stream.flush()
        os.fsync(stream.fileno())


def atomic_replace(target, data, mode):
    target.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix='.' + target.name + '.', suffix='.prepared',
                                              dir=target.parent)
    try:
        with os.fdopen(descriptor, 'wb') as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, target)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def apply_final(plan, index_raw, guards, originals, final, backup):
    backup = backup.absolute()
    require(not backup.exists(), 'Exclusive backup directory already exists')
    require(not backup.resolve().is_relative_to(MAIN.resolve()), 'Backup must be outside project')
    require(backup.parent.is_dir() and not backup.parent.is_symlink(), 'Backup parent unavailable')
    backup.mkdir()
    records, modes, written = [], {}, []
    # All originals and adjacent .meta bytes are durable before the first edit.
    for path, data in originals.items():
        target = checked_path(MAIN, path, source=True)
        mode = stat.S_IMODE(target.stat().st_mode) if data is not None else 0o644
        modes[path] = mode
        record = dict(path=path, existed=data is not None,
                      sha256=None if data is None else sha(data), mode=mode,
                      adjacentMetaOnly=path in plan['adjacentMetaHashes'])
        if data is not None:
            destination = checked_path(backup, 'originals/' + path)
            destination.parent.mkdir(parents=True, exist_ok=True)
            with destination.open('xb') as stream:
                stream.write(data)
                stream.flush()
                os.fsync(stream.fileno())
            require(sha(destination.read_bytes()) == record['sha256'], 'Backup verification failed')
            record['backupPath'] = 'originals/' + path
        records.append(record)
    with (backup / 'reviewed-index.json').open('xb') as stream:
        stream.write(index_raw)
        stream.flush()
        os.fsync(stream.fileno())
    exclusive_json(backup / 'backup-manifest.json', dict(utc=utc(), project=str(MAIN),
                   durableIndexSha256=INDEX_SHA, preparedPlanSha256=PLAN_SHA, files=records,
                   absenceMarkersPreserved=True, automaticRollback=False))
    exclusive_json(backup / 'apply-started.json', dict(utc=utc(), finalFiles=len(final),
                   layerOrder=ORDER, orderedEntries=len(plan['orderedSteps']),
                   unityCalledByScript=False, gitCalledByScript=False))
    try:
        for path, expected in guards.items():
            expected_file(checked_path(MAIN, path, source=True), expected)
        for path, data in sorted(final.items()):
            target = checked_path(MAIN, path, source=True)
            expected_file(target, guards[path])
            atomic_replace(target, data, modes[path])
            expected_file(target, sha(data))
            written.append(path)
        for path, expected in plan['adjacentMetaHashes'].items():
            expected_file(checked_path(MAIN, path, source=True), expected)
        exclusive_json(backup / 'apply-completed.json', dict(utc=utc(), result='ExactFinalBytesApplied',
                       writtenPaths=written, finalHashes=plan['finalHashes'],
                       adjacentMetadataUnchanged=True, unityCalledByScript=False,
                       gitCalledByScript=False, playerReadyClaim=False))
    except BaseException as error:
        exclusive_json(backup / 'apply-failed.json', dict(utc=utc(), error=str(error),
                       writtenPaths=written, automaticRollback=False,
                       instruction='Preserve source freeze. Review exact originals and current bytes before any explicit repair.'))
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply', action='store_true', help='Actually write reviewed final main files')
    parser.add_argument('--backup-dir', type=Path, help='New exclusive external backup directory for apply')
    parser.add_argument('--report', type=Path, help='Optional exclusive external read-only-preflight report')
    arguments = parser.parse_args()
    require(arguments.apply == (arguments.backup_dir is not None), '--apply requires --backup-dir and vice versa')
    plan, index_raw, guards, originals, final = load_reviewed_plan()
    report = dict(result='ExactReadOnlyPreflightPassed', utc=utc(), project=str(MAIN),
                  durableIndexSha256=INDEX_SHA, preparedPlanSha256=PLAN_SHA, layerOrder=ORDER,
                  orderedEntries=len(plan['orderedSteps']), finalFiles=len(final),
                  backedUpOrGuardedPaths=len(guards), modifiedOriginalFiles=sum(v is not None for v in plan['initialBaseHashes'].values()),
                  initiallyAbsentFiles=sum(v is None for v in plan['initialBaseHashes'].values()),
                  unityCalled=False, gitCalled=False, sourceWritten=False,
                  nativeIdleAndSourceFreezeMustBeVerifiedByRootBeforeApply=True)
    if arguments.report:
        require(not arguments.report.absolute().resolve().is_relative_to(MAIN.resolve()), 'Report must be external')
        exclusive_json(arguments.report, report)
    if arguments.apply:
        apply_final(plan, index_raw, guards, originals, final, arguments.backup_dir)
        report.update(result='ExactFinalBytesApplied', sourceWritten=True, backupDirectory=str(arguments.backup_dir))
    print(json.dumps(report, indent=2, sort_keys=True))


if __name__ == '__main__':
    main()
