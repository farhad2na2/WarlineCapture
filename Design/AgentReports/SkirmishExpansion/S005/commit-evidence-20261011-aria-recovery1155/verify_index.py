
from pathlib import Path
import gzip, hashlib, json
archive = Path(__file__).resolve().parent
repository = archive.parents[4]
index = json.loads((archive / 'index.json').read_text())
def sha(value): return hashlib.sha256(value).hexdigest()
for dependency in index['dependencies']:
    assert sha((repository / dependency['index']).read_bytes()) == dependency['indexSha256']
objects = {}
for obj in index['objects']:
    compressed = (archive / obj['object']).read_bytes()
    assert sha(compressed) == obj['gzipSha256'] and len(compressed) == obj['gzipBytes']
    original = gzip.decompress(compressed)
    assert sha(original) == obj['sha256'] and len(original) == obj['bytes']
    assert obj['sha256'] not in objects
    objects[obj['sha256']] = original
alias_bytes = {}
for file in index['files']:
    assert file['originalPath'] not in alias_bytes
    original = objects[file['sha256']]
    assert sha(original) == file['sha256'] and len(original) == file['bytes']
    alias_bytes[file['originalPath']] = original
def data(name): return alias_bytes[name]

PREFIX = 'Design/AgentReports/SkirmishExpansion/S005/'
OLD = '8c2cf8d559b00c4e9c5df62f241dbf71a8b309871fec021264a9c89f19a3c92a'
NEW = 'f65d132501e1661d90fe4829a50fa911ef822a4d395a22e063f9adaa0299fc33'
assert {r['run'] for r in index['runs']} == {'1142','1146','1150','1151','1153','1155'}
assert index['readinessClaim'] is False
for run in index['runs']:
    number = run['run']
    expected_source = NEW if number in ('1153','1155') else OLD
    assert run['sourceHash'] == expected_source
    aliases = run['prePostAliases']
    assert len({data(PREFIX + name) for name in aliases}) == 1
    for alias in aliases:
        fingerprint = alias.replace('-source-manifest.json', '-independent-fingerprint.json')
        assert json.loads(data(PREFIX + fingerprint))['sourceHash'] == expected_source
    if number == '1142':
        raw = json.loads(data(run['rawFailure']))
        assert raw['success'] is False
        assert raw['errors'][0]['code'] == 'INVALID_COMMAND_ARGS'
        assert 'eval has no parameter --timeout_ms' in raw['errors'][0]['message']
        assert run['nativeMatchStarted'] is False and run['nativeWrapperStarted'] is False
        continue
    proof = json.loads(data(run['proof']))
    log = data(run['log'])
    native = int(data(run['log'] + '.exit').strip())
    wrapper = int(data(run['log'] + '.wrapper-exit').strip())
    expected_exit = 0 if number in ('1150','1155') else 1
    assert native == wrapper == expected_exit == run['nativeExit'] == run['wrapperExit']
    assert proof['sourceHash'] == expected_source and proof['playerReady'] is False
    assert proof.get('fullSourceUnchanged', proof.get('sourceUnchanged')) is True
    assert sha(log) == run['logSha256']
    if 'logSha256' in proof:
        assert sha(log) == proof['logSha256']
    if 'fixture' in proof:
        assert sha(data(PREFIX + proof['fixture'])) == proof['fixtureSha256']
    if number == '1155':
        assert proof['result'] == 'Passed' and proof['suites'] == 15
        assert proof['requiredPassMarker'].encode() in log
        assert b'[AriaSkirmishPlanValidation] result=Passed cases=107' in log
        assert proof['nativeMatchCount'] == 0
    elif number == '1150':
        assert proof['result'].startswith('PassedScoped') and not proof['errors']
        checkpoint = data(PREFIX + proof['checkpointEvidence'])
        assert sha(checkpoint) == proof['checkpointSha256'] == run['checkpointSha256']
        assert len(proof['recoveryEvidenceRows']) == 4
        assert proof['actualPresentedPauseBeforeNativeTouch'] is True
        assert proof['normalMissionCertified'] is False
        touches = [json.loads(line) for line in data(PREFIX + proof['touchEvidence']).splitlines() if line.strip()]
        assert touches and all(row['receiptViolations'] == row['unexpected'] == row['interventions'] == 0 for row in touches)
    else:
        assert proof['result'] == 'Failed' and proof['errors']
    if number == '1146':
        touches = [json.loads(line) for line in data(PREFIX + proof['touchEvidence']).splitlines() if line.strip()]
        assert touches and all(row['receiptViolations'] == row['unexpected'] == row['interventions'] == 0 for row in touches)
    if number in ('1151','1153'):
        row = proof['nativeCountedRow']
        assert row[3] == expected_source and row[13:15] == ['Abort','ariaStopped:Blocked']
        assert proof['countedWinCertified'] is False and proof['countsForAriaSeedBaseline'] is False
        assert sha(data('/private/tmp/s005-run-normal-aria-' + number + '.py')) == proof['executedRunnerSha256']
        assert data(str(Path(row[-2]))) == data(PREFIX + proof['trace'])
        # Copied native logger output is intentionally a distinct stream from wrapper log.
        assert data(str(Path(row[-1]))) != log
        if number == '1153':
            assert float(row[15]) == 186.147
            audit = proof['inputAudit']
            assert audit['recordedContacts'] == audit['completedContacts'] == 79 and not audit['errors']
            commands = [json.loads(line) for line in data(PREFIX + proof['trace'] + '.commands.jsonl').splitlines() if line.strip()]
            assert sum(r.get('operation') == 'AttackMove' and r.get('verified') is True for r in commands) == 14
            snapshot = json.loads(data(PREFIX + 'native1153-approach-readonly-snapshot1.json'))
            assert snapshot['success'] and snapshot['data']['result']['success']
            prepared = json.loads(data('/private/tmp/S005AriaApproachReadOnly1153.manifest.json'))
            assert sha(data(prepared['readerPath'])) == prepared['readerSha256']
aria = '/private/tmp/aria1151-hidden-base-approach-fix/'
patch_manifest = json.loads(data(aria + 'manifest-r1.json'))
assert sha(data(aria + 'candidate-r1.patch')) == patch_manifest['patchSHA']
for file in patch_manifest['files']:
    assert sha(data(aria + file['path'])) == file['stagedSHA']
assert len(index['files']) == index['statistics']['selectedExactAliases'] == 163
assert len(index['objects']) == index['statistics']['uniqueObjects'] == 133
assert len(index['credentialScan']['logs']) == 5
assert all(not any(log['credentialMarkerCounts'].values()) for log in index['credentialScan']['logs'])

print('[S005ClosedAriaRecoveryEvidence] result=Passed runs=6 aliases=163 objects=133 normalWins=0 playerReady=0')
