#!/usr/bin/env python3
"""Record source/config/native UI identities before a wrapper validation."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
from datetime import datetime, timezone

ROOT = Path(__file__).resolve().parents[2]

def main():
    destination = Path(sys.argv[1])
    files = set()
    for pattern in ['Assets/Game/Scripts/**/*.cs', 'Assets/Tests/Editor/**/*.cs',
                    'Assets/Game/Configs/**/*.asset', 'Assets/Game/Resources/Localization/**/*.asset',
                    'Assets/Game/Prefabs/UI/**/*.prefab', 'Assets/Game/Scenes/*.unity',
                    'Assets/Game/Art/UI/Support/**/*', 'Packages/manifest.json', 'Packages/packages-lock.json',
                    'ProjectSettings/ProjectVersion.txt', 'Tools/CI/invoke_unity_macos.sh']:
        files.update(path for path in ROOT.glob(pattern) if path.is_file())
    document = {
        'capturedUtc': datetime.now(timezone.utc).isoformat(),
        'head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
        'dirty': subprocess.check_output(['git', 'status', '--short'], cwd=ROOT, text=True),
        'files': {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest()
                  for path in sorted(files)}
    }
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(document, indent=2) + '\n')
    print(f'[MissionRemediationCandidate] recorded files={len(files)} output={destination}')

if __name__ == '__main__':
    main()
