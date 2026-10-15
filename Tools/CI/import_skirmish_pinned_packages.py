"""Root-only coherent import, exact before/after pins, plan mode by default.

Run only at a verified idle native-lane boundary. Every package is checked before
any target is written. Historical generators are deliberately never rerun.
"""
import argparse
import hashlib
import json
from pathlib import Path
import uuid


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('manifests', nargs='+')
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    staged = {}
    for name in args.manifests:
        path = root / name
        manifest = json.loads(path.read_text(encoding='utf-8'))
        assert not ('files' in manifest and 'targets' in manifest), (name, 'Ambiguous target lists')
        entries = manifest.get('files', manifest.get('targets', []))
        assert isinstance(entries, list), (name, 'Target list required')
        assert entries or manifest.get('new_files'), (name, 'No consumed targets; unsupported or empty manifest')
        count_before = len(staged)
        for row in entries:
            target = row['target']
            source = row.get('draft') or row.get('source')
            if source is None:
                source = str((path.parent / 'Sources' / (target + '.txt')).relative_to(root))
            expected_before = row.get('before_sha256', row.get('current_imported_sha256'))
            expected_after = row.get('after_sha256', row.get('projected_sha256', row.get('sha256')))
            assert expected_after is not None, (target, 'Missing explicit after pin')
            append(root, staged, target, source, expected_before, expected_after)
        for row in manifest.get('new_files', []):
            assert row.get('target_must_be_absent') is True, row['target']
            append(root, staged, row['target'], row['source'], None, row['source_sha256'])
        print(f'[SkirmishPinnedPackage] manifest={name} verifiedTargets={len(staged) - count_before}')
    print(f'[SkirmishPinnedPackages] result=Verified targets={len(staged)} apply={args.apply}')
    if not args.apply:
        return
    for target, data in staged.items():
        path = root / target
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        meta = Path(str(path) + '.meta')
        if not meta.exists():
            meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\n', encoding='utf-8')
    print('[SkirmishPinnedPackages] result=Imported nativeValidation=Pending missionReadiness=Pending')


def append(root, staged, target, source, before, after):
    path = (root / target).resolve()
    assert path.is_relative_to(root.resolve()) and target.startswith('Assets/'), target
    assert target not in staged, (target, 'Overlapping package targets require explicit composition')
    data = (root / source).read_bytes()
    assert digest(data) == after, (target, 'Draft hash mismatch')
    actual = digest(path.read_bytes()) if path.exists() else None
    assert actual == before, (target, 'Before hash mismatch', before, actual)
    staged[target] = data


if __name__ == '__main__':
    main()
