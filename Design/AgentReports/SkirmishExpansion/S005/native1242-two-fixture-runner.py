#!/usr/bin/env python3
"""Fail-closed native rerun for the two corrected, test-only S005 fixtures."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

ROOT = Path('/Users/farhad/Projects/WarlineCapture')
REPORT = ROOT / 'Design/AgentReports/SkirmishExpansion/S005'
FINGERPRINTER = Path('/private/tmp/warline-s005-source-manifest.py')
EXPECTED_HASH = '746a4680c6445adf8c350cffb139c3cbb25362501507f6ed00be2b449a08650d'
EXPECTED_MANIFEST = REPORT / 'final-1242-two-fixture-pre-source-manifest.json'
EXPECTED_FINGERPRINT = REPORT / 'final-1242-two-fixture-pre-independent-fingerprint.json'
READINESS = REPORT / 'TECHNICAL_READINESS_CURRENT.json'
RUN = 'native1242-two-fixture'
SUITES = (
    ('economy', 'Game.Tests.Editor.SkirmishEnemyEconomyTests.RunFocusedValidation',
     '[SkirmishEnemyEconomyTests] result=Passed tests=11'),
    ('commander', 'Game.Tests.Editor.SkirmishEnemyCommanderTests.RunFocusedValidation',
     '[SkirmishEnemyCommanderTests] result=Passed cases=8 configSeedPairs=4 scope=isolated-shared-orders'),
)


def write_json(path, data):
    path.write_text(json.dumps(data, indent=2) + '\n')


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def manifest(prefix):
    command = ['rtk', 'proxy', 'python3', str(FINGERPRINTER), prefix]
    completed = subprocess.run(command, cwd=ROOT, capture_output=True, text=True)
    if completed.returncode:
        raise RuntimeError(f'Full source scan failed: {completed.returncode}: {completed.stderr[-1000:]}')
    path = REPORT / f'{prefix}-source-manifest.json'
    finger = REPORT / f'{prefix}-independent-fingerprint.json'
    return json.loads(path.read_text()), json.loads(finger.read_text())


def check_idle(path):
    raw = json.loads(path.read_text())
    data = raw.get('data', {})
    result = data.get('result', {})
    if (raw.get('success') is not True or data.get('success') is not True
            or result.get('success') is not True or raw.get('errors')
            or result.get('diagnostics') or data.get('target', {}).get('projectPath') != str(ROOT)
            or data.get('parameters', {}).get('file') != '/private/tmp/S005MainIdle1242.cs'
            or data.get('parameters', {}).get('entry') != 'S005MainIdle1242.Run'):
        raise RuntimeError('Native idle script failed target/compile/execution validation')
    if sha256(Path('/private/tmp/S005MainIdle1242.cs')) != '36a51b43c9bc71a73d3b629028010cee27ac03446b7bfe88d59a2b733558ce51':
        raise RuntimeError('Reconstructed read-only idle helper bytes changed')
    idle = json.loads(result['result'])
    required = ('applicationIsPlaying', 'editorIsCompiling', 'editorIsUpdating',
                'editorIsPlayingOrWillChangePlaymode')
    if (any(idle.get(key) is not False for key in required)
            or idle.get('assemblyCompilationState') != 'Idle'
            or idle.get('assemblyCompilationStateOwner') != 'UnityEditor.EditorApplication.isCompiling'
            or idle.get('dataPath') != str(ROOT / 'Assets')):
        raise RuntimeError(f'Native Editor is not proven idle: {idle}')
    return idle


def main():
    if len(sys.argv) != 2:
        raise RuntimeError('Usage: native1242-two-fixture-runner.py EXACT_IDLE_PROOF_JSON')
    proof_path = REPORT / f'{RUN}-result.json'
    pre_prefix = f'{RUN}-pre'
    post_prefix = f'{RUN}-post'
    reserved = [proof_path, REPORT / f'{pre_prefix}-source-manifest.json',
                REPORT / f'{pre_prefix}-independent-fingerprint.json',
                REPORT / f'{post_prefix}-source-manifest.json']
    for name, _, _ in SUITES:
        log = REPORT / f'{RUN}-{name}.log'
        reserved.extend((log, Path(str(log) + '.exit'), REPORT / f'{RUN}-{name}.wrapper-exit'))
    if any(path.exists() for path in reserved):
        raise RuntimeError('1242 artifact already exists; preserve it and use a new run id')
    if sha256(FINGERPRINTER) != 'bbcbe12d05c2fe24705f58113b5f6525115f13b3d3f55650e202ec804e687144':
        raise RuntimeError('Full source scanner bytes differ from reviewed helper')
    check_idle(Path(sys.argv[1]))
    expected = json.loads(EXPECTED_MANIFEST.read_text())
    expected_fingerprint = json.loads(EXPECTED_FINGERPRINT.read_text())
    if expected_fingerprint['sourceHash'] != EXPECTED_HASH:
        raise RuntimeError('Reviewed source fingerprint differs')
    pre, pre_fingerprint = manifest(pre_prefix)
    if pre != expected or pre_fingerprint['sourceHash'] != EXPECTED_HASH:
        raise RuntimeError('Complete source changed before native dispatch')
    state = json.loads(READINESS.read_text())
    if state.get('activeRun') != 'native1241-focused-diagnostic':
        raise RuntimeError('Unexpected active run: ' + str(state.get('activeRun')))
    state['previousIncompleteRun'] = state['activeRun']
    state['activeRun'] = RUN
    state['activeScope'] = 'Two corrected test-only focused suites; normal mission not proven'
    state['activePreManifest'] = f'{pre_prefix}-source-manifest.json'
    write_json(READINESS, state)
    results = []
    errors = []
    for name, method, marker in SUITES:
        log = REPORT / f'{RUN}-{name}.log'
        command = ['rtk', 'proxy', str(ROOT / 'Tools/CI/invoke_unity_macos.sh'),
                   '--reuse', '--timeout', '180', '--log', str(log), '--', '-executeMethod', method]
        out = REPORT / f'{RUN}-{name}.wrapper-output.txt'
        with out.open('x') as stream:
            wrapper = subprocess.run(command, cwd=ROOT, stdout=stream, stderr=subprocess.STDOUT).returncode
        (REPORT / f'{RUN}-{name}.wrapper-exit').write_text(str(wrapper) + '\n')
        bridge_file = Path(str(log) + '.exit')
        try:
            bridge = int(bridge_file.read_text()) if bridge_file.exists() else None
        except ValueError:
            bridge = None
        native = log.read_text(errors='replace') if log.exists() else ''
        result = {'suite': name, 'method': method, 'wrapperExit': wrapper,
                  'nativeExit': bridge, 'marker': marker, 'markerCount': native.count(marker),
                  'log': log.name, 'logSha256': sha256(log) if log.exists() else None,
                  'wrapperOutput': out.name}
        result['passed'] = wrapper == 0 and bridge == 0 and result['markerCount'] == 1
        results.append(result)
        if not result['passed']:
            errors.append(f'{name} failed exact native exit/marker checks')
            break
    post, post_fingerprint = manifest(post_prefix)
    unchanged = post == expected and post_fingerprint['sourceHash'] == EXPECTED_HASH
    if not unchanged:
        errors.append('Complete source changed during focused validation')
    if len(results) != len(SUITES):
        errors.append('Not all planned suites executed')
    qualification = {'result': 'PassedScopedFocused' if not errors else 'FailedRetained',
                     'sourceHash': EXPECTED_HASH, 'fullSourceUnchanged': unchanged,
                     'preManifest': f'{pre_prefix}-source-manifest.json',
                     'postManifest': f'{post_prefix}-source-manifest.json',
                     'results': results, 'errors': errors, 'normalMissionWins': 0,
                     'playerReady': False, 'postIdleProof': 'Pending separate native observation'}
    write_json(proof_path, qualification)
    print(json.dumps({'result': qualification['result'], 'proof': str(proof_path), 'errors': errors}), flush=True)
    return 0 if not errors else 3


if __name__ == '__main__':
    sys.exit(main())
